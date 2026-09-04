using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.DigitalTwin;
using BusinessModelApp.Core.Domain.Learning;
using BusinessModelApp.Core.Domain.Reality;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BusinessModelApp.Infrastructure.Learning
{
    /// <summary>
    /// Governed implementation of Phase 2 Batch 3 Institutional Learning & Causal Intelligence Engine.
    /// Core Invariant: MEMORY != LEARNING != KNOWLEDGE != TRUTH != POLICY.
    /// </summary>
    public class InstitutionalLearningService : IInstitutionalLearningService
    {
        private readonly AppDbContext _dbContext;
        private readonly ICompanyDigitalTwinService _digitalTwinService;
        private readonly IRealityDecayEngine _decayEngine;
        private readonly ILogger<InstitutionalLearningService> _logger;

        public InstitutionalLearningService(
            AppDbContext dbContext,
            ICompanyDigitalTwinService digitalTwinService,
            IRealityDecayEngine decayEngine,
            ILogger<InstitutionalLearningService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _digitalTwinService = digitalTwinService ?? throw new ArgumentNullException(nameof(digitalTwinService));
            _decayEngine = decayEngine ?? throw new ArgumentNullException(nameof(decayEngine));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<OutcomeRecord> RecordMissionOutcomeAsync(Guid workspaceId, OutcomeRecord outcome, CancellationToken ct = default)
        {
            if (workspaceId == Guid.Empty)
                throw new ArgumentException("WorkspaceId must not be empty.", nameof(workspaceId));
            if (outcome == null)
                throw new ArgumentNullException(nameof(outcome));

            outcome.WorkspaceId = workspaceId;
            if (outcome.Id == Guid.Empty)
                outcome.Id = Guid.NewGuid();

            outcome.RecordedAt = DateTime.UtcNow;

            // Deterministic calculation of deviations
            var revenueDelta = outcome.ActualRevenueINR - outcome.ExpectedRevenueINR;
            var costDelta = outcome.ActualCostINR - outcome.ExpectedCostINR;
            var durationDelta = outcome.ActualDurationMinutes - outcome.ExpectedDurationMinutes;

            if (string.IsNullOrWhiteSpace(outcome.DeviationSummary))
            {
                var sb = new StringBuilder();
                if (revenueDelta != 0) sb.Append($"Revenue Delta: ₹{revenueDelta:+0.00;-0.00}. ");
                if (costDelta != 0) sb.Append($"Cost Delta: ₹{costDelta:+0.00;-0.00}. ");
                if (durationDelta != 0) sb.Append($"Duration Delta: {durationDelta:+0;-0}m. ");

                if (outcome.SuccessStatus == OutcomeSuccessStatus.Failure)
                    sb.Append("Execution concluded with objective failure. ");
                else if (outcome.SuccessStatus == OutcomeSuccessStatus.PartialSuccess)
                    sb.Append("Execution achieved partial completion. ");
                else if (outcome.SuccessStatus == OutcomeSuccessStatus.BlockedByPolicy)
                    sb.Append("Execution halted by deterministic policy firewall. ");
                else
                    sb.Append("Execution concluded within target parameters. ");

                outcome.DeviationSummary = sb.ToString().Trim();
            }

            _dbContext.OutcomeRecords.Add(outcome);
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("[LearningEngine] Recorded outcome for Mission {MissionId} in Workspace {WorkspaceId}. Revenue Delta: {RevDelta}, Success: {Status}",
                outcome.MissionId, workspaceId, revenueDelta, outcome.SuccessStatus);

            return outcome;
        }

        public async Task<FailureRecord> DiagnoseFailureAsync(
            Guid workspaceId,
            Guid missionId,
            FailureRootCause rootCause,
            string diagnosis,
            string impact,
            CancellationToken ct = default)
        {
            if (workspaceId == Guid.Empty)
                throw new ArgumentException("WorkspaceId must not be empty.", nameof(workspaceId));
            if (string.IsNullOrWhiteSpace(diagnosis))
                throw new ArgumentException("Diagnosis statement is required.", nameof(diagnosis));

            var failure = new FailureRecord
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                MissionId = missionId,
                RootCause = rootCause,
                Diagnosis = diagnosis,
                Impact = impact ?? string.Empty,
                ObservedAt = DateTime.UtcNow,
                HasCorrectiveAction = false
            };

            _dbContext.FailureRecords.Add(failure);

            // Maintain chronological learning episode linking mission and diagnosed root cause
            var episode = new LearningEpisode
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                MissionId = missionId,
                FailureRecordId = failure.Id,
                RootCause = rootCause,
                Narrative = $"Diagnosed root cause '{rootCause}' for Mission {missionId}: {diagnosis}",
                EpisodeTimestamp = DateTime.UtcNow
            };
            _dbContext.LearningEpisodes.Add(episode);

            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("[LearningEngine] Diagnosed failure {FailureId} for Mission {MissionId} with RootCause {RootCause}",
                failure.Id, missionId, rootCause);

            return failure;
        }

        public async Task<CorrectionRecord> RecordCorrectionAsync(
            Guid workspaceId,
            Guid failureRecordId,
            string actionTaken,
            bool wasSuccessful,
            string outcomeSummary,
            CancellationToken ct = default)
        {
            if (workspaceId == Guid.Empty)
                throw new ArgumentException("WorkspaceId must not be empty.", nameof(workspaceId));

            var failure = await _dbContext.FailureRecords
                .FirstOrDefaultAsync(f => f.Id == failureRecordId && f.WorkspaceId == workspaceId, ct);

            if (failure == null)
                throw new KeyNotFoundException($"FailureRecord {failureRecordId} not found in Workspace {workspaceId}.");

            var correction = new CorrectionRecord
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                FailureRecordId = failureRecordId,
                ActionTaken = actionTaken,
                WasSuccessful = wasSuccessful,
                SubsequentOutcomeSummary = outcomeSummary ?? string.Empty,
                ImplementedAt = DateTime.UtcNow
            };

            _dbContext.CorrectionRecords.Add(correction);
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("[LearningEngine] Recorded correction {CorrectionId} for Failure {FailureId}. Successful: {Success}",
                correction.Id, failureRecordId, wasSuccessful);

            return correction;
        }

        public async Task<LearningRecord> GenerateLearningCandidateAsync(
            Guid workspaceId,
            Guid missionId,
            string statement,
            string context,
            string domain,
            double confidence,
            double causalConfidence,
            CancellationToken ct = default)
        {
            if (workspaceId == Guid.Empty)
                throw new ArgumentException("WorkspaceId must not be empty.", nameof(workspaceId));
            if (string.IsNullOrWhiteSpace(statement))
                throw new ArgumentException("Learning statement is required.", nameof(statement));

            // Check for exact duplicate candidate to prevent repetition inflation
            var existing = await _dbContext.LearningRecords
                .FirstOrDefaultAsync(l => l.WorkspaceId == workspaceId &&
                                         l.SourceMissionId == missionId &&
                                         l.Statement == statement, ct);

            if (existing != null)
            {
                _logger.LogWarning("[LearningEngine] Duplicate candidate detected for Mission {MissionId}. Skipping repetition inflation.", missionId);
                return existing;
            }

            // Capture Digital Twin snapshot if available for context provenance
            var twinSnapshot = await _dbContext.DigitalTwinSnapshots
                .Where(s => s.WorkspaceId == workspaceId)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync(ct);

            var record = new LearningRecord
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                SourceMissionId = missionId,
                Statement = statement,
                Context = context ?? string.Empty,
                Domain = string.IsNullOrWhiteSpace(domain) ? "General" : domain,
                Confidence = Math.Clamp(confidence, 0.05, 1.0),
                CausalConfidence = Math.Clamp(causalConfidence, 0.05, 1.0), // Decoupled from TruthConfidence!
                Classification = TruthClassification.Learning, // Never Fact!
                Tier = LearningTier.L1_Mission,
                State = LearningState.Candidate,
                Freshness = FreshnessState.VERIFIED,
                DigitalTwinSnapshotId = twinSnapshot?.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastObservedAt = DateTime.UtcNow,
                ValidationCount = 0,
                ContradictionCount = 0
            };

            record.ComputeIntegrityHash();

            _dbContext.LearningRecords.Add(record);

            var episode = new LearningEpisode
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                MissionId = missionId,
                LearningRecordId = record.Id,
                Narrative = $"Candidate learning extracted: {statement}",
                EpisodeTimestamp = DateTime.UtcNow
            };
            _dbContext.LearningEpisodes.Add(episode);

            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("[LearningEngine] Generated candidate learning {LearningId} for Mission {MissionId}. CausalConfidence: {CausalConf}",
                record.Id, missionId, record.CausalConfidence);

            return record;
        }

        public async Task<LearningRecord> ValidateAndPromoteLearningAsync(
            Guid workspaceId,
            Guid learningRecordId,
            bool isDirectAiCall = false,
            CancellationToken ct = default)
        {
            if (workspaceId == Guid.Empty)
                throw new ArgumentException("WorkspaceId must not be empty.", nameof(workspaceId));

            var record = await _dbContext.LearningRecords
                .FirstOrDefaultAsync(l => l.Id == learningRecordId && l.WorkspaceId == workspaceId, ct);

            if (record == null)
                throw new KeyNotFoundException($"LearningRecord {learningRecordId} not found in Workspace {workspaceId}.");

            // Apply decay check: if older than 30 days without corroboration, mark Aging
            var age = DateTime.UtcNow - record.CreatedAt;
            if (age.TotalDays > 60 && record.Freshness != FreshnessState.STALE)
            {
                record.Freshness = FreshnessState.STALE;
                record.State = LearningState.Stale;
            }
            else if (age.TotalDays > 30 && record.Freshness == FreshnessState.VERIFIED)
            {
                record.Freshness = FreshnessState.AGING;
                record.State = LearningState.Aging;
            }

            // Increment validation count on corroborated evaluation
            record.ValidationCount++;

            // Determine target tier and state based on cumulative validation count & causal confidence
            var targetTier = record.Tier;
            var targetState = record.State;

            if (record.ValidationCount == 1)
            {
                targetTier = LearningTier.L2_Agent;
                targetState = LearningState.Approved;
            }
            else if (record.ValidationCount == 2 && record.CausalConfidence >= 0.50)
            {
                targetTier = LearningTier.L3_Organizational;
                targetState = LearningState.Active;
            }
            else if (record.ValidationCount >= 3 && record.CausalConfidence >= 0.70)
            {
                targetTier = LearningTier.L4_Strategic;
                targetState = LearningState.Active;
            }
            else if (record.ValidationCount >= 4 && record.CausalConfidence >= 0.85)
            {
                targetTier = LearningTier.L5_ValidatedInstitutional;
                targetState = LearningState.Active;
            }

            // Invariant Gate: Block AI self-promotion or invalid promotion
            LearningPromotionGuard.ValidatePromotion(record, targetTier, targetState, isDirectAiCall);

            record.Tier = targetTier;
            record.State = targetState;
            record.LastValidatedAt = DateTime.UtcNow;
            record.UpdatedAt = DateTime.UtcNow;

            // Learning Crystallization: if L4 or L5 with high validation, propose procedure candidate
            if (record.Tier >= LearningTier.L4_Strategic && record.ValidationCount >= 3)
            {
                record.IsProcedureCandidate = true;
                record.CrystallizedProcedureJson = JsonSerializer.Serialize(new
                {
                    WorkflowCandidate = $"AutoProcedure_{record.Domain}",
                    Premise = record.Statement,
                    CausalConfidence = record.CausalConfidence,
                    AdvisoryOnly = true,
                    RequiresHumanApproval = true
                });
            }

            record.ComputeIntegrityHash();
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("[LearningEngine] Promoted learning {LearningId} to Tier {Tier}, State {State}. ValidationCount: {Count}",
                record.Id, record.Tier, record.State, record.ValidationCount);

            return record;
        }

        public async Task<IReadOnlyList<LearningContradictionRecord>> DetectContradictionsAsync(Guid workspaceId, CancellationToken ct = default)
        {
            if (workspaceId == Guid.Empty)
                throw new ArgumentException("WorkspaceId must not be empty.", nameof(workspaceId));

            var records = await _dbContext.LearningRecords
                .Where(l => l.WorkspaceId == workspaceId &&
                            l.State != LearningState.Rejected &&
                            l.State != LearningState.Superseded)
                .ToListAsync(ct);

            var existingContradictions = await _dbContext.LearningContradictions
                .Where(c => c.WorkspaceId == workspaceId)
                .ToListAsync(ct);

            var detected = new List<LearningContradictionRecord>();

            for (int i = 0; i < records.Count; i++)
            {
                for (int j = i + 1; j < records.Count; j++)
                {
                    var a = records[i];
                    var b = records[j];

                    // Check if contradiction already recorded
                    bool exists = existingContradictions.Any(c =>
                        (c.LearningRecordAId == a.Id && c.LearningRecordBId == b.Id) ||
                        (c.LearningRecordAId == b.Id && c.LearningRecordBId == a.Id));

                    if (exists) continue;

                    // Deterministic heuristic: opposite directional assertions on same domain/keywords
                    bool isContradiction = IsConflictingAssertion(a.Statement, b.Statement);

                    if (isContradiction)
                    {
                        var contradiction = new LearningContradictionRecord
                        {
                            Id = Guid.NewGuid(),
                            WorkspaceId = workspaceId,
                            LearningRecordAId = a.Id,
                            LearningRecordBId = b.Id,
                            Status = ContradictionStatus.DirectContradiction,
                            Details = $"Direct contradiction between [{a.Statement}] and [{b.Statement}]",
                            DetectedAt = DateTime.UtcNow
                        };

                        a.ContradictionCount++;
                        b.ContradictionCount++;

                        _dbContext.LearningContradictions.Add(contradiction);
                        detected.Add(contradiction);
                    }
                }
            }

            if (detected.Count > 0)
            {
                await _dbContext.SaveChangesAsync(ct);
                _logger.LogWarning("[LearningEngine] Detected {Count} new learning contradictions in Workspace {WorkspaceId}",
                    detected.Count, workspaceId);
            }

            return await _dbContext.LearningContradictions
                .Where(c => c.WorkspaceId == workspaceId)
                .ToListAsync(ct);
        }

        private static bool IsConflictingAssertion(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
                return false;

            string sa = a.ToLowerInvariant();
            string sb = b.ToLowerInvariant();

            // Extract shared keywords (excluding common stop words)
            var stopWords = new HashSet<string> { "the", "a", "an", "is", "in", "to", "for", "with", "and", "or", "by", "on", "at", "from" };
            var wordsA = sa.Split(new[] { ' ', '.', ',', ';', '!' }, StringSplitOptions.RemoveEmptyEntries)
                           .Where(w => !stopWords.Contains(w) && w.Length > 2)
                           .ToHashSet();
            var wordsB = sb.Split(new[] { ' ', '.', ',', ';', '!' }, StringSplitOptions.RemoveEmptyEntries)
                           .Where(w => !stopWords.Contains(w) && w.Length > 2)
                           .ToHashSet();

            // Check if they share meaningful subject tokens (e.g. "discounting", "conversion", "margin")
            var commonWords = wordsA.Intersect(wordsB).ToList();
            if (commonWords.Count == 0)
                return false;

            bool aIncreases = sa.Contains("increase") || sa.Contains("improves") || sa.Contains("boosts") || sa.Contains("higher");
            bool aDecreases = sa.Contains("decrease") || sa.Contains("reduces") || sa.Contains("harms") || sa.Contains("lower") || sa.Contains("without improving");

            bool bIncreases = sb.Contains("increase") || sb.Contains("improves") || sb.Contains("boosts") || sb.Contains("higher");
            bool bDecreases = sb.Contains("decrease") || sb.Contains("reduces") || sb.Contains("harms") || sb.Contains("lower") || sb.Contains("without improving");

            return (aIncreases && bDecreases) || (aDecreases && bIncreases);
        }

        public async Task<IReadOnlyList<ContextualLearningPromptItem>> RetrieveContextualLearningAsync(
            Guid workspaceId,
            string domain,
            string query,
            CancellationToken ct = default)
        {
            if (workspaceId == Guid.Empty)
                throw new ArgumentException("WorkspaceId must not be empty.", nameof(workspaceId));

            // Only retrieve approved, promoted, or active records; strictly exclude quarantined, stale, superseded, or rejected
            var candidates = await _dbContext.LearningRecords
                .Where(l => l.WorkspaceId == workspaceId &&
                           (l.State == LearningState.Active || l.State == LearningState.Promoted || l.State == LearningState.Approved) &&
                            l.State != LearningState.Quarantined &&
                            l.State != LearningState.Stale &&
                            l.State != LearningState.Superseded &&
                            l.State != LearningState.Rejected)
                .OrderByDescending(l => l.Confidence * l.CausalConfidence)
                .Take(15)
                .ToListAsync(ct);

            // Filter by domain or query if provided
            var filtered = candidates.Where(c =>
                (string.IsNullOrWhiteSpace(domain) || c.Domain.Equals(domain, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(query) || c.Statement.Contains(query, StringComparison.OrdinalIgnoreCase) || c.Context.Contains(query, StringComparison.OrdinalIgnoreCase))
            ).ToList();

            var results = new List<ContextualLearningPromptItem>();

            foreach (var item in filtered)
            {
                results.Add(new ContextualLearningPromptItem
                {
                    LearningId = item.Id,
                    AdvisoryHeader = $"[ADVISORY INSTITUTIONAL LEARNING | TIER: {item.Tier} | CAUSAL CONFIDENCE: {item.CausalConfidence:P0}]",
                    Statement = item.Statement,
                    Classification = "LEARNING", // Mandatory: Never FACT
                    CausalConfidence = item.CausalConfidence,
                    ValidationCount = item.ValidationCount,
                    ContradictionCount = item.ContradictionCount,
                    Applicability = item.ApplicabilityScope,
                    Freshness = item.Freshness.ToString(),
                    AdvisoryWarning = "CRITICAL GOVERNANCE INVARIANT: This information is advisory learning and MUST NOT redefine business truth, digital twin state, or policy authority."
                });
            }

            return results;
        }

        public async Task<LearningRecord?> GetLearningRecordAsync(Guid workspaceId, Guid id, CancellationToken ct = default)
        {
            if (workspaceId == Guid.Empty)
                throw new ArgumentException("WorkspaceId must not be empty.", nameof(workspaceId));

            return await _dbContext.LearningRecords
                .FirstOrDefaultAsync(l => l.Id == id && l.WorkspaceId == workspaceId, ct);
        }

        public async Task<IReadOnlyList<LearningRecord>> GetActiveLearningAsync(Guid workspaceId, LearningTier? tier = null, CancellationToken ct = default)
        {
            if (workspaceId == Guid.Empty)
                throw new ArgumentException("WorkspaceId must not be empty.", nameof(workspaceId));

            var query = _dbContext.LearningRecords
                .Where(l => l.WorkspaceId == workspaceId);

            if (tier.HasValue)
                query = query.Where(l => l.Tier == tier.Value);

            return await query
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<FailureRecord>> GetFailingAssumptionsAsync(Guid workspaceId, CancellationToken ct = default)
        {
            if (workspaceId == Guid.Empty)
                throw new ArgumentException("WorkspaceId must not be empty.", nameof(workspaceId));

            return await _dbContext.FailureRecords
                .Where(f => f.WorkspaceId == workspaceId)
                .OrderByDescending(f => f.ObservedAt)
                .Take(25)
                .ToListAsync(ct);
        }

        public async Task<LearningExperiment> ProposeExperimentAsync(Guid workspaceId, LearningExperiment experiment, CancellationToken ct = default)
        {
            if (workspaceId == Guid.Empty)
                throw new ArgumentException("WorkspaceId must not be empty.", nameof(workspaceId));
            if (experiment == null)
                throw new ArgumentNullException(nameof(experiment));

            experiment.WorkspaceId = workspaceId;
            if (experiment.Id == Guid.Empty)
                experiment.Id = Guid.NewGuid();

            experiment.Status = ExperimentStatus.Hypothesis;
            experiment.CreatedAt = DateTime.UtcNow;

            _dbContext.LearningExperiments.Add(experiment);
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("[LearningEngine] Proposed experiment {ExperimentId} in Workspace {WorkspaceId} for metric {Metric}",
                experiment.Id, workspaceId, experiment.TargetMetric);

            return experiment;
        }

        public async Task<LearningExperiment> RecordExperimentResultAsync(
            Guid workspaceId,
            Guid experimentId,
            decimal actualValue,
            string conclusion,
            CancellationToken ct = default)
        {
            if (workspaceId == Guid.Empty)
                throw new ArgumentException("WorkspaceId must not be empty.", nameof(workspaceId));

            var experiment = await _dbContext.LearningExperiments
                .FirstOrDefaultAsync(e => e.Id == experimentId && e.WorkspaceId == workspaceId, ct);

            if (experiment == null)
                throw new KeyNotFoundException($"Experiment {experimentId} not found in Workspace {workspaceId}.");

            experiment.ActualValue = actualValue;
            experiment.Conclusion = conclusion ?? string.Empty;
            experiment.Status = ExperimentStatus.Completed;
            experiment.CompletedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("[LearningEngine] Completed experiment {ExperimentId} with ActualValue {Actual}. Conclusion: {Conclusion}",
                experimentId, actualValue, conclusion);

            return experiment;
        }

        public async Task<LearningExplanation> ExplainLearningAsync(Guid workspaceId, Guid learningRecordId, CancellationToken ct = default)
        {
            if (workspaceId == Guid.Empty)
                throw new ArgumentException("WorkspaceId must not be empty.", nameof(workspaceId));

            var record = await _dbContext.LearningRecords
                .FirstOrDefaultAsync(l => l.Id == learningRecordId && l.WorkspaceId == workspaceId, ct);

            if (record == null)
                throw new KeyNotFoundException($"LearningRecord {learningRecordId} not found in Workspace {workspaceId}.");

            var contradictions = await _dbContext.LearningContradictions
                .Where(c => c.WorkspaceId == workspaceId && (c.LearningRecordAId == record.Id || c.LearningRecordBId == record.Id))
                .Select(c => c.Details)
                .ToListAsync(ct);

            var supportingMissions = new List<Guid>();
            if (record.SourceMissionId.HasValue)
                supportingMissions.Add(record.SourceMissionId.Value);

            var supportingEpisodes = await _dbContext.LearningEpisodes
                .Where(e => e.WorkspaceId == workspaceId && e.LearningRecordId == record.Id)
                .Select(e => e.MissionId)
                .Distinct()
                .ToListAsync(ct);

            foreach (var mId in supportingEpisodes)
            {
                if (!supportingMissions.Contains(mId))
                    supportingMissions.Add(mId);
            }

            return new LearningExplanation
            {
                LearningId = record.Id,
                Statement = record.Statement,
                Classification = "LEARNING",
                Confidence = record.Confidence,
                CausalConfidence = record.CausalConfidence,
                ValidationCount = record.ValidationCount,
                ContradictionCount = record.ContradictionCount,
                Freshness = record.Freshness.ToString(),
                Tier = record.Tier.ToString(),
                ApplicabilityScope = record.ApplicabilityScope,
                SupportingEvidenceIds = record.EvidenceRecordIds ?? new List<Guid>(),
                SupportingMissionIds = supportingMissions,
                ContradictingNotes = contradictions,
                Rationale = $"Observed in domain '{record.Domain}' with causal confidence {record.CausalConfidence:P0}. Validated across {record.ValidationCount} multi-mission cycles."
            };
        }
    }
}
