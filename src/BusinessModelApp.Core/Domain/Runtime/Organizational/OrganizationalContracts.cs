using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace BusinessModelApp.Core.Domain.Runtime.Organizational
{
    // =========================================================================
    // INVARIANT I25 — ORGANIZATIONAL AUTONOMY SOVEREIGNTY
    // INTELLIGENCE ≠ RESPONSIBILITY ≠ WORK ≠ WORK PLAN ≠ MISSION ≠ TASK ≠ ATTEMPT ≠ EFFECT ≠ OUTCOME ≠ LEARNING ≠ TRUTH
    // A WorkItem is an organizational intent/control object, NOT an execution authorization object.
    // INVARIANT I25-Q — EXECUTION BOUNDARY ISOLATION
    // No WorkProposal, WorkItem, WorkPlan, Responsibility, Assignment, Commitment,
    // or Outcome object may directly invoke a consequential connector or create an ExecutionPermit.
    // =========================================================================

    public enum WorkState
    {
        // Nominal Lifecycle
        Detected,
        Qualified,
        Planned,
        Assigned,
        Preparing,
        WaitingForGovernance,
        GovernanceApproved, // Organizational clearance to proceed to execution admission; DOES NOT EQUAL ExecutionPermit
        ExecutionAdmitted,
        Executing,
        Verifying,
        Completed,
        Measured,
        Closed,

        // Exception States
        Blocked,
        Paused,
        Expired,
        Cancelled,
        UnknownEffect,
        Escalated,
        Quarantined
    }

    public enum WorkPriority
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Critical = 3
    }

    public enum WorkUrgencyTier
    {
        Standard = 0,
        Elevated = 1,
        Urgent = 2,
        Immediate = 3
    }

    public enum WorkRiskTier
    {
        R0_Informational = 0,
        R1_InternalReversible = 1,
        R2_ExternalBounded = 2,
        R3_Consequential = 3,
        R4_CriticalSovereignty = 4
    }

    public enum WorkDependencyType
    {
        HardBlock,           // Dependent work cannot proceed past Preparing without satisfaction
        SoftRecommendation,  // Informational guidance; does not halt lifecycle
        Informational        // Contextual link
    }

    public enum AssigneeType
    {
        AgentFleet,
        HumanExpert,
        SubsystemWorker
    }

    public enum CommitmentStatus
    {
        Nominal,
        AtRisk,
        Breached,
        Fulfilled
    }

    public enum ProposalAdmissionStatus
    {
        Pending,
        Admitted,
        Rejected,
        Deduplicated
    }

    public enum EpistemicEvidenceKind
    {
        TelemetryFact,
        VerifiedMetric,
        CausalInference,
        SimulationProjection,
        HumanAttestation,
        Unknown
    }

    // --- First-Class Domain Models ---

    public sealed class OrganizationalResponsibility
    {
        public string ResponsibilityId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string BusinessDomain { get; set; } = string.Empty; // CashFlow, Sales, Margin, Ops, SLA, Governance
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> TargetOutcomes { get; set; } = new();
        public string AssignedLeadRole { get; set; } = string.Empty; // CEO, CFO, COO, CRO
        public bool IsActive { get; set; } = true;
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }

    public sealed class WorkObjective
    {
        public string Statement { get; set; } = string.Empty;
        public List<string> SuccessCriteria { get; set; } = new();
        public Dictionary<string, double> TargetMetrics { get; set; } = new();
        public List<string> NonGoals { get; set; } = new();
        public WorkRiskTier MaxPermissibleRisk { get; set; } = WorkRiskTier.R1_InternalReversible;
    }

    public sealed class WorkDeadline
    {
        public DateTime TargetUtc { get; set; }
        public DateTime HardCutoffUtc { get; set; }
        public bool AutoEscalateOnBreach { get; set; } = true;
        public int GracePeriodSeconds { get; set; } = 300;
    }

    public sealed class WorkEvidence
    {
        public string EvidenceId { get; set; } = string.Empty;
        public string WorkId { get; set; } = string.Empty;
        public EpistemicEvidenceKind EpistemicKind { get; set; } = EpistemicEvidenceKind.TelemetryFact;
        public string TelemetrySource { get; set; } = string.Empty;
        public string Sha256Hash { get; set; } = string.Empty;
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        public double VeracityScore { get; set; } = 1.0;
    }

    public sealed class WorkProposal
    {
        public string ProposalId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty; // Radar, Decision, Executive, DirectTelemetry
        public string SourceId { get; set; } = string.Empty;
        public string ResponsibilityId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public WorkObjective Objective { get; set; } = new();
        public List<string> EvidenceRefs { get; set; } = new();
        public WorkPriority Priority { get; set; } = WorkPriority.Medium;
        public WorkUrgencyTier Urgency { get; set; } = WorkUrgencyTier.Standard;
        public WorkRiskTier RiskTier { get; set; } = WorkRiskTier.R1_InternalReversible;
        public WorkDeadline? SuggestedDeadline { get; set; }
        public string SuggestedAssignee { get; set; } = string.Empty;
        public ProposalAdmissionStatus AdmissionStatus { get; set; } = ProposalAdmissionStatus.Pending;
        public string RejectionReason { get; set; } = string.Empty;
        public string ProvenanceHash { get; set; } = string.Empty;
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        public void ComputeProvenanceHash()
        {
            var raw = $"{ProposalId}:{TenantId}:{SourceType}:{SourceId}:{ResponsibilityId}:{Title}:{Priority}:{RiskTier}:{CreatedUtc:O}";
            using var sha = SHA256.Create();
            ProvenanceHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }
    }

    public sealed class WorkAssignment
    {
        public string AssignmentId { get; set; } = string.Empty;
        public string WorkId { get; set; } = string.Empty;
        public AssigneeType AssigneeType { get; set; } = AssigneeType.AgentFleet;
        public string AssigneeId { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime LeasedUntilUtc { get; set; } = DateTime.UtcNow.AddHours(2);
        public string Status { get; set; } = "Active";
    }

    public sealed class WorkCommitment
    {
        public string CommitmentId { get; set; } = string.Empty;
        public string WorkId { get; set; } = string.Empty;
        public string Deliverable { get; set; } = string.Empty;
        public DateTime DueUtc { get; set; }
        public int MaxSlaSeconds { get; set; } = 3600;
        public CommitmentStatus Status { get; set; } = CommitmentStatus.Nominal;
        public DateTime? EvaluatedAtUtc { get; set; }
        public string Note { get; set; } = string.Empty;
    }

    public sealed class WorkDependency
    {
        public string DependencyId { get; set; } = string.Empty;
        public string DependentWorkId { get; set; } = string.Empty; // The work item waiting
        public string RequiredWorkId { get; set; } = string.Empty;  // The prerequisite work item
        public WorkDependencyType DependencyType { get; set; } = WorkDependencyType.HardBlock;
        public bool IsSatisfied { get; set; } = false;
        public string SatisfactionEvidenceHash { get; set; } = string.Empty;
        public DateTime? SatisfiedAtUtc { get; set; }
    }

    public sealed class WorkPlan
    {
        public string PlanId { get; set; } = string.Empty;
        public string WorkId { get; set; } = string.Empty;
        public List<string> DecomposedMissions { get; set; } = new();
        public string SequenceType { get; set; } = "Sequential"; // Sequential, Parallel, DAG
        public List<string> Checkpoints { get; set; } = new();
        public string FallbackStrategy { get; set; } = "SafeStop";
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }

    public sealed class WorkEscalation
    {
        public string EscalationId { get; set; } = string.Empty;
        public string WorkId { get; set; } = string.Empty;
        public string TriggerReason { get; set; } = string.Empty;
        public WorkUrgencyTier UrgencyTier { get; set; } = WorkUrgencyTier.Urgent;
        public string RequiredApproverRole { get; set; } = "CEO";
        public DateTime InitiatedUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedUtc { get; set; }
        public string ResolutionNote { get; set; } = string.Empty;
        public bool IsResolved => ResolvedUtc.HasValue;
    }

    public sealed class WorkOutcome
    {
        public string OutcomeId { get; set; } = string.Empty;
        public string WorkId { get; set; } = string.Empty;
        public string ClaimedOutcomeSummary { get; set; } = string.Empty;
        public string VerifiedOutcomeSummary { get; set; } = string.Empty;
        public Dictionary<string, double> ActualMetrics { get; set; } = new();
        public Dictionary<string, double> PredictedVsActualVariance { get; set; } = new();
        public double SuccessScore { get; set; } = 0.0; // 0.0 to 1.0
        public bool IsVerified { get; set; } = false;
        public string VerificationEvidenceHash { get; set; } = string.Empty;
        public string VerifiedBy { get; set; } = string.Empty;
        public string RealizedValueSummary { get; set; } = string.Empty;
        public List<string> LessonsLearned { get; set; } = new();
        public DateTime RecordedUtc { get; set; } = DateTime.UtcNow;
    }

    public sealed class WorkItem
    {
        public string WorkId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string ResponsibilityId { get; set; } = string.Empty;
        public string? ParentWorkId { get; set; }
        public string ProposalId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public WorkObjective Objective { get; set; } = new();
        public WorkState State { get; set; } = WorkState.Detected;
        public WorkPriority Priority { get; set; } = WorkPriority.Medium;
        public WorkUrgencyTier Urgency { get; set; } = WorkUrgencyTier.Standard;
        public WorkRiskTier RiskTier { get; set; } = WorkRiskTier.R1_InternalReversible;
        public double PriorityScore { get; set; } = 0.5; // Multi-dimensional deterministic score
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public WorkDeadline? Deadline { get; set; }
        public List<string> DependencyIds { get; set; } = new();
        public WorkAssignment? Assignment { get; set; }
        public List<WorkCommitment> Commitments { get; set; } = new();
        public WorkEscalation? EscalationRecord { get; set; }
        public WorkOutcome? OutcomeRecord { get; set; }
        public string ProvenanceHash { get; set; } = string.Empty;

        // Cryptographic Lineage Tracking
        public string? MissionGraphId { get; set; }
        public string? MissionRunId { get; set; }

        public void ComputeProvenanceHash()
        {
            var raw = $"{WorkId}:{TenantId}:{ResponsibilityId}:{ProposalId}:{Title}:{State}:{Priority}:{RiskTier}:{CreatedUtc:O}";
            using var sha = SHA256.Create();
            ProvenanceHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }
    }

    public sealed class WorkPriorityPolicy
    {
        public string PolicyVersion { get; set; } = "WPP-DEFAULT-v1";
        public double PriorityWeight { get; set; } = 0.30;
        public double UrgencyWeight { get; set; } = 0.20;
        public double RiskWeight { get; set; } = 0.20;
        public double StrategicWeight { get; set; } = 0.30;

        public double CalculateScore(WorkPriority priority, WorkUrgencyTier urgency, WorkRiskTier risk, double strategicImportance)
        {
            double pNorm = (int)priority / 3.0;
            double uNorm = (int)urgency / 3.0;
            double rNorm = (int)risk / 4.0;
            double sNorm = Math.Clamp(strategicImportance, 0.0, 1.0);

            double score = (pNorm * PriorityWeight) +
                           (uNorm * UrgencyWeight) +
                           (rNorm * RiskWeight) +
                           (sNorm * StrategicWeight);

            return Math.Round(Math.Clamp(score, 0.0, 1.0), 4);
        }
    }

    public sealed class WorkMissionLineageRecord
    {
        public string LineageId { get; set; } = string.Empty;
        public string ResponsibilityId { get; set; } = string.Empty;
        public string WorkId { get; set; } = string.Empty;
        public string? WorkPlanId { get; set; }
        public string? MissionGraphId { get; set; }
        public string? MissionRunId { get; set; }
        public string? MissionNodeId { get; set; }
        public string? AgentInstanceId { get; set; }
        public string? CapabilityId { get; set; }
        public string? ExecutionIntentId { get; set; }
        public string? ExecutionAttemptId { get; set; }
        public string? ExternalEffectSummary { get; set; }
        public string? OutcomeId { get; set; }
        public string ProvenanceChainHash { get; set; } = string.Empty;
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

        public void ComputeChainHash()
        {
            var raw = $"{ResponsibilityId}->{WorkId}->{WorkPlanId}->{MissionGraphId}->{MissionRunId}->" +
                      $"{MissionNodeId}->{AgentInstanceId}->{CapabilityId}->{ExecutionIntentId}->" +
                      $"{ExecutionAttemptId}->{ExternalEffectSummary}->{OutcomeId}";
            using var sha = SHA256.Create();
            ProvenanceChainHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }
    }
}
