using System;
using System.Collections.Generic;
using BusinessModelApp.Core.Domain.Common;

namespace BusinessModelApp.Core.Domain.Missions
{
    public enum DurableMissionState
    {
        // Phase 1 v1.2 Planning & Validation Lifecycle (12-State Sequence)
        Draft = 0,
        WorldModelBuilt = 1,
        RevenueBaselineCalculated = 2,
        StrategiesGenerated = 3,
        SimulationComplete = 4,
        FeasibilityChecked = 5,
        ConstitutionValidated = 6,
        DecisionRecorded = 7,
        ExecutiveApprovalRequired = 8,
        ExecutiveApproved = 9,
        MissionPrepared = 10,
        ReadyForExecution = 11,

        // Operational Execution States (Phase 2 boundary)
        Running = 12,
        Paused = 13,
        Blocked = 14,
        Failed = 15,
        Completed = 16,
        Cancelled = 17,

        // Backwards compatibility aliases
        Created = 0,
        Planned = 10,
        WaitingForApproval = 8,
        Ready = 11
    }

    public class DurableMission : Entity, ISoftDeletable
    {
        public Guid WorkspaceId { get; set; }
        public Guid ObjectiveId { get; set; }
        public Guid? StrategyId { get; set; }
        public Guid? DecisionRecordId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DurableMissionState State { get; set; } = DurableMissionState.Draft;

        // Checkpoint Tracking
        public int CurrentCheckpointIndex { get; set; } = 0;
        public int TotalPlannedSteps { get; set; } = 1;
        public string LastCompletedStepName { get; set; } = string.Empty;
        public DateTime? LastCheckpointSavedAt { get; set; }

        // Failure & Governance Context
        public string? BlockedReason { get; set; }
        public Guid? PendingApprovalRequestId { get; set; }
        public decimal AllocatedBudgetINR { get; set; } = 10000m;
        public decimal SpentBudgetINR { get; set; } = 0m;

        // Navigation collection
        public List<DurableMissionCheckpoint> Checkpoints { get; set; } = new();

        public bool IsDeleted { get; private set; }

        public void MarkAsDeleted()
        {
            IsDeleted = true;
            UpdateTimestamps();
        }

        public void TransitionTo(DurableMissionState newState, string? reason = null)
        {
            State = newState;
            if (!string.IsNullOrEmpty(reason))
            {
                BlockedReason = reason;
            }
            UpdateTimestamps();
        }
    }

    public class DurableMissionCheckpoint : Entity
    {
        public Guid MissionId { get; set; }
        public int StepIndex { get; set; }
        public string StepName { get; set; } = string.Empty;
        public string AgentRole { get; set; } = "Orchestrator";

        // State Payloads for Resumption
        public string InputContextJson { get; set; } = "{}";
        public string ExecutionOutputJson { get; set; } = "{}";
        public string EnvironmentStateSnapshotJson { get; set; } = "{}";

        public bool IsCompleted { get; set; } = false;
        public bool IsBlockedOnApproval { get; set; } = false;
        public Guid? ApprovalRequestId { get; set; }

        public string? FailureError { get; set; }
        public DateTime CheckpointTimestamp { get; set; } = DateTime.UtcNow;
    }
}
