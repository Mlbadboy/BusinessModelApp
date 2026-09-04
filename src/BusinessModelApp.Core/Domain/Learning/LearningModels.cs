using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BusinessModelApp.Core.Domain.Common;
using BusinessModelApp.Core.Domain.DigitalTwin;
using BusinessModelApp.Core.Domain.Reality;

namespace BusinessModelApp.Core.Domain.Learning
{
    public enum LearningState
    {
        Candidate = 0,    // Newly observed lesson; untrusted and unverified
        Validating = 1,   // Under active corroboration and multi-mission evaluation
        Quarantined = 2,  // Flagged due to unverified claims, anomalous assumptions, or poison risk
        Approved = 3,     // Meets deterministic validation criteria and passed contradiction checks
        Promoted = 4,     // Formally admitted into an elevated learning tier (L2-L5)
        Active = 5,       // Currently active operational knowledge used for advisory strategy context
        Aging = 6,        // Time since observation exceeds aging threshold; confidence decaying
        Stale = 7,        // Decayed past planning threshold; excluded from automatic prompt inclusion
        Superseded = 8,   // Replaced by newer, higher-confidence, or better-contextualized learning
        Rejected = 9      // Disproven or contradictory; permanently archived
    }

    public enum LearningTier
    {
        L0_Session = 0,               // Ephemeral execution scratchpad observations
        L1_Mission = 1,               // Single completed mission outcome lessons
        L2_Agent = 2,                 // Agent-specific performance heuristics and tool reliability
        L3_Organizational = 3,        // Cross-department tenant knowledge
        L4_Strategic = 4,             // Validated commercial, pricing, and campaign insights
        L5_ValidatedInstitutional = 5 // High-confidence institutional playbooks proven across repeated cycles
    }

    public enum FailureRootCause
    {
        BadEvidence = 1,          // Data source was inaccurate, manipulated, or incorrect
        StaleEvidence = 2,        // Data decayed past freshness thresholds before execution completed
        IncorrectAssumption = 3,  // Initial strategic or economic premise was flawed
        ModelReasoning = 4,       // LLM failed on deduction, instruction following, or logical synthesis
        AgentBehavior = 5,        // Agent violated sequence, failed recovery, or picked suboptimal action
        ToolBehavior = 6,         // External API or connector returned error, timeout, or schema failure
        Strategy = 7,             // Formulated route was unviable under current market realities
        MarketChange = 8,         // External economic, regulatory, or competitive shift occurred
        HumanIntervention = 9,    // Explicit human operator override altered path or stopped execution
        PolicyRestriction = 10,   // Constitution Policy Engine blocked execution due to budget/risk cap
        DataQuality = 11,         // Malformed payload, missing fields, or encoding issue
        ExecutionFailure = 12,    // Process crash, power interruption, network disconnect
        Unknown = 13              // Insufficient evidence to determine root cause (Valid first-class state)
    }

    public enum ContradictionStatus
    {
        NotContradictory = 1,
        ContextuallyDifferent = 2,
        PartialContradiction = 3,
        DirectContradiction = 4,
        Unresolved = 5
    }

    public enum ExperimentStatus
    {
        Hypothesis = 0,
        Design = 1,
        Approval = 2,
        Execution = 3,
        Measurement = 4,
        Analysis = 5,
        Validation = 6,
        Learning = 7,
        Completed = 8,
        Cancelled = 9
    }

    public enum OutcomeSuccessStatus
    {
        Success = 1,
        PartialSuccess = 2,
        Failure = 3,
        BlockedByPolicy = 4
    }

    /// <summary>
    /// Governed institutional learning record with full provenance, metrology, and causal confidence.
    /// Invariant: Learning is NOT truth, NOT policy, and NOT authority!
    /// </summary>
    public class LearningRecord : Entity
    {
        public Guid WorkspaceId { get; set; }
        public Guid? SourceMissionId { get; set; }
        public Guid? SourceDecisionId { get; set; }
        public string? SourceAgentId { get; set; }
        public string? SourceModelId { get; set; }
        public Guid? DigitalTwinSnapshotId { get; set; }

        public LearningTier Tier { get; set; } = LearningTier.L1_Mission;
        public LearningState State { get; set; } = LearningState.Candidate;
        public TruthClassification Classification { get; set; } = TruthClassification.Learning;

        public string Statement { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string ApplicableDimensionsJson { get; set; } = "[]";

        public List<Guid> EvidenceRecordIds { get; set; } = new();
        public List<string> GroundingEvidenceHashes { get; set; } = new();

        public double Confidence { get; set; } = 0.50;
        public double CausalConfidence { get; set; } = 0.40; // Decoupled from TruthConfidence!

        public int SampleSize { get; set; } = 1;
        public int ValidationCount { get; set; } = 0;
        public int ContradictionCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastValidatedAt { get; set; }
        public DateTime? LastObservedAt { get; set; } = DateTime.UtcNow;

        public FreshnessState Freshness { get; set; } = FreshnessState.VERIFIED;
        public Guid? SupersededBy { get; set; }

        public string ApplicabilityScope { get; set; } = "Enterprise Mid-Market B2B";
        public string Provenance { get; set; } = "MissionOutcome.Evaluation";
        public int Version { get; set; } = 1;
        public string IntegrityHash { get; set; } = string.Empty;

        public bool IsProcedureCandidate { get; set; } = false;
        public string? CrystallizedProcedureJson { get; set; }

        public string ComputeIntegrityHash()
        {
            var raw = $"{Id:N}|{WorkspaceId:N}|{Statement}|{Tier}|{State}|{Confidence:F4}|{CausalConfidence:F4}|{ValidationCount}|{CreatedAt:O}";
            using var sha = SHA256.Create();
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            IntegrityHash = BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
            return IntegrityHash;
        }

        public double CalculateLearningValue()
        {
            double usefulness = (Tier >= LearningTier.L3_Organizational ? 1.0 : 0.7);
            double freshnessFactor = Freshness == FreshnessState.VERIFIED ? 1.0 : Freshness == FreshnessState.AGING ? 0.7 : 0.2;
            double validationStrength = Math.Min(1.0, (ValidationCount + 1) / 5.0);
            double contradictionPenalty = ContradictionCount > 0 ? 0.5 : 1.0;

            return Math.Round(usefulness * Confidence * CausalConfidence * freshnessFactor * validationStrength * contradictionPenalty, 4);
        }
    }

    /// <summary>
    /// Outcome of a mission execution comparing expected vs actual reality.
    /// </summary>
    public class OutcomeRecord : Entity
    {
        public Guid WorkspaceId { get; set; }
        public Guid MissionId { get; set; }
        public Guid? DecisionId { get; set; }

        public decimal ExpectedRevenueINR { get; set; }
        public decimal ActualRevenueINR { get; set; }
        public decimal RevenueDeltaINR => ActualRevenueINR - ExpectedRevenueINR;

        public decimal ExpectedCostINR { get; set; }
        public decimal ActualCostINR { get; set; }
        public decimal CostDeltaINR => ActualCostINR - ExpectedCostINR;

        public int ExpectedDurationMinutes { get; set; }
        public int ActualDurationMinutes { get; set; }
        public int DurationDeltaMinutes => ActualDurationMinutes - ExpectedDurationMinutes;

        public double ExpectedWinProbability { get; set; }
        public double ActualWinProbability { get; set; }

        public OutcomeSuccessStatus SuccessStatus { get; set; } = OutcomeSuccessStatus.Success;
        public string DeviationSummary { get; set; } = string.Empty;
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Failure record capturing diagnosed deviation and mapped to root-cause taxonomy.
    /// </summary>
    public class FailureRecord : Entity
    {
        public Guid WorkspaceId { get; set; }
        public Guid MissionId { get; set; }
        public FailureRootCause RootCause { get; set; } = FailureRootCause.Unknown;
        public string Diagnosis { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;
        public string EvidenceHashesJson { get; set; } = "[]";
        public bool HasCorrectiveAction { get; set; } = false;
        public Guid? CorrectionRecordId { get; set; }
        public DateTime ObservedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Corrective action implemented in response to an observed failure.
    /// </summary>
    public class CorrectionRecord : Entity
    {
        public Guid WorkspaceId { get; set; }
        public Guid FailureRecordId { get; set; }
        public string ActionTaken { get; set; } = string.Empty;
        public bool WasSuccessful { get; set; } = false;
        public string SubsequentOutcomeSummary { get; set; } = string.Empty;
        public DateTime ImplementedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Learning episode linking mission, outcome, failure, and candidate learning.
    /// </summary>
    public class LearningEpisode : Entity
    {
        public Guid WorkspaceId { get; set; }
        public Guid MissionId { get; set; }
        public Guid? DecisionId { get; set; }
        public Guid? OutcomeRecordId { get; set; }
        public Guid? FailureRecordId { get; set; }
        public Guid? LearningRecordId { get; set; }
        public FailureRootCause RootCause { get; set; } = FailureRootCause.Unknown;
        public string Narrative { get; set; } = string.Empty;
        public DateTime EpisodeTimestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Multi-source dispute between two contradictory learning assertions.
    /// </summary>
    public class LearningContradictionRecord : Entity
    {
        public Guid WorkspaceId { get; set; }
        public Guid LearningRecordAId { get; set; }
        public Guid LearningRecordBId { get; set; }
        public ContradictionStatus Status { get; set; } = ContradictionStatus.Unresolved;
        public string Details { get; set; } = string.Empty;
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
        public string? ResolutionNote { get; set; }
    }

    /// <summary>
    /// Controlled experimental trial to validate or disprove a learning hypothesis.
    /// </summary>
    public class LearningExperiment : Entity
    {
        public Guid WorkspaceId { get; set; }
        public string Hypothesis { get; set; } = string.Empty;
        public string TargetMetric { get; set; } = string.Empty;
        public decimal BaselineValue { get; set; }
        public decimal ExpectedValue { get; set; }
        public decimal? ActualValue { get; set; }
        public int SampleSize { get; set; } = 10;
        public decimal BudgetCapINR { get; set; } = 5000m;
        public decimal SpentINR { get; set; } = 0m;
        public ExperimentStatus Status { get; set; } = ExperimentStatus.Hypothesis;
        public string Conclusion { get; set; } = string.Empty;
        public Guid? ResultingLearningRecordId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
    }

    /// <summary>
    /// Deterministic guard preventing unauthorized promotion, AI self-promotion, and poison attacks.
    /// </summary>
    public static class LearningPromotionGuard
    {
        public static void ValidatePromotion(
            LearningRecord record,
            LearningTier targetTier,
            LearningState targetState,
            bool isDirectAiCall = false)
        {
            if (isDirectAiCall)
            {
                throw new InvalidOperationException("AI cannot directly authorize learning promotion or transition state. Governed evaluation required.");
            }

            if (targetState == LearningState.Approved || targetState == LearningState.Promoted || targetState == LearningState.Active)
            {
                if (record.State == LearningState.Quarantined)
                {
                    throw new InvalidOperationException("Quarantined learning cannot be promoted or activated until formal governance review completes.");
                }

                if (record.Freshness == FreshnessState.STALE || record.Freshness == FreshnessState.UNKNOWN)
                {
                    throw new InvalidOperationException("Stale or decayed learning cannot be promoted to Active institutional knowledge.");
                }

                if (record.ContradictionCount > 0 && targetTier >= LearningTier.L4_Strategic)
                {
                    throw new InvalidOperationException("Learning with unresolved contradictions cannot be promoted to Strategic or Institutional tier.");
                }

                if (record.ValidationCount < 1 && targetTier >= LearningTier.L2_Agent)
                {
                    throw new InvalidOperationException("Promotion above Mission tier requires at least 1 independent empirical validation.");
                }

                if (record.ValidationCount < 3 && targetTier >= LearningTier.L5_ValidatedInstitutional)
                {
                    throw new InvalidOperationException("Promotion to Validated Institutional tier requires at least 3 independent empirical validations.");
                }

                if (record.CausalConfidence < 0.50 && targetTier >= LearningTier.L3_Organizational)
                {
                    throw new InvalidOperationException($"Promotion to Organizational tier requires Causal Confidence >= 0.50. Current: {record.CausalConfidence:F2}.");
                }
            }
        }
    }
}
