using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Domain.Runtime.Capabilities;
using BusinessModelApp.Core.Domain.Runtime.Workers;

namespace BusinessModelApp.Core.Interfaces.Runtime.Workers
{
    public interface IWorkerStore
    {
        Task SaveWorkerDefinitionAsync(WorkerDefinition definition, CancellationToken ct = default);
        Task<WorkerDefinition?> GetWorkerDefinitionAsync(WorkerDefinitionId definitionId, CancellationToken ct = default);
        Task<IReadOnlyList<WorkerDefinition>> GetWorkerDefinitionsAsync(Guid workspaceId, CancellationToken ct = default);

        Task SaveWorkerInstanceAsync(WorkerInstance instance, CancellationToken ct = default);
        Task<WorkerInstance?> GetWorkerInstanceAsync(WorkerInstanceId instanceId, CancellationToken ct = default);
        Task<IReadOnlyList<WorkerInstance>> GetWorkerInstancesAsync(Guid workspaceId, CancellationToken ct = default);

        Task SaveWorkerProcessAsync(WorkerProcess process, CancellationToken ct = default);
        Task<WorkerProcess?> GetWorkerProcessAsync(WorkerProcessId processId, CancellationToken ct = default);

        Task SaveCapabilityDefinitionAsync(UniversalCapabilityDefinition capability, CancellationToken ct = default);
        Task<UniversalCapabilityDefinition?> GetCapabilityDefinitionAsync(CapabilityId capabilityId, CancellationToken ct = default);
        Task<IReadOnlyList<UniversalCapabilityDefinition>> GetCapabilityDefinitionsAsync(CancellationToken ct = default);

        Task SaveActionProposalAsync(WorkerActionProposal proposal, CancellationToken ct = default);
        Task<WorkerActionProposal?> GetActionProposalAsync(Guid proposalId, CancellationToken ct = default);
        Task<IReadOnlyList<WorkerActionProposal>> GetActionProposalsAsync(Guid workspaceId, CancellationToken ct = default);

        Task RecordIsolationViolationAsync(WorkerIsolationViolation violation, CancellationToken ct = default);
        Task<IReadOnlyList<WorkerIsolationViolation>> GetIsolationViolationsAsync(Guid workspaceId, CancellationToken ct = default);

        Task SaveHealthSnapshotAsync(WorkerHealthSnapshot snapshot, CancellationToken ct = default);
        Task<WorkerHealthSnapshot?> GetHealthSnapshotAsync(WorkerDefinitionId definitionId, CancellationToken ct = default);
        Task<IReadOnlyList<WorkerHealthSnapshot>> GetHealthSnapshotsAsync(CancellationToken ct = default);
    }

    public interface IWorkerResolver
    {
        Task<WorkerResolutionDecision> ResolveWorkerAsync(WorkerResolutionRequest request, CancellationToken ct = default);
    }

    public interface IWorkerSandboxManager
    {
        Task<string> CreateSandboxAsync(Guid workspaceId, WorkerInstanceId instanceId, WorkerModality modality, WorkerSandboxProfile profile, CancellationToken ct = default);
        Task<bool> ValidateResourceLimitsAsync(string sandboxId, WorkerResourceUsage usage, CancellationToken ct = default);
        Task RecordViolationAsync(WorkerIsolationViolation violation, CancellationToken ct = default);
        Task TeardownSandboxAsync(string sandboxId, CancellationToken ct = default);
    }

    public interface IWorkerModalityAdapter
    {
        WorkerModality Modality { get; }
        Task<WorkerAttempt> ExecuteCapabilityAsync(
            WorkerInstance instance,
            UniversalCapabilityDefinition capability,
            string payloadJson,
            string sandboxId,
            CancellationToken ct = default);
    }

    public interface IWorkerActionProposalGateway
    {
        Task<ActionProposalValidationResult> ValidateProposalAsync(
            WorkerActionProposal proposal,
            UniversalCapabilityDefinition capability,
            CancellationToken ct = default);

        Task<bool> SubmitToRuntimeAdmissionAsync(
            WorkerActionProposal proposal,
            CancellationToken ct = default);
    }

    public interface IWorkerHealthManager
    {
        Task<WorkerHealthSnapshot> GetHealthAsync(WorkerDefinitionId definitionId, WorkerModality modality, CancellationToken ct = default);
        Task RecordExecutionResultAsync(
            WorkerDefinitionId definitionId,
            WorkerModality modality,
            bool isSuccess,
            bool isCrash,
            bool isTimeout,
            bool isUnknownEffect,
            TimeSpan duration,
            CancellationToken ct = default);
        Task QuarantineWorkerAsync(WorkerDefinitionId definitionId, string reason, CancellationToken ct = default);
    }

    public interface IWorkerRecoveryManager
    {
        Task<NodeExecutionEffect> ReconcileUnknownEffectAsync(WorkerAttempt attempt, CancellationToken ct = default);
    }

    public interface IWorkerFabric
    {
        Task<WorkerResolutionDecision> ResolveWorkerAsync(WorkerResolutionRequest request, CancellationToken ct = default);
        Task<WorkerInstance> ProvisionWorkerAsync(WorkerResolutionDecision decision, AgentInstanceId agentInstanceId, long fenceToken, CancellationToken ct = default);
        Task<WorkerAttempt> ExecuteAsync(WorkerInstance instance, UniversalCapabilityDefinition capability, string payloadJson, CancellationToken ct = default);
        Task TeardownWorkerAsync(WorkerInstanceId instanceId, string reason, CancellationToken ct = default);
    }
}
