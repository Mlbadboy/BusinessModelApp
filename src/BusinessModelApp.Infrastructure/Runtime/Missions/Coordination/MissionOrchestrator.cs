using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime.Missions;
using BusinessModelApp.Core.Domain.Runtime.Organizational;
using BusinessModelApp.Core.Interfaces.Runtime.Missions;

namespace BusinessModelApp.Infrastructure.Runtime.Missions.Coordination
{
    /// <summary>
    /// Unified Mission Coordination Facade.
    /// Invariant I27-I: Coordinates admission, dependencies, locks, and drain signaling without creating a second runtime.
    /// Invariant I27-L: Cannot create, sign, or delegate ExecutionPermits.
    /// </summary>
    public class MissionOrchestrator : IMissionOrchestrator
    {
        private readonly IMissionAdmissionController _admissionController;
        private readonly IMissionResourceArbiter _resourceArbiter;
        private readonly ICrossMissionDependencyResolver _dependencyResolver;
        private readonly ICancellationCascadeCoordinator _cancellationCoordinator;
        private readonly IMissionTelemetryFeedbackChannel _telemetryChannel;
        private readonly IMissionCoordinationStore _store;

        public MissionOrchestrator(
            IMissionAdmissionController admissionController,
            IMissionResourceArbiter resourceArbiter,
            ICrossMissionDependencyResolver dependencyResolver,
            ICancellationCascadeCoordinator cancellationCoordinator,
            IMissionTelemetryFeedbackChannel telemetryChannel,
            IMissionCoordinationStore store)
        {
            _admissionController = admissionController ?? throw new ArgumentNullException(nameof(admissionController));
            _resourceArbiter = resourceArbiter ?? throw new ArgumentNullException(nameof(resourceArbiter));
            _dependencyResolver = dependencyResolver ?? throw new ArgumentNullException(nameof(dependencyResolver));
            _cancellationCoordinator = cancellationCoordinator ?? throw new ArgumentNullException(nameof(cancellationCoordinator));
            _telemetryChannel = telemetryChannel ?? throw new ArgumentNullException(nameof(telemetryChannel));
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<(bool Admitted, MissionAdmissionTicket? Ticket, string? Reason)> AdmitMissionProposalAsync(
            string tenantId,
            MissionGraphProposal proposal,
            string workId,
            WorkRiskTier riskTier,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId is required.", nameof(tenantId));
            if (proposal == null) throw new ArgumentNullException(nameof(proposal));

            // Invariant I27-K: Ticket ≠ Permit ≠ CapabilityLease
            return await _admissionController.EvaluateAdmissionAsync(
                tenantId,
                proposal,
                workId,
                riskTier,
                null,
                ct);
        }

        public async Task<OrchestratorActiveState> GetActiveStateAsync(
            string tenantId,
            CancellationToken ct = default)
        {
            var tickets = await _store.ListTicketsAsync(tenantId, ct);
            var locks = await _store.ListLocksAsync(tenantId, ct);
            var now = DateTime.UtcNow;

            var activeTickets = tickets.Where(t => t.Decision == AdmissionDecisionStatus.Admitted && t.ExpiresUtc > now).ToList();
            var queuedTickets = tickets.Where(t => t.Decision == AdmissionDecisionStatus.Queued && t.ExpiresUtc > now).ToList();
            var heldLocks = locks.Where(l => !l.IsExpired(now)).ToList();

            return new OrchestratorActiveState
            {
                TenantId = tenantId,
                ActiveMissionCount = activeTickets.Count,
                QueuedMissionCount = queuedTickets.Count,
                HeldLockCount = heldLocks.Count,
                ActiveMissionIds = activeTickets.Select(t => t.MissionId).Distinct().ToList(),
                QueuedMissionIds = queuedTickets.Select(t => t.MissionId).Distinct().ToList()
            };
        }

        public async Task<MissionCancellationReceipt> CancelMissionCascadingAsync(
            string tenantId,
            string missionId,
            string workId,
            string reason,
            string actor,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId is required.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(missionId)) throw new ArgumentException("MissionId is required.", nameof(missionId));

            var signal = new MissionDrainSignal
            {
                DrainId = $"DRN-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                TenantId = tenantId,
                MissionId = missionId,
                WorkId = workId,
                Reason = reason,
                InitiatedByActor = actor,
                RequestedUtc = DateTime.UtcNow
            };

            return await _cancellationCoordinator.CoordinateDrainAsync(signal, ct);
        }
    }
}
