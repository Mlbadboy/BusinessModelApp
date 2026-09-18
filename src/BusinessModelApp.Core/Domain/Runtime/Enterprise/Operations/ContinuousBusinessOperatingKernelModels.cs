using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Operations
{
    public enum BusinessCycleStage
    {
        Observe = 0,
        Evidence = 1,
        Interpret = 2,
        Prioritize = 3,
        Allocate = 4,
        Plan = 5,
        Govern = 6,
        Execute = 7,
        Verify = 8,
        Measure = 9,
        Learn = 10,
        Checkpoint = 11,
        Concluded = 12
    }

    public enum BusinessCycleStatus
    {
        Planned = 0,
        Running = 1,
        Paused = 2,
        Completed = 3,
        TerminatedTimeout = 4,
        AbortedGuardrailBreach = 5,
        RecoveredAfterCrash = 6
    }

    public sealed class BusinessCycleCheckpoint
    {
        public string CheckpointId { get; init; } = Guid.NewGuid().ToString("N");
        public string CycleId { get; init; } = string.Empty;
        public BusinessCycleStage Stage { get; init; }
        public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
        public string StateDigestSha256 { get; init; } = string.Empty;
        public int ExecutedMissionsCount { get; init; }
        public decimal SpentBudgetINR { get; init; }
        public string Notes { get; init; } = string.Empty;
    }

    public sealed class BusinessCycleTermination
    {
        public string TerminationId { get; init; } = Guid.NewGuid().ToString("N");
        public string CycleId { get; init; } = string.Empty;
        public BusinessCycleStatus FinalStatus { get; init; }
        public string Reason { get; init; } = string.Empty;
        public DateTime TerminatedAtUtc { get; init; } = DateTime.UtcNow;
        public bool RequiresHumanIntervention { get; init; }
    }

    public sealed class BusinessCycleOutcome
    {
        public string OutcomeId { get; init; } = Guid.NewGuid().ToString("N");
        public string CycleId { get; init; } = string.Empty;
        public int OpportunitiesIdentified { get; init; }
        public int OutboundTouchesSent { get; init; }
        public int DealsNegotiated { get; init; }
        public int CustomersOnboarded { get; init; }
        public decimal RealizedCashINR { get; init; }
        public decimal FullyLoadedCostsINR { get; init; }
        public decimal NetContributionINR => RealizedCashINR - FullyLoadedCostsINR;
        public decimal HealthScoreDelta { get; init; }
        public bool IsEconomicallyPositive => NetContributionINR > 0;
        public DateTime RecordedAtUtc { get; init; } = DateTime.UtcNow;
    }

    public sealed class BusinessCycle
    {
        public string CycleId { get; init; } = Guid.NewGuid().ToString("N");
        public required string TenantId { get; init; }
        public required string BusinessObjectiveId { get; init; }
        public required string GrowthObjectiveId { get; init; }
        public int CycleNumber { get; init; } = 1;
        public BusinessCycleStage CurrentStage { get; private set; } = BusinessCycleStage.Observe;
        public BusinessCycleStatus Status { get; private set; } = BusinessCycleStatus.Planned;

        // Resource & Time Limits (Law I42-A)
        public TimeSpan MaxDuration { get; init; } = TimeSpan.FromHours(4);
        public decimal MaxBudgetINR { get; init; } = 500_000m;
        public decimal CurrentSpentBudgetINR { get; private set; }
        public int MaxMissionsCount { get; init; } = 10;
        public List<string> AssignedMissionIds { get; } = new();

        public DateTime StartedAtUtc { get; private set; } = DateTime.UtcNow;
        public DateTime? CompletedAtUtc { get; private set; }
        public List<BusinessCycleCheckpoint> Checkpoints { get; } = new();
        public BusinessCycleOutcome? Outcome { get; private set; }
        public BusinessCycleTermination? Termination { get; private set; }

        public void Start()
        {
            if (Status != BusinessCycleStatus.Planned && Status != BusinessCycleStatus.Paused)
                throw new InvalidOperationException($"Cannot start cycle in status {Status}.");

            Status = BusinessCycleStatus.Running;
            StartedAtUtc = DateTime.UtcNow;
            AdvanceStage(BusinessCycleStage.Observe);
        }

        public void AdvanceStage(BusinessCycleStage nextStage)
        {
            if (Status != BusinessCycleStatus.Running)
                throw new InvalidOperationException($"Cannot advance stage when cycle is {Status}.");

            CurrentStage = nextStage;
            var checkpoint = new BusinessCycleCheckpoint
            {
                CycleId = CycleId,
                Stage = nextStage,
                ExecutedMissionsCount = AssignedMissionIds.Count,
                SpentBudgetINR = CurrentSpentBudgetINR,
                Notes = $"Advanced to {nextStage} stage"
            };
            Checkpoints.Add(checkpoint);

            if (nextStage == BusinessCycleStage.Concluded)
            {
                Status = BusinessCycleStatus.Completed;
                CompletedAtUtc = DateTime.UtcNow;
            }
        }

        public void RecordSpend(decimal amount)
        {
            if (amount < 0) throw new ArgumentException("Spend amount cannot be negative.", nameof(amount));
            CurrentSpentBudgetINR += amount;

            if (CurrentSpentBudgetINR > MaxBudgetINR)
            {
                Terminate(BusinessCycleStatus.AbortedGuardrailBreach, $"Cycle budget of {MaxBudgetINR:N0} INR exceeded (Current: {CurrentSpentBudgetINR:N0} INR).", true);
            }
        }

        public void AssignMission(string missionId)
        {
            if (string.IsNullOrWhiteSpace(missionId)) throw new ArgumentException("Mission ID required.", nameof(missionId));
            if (AssignedMissionIds.Count >= MaxMissionsCount)
            {
                throw new InvalidOperationException($"Max concurrent missions limit ({MaxMissionsCount}) reached for cycle.");
            }
            AssignedMissionIds.Add(missionId.Trim());
        }

        public void SetOutcome(BusinessCycleOutcome outcome)
        {
            Outcome = outcome ?? throw new ArgumentNullException(nameof(outcome));
        }

        public void Terminate(BusinessCycleStatus status, string reason, bool requiresHuman)
        {
            Status = status;
            CompletedAtUtc = DateTime.UtcNow;
            Termination = new BusinessCycleTermination
            {
                CycleId = CycleId,
                FinalStatus = status,
                Reason = reason,
                RequiresHumanIntervention = requiresHuman
            };
        }

        public void RecoverFromCrash(string recoveryNotes)
        {
            Status = BusinessCycleStatus.RecoveredAfterCrash;
            Checkpoints.Add(new BusinessCycleCheckpoint
            {
                CycleId = CycleId,
                Stage = CurrentStage,
                ExecutedMissionsCount = AssignedMissionIds.Count,
                SpentBudgetINR = CurrentSpentBudgetINR,
                Notes = $"Recovered from crash. {recoveryNotes}"
            });
            Status = BusinessCycleStatus.Running;
        }
    }
}
