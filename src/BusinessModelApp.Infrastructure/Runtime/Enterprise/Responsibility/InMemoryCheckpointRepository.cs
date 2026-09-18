using System.Collections.Concurrent;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Responsibility;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Responsibility;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Responsibility;

public sealed class InMemoryCheckpointRepository : ICycleCheckpointRepository
{
    private readonly ConcurrentDictionary<string, List<ResponsibilityCycleCheckpoint>> _checkpointsByTenant = new();
    private readonly ConcurrentDictionary<string, List<DurableResponsibilityCycle>> _cyclesByTenant = new();
    private readonly ConcurrentDictionary<string, long> _sequenceNumbersByTenant = new();

    public Task SaveCheckpointAsync(ResponsibilityCycleCheckpoint checkpoint)
    {
        if (checkpoint == null) throw new ArgumentNullException(nameof(checkpoint));
        if (string.IsNullOrWhiteSpace(checkpoint.TenantId))
            throw new ArgumentException("TenantId is mandatory for cycle checkpoints. (I37-G)", nameof(checkpoint));

        var list = _checkpointsByTenant.GetOrAdd(checkpoint.TenantId, _ => new List<ResponsibilityCycleCheckpoint>());
        lock (list)
        {
            list.Add(checkpoint);
        }
        return Task.CompletedTask;
    }

    public Task<ResponsibilityCycleCheckpoint?> GetLatestCheckpointAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));

        if (_checkpointsByTenant.TryGetValue(tenantId, out var list))
        {
            lock (list)
            {
                return Task.FromResult(list.OrderByDescending(c => c.SequenceNumber).FirstOrDefault());
            }
        }
        return Task.FromResult<ResponsibilityCycleCheckpoint?>(null);
    }

    public Task<IReadOnlyList<ResponsibilityCycleCheckpoint>> ListCheckpointsAsync(string tenantId, int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));

        if (_checkpointsByTenant.TryGetValue(tenantId, out var list))
        {
            lock (list)
            {
                return Task.FromResult<IReadOnlyList<ResponsibilityCycleCheckpoint>>(
                    list.OrderByDescending(c => c.SequenceNumber).Take(limit).ToList());
            }
        }
        return Task.FromResult<IReadOnlyList<ResponsibilityCycleCheckpoint>>(Array.Empty<ResponsibilityCycleCheckpoint>());
    }

    public Task SaveCycleAsync(DurableResponsibilityCycle cycle)
    {
        if (cycle == null) throw new ArgumentNullException(nameof(cycle));
        if (string.IsNullOrWhiteSpace(cycle.TenantId))
            throw new ArgumentException("TenantId is mandatory for responsibility cycles. (I37-G)", nameof(cycle));

        var list = _cyclesByTenant.GetOrAdd(cycle.TenantId, _ => new List<DurableResponsibilityCycle>());
        lock (list)
        {
            // Update existing or add new
            var existingIdx = list.FindIndex(c => c.CycleId == cycle.CycleId);
            if (existingIdx >= 0)
            {
                list[existingIdx] = cycle;
            }
            else
            {
                list.Add(cycle);
            }
        }
        return Task.CompletedTask;
    }

    public Task<DurableResponsibilityCycle?> GetCycleByIdAsync(string tenantId, string cycleId)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (string.IsNullOrWhiteSpace(cycleId)) throw new ArgumentNullException(nameof(cycleId));

        if (_cyclesByTenant.TryGetValue(tenantId, out var list))
        {
            lock (list)
            {
                var found = list.FirstOrDefault(c => c.CycleId == cycleId);
                if (found != null) return Task.FromResult<DurableResponsibilityCycle?>(found);
            }
        }

        // Multi-tenant penetration defense (I37-G, I37-H)
        foreach (var (otherTenant, otherList) in _cyclesByTenant)
        {
            if (otherTenant == tenantId) continue;
            lock (otherList)
            {
                if (otherList.Any(c => c.CycleId == cycleId))
                {
                    throw new UnauthorizedAccessException($"Cross-tenant responsibility cycle access violation: Tenant '{tenantId}' cannot access cycle belonging to '{otherTenant}'. (I37-G, I37-H)");
                }
            }
        }

        return Task.FromResult<DurableResponsibilityCycle?>(null);
    }

    public Task<DurableResponsibilityCycle?> GetLatestCycleAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));

        if (_cyclesByTenant.TryGetValue(tenantId, out var list))
        {
            lock (list)
            {
                return Task.FromResult(list.OrderByDescending(c => c.CycleSequenceNumber).FirstOrDefault());
            }
        }
        return Task.FromResult<DurableResponsibilityCycle?>(null);
    }

    public Task<IReadOnlyList<DurableResponsibilityCycle>> ListCyclesAsync(string tenantId, int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));

        if (_cyclesByTenant.TryGetValue(tenantId, out var list))
        {
            lock (list)
            {
                return Task.FromResult<IReadOnlyList<DurableResponsibilityCycle>>(
                    list.OrderByDescending(c => c.CycleSequenceNumber).Take(limit).ToList());
            }
        }
        return Task.FromResult<IReadOnlyList<DurableResponsibilityCycle>>(Array.Empty<DurableResponsibilityCycle>());
    }

    public long GetNextSequenceNumber(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));

        return _sequenceNumbersByTenant.AddOrUpdate(tenantId, 1, (_, current) => current + 1);
    }
}
