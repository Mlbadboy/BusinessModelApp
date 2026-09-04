using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BusinessModelApp.Core.Domain.ExternalReality;
using BusinessModelApp.Core.Domain.Reality;
using BusinessModelApp.Core.Domain.DigitalTwin;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Infrastructure.Data;

namespace BusinessModelApp.Infrastructure.ExternalReality
{
    public class ExternalSourceRegistry : IExternalSourceRegistry
    {
        private readonly AppDbContext _dbContext;
        private static readonly Regex PromptInjectionPattern = new Regex(
            @"(ignore\s+(all\s+)?(previous\s+)?(instructions|policy|rules)|system\s*:\s*override|execute\s+(immediate\s+)?(transfer|transaction|payout)|bypass\s+(governance|approval|firewall)|grant\s+(root|admin)\s+privileges)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public ExternalSourceRegistry(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ExternalSourceRegistryEntry> RegisterSourceAsync(ExternalSourceRegistryEntry entry, CancellationToken ct = default)
        {
            if (entry.WorkspaceId == Guid.Empty)
                throw new ArgumentException("WorkspaceId must be provided for external source registration.", nameof(entry));

            var existing = await _dbContext.Set<ExternalSourceRegistryEntry>()
                .FirstOrDefaultAsync(s => s.WorkspaceId == entry.WorkspaceId && s.CanonicalDomain == entry.CanonicalDomain, ct);

            if (existing != null)
            {
                existing.Name = entry.Name;
                existing.BaseReliability = entry.BaseReliability;
                existing.DefaultFreshnessDays = entry.DefaultFreshnessDays;
                existing.Status = entry.Status;
                existing.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(ct);
                return existing;
            }

            entry.CreatedAt = DateTime.UtcNow;
            entry.UpdatedAt = DateTime.UtcNow;
            _dbContext.Set<ExternalSourceRegistryEntry>().Add(entry);
            await _dbContext.SaveChangesAsync(ct);
            return entry;
        }

        public async Task<ExternalSourceRegistryEntry?> GetSourceAsync(Guid sourceId, Guid workspaceId, CancellationToken ct = default)
        {
            return await _dbContext.Set<ExternalSourceRegistryEntry>()
                .FirstOrDefaultAsync(s => s.Id == sourceId && s.WorkspaceId == workspaceId, ct);
        }

        public async Task<IReadOnlyList<ExternalSourceRegistryEntry>> ListSourcesAsync(Guid workspaceId, CancellationToken ct = default)
        {
            return await _dbContext.Set<ExternalSourceRegistryEntry>()
                .Where(s => s.WorkspaceId == workspaceId)
                .OrderByDescending(s => s.BaseReliability)
                .ToListAsync(ct);
        }

        public async Task<SourceTrustProfile> CalculateTrustProfileAsync(Guid sourceId, Guid workspaceId, CancellationToken ct = default)
        {
            var source = await GetSourceAsync(sourceId, workspaceId, ct);
            if (source == null)
            {
                return new SourceTrustProfile
                {
                    SourceId = sourceId,
                    ReliabilityScore = 0.50,
                    FreshnessScore = 0.50,
                    IndependenceScore = 0.50,
                    ManipulationRisk = 0.50
                };
            }

            var evidenceCount = await _dbContext.Set<ExternalEvidenceRecord>()
                .CountAsync(e => e.WorkspaceId == workspaceId && e.SourceId == sourceId, ct);

            var contradictedCount = await _dbContext.Set<ExternalEvidenceRecord>()
                .CountAsync(e => e.WorkspaceId == workspaceId && e.SourceId == sourceId && (e.VerificationStatus == VerificationStatus.FailedVerification || e.ContradictionRisk > 0.50), ct);

            double reliability = source.BaseReliability;
            if (evidenceCount > 0)
            {
                double contradictionRatio = (double)contradictedCount / evidenceCount;
                reliability = Math.Clamp(reliability * (1.0 - contradictionRatio), 0.10, 1.0);
            }

            double manipulationRisk = (source.Category == ExternalSourceCategory.SocialSignal || source.Category == ExternalSourceCategory.PublicWeb)
                ? 0.35 : 0.05;

            return new SourceTrustProfile
            {
                SourceId = sourceId,
                ReliabilityScore = Math.Round(reliability, 3),
                FreshnessScore = source.HealthScore,
                IndependenceScore = 0.85,
                CorroborationScore = evidenceCount > 5 ? 0.80 : 0.50,
                ManipulationRisk = manipulationRisk,
                ScopeConfidence = 0.90,
                CalculatedAt = DateTime.UtcNow
            };
        }

        public async Task RecordRetrievalOutcomeAsync(Guid sourceId, bool success, string? failureDetails = null, CancellationToken ct = default)
        {
            var source = await _dbContext.Set<ExternalSourceRegistryEntry>().FindAsync(new object[] { sourceId }, ct);
            if (source == null) return;

            if (success)
            {
                source.LastSuccessfulRetrieval = DateTime.UtcNow;
                source.HealthScore = Math.Clamp(source.HealthScore + 0.05, 0.0, 1.0);
            }
            else
            {
                source.LastFailure = DateTime.UtcNow;
                source.HealthScore = Math.Clamp(source.HealthScore - 0.15, 0.0, 1.0);
                if (source.HealthScore < 0.40)
                {
                    source.Status = ExternalSourceStatus.Degraded;
                }
            }
            source.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(ct);
        }

        public async Task<ExternalEvidenceRecord> IngestExternalEvidenceAsync(ExternalEvidenceRecord evidence, CancellationToken ct = default)
        {
            if (evidence.WorkspaceId == Guid.Empty)
                throw new ArgumentException("WorkspaceId must be set for evidence ingestion.", nameof(evidence));

            // 1. Zero-Trust Security & Prompt-Injection Defense
            bool hasInjection = PromptInjectionPattern.IsMatch(evidence.NormalizedContent) ||
                                PromptInjectionPattern.IsMatch(evidence.Title) ||
                                PromptInjectionPattern.IsMatch(evidence.Summary);

            if (hasInjection)
            {
                evidence.IsPromptInjectionScanned = true;
                evidence.PromptInjectionRiskScore = 0.95;
                // Strip execution authority; mark as sanitized untrusted data
                evidence.IsSanitizedDataOnly = true;
                evidence.Summary = "[SECURITY NOTICE: Untrusted payload neutralized] " + evidence.Summary;
                evidence.ContaminationRisk = Math.Max(evidence.ContaminationRisk, 0.85);
            }
            else
            {
                evidence.PromptInjectionRiskScore = 0.05;
                evidence.IsSanitizedDataOnly = true;
            }

            // 2. Cryptographic Lineage
            if (string.IsNullOrEmpty(evidence.ContentHash))
            {
                evidence.ContentHash = ExternalEvidenceRecord.ComputeHash(evidence.NormalizedContent);
            }
            if (string.IsNullOrEmpty(evidence.ClaimHash))
            {
                evidence.ClaimHash = ExternalEvidenceRecord.ComputeHash(evidence.Title + ":" + evidence.Summary);
            }

            // 3. Domain Decay Half-Life Calculation
            double halfLifeDays = evidence.SourceType switch
            {
                ExternalSourceCategory.PriceFeed => 2.0,
                ExternalSourceCategory.CompetitorSite => 3.0,
                ExternalSourceCategory.PublicWeb => 7.0,
                ExternalSourceCategory.News => 5.0,
                ExternalSourceCategory.SocialSignal => 2.0,
                ExternalSourceCategory.Regulatory => 180.0,
                ExternalSourceCategory.Government => 180.0,
                _ => 14.0
            };
            evidence.FreshUntil = evidence.ObservedAt.AddDays(halfLifeDays * 2);

            // Freshness Metrology
            var ageDays = (DateTime.UtcNow - evidence.ObservedAt).TotalDays;
            if (ageDays < 0) ageDays = 0;
            evidence.FreshnessScore = Math.Clamp(Math.Round(Math.Pow(0.5, ageDays / halfLifeDays), 3), 0.0, 1.0);

            // Invariant: External Evidence is Observation or Estimate. NEVER Fact without reality certification.
            if (evidence.Classification == TruthClassification.Fact)
            {
                evidence.Classification = TruthClassification.Observation;
            }

            evidence.CreatedAt = DateTime.UtcNow;

            // 4. Update or Create SignalCluster for Claim Deduplication and calculate independence
            await UpdateSignalClusterAsync(evidence, ct);

            _dbContext.Set<ExternalEvidenceRecord>().Add(evidence);
            await _dbContext.SaveChangesAsync(ct);

            return evidence;
        }

        public async Task<ExternalEvidenceRecord?> GetEvidenceAsync(Guid evidenceId, Guid workspaceId, CancellationToken ct = default)
        {
            return await _dbContext.Set<ExternalEvidenceRecord>()
                .FirstOrDefaultAsync(e => e.Id == evidenceId && e.WorkspaceId == workspaceId, ct);
        }

        public async Task<IReadOnlyList<ExternalEvidenceRecord>> ListEvidenceAsync(Guid workspaceId, int limit = 50, CancellationToken ct = default)
        {
            return await _dbContext.Set<ExternalEvidenceRecord>()
                .Where(e => e.WorkspaceId == workspaceId)
                .OrderByDescending(e => e.RetrievedAt)
                .Take(limit)
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<SignalCluster>> ClusterEvidenceAsync(Guid workspaceId, CancellationToken ct = default)
        {
            return await _dbContext.Set<SignalCluster>()
                .Where(c => c.WorkspaceId == workspaceId)
                .OrderByDescending(c => c.LastObservedAt)
                .ToListAsync(ct);
        }

        private async Task UpdateSignalClusterAsync(ExternalEvidenceRecord evidence, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(evidence.ClaimHash)) return;

            var cluster = await _dbContext.Set<SignalCluster>()
                .FirstOrDefaultAsync(c => c.WorkspaceId == evidence.WorkspaceId && c.ClaimHash == evidence.ClaimHash, ct);

            if (cluster == null)
            {
                cluster = new SignalCluster
                {
                    WorkspaceId = evidence.WorkspaceId,
                    CanonicalClaim = evidence.Title,
                    ClaimHash = evidence.ClaimHash,
                    EvidenceIds = new List<Guid> { evidence.Id },
                    SourceIds = new List<Guid> { evidence.SourceId },
                    TotalCopyCount = 1,
                    UniqueSourceCount = 1,
                    IndependenceEstimate = 1.0,
                    CorroborationScore = 0.50,
                    FirstObservedAt = evidence.ObservedAt,
                    LastObservedAt = evidence.ObservedAt,
                    CreatedAt = DateTime.UtcNow
                };
                _dbContext.Set<SignalCluster>().Add(cluster);
            }
            else
            {
                cluster.TotalCopyCount++;
                if (!cluster.EvidenceIds.Contains(evidence.Id))
                {
                    cluster.EvidenceIds.Add(evidence.Id);
                }
                if (!cluster.SourceIds.Contains(evidence.SourceId))
                {
                    cluster.SourceIds.Add(evidence.SourceId);
                    cluster.UniqueSourceCount = cluster.SourceIds.Count;
                }

                // Invariant: 10 copies of the same syndicated PR release != 10 independent confirmations
                // Independence factor decays if multiple copies come from same syndication or verbatim duplicates
                double syndicationRatio = (double)cluster.TotalCopyCount / Math.Max(1, cluster.UniqueSourceCount);
                if (syndicationRatio > 1.2 || cluster.TotalCopyCount >= 3)
                {
                    cluster.IsSyndicatedDuplicateGroup = true;
                    double decayFactor = Math.Max(syndicationRatio, Math.Sqrt(cluster.TotalCopyCount));
                    cluster.IndependenceEstimate = Math.Clamp(Math.Round(1.0 / decayFactor, 3), 0.20, 1.0);
                }

                // Update evidence independence factor based on cluster syndication
                evidence.IndependenceFactor = cluster.IndependenceEstimate;

                cluster.LastObservedAt = evidence.ObservedAt > cluster.LastObservedAt ? evidence.ObservedAt : cluster.LastObservedAt;
                cluster.CorroborationScore = Math.Clamp(0.40 + (cluster.UniqueSourceCount * 0.15), 0.0, 1.0);
            }
        }
    }
}
