using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Domain.Runtime.Constraints;
using BusinessModelApp.Core.Interfaces.Runtime.Constraints;

namespace BusinessModelApp.Infrastructure.Runtime.Constraints
{
    public class ResourceReservationEngine : IResourceReservationEngine
    {
        private readonly IBusinessConstraintStore _store;
        private readonly ConcurrentDictionary<string, ReservationId> _idempotencyIndex = new(StringComparer.Ordinal);
        private readonly object _syncLock = new();

        public ResourceReservationEngine(IBusinessConstraintStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<ReservationResult> RequestReservationAsync(
            Guid workspaceId,
            MissionId missionId,
            MissionNodeId? nodeId,
            ResourceClass resourceClass,
            double requestedAmount,
            string idempotencyKey,
            TimeSpan? ttl = null,
            CancellationToken ct = default)
        {
            if (requestedAmount <= 0)
                return ReservationResult.Denied("Requested amount must be greater than zero.", 0.0);

            // 1. Idempotency Check
            if (!string.IsNullOrWhiteSpace(idempotencyKey) && _idempotencyIndex.TryGetValue(idempotencyKey, out var existingId))
            {
                var existing = await _store.GetReservationAsync(existingId, ct);
                if (existing != null && existing.State == ReservationState.Reserved)
                {
                    double avail = await GetAvailableResourceAmountAsync(workspaceId, resourceClass, ct);
                    return ReservationResult.Granted(existing, avail + existing.ReservedAmount, avail);
                }
            }

            // 2. Atomic Reservation Check under lock
            lock (_syncLock)
            {
                var twinState = _store.GetDigitalTwinStateAsync(workspaceId, ct).GetAwaiter().GetResult() ?? new DigitalTwinBusinessState { WorkspaceId = workspaceId };
                var activeReservations = _store.GetActiveReservationsAsync(workspaceId, resourceClass, ct).GetAwaiter().GetResult();
                double alreadyReserved = activeReservations.Sum(r => r.ReservedAmount);

                double totalPool = resourceClass switch
                {
                    ResourceClass.Cash => twinState.CurrentCashBalanceINR,
                    ResourceClass.Budget => twinState.CurrentCashBalanceINR * 0.5,
                    ResourceClass.HumanApproval => Math.Max(20 - twinState.PendingHumanApprovals, 0),
                    ResourceClass.Concurrency => Math.Max(10 - twinState.ActiveConcurrentMissions, 0),
                    ResourceClass.Inventory => twinState.InventoryValueINR,
                    _ => 1_000_000.0
                };

                double available = totalPool - alreadyReserved;

                if (requestedAmount > available)
                {
                    return ReservationResult.Denied($"Insufficient available {resourceClass}. Requested amount exceeds available pool. Available: {available:N2}, Requested: {requestedAmount:N2}.", available);
                }

                var reservation = new ResourceReservation
                {
                    ReservationId = ReservationId.New(),
                    WorkspaceId = workspaceId,
                    MissionId = missionId,
                    NodeId = nodeId,
                    ResourceClass = resourceClass,
                    RequestedAmount = requestedAmount,
                    ReservedAmount = requestedAmount,
                    State = ReservationState.Reserved,
                    IdempotencyKey = idempotencyKey,
                    ExpiresAt = DateTimeOffset.UtcNow.Add(ttl ?? TimeSpan.FromMinutes(15)),
                    ReservationVersion = 1
                };

                _store.SaveReservationAsync(reservation, ct).GetAwaiter().GetResult();
                if (!string.IsNullOrWhiteSpace(idempotencyKey))
                {
                    _idempotencyIndex[idempotencyKey] = reservation.ReservationId;
                }

                return ReservationResult.Granted(reservation, available, available - requestedAmount);
            }
        }

        public async Task<bool> CommitReservationAsync(ReservationId reservationId, double consumedAmount, CancellationToken ct = default)
        {
            var res = await _store.GetReservationAsync(reservationId, ct);
            if (res == null || res.State != ReservationState.Reserved) return false;

            if (res.ExpiresAt < DateTimeOffset.UtcNow)
            {
                res.State = ReservationState.Expired;
                await _store.SaveReservationAsync(res, ct);
                return false;
            }

            res.State = ReservationState.Committed;
            res.ConsumedAmount = consumedAmount > 0 ? consumedAmount : res.ReservedAmount;
            res.CommittedAt = DateTimeOffset.UtcNow;
            res.ReservationVersion++;
            await _store.SaveReservationAsync(res, ct);
            return true;
        }

        public async Task<bool> ReleaseReservationAsync(ReservationId reservationId, CancellationToken ct = default)
        {
            var res = await _store.GetReservationAsync(reservationId, ct);
            if (res == null || res.State == ReservationState.Released) return false;

            res.State = ReservationState.Released;
            res.ReleasedAt = DateTimeOffset.UtcNow;
            res.ReservationVersion++;
            await _store.SaveReservationAsync(res, ct);
            return true;
        }

        public async Task<double> GetAvailableResourceAmountAsync(Guid workspaceId, ResourceClass resourceClass, CancellationToken ct = default)
        {
            var twinState = await _store.GetDigitalTwinStateAsync(workspaceId, ct) ?? new DigitalTwinBusinessState { WorkspaceId = workspaceId };
            var activeReservations = await _store.GetActiveReservationsAsync(workspaceId, resourceClass, ct);
            double alreadyReserved = activeReservations.Sum(r => r.ReservedAmount);

            double totalPool = resourceClass switch
            {
                ResourceClass.Cash => twinState.CurrentCashBalanceINR,
                ResourceClass.Budget => twinState.CurrentCashBalanceINR * 0.5,
                ResourceClass.HumanApproval => Math.Max(20 - twinState.PendingHumanApprovals, 0),
                ResourceClass.Concurrency => Math.Max(10 - twinState.ActiveConcurrentMissions, 0),
                ResourceClass.Inventory => twinState.InventoryValueINR,
                _ => 1_000_000.0
            };

            return Math.Max(totalPool - alreadyReserved, 0.0);
        }
    }
}
