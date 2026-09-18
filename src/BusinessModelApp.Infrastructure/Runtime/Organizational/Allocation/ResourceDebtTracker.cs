using System.Collections.Concurrent;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Allocation;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Allocation;

/// <summary>
/// Persistent tracker for Resource Allocation Debt.
/// Legitimate Demands > Available Capacity -> Resource Allocation Debt.
/// </summary>
public class ResourceDebtTracker : IResourceDebtTracker
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ResourceDebtRecord>> _debtsByTenant = new();

    public Task<ResourceDebtRecord> RecordShortfallDebtAsync(string tenantId, ResourceDebtType type, double amount, string description)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));

        var debt = new ResourceDebtRecord
        {
            DebtId = Guid.NewGuid().ToString("N"),
            TenantId = tenantId,
            DebtType = type,
            ShortfallAmount = Math.Round(Math.Max(0.0, amount), 2),
            Description = description ?? string.Empty,
            AgeInDays = 0,
            CompoundingFactor = 1.0,
            FirstDeferredUtc = DateTime.UtcNow,
            LastEvaluatedUtc = DateTime.UtcNow,
            IsSatisfied = false
        };

        var tenantStore = _debtsByTenant.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, ResourceDebtRecord>());
        tenantStore[debt.DebtId] = debt;
        return Task.FromResult(debt);
    }

    public Task<IReadOnlyList<ResourceDebtRecord>> ListDebtsAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || !_debtsByTenant.TryGetValue(tenantId, out var tenantStore))
        {
            return Task.FromResult<IReadOnlyList<ResourceDebtRecord>>(Array.Empty<ResourceDebtRecord>());
        }

        var now = DateTime.UtcNow;
        foreach (var debt in tenantStore.Values)
        {
            if (!debt.IsSatisfied)
            {
                debt.AgeInDays = Math.Max(0, (int)(now - debt.FirstDeferredUtc).TotalDays);
                debt.CompoundingFactor = Math.Round(1.0 + (debt.AgeInDays / 30.0) * 0.05, 3);
                debt.LastEvaluatedUtc = now;
            }
        }

        return Task.FromResult<IReadOnlyList<ResourceDebtRecord>>(tenantStore.Values.ToList());
    }

    public Task<bool> RemediateDebtAsync(string tenantId, string debtId)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(debtId)) return Task.FromResult(false);

        if (_debtsByTenant.TryGetValue(tenantId, out var tenantStore) && tenantStore.TryGetValue(debtId, out var debt))
        {
            lock (debt)
            {
                debt.IsSatisfied = true;
                debt.LastEvaluatedUtc = DateTime.UtcNow;
            }
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }
}
