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
                    HistoricalReliability = 0.50,
                    FreshnessBehavior = 0.50,
                    Independence = 0.50,
                    VerificationSuccess = 0.50,
                    DomainExpertise = 0.50,
                    ManipulationHistory = 0.50,
                    TenantIsolation = 1.0,
                    CompositeTrustScore = 0.50
                };
            }

            var evidenceCount = await _dbContext.Set<ExternalEvidenceRecord>()
                .CountAsync(e => e.WorkspaceId == workspaceId && e.SourceId == sourceId, ct);

            var contradictedCount = await _dbContext.Set<ExternalEvidenceRecord>()
                .CountAsync(e => e.WorkspaceId == workspaceId && e.SourceId == sourceId && (e.VerificationStatus == VerificationStatus.FailedVerification || e.ContradictionRisk > 0.50), ct);

            double historicalReliability = source.BaseReliability;
            if (evidenceCount > 0)
            {
                double contradictionRatio = (double)contradictedCount / evidenceCount;
                historicalReliability = Math.Clamp(historicalReliability * (1.0 - contradictionRatio), 0.10, 1.0);
            }

            double independence = source.Category switch
            {
                ExternalSourceCategory.PartnerApi => 0.90,
                ExternalSourceCategory.Financial => 0.90,
                ExternalSourceCategory.Regulatory => 0.95,
                ExternalSourceCategory.Government => 0.95,
                ExternalSourceCategory.News => 0.80,
                ExternalSourceCategory.PublicWeb => 0.75,
                ExternalSourceCategory.SocialSignal => 0.60,
                _ => 0.80
            };

            double domainExpertise = source.Category switch
            {
                ExternalSourceCategory.Regulatory => 0.95,
                ExternalSourceCategory.Government => 0.95,
                ExternalSourceCategory.Financial => 0.90,
                ExternalSourceCategory.PriceFeed => 0.85,
                ExternalSourceCategory.IndustryReport => 0.85,
                ExternalSourceCategory.CompetitorSite => 0.80,
                _ => 0.70
            };

            double verificationSuccess = source.VerificationSuccessRate;
            double freshnessBehavior = source.HealthScore;
            double manipulationHistory = source.ManipulationHistoryScore;
            double tenantIsolation = 1.0; // Strictly workspace-isolated

            // Multi-dimensional composite calculation:
            // SourceTrust = HistoricalReliability + Independence + VerificationSuccess + DomainExpertise + FreshnessBehavior + ManipulationHistory + TenantIsolation
            double composite = (historicalReliability * 0.20) +
                               (independence * 0.15) +
                               (verificationSuccess * 0.20) +
                               (domainExpertise * 0.10) +
                               (freshnessBehavior * 0.15) +
                               (manipulationHistory * 0.10) +
                               (tenantIsolation * 0.10);

            // Temporal quarantine check: If source is quarantined, trust degrades sharply
            if (source.IsQuarantined)
            {
                composite = Math.Min(composite * 0.25, 0.20);
            }

            composite = Math.Clamp(Math.Round(composite, 3), 0.0, 1.0);

            return new SourceTrustProfile
            {
                SourceId = sourceId,
                HistoricalReliability = Math.Round(historicalReliability, 3),
                Independence = Math.Round(independence, 3),
                VerificationSuccess = Math.Round(verificationSuccess, 3),
                DomainExpertise = Math.Round(domainExpertise, 3),
                FreshnessBehavior = Math.Round(freshnessBehavior, 3),
                ManipulationHistory = Math.Round(manipulationHistory, 3),
                TenantIsolation = 1.0,
                CompositeTrustScore = composite,
                CorroborationScore = evidenceCount > 5 ? 0.80 : 0.50,
                ManipulationRisk = Math.Round(1.0 - manipulationHistory, 3),
                ScopeConfidence = 0.90,
                IsQuarantined = source.IsQuarantined,
                QuarantineReason = source.QuarantineReason,
                ConsecutiveCleanVerifications = source.ConsecutiveCleanVerifications,
                ReverificationRequiredCount = 3,
                TrustRestoredAt = source.TrustRestoredAt,
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

        public async Task PenalizeSourceOnAnomalyAsync(Guid sourceId, Guid workspaceId, string reason, CancellationToken ct = default)
        {
            var source = await GetSourceAsync(sourceId, workspaceId, ct);
            if (source == null) return;

            source.AnomalousClaimCount++;
            source.ManipulationHistoryScore = Math.Clamp(source.ManipulationHistoryScore - 0.45, 0.05, 1.0);
            source.VerificationSuccessRate = Math.Clamp(source.VerificationSuccessRate - 0.40, 0.05, 1.0);
            source.IsQuarantined = true;
            source.QuarantineReason = reason;
            source.QuarantinedAt = DateTime.UtcNow;
            source.ConsecutiveCleanVerifications = 0;
            source.Status = ExternalSourceStatus.Suspended;
            source.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(ct);
        }

        public async Task RecordVerifiedCleanObservationAsync(Guid sourceId, Guid workspaceId, CancellationToken ct = default)
        {
            var source = await GetSourceAsync(sourceId, workspaceId, ct);
            if (source == null) return;

            if (source.IsQuarantined)
            {
                source.ConsecutiveCleanVerifications++;
                if (source.ConsecutiveCleanVerifications >= 3)
                {
                    // Safe auditable trust restoration
                    source.IsQuarantined = false;
                    source.QuarantineReason = null;
                    source.Status = ExternalSourceStatus.Active;
                    source.TrustRestoredAt = DateTime.UtcNow;
                    source.ManipulationHistoryScore = Math.Clamp(source.ManipulationHistoryScore + 0.40, 0.10, 0.85);
                    source.VerificationSuccessRate = Math.Clamp(source.VerificationSuccessRate + 0.35, 0.10, 0.85);
                }
            }
            else
            {
                source.VerificationSuccessRate = Math.Clamp(source.VerificationSuccessRate + 0.02, 0.0, 1.0);
            }
            source.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(ct);
        }

        public async Task<ExternalEvidenceRecord> IngestExternalEvidenceAsync(ExternalEvidenceRecord evidence, CancellationToken ct = default)
        {
            if (evidence.WorkspaceId == Guid.Empty)
                throw new ArgumentException("WorkspaceId must be set for evidence ingestion.", nameof(evidence));

            // Formal State Machine Transition: Untrusted -> Ingested
            evidence.PromotionState = ExternalPromotionState.Ingested;

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

            // Formal State Machine Transition: Ingested -> Sanitized
            evidence.PromotionState = ExternalPromotionState.Sanitized;

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

            // Formal State Machine Transition: Sanitized -> Classified
            evidence.PromotionState = ExternalPromotionState.Classified;

            // Check if source is quarantined
            var source = await GetSourceAsync(evidence.SourceId, evidence.WorkspaceId, ct);
            if (source != null && source.IsQuarantined)
            {
                evidence.SourceReliability = Math.Min(evidence.SourceReliability, 0.20);
                evidence.EvidenceStrength = Math.Min(evidence.EvidenceStrength, 0.25);
                evidence.ContaminationRisk = Math.Max(evidence.ContaminationRisk, 0.70);
            }

            evidence.CreatedAt = DateTime.UtcNow;

            // 4. Update or Create SignalCluster for Claim Deduplication and calculate independence graph
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
                    ClusterRootSourceId = evidence.SourceId,
                    EvidenceIds = new List<Guid> { evidence.Id },
                    SourceIds = new List<Guid> { evidence.SourceId },
                    TotalCopyCount = 1,
                    UniqueSourceCount = 1,
                    IndependentEvidenceCount = 1,
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

                // Invariant: Five websites repeating the same press release are NOT five independent confirmations.
                // IndependentEvidenceCount != SourceCount
                // Verbatim claim cluster shares a single root release -> IndependentEvidenceCount remains 1!
                cluster.IndependentEvidenceCount = 1;
                cluster.IsSyndicatedDuplicateGroup = true;

                double syndicationRatio = (double)cluster.TotalCopyCount / Math.Max(1, cluster.UniqueSourceCount);
                double decayFactor = Math.Max(syndicationRatio, Math.Sqrt(cluster.TotalCopyCount));
                cluster.IndependenceEstimate = Math.Clamp(Math.Round(1.0 / decayFactor, 3), 0.20, 1.0);

                // Update evidence independence factor based on cluster syndication
                evidence.IndependenceFactor = cluster.IndependenceEstimate;

                cluster.LastObservedAt = evidence.ObservedAt > cluster.LastObservedAt ? evidence.ObservedAt : cluster.LastObservedAt;
                cluster.CorroborationScore = Math.Clamp(0.40 + (cluster.UniqueSourceCount * 0.10), 0.0, 1.0);

                if (cluster.CorroborationScore >= 0.70 && cluster.UniqueSourceCount >= 2 && !cluster.IsSyndicatedDuplicateGroup)
                {
                    evidence.PromotionState = ExternalPromotionState.Corroborated;
                }
            }
        }
    }
}
