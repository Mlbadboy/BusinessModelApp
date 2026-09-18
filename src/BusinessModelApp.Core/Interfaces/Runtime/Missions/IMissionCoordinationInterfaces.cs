using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime.Missions;
using BusinessModelApp.Core.Domain.Runtime.Organizational;

namespace BusinessModelApp.Core.Interfaces.Runtime.Missions
{
    public interface IMissionCoordinationStore
    {
        Task SaveTicketAsync(MissionAdmissionTicket ticket, CancellationToken ct = default);
        Task<MissionAdmissionTicket?> GetTicketAsync(string tenantId, string ticketId, CancellationToken ct = default);
        Task<IReadOnlyList<MissionAdmissionTicket>> ListTicketsAsync(string tenantId, CancellationToken ct = default);

        Task SaveLockAsync(MissionResourceLock lockItem, CancellationToken ct = default);
        Task<MissionResourceLock?> GetLockAsync(string tenantId, string canonicalKey, CancellationToken ct = default);
        Task<IReadOnlyList<MissionResourceLock>> ListLocksAsync(string tenantId, CancellationToken ct = default);
        Task DeleteLockAsync(string tenantId, string canonicalKey, CancellationToken ct = default);

        Task SaveDependencyAsync(CrossMissionDependency dependency, CancellationToken ct = default);
        Task<IReadOnlyList<CrossMissionDependency>> ListDependenciesAsync(string tenantId, string consumerMissionId, CancellationToken ct = default);

        Task SaveReceiptAsync(MissionCancellationReceipt receipt, CancellationToken ct = default);
        Task<MissionCancellationReceipt?> GetReceiptAsync(string tenantId, string receiptId, CancellationToken ct = default);

        Task SaveTelemetryAsync(MissionTelemetryFeedback telemetry, CancellationToken ct = default);
        Task<IReadOnlyList<MissionTelemetryFeedback>> ListTelemetryAsync(string tenantId, string workId, CancellationToken ct = default);
    }

    public interface IMissionAdmissionController
    {
        Task<(bool Admitted, MissionAdmissionTicket? Ticket, string? Reason)> EvaluateAdmissionAsync(
            string tenantId,
            MissionGraphProposal proposal,
            string workId,
            WorkRiskTier riskTier,
            TenantMissionConcurrencyPolicy? policyOverride = null,
            CancellationToken ct = default);

        Task CompleteMissionAsync(string tenantId, string missionId, CancellationToken ct = default);
    }

    public interface IMissionResourceArbiter
    {
        Task<(bool Acquired, MissionResourceLock? Lock, string? Reason)> AcquireLockAsync(
            string tenantId,
            string resourceNamespace,
            string resourceType,
            string resourceId,
            string missionId,
            string nodeId,
            TimeSpan? ttl = null,
            CancellationToken ct = default);

        Task<(bool AllAcquired, IReadOnlyList<MissionResourceLock> Locks, string? Reason)> AcquireMultipleLocksCanonicalAsync(
            string tenantId,
            IReadOnlyList<(string Namespace, string Type, string ResourceId)> resources,
            string missionId,
            string nodeId,
            TimeSpan? ttl = null,
            CancellationToken ct = default);

        Task<bool> ReleaseLockAsync(
            string tenantId,
            string resourceNamespace,
            string resourceType,
            string resourceId,
            long fencingToken,
            CancellationToken ct = default);

        Task<bool> ValidateFencingTokenAsync(
            string tenantId,
            string resourceNamespace,
            string resourceType,
            string resourceId,
            long fencingToken,
            CancellationToken ct = default);
    }

    public interface ICrossMissionDependencyResolver
    {
        Task<CrossMissionDependency> RegisterDependencyAsync(CrossMissionDependency dep, CancellationToken ct = default);
        Task<bool> AreDependenciesSatisfiedAsync(string tenantId, string consumerMissionId, string consumerNodeId, CancellationToken ct = default);
        Task MarkArtifactProducedAsync(string tenantId, string producerMissionId, string artifactType, string artifactId, CancellationToken ct = default);
    }

    public interface ICancellationCascadeCoordinator
    {
        Task<MissionCancellationReceipt> CoordinateDrainAsync(MissionDrainSignal signal, CancellationToken ct = default);
    }

    public interface IMissionTelemetryFeedbackChannel
    {
        Task RecordTelemetryAsync(MissionTelemetryFeedback feedback, CancellationToken ct = default);
        Task<IReadOnlyList<MissionTelemetryFeedback>> GetTelemetryForWorkAsync(string tenantId, string workId, CancellationToken ct = default);
    }

    public interface IMissionOrchestrator
    {
        Task<(bool Admitted, MissionAdmissionTicket? Ticket, string? Reason)> AdmitMissionProposalAsync(
            string tenantId,
            MissionGraphProposal proposal,
            string workId,
            WorkRiskTier riskTier,
            CancellationToken ct = default);

        Task<OrchestratorActiveState> GetActiveStateAsync(string tenantId, CancellationToken ct = default);

        Task<MissionCancellationReceipt> CancelMissionCascadingAsync(
            string tenantId,
            string missionId,
            string workId,
            string reason,
            string actor,
            CancellationToken ct = default);
    }
}
