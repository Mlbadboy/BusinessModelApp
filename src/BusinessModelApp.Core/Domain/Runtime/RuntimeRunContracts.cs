using System;

namespace BusinessModelApp.Core.Domain.Runtime
{
    public record RunResourceBudget
    {
        public long MaxTokens { get; init; } = 100_000;
        public long MaxDurationMs { get; init; } = 300_000; // 5 minutes default
        public int MaxToolCalls { get; init; } = 50;
        public decimal MaxFinancialExposure { get; init; } = 0.00m;

        public static RunResourceBudget Default => new();
    }

    public class RuntimeRun
    {
        public RuntimeRunId RunId { get; init; }
        public Guid WorkspaceId { get; init; }
        public MissionGraphId MissionGraphId { get; init; }
        public int RunVersion { get; set; } = 1;
        public RunState State { get; set; } = RunState.Pending;
        public int CurrentAttemptNumber { get; set; } = 0;
        public LeaseId? ActiveLeaseId { get; set; }
        public FenceToken ActiveFenceToken { get; set; } = FenceToken.Initial;
        public RunResourceBudget ResourceBudget { get; init; } = RunResourceBudget.Default;
        public string PolicySnapshotId { get; init; } = string.Empty;
        public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        public RuntimeRun(RuntimeRunId runId, Guid workspaceId, MissionGraphId missionGraphId, string? policySnapshotId = null)
        {
            if (workspaceId == Guid.Empty)
                throw new ArgumentException("WorkspaceId cannot be empty.", nameof(workspaceId));

            RunId = runId;
            WorkspaceId = workspaceId;
            MissionGraphId = missionGraphId;
            PolicySnapshotId = policySnapshotId ?? $"POL-SNAP-{Guid.NewGuid():N}";
        }
    }

    public class AgentDefinition
    {
        public AgentDefinitionId DefinitionId { get; init; }
        public string Role { get; init; } = string.Empty;
        public string CapabilityProfile { get; init; } = string.Empty;
        public string PolicyProfile { get; init; } = string.Empty;
        public string ModelProfile { get; init; } = string.Empty;

        public AgentDefinition(AgentDefinitionId definitionId, string role)
        {
            DefinitionId = definitionId;
            Role = role;
        }
    }

    public class AgentInstance
    {
        public AgentInstanceId InstanceId { get; init; }
        public AgentDefinitionId DefinitionId { get; init; }
        public Guid WorkspaceId { get; init; }
        public string WorkerId { get; set; } = string.Empty;
        public AgentInstanceState RuntimeState { get; set; } = AgentInstanceState.Created;
        public AgentHealthStatus HealthStatus { get; set; } = AgentHealthStatus.Healthy;
        public LeaseId? CurrentLeaseId { get; set; }
        public DateTime LastHeartbeatUtc { get; set; } = DateTime.UtcNow;

        public AgentInstance(AgentInstanceId instanceId, AgentDefinitionId definitionId, Guid workspaceId)
        {
            if (workspaceId == Guid.Empty)
                throw new ArgumentException("WorkspaceId cannot be empty.", nameof(workspaceId));

            InstanceId = instanceId;
            DefinitionId = definitionId;
            WorkspaceId = workspaceId;
        }
    }
}
