using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace BusinessModelApp.Core.Domain.Runtime.Organizational
{
    /// <summary>
    /// Constitutional Invariant I28: Organizational Memory Sovereignty
    /// MEMORY ≠ LEARNING ≠ KNOWLEDGE ≠ TRUTH ≠ HYPOTHESIS ≠ POLICY ≠ GOVERNANCE ≠ AUTHORITY ≠ EXECUTION
    /// </summary>
    public static class OrganizationalMemorySovereignty
    {
        public const string InvariantName = "I28";
        public const string InvariantStatement =
            "MEMORY ≠ LEARNING ≠ KNOWLEDGE ≠ TRUTH ≠ HYPOTHESIS ≠ POLICY ≠ GOVERNANCE ≠ AUTHORITY ≠ EXECUTION";

        public const string I28_A_MemoryNotGroundTruth =
            "I28-A: Memory records historical assertions, observations, and trajectories. Empirical facts require cryptographic evidence references.";

        public const string I28_B_MemoryNotLearning =
            "I28-B: Remembering an episodic outcome is not causal induction or policy generalization. Learning explicitly consumes verified trajectories.";

        public const string I28_C_MemoryNotPolicyOrDoctrine =
            "I28-C: Historical precedent cannot alter operational doctrines, governance gates, or PRG-1 rules without human approval.";

        public const string I28_D_MemoryNotExecutionAuthority =
            "I28-D: Stored memory, prior precedents, or cached plans cannot issue or modify an ExecutionPermit or bypass the Batch 6 Firewall.";

        public const string I28_E_RealityChangesInvalidateApplicabilityNotHistory =
            "I28-E: When reality shifts, historical memory remains true for its era; its Current Applicability degrades, but historical truth is never rewritten.";

        public const string I28_F_AntiPatternsAreWarningsNotProhibitions =
            "I28-F: Anti-patterns inform applicability analysis and raise warnings; only Policy/Constraint engines can issue hard execution blocks.";

        public const string I28_G_BoundedContextAssembly =
            "I28-G: Context assembler produces bounded, prioritized, token-capped snapshots to prevent prompt bloat and hallucination cascades.";

        public const string I28_H_MultiTenantPartitioning =
            "I28-H: Context records are strictly partitioned by TenantId.";

        public const string I28_I_MemoryCannotUpgradeEpistemicStatus =
            "I28-I: Retrieval frequency never converts Unknown to Fact, Hypothesis to Knowledge, or Simulation to Reality.";

        public const string I28_J_RetrievalNotValidation =
            "I28-J: Retrieved memories must pass through Evidence Validation, Freshness Checking, and Epistemic Classification before context eligibility.";

        public const string I28_K_MemoryCannotSelfReinforce =
            "I28-K: Memory-derived evidence cannot cite itself or other memories to increase confidence. Confidence increases only via independent authorized evidence.";

        public const string I28_L_ContextSnapshotNotTruthSnapshot =
            "I28-L: OrganizationalContextSnapshot is a decision-support artifact, never replacing Reality Envelopes, Digital Twins, or Evidence Graphs.";

        public const string I28_M_HistoricalContextImmutability =
            "I28-M: Historical context snapshots used by past missions/decisions are permanently reconstructible from original versioned inputs and hashes.";

        public const string I28_N_MemoryWriteSovereignty =
            "I28-N: No AI model, agent, worker, or retrieved document can directly write authoritative organizational memory. All writes require governed provenance and classification.";
    }

    public enum EpistemicStatus
    {
        Unknown = 0,
        Hypothesis = 1,
        Inference = 2,
        ObservedFact = 3,
        VerifiedTruth = 4
    }

    public enum CurrentApplicability
    {
        Inapplicable = 0,
        Low = 1,
        Medium = 2,
        High = 3
    }

    public enum FreshnessStatus
    {
        Unknown = 0,
        Fresh = 1,
        Aging = 2,
        Stale = 3,
        Expired = 4,
        Conflicted = 5
    }

    public sealed class ContextAssemblyPolicy
    {
        public int MaxContextTokens { get; set; } = 8_000;
        public int MaxPrecedents { get; set; } = 5;
        public int MaxAntiPatterns { get; set; } = 3;
        public int MaxTrajectoryRecords { get; set; } = 5;
        public double MinEvidenceQuality { get; set; } = 0.70;
        public CurrentApplicability MinApplicability { get; set; } = CurrentApplicability.Medium;
        public string RankingPolicyVersion { get; set; } = "CAP-v1-DEFAULT";
        public TimeSpan FreshnessWindow { get; set; } = TimeSpan.FromDays(30);
    }

    public sealed class OrganizationalPrecedent
    {
        public string PrecedentId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string SourceTrajectoryId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string HistoricalDecisionId { get; set; } = string.Empty;
        public string HistoricalOutcomeId { get; set; } = string.Empty;
        public string ObservedConditions { get; set; } = string.Empty;
        public string PredictedOutcome { get; set; } = string.Empty;
        public string ActualOutcome { get; set; } = string.Empty;
        public double Variance { get; set; } = 0.0;
        public EpistemicStatus EpistemicClassification { get; set; } = EpistemicStatus.ObservedFact;
        public List<string> EvidenceReferences { get; set; } = new();
        public List<string> ApplicabilityConditions { get; set; } = new();
        public List<string> CounterExamples { get; set; } = new();
        public FreshnessStatus Freshness { get; set; } = FreshnessStatus.Fresh;
        public CurrentApplicability Applicability { get; set; } = CurrentApplicability.High;
        public bool IsHistoricallyValid { get; set; } = true;
        public string PolicySnapshotHash { get; set; } = string.Empty;
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public string ProvenanceHash { get; set; } = string.Empty;

        public void ComputeProvenance()
        {
            var raw = $"{PrecedentId}:{TenantId}:{SourceTrajectoryId}:{HistoricalDecisionId}:{HistoricalOutcomeId}:{Variance}:{EpistemicClassification}:{CreatedUtc:O}";
            using var sha = SHA256.Create();
            ProvenanceHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }
    }

    public sealed class OrganizationalAntiPattern
    {
        public string AntiPatternId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string FailurePatternSummary { get; set; } = string.Empty;
        public List<string> HistoricalOutcomeRefs { get; set; } = new();
        public List<string> ApplicabilityFilter { get; set; } = new();
        public string WarningDirective { get; set; } = string.Empty;
        public List<string> LessonsLearned { get; set; } = new();
        public string Severity { get; set; } = "High";
        public bool IsActive { get; set; } = true;
        public DateTime RecordedUtc { get; set; } = DateTime.UtcNow;
    }

    public sealed class OrganizationalTrajectory
    {
        public string TrajectoryId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string ResponsibilityId { get; set; } = string.Empty;
        public string WorkProposalId { get; set; } = string.Empty;
        public string WorkId { get; set; } = string.Empty;
        public string? WorkPlanId { get; set; }
        public string? MissionProposalId { get; set; }
        public string? MissionId { get; set; }
        public string? MissionRunId { get; set; }
        public List<string> NodeIds { get; set; } = new();
        public string? OutcomeId { get; set; }
        public string? LearningEpisodeId { get; set; }
        public DateTime StartedUtc { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedUtc { get; set; }
        public string ProvenanceHash { get; set; } = string.Empty;

        public void ComputeProvenance()
        {
            var raw = $"{TrajectoryId}:{TenantId}:{ResponsibilityId}:{WorkProposalId}:{WorkId}:{WorkPlanId}:{MissionId}:{OutcomeId}:{StartedUtc:O}:{CompletedUtc:O}";
            using var sha = SHA256.Create();
            ProvenanceHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }
    }

    public sealed class MemoryProvenanceLineage
    {
        public string MemoryId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string SourceRecordType { get; set; } = string.Empty;
        public string SourceRecordId { get; set; } = string.Empty;
        public string EvidenceHash { get; set; } = string.Empty;
        public string TelemetryRef { get; set; } = string.Empty;
        public string VerificationHash { get; set; } = string.Empty;
        public string OutcomeRef { get; set; } = string.Empty;
        public string ProvenanceGraphSummary { get; set; } = string.Empty;
        public DateTime GroundedUtc { get; set; } = DateTime.UtcNow;
    }

    public sealed class ContextSnapshotItem
    {
        public string ItemId { get; set; } = string.Empty;
        public string ItemType { get; set; } = string.Empty; // Precedent, AntiPattern, Trajectory, Fact
        public string Content { get; set; } = string.Empty;
        public double Score { get; set; }
        public int EstimatedTokens { get; set; }
        public EpistemicStatus EpistemicStatus { get; set; }
        public FreshnessStatus Freshness { get; set; }
        public CurrentApplicability Applicability { get; set; }
    }

    public sealed class OrganizationalContextSnapshot
    {
        public string SnapshotId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string TargetWorkId { get; set; } = string.Empty;
        public int TokenBudget { get; set; }
        public int EstimatedTokenCount { get; set; }
        public List<ContextSnapshotItem> ItemsIncluded { get; set; } = new();
        public List<string> ItemsExcluded { get; set; } = new();
        public Dictionary<string, string> ExclusionReasons { get; set; } = new();
        public string RankingPolicyVersion { get; set; } = string.Empty;
        public string InputHashes { get; set; } = string.Empty;
        public DateTime AssemblyTimestamp { get; set; } = DateTime.UtcNow;
        public string SnapshotHash { get; set; } = string.Empty;

        public void ComputeSnapshotHash()
        {
            var raw = $"{SnapshotId}:{TenantId}:{TargetWorkId}:{TokenBudget}:{EstimatedTokenCount}:{ItemsIncluded.Count}:{AssemblyTimestamp:O}";
            using var sha = SHA256.Create();
            SnapshotHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }
    }

    public sealed class FreshnessEvaluationResult
    {
        public string MemoryId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public FreshnessStatus PreviousFreshness { get; set; }
        public FreshnessStatus NewFreshness { get; set; }
        public CurrentApplicability PreviousApplicability { get; set; }
        public CurrentApplicability NewApplicability { get; set; }
        public bool HistoricalValidityUnchanged { get; set; } = true;
        public string Rationale { get; set; } = string.Empty;
        public DateTime EvaluatedUtc { get; set; } = DateTime.UtcNow;
    }
}
