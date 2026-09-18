using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Multimodal
{
    public enum ComputerSessionState
    {
        Created = 1,
        Initializing = 2,
        Observing = 3,
        Interpreting = 4,
        Planning = 5,
        GovernanceCheck = 6,
        ActionAdmitted = 7,
        Executing = 8,
        Verifying = 9,
        Succeeded = 10,
        Blocked = 11,
        Paused = 12,
        WaitingForHuman = 13,
        Failed = 14,
        UnknownEffect = 15,
        RecoveryRequired = 16,
        Cancelled = 17
    }

    public record StateTransitionRecord(
        ComputerSessionState FromState,
        ComputerSessionState ToState,
        string Reason,
        DateTime TimestampUtc,
        string TriggeredBy
    );

    public class ComputerSession
    {
        public string SessionId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string ApplicationContext { get; set; } = string.Empty;
        public string TargetGoal { get; set; } = string.Empty;
        public ComputerSessionState CurrentState { get; set; } = ComputerSessionState.Created;
        public string? ActiveSnapshotId { get; set; }
        public string? ActiveProposalId { get; set; }
        public List<StateTransitionRecord> History { get; set; } = new();
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
        public bool IsEmergencyKilled { get; set; }

        public void TransitionTo(ComputerSessionState newState, string reason, string triggeredBy = "System")
        {
            var oldState = CurrentState;
            CurrentState = newState;
            UpdatedAtUtc = DateTime.UtcNow;
            History.Add(new StateTransitionRecord(oldState, newState, reason, UpdatedAtUtc, triggeredBy));
        }

        public bool IsTerminal => CurrentState == ComputerSessionState.Succeeded ||
                                  CurrentState == ComputerSessionState.Failed ||
                                  CurrentState == ComputerSessionState.Cancelled;
    }
}
