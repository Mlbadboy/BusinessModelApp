using System;

namespace BusinessModelApp.Core.Domain.Runtime
{
    public enum ResponsibilityState
    {
        Draft = 1,
        Active = 2,
        Evaluating = 3,
        Triggered = 4,
        AtRisk = 5,
        Escalated = 6,
        Paused = 7,
        Retired = 8,
        Killed = 9
    }

    public enum MissionGraphState
    {
        Initialized = 1,
        Planning = 2,
        Active = 3,
        WaitingHuman = 4,
        Verifying = 5,
        Completed = 6,
        Failed = 7,
        Compensating = 8,
        Cancelled = 9,
        Killed = 10
    }

    public enum MissionNodeState
    {
        Pending = 1,
        Ready = 2,
        Running = 3,
        Waiting = 4,
        Blocked = 5,
        AwaitingHuman = 6,
        Verifying = 7,
        Succeeded = 8,
        Failed = 9,
        Skipped = 10,
        Cancelled = 11,
        Killed = 12
    }

    public enum RunState
    {
        Pending = 1,
        Admitted = 2,
        Running = 3,
        Suspended = 4,
        AwaitingHuman = 5,
        Reconciling = 6,
        Completed = 7,
        Failed = 8,
        Cancelled = 9,
        Killed = 10
    }

    public enum AttemptState
    {
        Claimed = 1,
        Executing = 2,
        Verifying = 3,
        Succeeded = 4,
        Failed = 5,
        TimedOut = 6,
        UnknownEffect = 7,
        AbortedByFence = 8
    }

    public enum AgentInstanceState
    {
        Created = 1,
        Ready = 2,
        Running = 3,
        Waiting = 4,
        Blocked = 5,
        AwaitingHuman = 6,
        Recovering = 7,
        Completed = 8,
        Suspended = 9,
        Terminated = 10,
        Failed = 11,
        Retired = 12
    }

    public enum CapabilityState
    {
        Registered = 1,
        Active = 2,
        Sandboxed = 3,
        Deprecated = 4,
        Revoked = 5,
        Killed = 6
    }

    public enum AgentHealthStatus
    {
        Healthy = 1,
        Degraded = 2,
        Saturated = 3,
        Unresponsive = 4,
        Quarantined = 5,
        Killed = 6
    }

    public enum BusinessControlMode
    {
        OBSERVE = 1,
        ADVISE = 2,
        SIMULATE = 3,
        PREPARE = 4,
        EXECUTE_WITH_APPROVAL = 5,
        EXECUTE_BOUNDED = 6,
        AUTONOMOUS = 7
    }

    public enum ExecutionOutcomeStatus
    {
        NotStarted = 1,
        Started = 2,
        Succeeded = 3,
        Failed = 4,
        Cancelled = 5,
        TimedOut = 6,
        Unknown = 7,
        CompensationRequired = 8
    }
}
