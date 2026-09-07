using System;
using System.Collections.Generic;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Domain.Runtime.Capabilities;

namespace BusinessModelApp.Core.Domain.Runtime.Workers
{
    /// <summary>
    /// Supported Universal Worker execution modalities (closed governed enum).
    /// Arbitrary modalities cannot be injected at runtime without certified registration.
    /// </summary>
    public enum WorkerModality
    {
        Api,
        Mcp,
        Browser,
        Desktop
    }

    /// <summary>
    /// Strict lifecycle states for workers in the universal fabric.
    /// </summary>
    public enum WorkerState
    {
        Requested,
        Resolving,
        Admitted,
        Provisioning,
        Ready,
        Leased,
        Running,
        Succeeded,
        Verifying,
        Completed,
        ProvisioningFailed,
        CapabilityDenied,
        ResourceExhausted,
        LeaseExpired,
        FenceRejected,
        SandboxViolation,
        PolicyBlocked,
        Killed,
        TimedOut,
        UnknownEffect
    }

    public readonly record struct WorkerDefinitionId(Guid Value)
    {
        public static WorkerDefinitionId New() => new(Guid.NewGuid());
        public static WorkerDefinitionId From(Guid value) => new(value);
        public static WorkerDefinitionId From(string value) => new(Guid.Parse(value));
        public override string ToString() => Value.ToString("D");
    }

    public readonly record struct WorkerInstanceId(Guid Value)
    {
        public static WorkerInstanceId New() => new(Guid.NewGuid());
        public static WorkerInstanceId From(Guid value) => new(value);
        public static WorkerInstanceId From(string value) => new(Guid.Parse(value));
        public override string ToString() => Value.ToString("D");
    }

    public readonly record struct WorkerProcessId(Guid Value)
    {
        public static WorkerProcessId New() => new(Guid.NewGuid());
        public static WorkerProcessId From(Guid value) => new(value);
        public static WorkerProcessId From(string value) => new(Guid.Parse(value));
        public override string ToString() => Value.ToString("D");
    }

    public readonly record struct WorkerAttemptId(Guid Value)
    {
        public static WorkerAttemptId New() => new(Guid.NewGuid());
        public static WorkerAttemptId From(Guid value) => new(value);
        public static WorkerAttemptId From(string value) => new(Guid.Parse(value));
        public override string ToString() => Value.ToString("D");
    }

    /// <summary>
    /// Worker Definition: The blueprint specifying modality, execution runtime environment,
    /// and required sandbox baseline profile.
    /// </summary>
    public class WorkerDefinition
    {
        public WorkerDefinitionId WorkerDefinitionId { get; init; } = WorkerDefinitionId.New();
        public Guid WorkspaceId { get; init; }
        public string Title { get; init; } = string.Empty;
        public WorkerModality Modality { get; init; }
        public string RuntimeIdentifier { get; init; } = "standard-v1";
        public string Version { get; init; } = "1.0.0";
        public HashSet<string> SupportedCapabilityIds { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        public WorkerSandboxProfile DefaultSandboxProfile { get; init; } = new();
        public bool IsActive { get; set; } = true;
        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Worker Instance: A concrete leased worker allocation bound to a mission attempt,
    /// an agent instance, and governed capability lease.
    /// </summary>
    public class WorkerInstance
    {
        public WorkerInstanceId WorkerInstanceId { get; init; } = WorkerInstanceId.New();
        public WorkerDefinitionId WorkerDefinitionId { get; init; }
        public Guid WorkspaceId { get; init; }
        public AgentInstanceId AgentInstanceId { get; init; }
        public CapabilityId LeasedCapabilityId { get; init; }
        public WorkerModality Modality { get; init; }
        public WorkerState State { get; set; } = WorkerState.Requested;
        public WorkerProcessId? ActiveProcessId { get; set; }
        public WorkerAttemptId? CurrentAttemptId { get; set; }
        public long FenceToken { get; set; }
        public DateTimeOffset? LeasedAt { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
        public string? TerminationReason { get; set; }
    }

    /// <summary>
    /// Worker Process: Represents the isolated OS, container, or browser runtime process.
    /// </summary>
    public class WorkerProcess
    {
        public WorkerProcessId ProcessId { get; init; } = WorkerProcessId.New();
        public WorkerInstanceId WorkerInstanceId { get; init; }
        public Guid WorkspaceId { get; init; }
        public string SandboxId { get; init; } = Guid.NewGuid().ToString("N");
        public int OsProcessId { get; init; } = Random.Shared.Next(1000, 99999);
        public WorkerModality Modality { get; init; }
        public WorkerResourceUsage CurrentUsage { get; set; } = new();
        public bool IsRunning { get; set; } = true;
        public DateTimeOffset SpawnedAt { get; init; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? TerminatedAt { get; set; }
    }

    /// <summary>
    /// Worker Attempt: Tracks an individual execution run of a capability under a leased worker.
    /// </summary>
    public class WorkerAttempt
    {
        public WorkerAttemptId AttemptId { get; init; } = WorkerAttemptId.New();
        public WorkerInstanceId WorkerInstanceId { get; init; }
        public CapabilityId CapabilityId { get; init; }
        public int AttemptNumber { get; init; } = 1;
        public NodeExecutionEffect ResultingEffect { get; set; } = NodeExecutionEffect.NoEffect;
        public bool IsSuccess { get; set; }
        public string? FailureReason { get; set; }
        public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? CompletedAt { get; set; }
    }
}
