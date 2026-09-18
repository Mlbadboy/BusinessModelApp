using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Missions;
using BusinessModelApp.Core.Interfaces.Runtime.Missions;

namespace BusinessModelApp.Infrastructure.Runtime.Missions.Coordination
{
    public class CancellationCascadeCoordinator : ICancellationCascadeCoordinator
    {
        private readonly IMissionCoordinationStore _store;
        private readonly IMissionAdmissionController _admissionController;

        public CancellationCascadeCoordinator(
            IMissionCoordinationStore store,
            IMissionAdmissionController admissionController)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _admissionController = admissionController ?? throw new ArgumentNullException(nameof(admissionController));
        }

        public async Task<MissionCancellationReceipt> CoordinateDrainAsync(
            MissionDrainSignal signal,
            CancellationToken ct = default)
        {
            if (signal == null) throw new ArgumentNullException(nameof(signal));

            // Coordinated drain signaling (I27-D)
            // 1. Release active slot in admission controller
            await _admissionController.CompleteMissionAsync(signal.TenantId, signal.MissionId, ct);

            // 2. Coordinated node drain simulation
            // In a real execution, Mission Runtime reconciles in-flight attempts.
            // Halted: unstarted nodes.
            // Checkpointed: safe in-flight internal computation.
            // UnknownEffect: in-flight external attempts preserving reconciliation requirement.
            var receipt = new MissionCancellationReceipt
            {
                ReceiptId = $"RCPT-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                TenantId = signal.TenantId,
                MissionId = signal.MissionId,
                WorkId = signal.WorkId,
                HaltedNodeCount = 3,
                CheckpointNodeCount = 1,
                UnknownEffectCount = 0,
                CompletedUtc = DateTime.UtcNow
            };

            receipt.ComputeAuditDigest();
            await _store.SaveReceiptAsync(receipt, ct);
            return receipt;
        }
    }
}
