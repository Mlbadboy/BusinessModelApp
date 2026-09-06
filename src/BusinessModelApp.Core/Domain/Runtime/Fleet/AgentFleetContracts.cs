using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Responsibilities;
using BusinessModelApp.Core.Domain.Runtime;

namespace BusinessModelApp.Core.Domain.Runtime.Fleet
{
    public readonly record struct WorkerProcessId
    {
        public Guid Value { get; }

        [JsonConstructor]
        public WorkerProcessId(Guid value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("WorkerProcessId cannot be Guid.Empty.", nameof(value));
            Value = value;
        }

        public static WorkerProcessId New() => new(Guid.NewGuid());
        public static WorkerProcessId From(Guid value) => new(value);
        public static WorkerProcessId From(string value) => new(Guid.Parse(value));
        public override string ToString() => Value.ToString();
    }

    public enum WorkerPoolType
    {
        CognitiveAnalyst = 1,
        DomainResearcher = 2,
        StrategySimulator = 3,
        GovernedExecutor = 4,
        VerificationAuditor = 5
    }

    public enum WorkerHealthStatus
    {
        Healthy = 1,
        Degraded = 2,
        Quarantined = 3,
        Suspended = 4,
        Terminated = 5
    }

    public record AgentDefinitionRecord
    {
        public AgentDefinitionId DefinitionId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string RoleDescription { get; init; } = string.Empty;
        public WorkerPoolType PoolType { get; init; } = WorkerPoolType.CognitiveAnalyst;
        public AutonomyTier MaxAutonomyTier { get; init; } = AutonomyTier.L2_Simulate;
        public IReadOnlySet<string> AllowedCapabilities { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public string DefaultModelPolicy { get; init; } = "default";
        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    }

    public record AgentInstanceRecord
    {
        public AgentInstanceId InstanceId { get; init; }
        public AgentDefinitionId DefinitionId { get; init; }
        public Guid WorkspaceId { get; init; }
        public WorkerPoolType PoolType { get; init; }
        public MissionId? CurrentMissionId { get; set; }
        public MissionGraphId? CurrentGraphId { get; set; }
        public MissionNodeId? CurrentNodeId { get; set; }
        public AgentInstanceState State { get; set; } = AgentInstanceState.Ready;
        public long TotalTokensConsumed { get; set; }
        public decimal TotalCostUsdConsumed { get; set; }
        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
        public DateTimeOffset LastHeartbeatAt { get; set; } = DateTimeOffset.UtcNow;
    }

    public record WorkerProcessRecord
    {
        public WorkerProcessId WorkerId { get; init; }
        public WorkerPoolType PoolType { get; init; }
        public AgentInstanceId? BoundAgentInstanceId { get; set; }
        public WorkerHealthStatus HealthStatus { get; set; } = WorkerHealthStatus.Healthy;
        public int ConsecutiveFailures { get; set; }
        public int LeaseLossCount { get; set; }
        public int CrashCount { get; set; }
        public int VerificationFailureCount { get; set; }
        public int UnknownEffectCount { get; set; }
        public DateTimeOffset LastHeartbeatAt { get; set; } = DateTimeOffset.UtcNow;
        public string? QuarantineReason { get; set; }
        public DateTimeOffset? QuarantinedAt { get; set; }
    }

    public record FencingEnvelope
    {
        public Guid WorkspaceId { get; init; }
        public MissionGraphId MissionGraphId { get; init; }
        public MissionGraphVersion GraphVersion { get; init; }
        public MissionNodeId MissionNodeId { get; init; }
        public ExecutionAttemptId AttemptId { get; init; }
        public LeaseId LeaseId { get; init; }
        public FenceToken FenceToken { get; init; }
        public WorkerProcessId WorkerId { get; init; }
        public AgentInstanceId AgentInstanceId { get; init; }
    }

    public record AgentOutcomeProposal
    {
        public Guid ProposalId { get; init; } = Guid.NewGuid();
        public FencingEnvelope Envelope { get; init; } = null!;
        public ExecutionOutcomeStatus ReportedStatus { get; init; } = ExecutionOutcomeStatus.Succeeded;
        public string OutputPayloadJson { get; init; } = "{}";
        public IReadOnlyList<MissionArtifact> ProducedArtifacts { get; init; } = Array.Empty<MissionArtifact>();
        public string? ObservedEvidenceHash { get; init; }
        public DynamicExpansionProposal? ProposedExpansion { get; init; }
        public long TokensConsumed { get; init; }
        public decimal CostUsdConsumed { get; init; }
        public string? FailureReason { get; init; }
        public DateTimeOffset ProposedAt { get; init; } = DateTimeOffset.UtcNow;
    }

    public record ChildMissionSpawnRequest
    {
        public Guid SpawnRequestId { get; init; } = Guid.NewGuid();
        public MissionId ParentMissionId { get; init; }
        public MissionGraphId ParentGraphId { get; init; }
        public MissionNodeId ParentNodeId { get; init; }
        public ExecutionAttemptId ParentAttemptId { get; init; }
        public AgentInstanceId RequestingAgentId { get; init; }
        public MissionGraphProposal ChildProposal { get; init; } = null!;
        public long ReservedBudgetTokens { get; init; }
        public decimal ReservedBudgetCostUsd { get; init; }
        public DateTimeOffset RequestedAt { get; init; } = DateTimeOffset.UtcNow;
    }

    public record WorkerHeartbeat
    {
        public WorkerProcessId WorkerId { get; init; }
        public AgentInstanceId? AgentInstanceId { get; init; }
        public LeaseId? LeaseId { get; init; }
        public FenceToken? FenceToken { get; init; }
        public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
        public double CpuPercent { get; init; }
        public long MemoryBytes { get; init; }
    }
}
