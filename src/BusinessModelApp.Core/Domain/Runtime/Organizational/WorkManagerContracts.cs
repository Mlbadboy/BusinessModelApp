using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace BusinessModelApp.Core.Domain.Runtime.Organizational
{
    // =========================================================================
    // INVARIANT I26 — AUTONOMOUS WORK MANAGEMENT SOVEREIGNTY
    // WORK MANAGEMENT ≠ EXECUTION AUTHORITY ≠ MISSION RUNTIME ≠ TRUTH ≠ GOVERNANCE ≠ LEARNING
    //
    // Subordinate Constitutional Laws:
    // I26-A: Manager ≠ Execution Engine (Manager coordinates work; Mission Runtime executes missions)
    // I26-B: Manager ≠ ExecutionPermit Authority (Manager cannot create or grant ExecutionPermit)
    // I26-C: Manager ≠ Truth Authority (Manager cannot manufacture or alter certified business facts)
    // I26-D: Manager ≠ Governance Authority (Manager stages items; PRG-1 / Humans decide)
    // I26-E: Manager ≠ Worker Dispatcher (Manager cannot dispatch or invoke external connectors)
    // I26-F: Manager ≠ Policy Author (Manager cannot self-author or elevate policy rules)
    // I26-G: Manager ≠ Budget Authority (Manager cannot increase or bypass execution budgets)
    // I26-H: Manager ≠ Outcome Verifier (Outcome verification requires independent evidence)
    // I26-I: Manager ≠ Self-Escalating Authority (Priority cannot manufacture emergency authority)
    // I26-J: Manager Cycles Must Be Bounded (Fail-closed computational resource ceilings)
    // I26-K: Manager Must Be Idempotent (Same inputs yield bit-for-bit identical cycle results)
    // I26-L: Manager Must Be Replayable (Deterministic verification from snapshot hashes)
    // I26-M: Manager Must Be Tenant-Isolated (Strict partition by TenantId)
    // I26-N: Manager Must Preserve UNKNOWN (Missing data surfaces as UNKNOWN, never smoothed)
    // I26-O: Manager Cannot Mutate Certified Truth (Zero mutation of reality or financial ledgers)
    // I26-P: Manager Cannot Create Execution Authority (ManagerRun ≠ MissionRun)
    // I26-Q: Execution Boundary Isolation Law:
    //        No OrganizationalWorkManager, No WorkDecompositionEngine, No PortfolioPrioritizer,
    //        No GovernanceQueueManager, No ManagerRun, No WorkPlan, No MissionGraphProposal
    //        may create, sign, issue, delegate, or modify a Batch 6 ExecutionPermit.
    // =========================================================================

    public enum ManagerTriggerType
    {
        ScheduledTick,
        RealityEvent,
        IntelligenceSignal,
        ManualTrigger
    }

    public enum ManagerRunStatus
    {
        Initialized,
        Running,
        Completed,
        BudgetExceeded,
        Failed
    }

    public enum GovernanceQueueStatus
    {
        Pending,
        Approved,
        Rejected,
        Cancelled
    }

    public sealed class WorkManagerBudget
    {
        public int MaxCandidateResponsibilities { get; init; } = 25;
        public int MaxWorkItemsPerCycle { get; init; } = 50;
        public int MaxProposalsPerCycle { get; init; } = 25;
        public int MaxDecompositionsPerCycle { get; init; } = 10;
        public int MaxMissionProposalsPerCycle { get; init; } = 10;
        public int MaxGovernanceQueueMutations { get; init; } = 20;
        public int MaxManagerRuntimeMs { get; init; } = 5000;
    }

    public sealed class ExecutionAuthorizationCheckpoint
    {
        public string CheckpointId { get; init; } = string.Empty;
        public string WorkId { get; init; } = string.Empty;
        public string NodeId { get; init; } = string.Empty;
        public WorkRiskTier RiskTier { get; init; } = WorkRiskTier.R1_InternalReversible;
        public bool IsExecutionPermitRequired => RiskTier >= WorkRiskTier.R2_ExternalBounded;
        public string Notice => "Checkpoint ≠ Permit. Indicates downstream Batch 6 authorization may be required.";
    }

    public sealed class WorkManagerRun
    {
        public string ManagerRunId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public ManagerTriggerType TriggerType { get; set; } = ManagerTriggerType.ScheduledTick;
        public string TriggerId { get; set; } = string.Empty;
        public long CycleNumber { get; set; } = 1;
        public DateTime StartedUtc { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedUtc { get; set; }
        public string InputSnapshotHash { get; set; } = string.Empty;
        public string PolicySnapshotHash { get; set; } = string.Empty;
        public string WorkPortfolioSnapshotHash { get; set; } = string.Empty;
        public ManagerRunStatus Status { get; set; } = ManagerRunStatus.Initialized;
        public string ResultHash { get; set; } = string.Empty;
        public string? ParentRunId { get; set; }

        public string ComputeIdempotencyKey()
        {
            var raw = $"{TenantId}:{TriggerId}:{CycleNumber}:{InputSnapshotHash}:{PolicySnapshotHash}";
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }

        public void ComputeResultHash(string summary)
        {
            var raw = $"{ManagerRunId}:{TenantId}:{CycleNumber}:{Status}:{summary}:{CompletedUtc:O}";
            using var sha = SHA256.Create();
            ResultHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }
    }

    public sealed class GovernanceQueueItem
    {
        public string QueueItemId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string WorkId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public WorkRiskTier RiskTier { get; set; } = WorkRiskTier.R1_InternalReversible;
        public string RequiredApproverRole { get; set; } = "CEO";
        public double PriorityScore { get; set; }
        public WorkUrgencyTier UrgencyTier { get; set; } = WorkUrgencyTier.Standard;
        public string DecisionCandidateSummary { get; set; } = string.Empty;
        public List<string> AlternativesConsidered { get; set; } = new();
        public string StatusQuoRisk { get; set; } = string.Empty;
        public string? Prg1ApprovalRequestId { get; set; } // Reference to PRG-1 HumanApprovalRequest
        public GovernanceQueueStatus Status { get; set; } = GovernanceQueueStatus.Pending;
        public DateTime StagedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? DecidedAtUtc { get; set; }
        public string? DecidedByActor { get; set; }
        public string? DecisionNote { get; set; }
    }

    public sealed class WorkPortfolioRank
    {
        public string WorkId { get; set; } = string.Empty;
        public string ResponsibilityId { get; set; } = string.Empty;
        public double ComputedRankScore { get; set; }
        public WorkPriority Priority { get; set; }
        public WorkUrgencyTier Urgency { get; set; }
        public WorkRiskTier Risk { get; set; }
        public double DeadlinePressure { get; set; }
        public double StrategicImportance { get; set; }
        public double DependencyCriticality { get; set; }
        public double BottleneckSeverity { get; set; }
        public double AgeMinutes { get; set; }
        public double StarvationBoost { get; set; }
        public string NextAction { get; set; } = string.Empty;
    }

    public sealed class PortfolioSchedulingPolicy
    {
        public string PolicyVersion { get; set; } = "PSP-DEFAULT-v1";
        public double PriorityWeight { get; init; } = 0.25;
        public double UrgencyWeight { get; init; } = 0.15;
        public double BottleneckWeight { get; init; } = 0.25;
        public double StrategicWeight { get; init; } = 0.15;
        public double DeadlinePressureWeight { get; init; } = 0.10;
        public double StarvationWeight { get; init; } = 0.10;
        public double StarvationThresholdMinutes { get; init; } = 120.0; // 2 hours

        public double CalculateRankScore(
            WorkPriority priority,
            WorkUrgencyTier urgency,
            double bottleneckSeverity,
            double strategicImportance,
            double deadlinePressure,
            double ageMinutes)
        {
            double pNorm = (int)priority / 3.0;
            double uNorm = (int)urgency / 3.0;
            double bNorm = Math.Clamp(bottleneckSeverity, 0.0, 1.0);
            double sNorm = Math.Clamp(strategicImportance, 0.0, 1.0);
            double dNorm = Math.Clamp(deadlinePressure, 0.0, 1.0);

            double starvationBoost = 0.0;
            if (ageMinutes > StarvationThresholdMinutes)
            {
                starvationBoost = Math.Clamp((ageMinutes - StarvationThresholdMinutes) / 240.0, 0.0, 1.0);
            }

            double score = (pNorm * PriorityWeight) +
                           (uNorm * UrgencyWeight) +
                           (bNorm * BottleneckWeight) +
                           (sNorm * StrategicWeight) +
                           (dNorm * DeadlinePressureWeight) +
                           (starvationBoost * StarvationWeight);

            return Math.Round(Math.Clamp(score, 0.0, 1.0), 4);
        }
    }

    public sealed class WorkManagerCycleResult
    {
        public string RunId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public int ResponsibilitiesEvaluated { get; set; }
        public int ProposalsEvaluated { get; set; }
        public int WorkItemsPlanned { get; set; }
        public int DependenciesResolved { get; set; }
        public int CommitmentsAudited { get; set; }
        public int EscalationsTriggered { get; set; }
        public int GovernanceItemsStaged { get; set; }
        public int MissionProposalsEmitted { get; set; }
        public long DurationMs { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string ResultSummaryHash { get; set; } = string.Empty;
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    }
}
