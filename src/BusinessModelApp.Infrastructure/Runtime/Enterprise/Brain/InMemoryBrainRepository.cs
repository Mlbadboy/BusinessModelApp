using System.Collections.Concurrent;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Brain;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Brain;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Brain;

public sealed class InMemoryBrainRepository : IBrainAuditRepository
{
    private readonly ConcurrentDictionary<string, List<ExecutiveCognitiveState>> _snapshotsByTenant = new();

    public Task SaveSnapshotAsync(ExecutiveCognitiveState snapshot)
    {
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
        if (string.IsNullOrWhiteSpace(snapshot.TenantId))
            throw new ArgumentException("TenantId is mandatory for cognitive state persistence. (I36-H)", nameof(snapshot));

        var list = _snapshotsByTenant.GetOrAdd(snapshot.TenantId, _ => new List<ExecutiveCognitiveState>());
        lock (list)
        {
            // Immutable append-only store (I36-R)
            list.Add(snapshot);
        }
        return Task.CompletedTask;
    }

    public Task<ExecutiveCognitiveState?> GetLatestSnapshotAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));

        if (_snapshotsByTenant.TryGetValue(tenantId, out var list))
        {
            lock (list)
            {
                return Task.FromResult(list.OrderByDescending(s => s.SnapshotUtc).FirstOrDefault());
            }
        }
        return Task.FromResult<ExecutiveCognitiveState?>(null);
    }

    public Task<ExecutiveCognitiveState?> GetSnapshotByIdAsync(string tenantId, string snapshotId)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (string.IsNullOrWhiteSpace(snapshotId)) throw new ArgumentNullException(nameof(snapshotId));

        if (_snapshotsByTenant.TryGetValue(tenantId, out var list))
        {
            lock (list)
            {
                var found = list.FirstOrDefault(s => s.SnapshotId == snapshotId);
                if (found != null) return Task.FromResult<ExecutiveCognitiveState?>(found);
            }
        }

        // Multi-tenant penetration defense (I36-H, I36-Y): If exists under another tenant, throw UnauthorizedAccessException
        foreach (var (otherTenant, otherList) in _snapshotsByTenant)
        {
            if (otherTenant == tenantId) continue;
            lock (otherList)
            {
                if (otherList.Any(s => s.SnapshotId == snapshotId))
                {
                    throw new UnauthorizedAccessException($"Cross-tenant cognitive access violation: Tenant '{tenantId}' cannot access snapshot belonging to '{otherTenant}'. (I36-H, I36-Y)");
                }
            }
        }

        return Task.FromResult<ExecutiveCognitiveState?>(null);
    }

    public Task<IReadOnlyList<ExecutiveCognitiveState>> ListSnapshotsAsync(string tenantId, int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));

        if (_snapshotsByTenant.TryGetValue(tenantId, out var list))
        {
            lock (list)
            {
                return Task.FromResult<IReadOnlyList<ExecutiveCognitiveState>>(
                    list.OrderByDescending(s => s.SnapshotUtc).Take(limit).ToList());
            }
        }
        return Task.FromResult<IReadOnlyList<ExecutiveCognitiveState>>(Array.Empty<ExecutiveCognitiveState>());
    }
}
