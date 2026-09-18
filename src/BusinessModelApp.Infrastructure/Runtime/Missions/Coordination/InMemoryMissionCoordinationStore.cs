using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Missions;
using BusinessModelApp.Core.Interfaces.Runtime.Missions;

namespace BusinessModelApp.Infrastructure.Runtime.Missions.Coordination
{
    public class InMemoryMissionCoordinationStore : IMissionCoordinationStore
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, MissionAdmissionTicket>> _tickets = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, MissionResourceLock>> _locks = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, CrossMissionDependency>> _dependencies = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, MissionCancellationReceipt>> _receipts = new();
        private readonly ConcurrentDictionary<string, List<MissionTelemetryFeedback>> _telemetry = new();
        private readonly object _telemetryLock = new();

        public Task SaveTicketAsync(MissionAdmissionTicket ticket, CancellationToken ct = default)
        {
            if (ticket == null) throw new ArgumentNullException(nameof(ticket));
            var dict = _tickets.GetOrAdd(ticket.TenantId, _ => new ConcurrentDictionary<string, MissionAdmissionTicket>());
            dict[ticket.TicketId] = ticket;
            return Task.CompletedTask;
        }

        public Task<MissionAdmissionTicket?> GetTicketAsync(string tenantId, string ticketId, CancellationToken ct = default)
        {
            if (_tickets.TryGetValue(tenantId, out var dict) && dict.TryGetValue(ticketId, out var ticket))
            {
                return Task.FromResult<MissionAdmissionTicket?>(ticket);
            }
            return Task.FromResult<MissionAdmissionTicket?>(null);
        }

        public Task<IReadOnlyList<MissionAdmissionTicket>> ListTicketsAsync(string tenantId, CancellationToken ct = default)
        {
            if (_tickets.TryGetValue(tenantId, out var dict))
            {
                return Task.FromResult<IReadOnlyList<MissionAdmissionTicket>>(dict.Values.OrderByDescending(t => t.IssuedUtc).ToList());
            }
            return Task.FromResult<IReadOnlyList<MissionAdmissionTicket>>(Array.Empty<MissionAdmissionTicket>());
        }

        public Task SaveLockAsync(MissionResourceLock lockItem, CancellationToken ct = default)
        {
            if (lockItem == null) throw new ArgumentNullException(nameof(lockItem));
            var dict = _locks.GetOrAdd(lockItem.TenantId, _ => new ConcurrentDictionary<string, MissionResourceLock>());
            dict[lockItem.CanonicalKey] = lockItem;
            return Task.CompletedTask;
        }

        public Task<MissionResourceLock?> GetLockAsync(string tenantId, string canonicalKey, CancellationToken ct = default)
        {
            if (_locks.TryGetValue(tenantId, out var dict) && dict.TryGetValue(canonicalKey, out var lockItem))
            {
                return Task.FromResult<MissionResourceLock?>(lockItem);
            }
            return Task.FromResult<MissionResourceLock?>(null);
        }

        public Task<IReadOnlyList<MissionResourceLock>> ListLocksAsync(string tenantId, CancellationToken ct = default)
        {
            if (_locks.TryGetValue(tenantId, out var dict))
            {
                return Task.FromResult<IReadOnlyList<MissionResourceLock>>(dict.Values.ToList());
            }
            return Task.FromResult<IReadOnlyList<MissionResourceLock>>(Array.Empty<MissionResourceLock>());
        }

        public Task DeleteLockAsync(string tenantId, string canonicalKey, CancellationToken ct = default)
        {
            if (_locks.TryGetValue(tenantId, out var dict))
            {
                dict.TryRemove(canonicalKey, out _);
            }
            return Task.CompletedTask;
        }

        public Task SaveDependencyAsync(CrossMissionDependency dependency, CancellationToken ct = default)
        {
            if (dependency == null) throw new ArgumentNullException(nameof(dependency));
            var dict = _dependencies.GetOrAdd(dependency.TenantId, _ => new ConcurrentDictionary<string, CrossMissionDependency>());
            dict[dependency.DependencyId] = dependency;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<CrossMissionDependency>> ListDependenciesAsync(string tenantId, string consumerMissionId, CancellationToken ct = default)
        {
            if (_dependencies.TryGetValue(tenantId, out var dict))
            {
                var list = dict.Values.Where(d => d.ConsumerMissionId == consumerMissionId).ToList();
                return Task.FromResult<IReadOnlyList<CrossMissionDependency>>(list);
            }
            return Task.FromResult<IReadOnlyList<CrossMissionDependency>>(Array.Empty<CrossMissionDependency>());
        }

        public Task SaveReceiptAsync(MissionCancellationReceipt receipt, CancellationToken ct = default)
        {
            if (receipt == null) throw new ArgumentNullException(nameof(receipt));
            var dict = _receipts.GetOrAdd(receipt.TenantId, _ => new ConcurrentDictionary<string, MissionCancellationReceipt>());
            dict[receipt.ReceiptId] = receipt;
            return Task.CompletedTask;
        }

        public Task<MissionCancellationReceipt?> GetReceiptAsync(string tenantId, string receiptId, CancellationToken ct = default)
        {
            if (_receipts.TryGetValue(tenantId, out var dict) && dict.TryGetValue(receiptId, out var receipt))
            {
                return Task.FromResult<MissionCancellationReceipt?>(receipt);
            }
            return Task.FromResult<MissionCancellationReceipt?>(null);
        }

        public Task SaveTelemetryAsync(MissionTelemetryFeedback telemetry, CancellationToken ct = default)
        {
            if (telemetry == null) throw new ArgumentNullException(nameof(telemetry));
            lock (_telemetryLock)
            {
                var list = _telemetry.GetOrAdd(telemetry.TenantId, _ => new List<MissionTelemetryFeedback>());
                list.Add(telemetry);
            }
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<MissionTelemetryFeedback>> ListTelemetryAsync(string tenantId, string workId, CancellationToken ct = default)
        {
            lock (_telemetryLock)
            {
                if (_telemetry.TryGetValue(tenantId, out var list))
                {
                    return Task.FromResult<IReadOnlyList<MissionTelemetryFeedback>>(list.Where(t => t.WorkId == workId).ToList());
                }
                return Task.FromResult<IReadOnlyList<MissionTelemetryFeedback>>(Array.Empty<MissionTelemetryFeedback>());
            }
        }
    }
}
