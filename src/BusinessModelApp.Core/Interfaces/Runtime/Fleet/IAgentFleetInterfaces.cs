using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Domain.Runtime.Fleet;
using BusinessModelApp.Core.Interfaces.Missions;

namespace BusinessModelApp.Core.Interfaces.Runtime.Fleet
{
    public record LeaseGrantResult
    {
        public bool IsGranted { get; init; }
        public RuntimeLease? Lease { get; init; }
        public FenceToken FenceToken { get; init; }
        public ExecutionAttemptId AttemptId { get; init; }
        public string? FailureReason { get; init; }

        public static LeaseGrantResult Granted(RuntimeLease lease, FenceToken token, ExecutionAttemptId attemptId) =>
            new() { IsGranted = true, Lease = lease, FenceToken = token, AttemptId = attemptId };

        public static LeaseGrantResult Denied(string reason) =>
            new() { IsGranted = false, FailureReason = reason };
    }

    public record FencingValidationResult
    {
        public bool IsValid { get; init; }
        public string? FailureReason { get; init; }
        public bool IsStaleGraphVersion { get; init; }
        public bool IsStaleToken { get; init; }
        public bool IsExpiredLease { get; init; }

        public static FencingValidationResult Valid() => new() { IsValid = true };

        public static FencingValidationResult StaleGraph(string reason) =>
            new() { IsValid = false, IsStaleGraphVersion = true, FailureReason = reason };

        public static FencingValidationResult StaleToken(string reason) =>
            new() { IsValid = false, IsStaleToken = true, FailureReason = reason };

        public static FencingValidationResult Expired(string reason) =>
            new() { IsValid = false, IsExpiredLease = true, FailureReason = reason };

        public static FencingValidationResult Invalid(string reason) =>
            new() { IsValid = false, FailureReason = reason };
    }

    public record OutcomeAdmissionResult
    {
        public bool IsAdmitted { get; init; }
        public string? FailureReason { get; init; }
        public MissionNodeState ResultingNodeState { get; init; }
        public NodeExecutionEffect ResultingEffect { get; init; }
        public string? VerifiedEvidenceHash { get; init; }
        public Guid? AuditEntryId { get; init; }

        public static OutcomeAdmissionResult Admitted(MissionNodeState state, NodeExecutionEffect effect, string? hash = null) =>
            new() { IsAdmitted = true, ResultingNodeState = state, ResultingEffect = effect, VerifiedEvidenceHash = hash };

        public static OutcomeAdmissionResult Rejected(string reason, MissionNodeState state = MissionNodeState.Failed, NodeExecutionEffect effect = NodeExecutionEffect.EffectFailed) =>
            new() { IsAdmitted = false, FailureReason = reason, ResultingNodeState = state, ResultingEffect = effect };
    }

    public record ChildMissionSpawnResult
    {
        public bool IsSpawned { get; init; }
        public MissionId? ChildMissionId { get; init; }
        public MissionGraphId? ChildGraphId { get; init; }
        public string? FailureReason { get; init; }

        public static ChildMissionSpawnResult Success(MissionId missionId, MissionGraphId graphId) =>
            new() { IsSpawned = true, ChildMissionId = missionId, ChildGraphId = graphId };

        public static ChildMissionSpawnResult Failed(string reason) =>
            new() { IsSpawned = false, FailureReason = reason };
    }

    public interface IWorkerLeaseCoordinator
    {
        Task<LeaseGrantResult> AcquireNodeLeaseAsync(
            Guid workspaceId,
            MissionGraphId graphId,
            MissionGraphVersion graphVersion,
            MissionNodeId nodeId,
            WorkerProcessId workerId,
            AgentInstanceId agentId,
            TimeSpan duration,
            CancellationToken ct = default);

        Task<FencingValidationResult> ValidateEnvelopeAsync(
            FencingEnvelope envelope,
            MissionGraph currentGraph,
            CancellationToken ct = default);

        Task<bool> ReleaseLeaseAsync(LeaseId leaseId, FenceToken token, CancellationToken ct = default);
    }

    public interface IAgentOutcomeAdmissionGate
    {
        Task<OutcomeAdmissionResult> AdmitOutcomeProposalAsync(
            AgentOutcomeProposal proposal,
            MissionGraph graph,
            TenantMissionPolicyContext tenantPolicy,
            CancellationToken ct = default);
    }

    public interface IFleetOrchestrator
    {
        Task<WorkerProcessRecord?> DispatchNodeAsync(
            MissionGraph graph,
            MissionNodeRecord node,
            CancellationToken ct = default);
    }

    public interface IAgentDispatcher
    {
        Task<AgentOutcomeProposal> ExecuteAttemptAsync(
            FencingEnvelope envelope,
            MissionNodeRecord node,
            AgentInstanceRecord agent,
            CancellationToken ct = default);
    }

    public interface IChildMissionGate
    {
        Task<ChildMissionSpawnResult> SpawnChildMissionAsync(
            ChildMissionSpawnRequest request,
            MissionGraph parentGraph,
            TenantMissionPolicyContext tenantPolicy,
            CancellationToken ct = default);
    }

    public interface IFleetHealthMonitor
    {
        Task RecordHeartbeatAsync(WorkerHeartbeat heartbeat, CancellationToken ct = default);
        Task<IReadOnlyList<WorkerProcessRecord>> CheckHealthAsync(TimeSpan timeoutThreshold, CancellationToken ct = default);
        Task QuarantineWorkerAsync(WorkerProcessId workerId, string reason, CancellationToken ct = default);
    }

    public interface IAgentFleetStore
    {
        Task RegisterDefinitionAsync(AgentDefinitionRecord definition, CancellationToken ct = default);
        Task<AgentDefinitionRecord?> GetDefinitionAsync(AgentDefinitionId definitionId, CancellationToken ct = default);
        Task RegisterInstanceAsync(AgentInstanceRecord instance, CancellationToken ct = default);
        Task<AgentInstanceRecord?> GetInstanceAsync(AgentInstanceId instanceId, CancellationToken ct = default);
        Task RegisterWorkerAsync(WorkerProcessRecord worker, CancellationToken ct = default);
        Task<WorkerProcessRecord?> GetWorkerAsync(WorkerProcessId workerId, CancellationToken ct = default);
        Task<IReadOnlyList<WorkerProcessRecord>> GetAvailableWorkersAsync(WorkerPoolType poolType, CancellationToken ct = default);
        Task<IReadOnlyList<WorkerProcessRecord>> GetAllWorkersAsync(CancellationToken ct = default);
    }
}
