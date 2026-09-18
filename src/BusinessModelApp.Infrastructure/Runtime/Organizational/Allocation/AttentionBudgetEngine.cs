using System.Collections.Concurrent;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Allocation;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Allocation;

/// <summary>
/// Engine for managing cognitive organizational attention budgets.
/// Invariant I32-A: Attention != Priority. Attention is treated as a scarce, governable budget.
/// </summary>
public class AttentionBudgetEngine : IAttentionBudgetEngine
{
    private readonly ConcurrentDictionary<string, AttentionBudget> _budgets = new();

    public Task<AttentionBudget> GetAttentionBudgetAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));

        var budget = _budgets.GetOrAdd(tenantId, t => new AttentionBudget
        {
            TenantId = t,
            TotalAttentionUnits = 100.0,
            CommittedAttentionUnits = 0.0,
            DomainCommitments = new Dictionary<string, double>()
        });

        return Task.FromResult(budget);
    }

    public async Task<bool> CommitAttentionUnitsAsync(string tenantId, string domain, double units)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(domain) || units <= 0) return false;

        var budget = await GetAttentionBudgetAsync(tenantId);
        lock (budget)
        {
            if (budget.CommittedAttentionUnits + units > budget.TotalAttentionUnits)
            {
                return false; // Exceeds scarce attention budget
            }

            budget.DomainCommitments.TryGetValue(domain, out var current);
            budget.DomainCommitments[domain] = current + units;
            budget.CommittedAttentionUnits += units;
            return true;
        }
    }

    public async Task ReleaseAttentionUnitsAsync(string tenantId, string domain, double units)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(domain) || units <= 0) return;

        var budget = await GetAttentionBudgetAsync(tenantId);
        lock (budget)
        {
            if (budget.DomainCommitments.TryGetValue(domain, out var current))
            {
                double released = Math.Min(current, units);
                budget.DomainCommitments[domain] = Math.Max(0.0, current - released);
                budget.CommittedAttentionUnits = Math.Max(0.0, budget.CommittedAttentionUnits - released);
            }
        }
    }
}
