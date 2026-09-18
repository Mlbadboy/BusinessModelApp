using System.Collections.Concurrent;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Allocation;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Allocation;

/// <summary>
/// Thread-safe multi-tenant in-memory allocation ledger.
/// Invariant I32-N: Allocation Expiry. Unused reservations expire deterministically after their TTL.
/// Invariant I32-T: Reservation Authority Uniqueness.
/// </summary>
public class InMemoryAllocationLedger : IAllocationLedger
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, AllocationRecord>> _recordsByTenant = new();

    public Task SaveRecordAsync(string tenantId, AllocationRecord record)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (record == null) throw new ArgumentNullException(nameof(record));

        var tenantStore = _recordsByTenant.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, AllocationRecord>());
        tenantStore[record.AllocationId] = record;
        return Task.CompletedTask;
    }

    public Task<AllocationRecord?> GetRecordAsync(string tenantId, string allocationId)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(allocationId))
            return Task.FromResult<AllocationRecord?>(null);

        if (_recordsByTenant.TryGetValue(tenantId, out var tenantStore) && tenantStore.TryGetValue(allocationId, out var rec))
        {
            return Task.FromResult<AllocationRecord?>(rec);
        }
        return Task.FromResult<AllocationRecord?>(null);
    }

    public Task<IReadOnlyList<AllocationRecord>> ListRecordsAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || !_recordsByTenant.TryGetValue(tenantId, out var tenantStore))
        {
            return Task.FromResult<IReadOnlyList<AllocationRecord>>(Array.Empty<AllocationRecord>());
        }
        return Task.FromResult<IReadOnlyList<AllocationRecord>>(tenantStore.Values.ToList());
    }

    public Task<int> ExpireStaleReservationsAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || !_recordsByTenant.TryGetValue(tenantId, out var tenantStore))
        {
            return Task.FromResult(0);
        }

        int expiredCount = 0;
        var now = DateTime.UtcNow;

        foreach (var record in tenantStore.Values)
        {
            if (record.State is AllocationLifecycleState.ArbitrationAdmitted or AllocationLifecycleState.Reserved)
            {
                if (now > record.ExpiresAtUtc)
                {
                    lock (record)
                    {
                        record.State = AllocationLifecycleState.Expired;
                        expiredCount++;
                    }
                }
            }
        }

        return Task.FromResult(expiredCount);
    }
}
