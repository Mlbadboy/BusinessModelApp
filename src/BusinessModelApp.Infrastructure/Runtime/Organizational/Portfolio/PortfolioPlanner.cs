using System.Security.Cryptography;
using System.Text;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Allocation;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Portfolio;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Portfolio;

/// <summary>
/// Deterministic capacity-aware sequencer matching work candidates to OARA allocation envelope.
/// Invariant I33-G: Portfolio Optimization Cannot Violate 3.5 Constraints.
/// Invariant I33-S: OARA Allocation Ceiling Sovereignty. PortfolioAllocatedCapacity <= OARAAllocatedCapacity.
/// Invariant I33-T: Deterministic Portfolio Optimization. Same inputs produce identical hash.
/// </summary>
public class PortfolioPlanner : IPortfolioPlanner
{
    private readonly IDependencyGraphResolver _dependencyResolver;

    public PortfolioPlanner(IDependencyGraphResolver dependencyResolver)
    {
        _dependencyResolver = dependencyResolver ?? throw new ArgumentNullException(nameof(dependencyResolver));
    }

    public OrganizationalPortfolio PlanPortfolio(
        string tenantId,
        IReadOnlyList<PortfolioWorkItem> candidateItems,
        PortfolioCapacityEnvelope capacityEnvelope,
        string activeStrategicRegime = "GrowthAndResilience",
        string constraintSnapshotHash = "ConstraintPass")
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (capacityEnvelope == null) throw new ArgumentNullException(nameof(capacityEnvelope));

        var portfolio = new OrganizationalPortfolio
        {
            PortfolioId = Guid.NewGuid().ToString("N"),
            TenantId = tenantId,
            GeneratedUtc = DateTime.UtcNow,
            CapacityEnvelope = capacityEnvelope,
            AllocationSnapshotHash = capacityEnvelope.AllocationSnapshotHash,
            ConstraintSnapshotHash = constraintSnapshotHash,
            StrategicRegimeSnapshotHash = ComputeRegimeHash(activeStrategicRegime)
        };

        if (candidateItems == null || candidateItems.Count == 0)
        {
            portfolio.ComputeDecisionHash();
            return portfolio;
        }

        // 1. Tenant Isolation Filter (I33-L)
        var tenantCandidates = candidateItems.Where(i => i.TenantId == tenantId).ToList();

        // 2. Dependency Ordering & Acyclic Validation (I33-F, I33-V)
        var (depState, _, orderedIds) = _dependencyResolver.ValidateAndSortDependenciesAsync(tenantId, tenantCandidates).GetAwaiter().GetResult();
        if (depState != DependencyValidationState.Satisfied)
        {
            // Fail-closed (I33-R): If dependency cycle exists, blocked items remain in portfolio with Blocked state
            portfolio.WorkItems = tenantCandidates;
            portfolio.State = PortfolioLifecycleState.Blocked;
            portfolio.ComputeDecisionHash();
            return portfolio;
        }

        // 3. Deterministic Scoring Multi-Objective Ladder (I33-T)
        // Score = CategoryWeight + StrategicAlignment + Value/Risk + DeadlinePressure + AntiStarvation
        var scoredCandidates = tenantCandidates
            .Select(item =>
            {
                double categoryScore = item.Category switch
                {
                    PortfolioWorkCategory.Obligatory => 1.5, // Statutory/Safety/Debt must take precedence
                    PortfolioWorkCategory.Strategic => activeStrategicRegime == "Growth" ? 1.2 : 1.0,
                    _ => 0.8 // Operational baseline
                };

                double regimeBonus = 0.0;
                if (activeStrategicRegime == "CashPreservation" && item.ResourceDemands.ContainsKey(OrganizationalResourceType.LiquidityBuffer))
                {
                    regimeBonus = -0.5; // Penalize cash drain under CashPreservation
                }
                else if (activeStrategicRegime == "Resilience" && item.IsReadinessDebtRemediation)
                {
                    regimeBonus = 0.4;
                }

                double valueScore = Math.Clamp(item.ExpectedValue * 0.15, 0.0, 0.30);
                double riskDiscount = item.RiskScore * 0.10;
                double reversibilityBonus = item.ReversibilityScore * 0.05;
                double deadlineScore = item.DeadlinePressure * 0.15;
                double strategicScore = item.StrategicAlignmentScore * 0.25;

                double totalDeterministicScore = Math.Round(
                    categoryScore + regimeBonus + strategicScore + valueScore + deadlineScore + reversibilityBonus - riskDiscount,
                    4);

                return new
                {
                    Item = item,
                    Score = totalDeterministicScore,
                    TieBreak = GetCanonicalHash(item.WorkItemId)
                };
            })
            .OrderByDescending(x => (int)x.Item.Category == 2 ? 1 : 0) // Obligatory first
            .ThenByDescending(x => x.Score)
            .ThenBy(x => x.TieBreak) // Deterministic tie-break
            .ToList();

        // 4. Capacity Envelope Matching (I33-S: PortfolioAllocatedCapacity <= OARAAllocatedCapacity)
        var workingAllocations = capacityEnvelope.AvailableCapacities
            .ToDictionary(k => k.Key, v => v.Value);

        // Reset committed capacities
        capacityEnvelope.CommittedCapacities = workingAllocations.ToDictionary(k => k.Key, _ => 0.0);

        var plannedItems = new List<PortfolioWorkItem>();

        foreach (var entry in scoredCandidates)
        {
            var item = entry.Item;
            bool canFulfill = true;

            // Check if demand exceeds available OARA allocation
            foreach (var (type, requested) in item.ResourceDemands)
            {
                if (requested <= 0) continue;
                workingAllocations.TryGetValue(type, out var available);
                if (available < requested)
                {
                    canFulfill = false;
                    break;
                }
            }

            if (canFulfill)
            {
                // Admit and grant full capacity
                item.State = PortfolioLifecycleState.ResourceAligned;
                item.AllocatedCapacities = new Dictionary<OrganizationalResourceType, double>(item.ResourceDemands);

                // Deduct from working pool and commit to envelope
                foreach (var (type, requested) in item.ResourceDemands)
                {
                    workingAllocations[type] = Math.Max(0.0, workingAllocations[type] - requested);
                    capacityEnvelope.CommittedCapacities[type] += requested;
                }
            }
            else
            {
                // Invariant I33-S: Cannot exceed OARA ceiling -> Must Defer or Wait
                item.State = PortfolioLifecycleState.Deferred;
                item.AllocatedCapacities.Clear();
            }

            plannedItems.Add(item);
        }

        portfolio.WorkItems = plannedItems;
        portfolio.State = plannedItems.Any(i => i.State == PortfolioLifecycleState.ResourceAligned)
            ? PortfolioLifecycleState.PlanReady
            : PortfolioLifecycleState.Deferred;

        portfolio.ComputeDecisionHash();
        return portfolio;
    }

    private static string ComputeRegimeHash(string regime)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(regime)));
    }

    private static string GetCanonicalHash(string input)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(input)));
    }
}
