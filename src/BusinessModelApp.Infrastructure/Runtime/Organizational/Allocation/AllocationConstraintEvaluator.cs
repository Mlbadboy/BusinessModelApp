using BusinessModelApp.Core.Domain.Runtime.Organizational.Allocation;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Allocation;

/// <summary>
/// Evaluates hard business constraints and statutory boundaries.
/// Invariant I32-G: Constraints Cannot Be Optimized Away.
/// Invariant I32-H: Budget Ceiling Is Hard.
/// Invariant I32-R: Deterministic Arbitration Non-Compensation. Hard constraint breaches can NEVER be offset by downstream ROI or urgency.
/// Invariant I32-W: Simulation Capacity Cannot Become Real Capacity.
/// </summary>
public class AllocationConstraintEvaluator : IAllocationConstraintEvaluator
{
    public (bool IsAdmissible, string? RejectionReason) EvaluateHardConstraints(
        string tenantId,
        AllocationDemand demand,
        IReadOnlyList<OrganizationalResource> availableResources)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) return (false, "TenantId is required.");
        if (demand == null) return (false, "Demand is required.");

        var resourceMap = availableResources?.ToDictionary(r => r.ResourceType) 
            ?? new Dictionary<OrganizationalResourceType, OrganizationalResource>();

        foreach (var (reqType, reqAmount) in demand.RequestedCapacities)
        {
            if (reqAmount <= 0) continue;

            if (!resourceMap.TryGetValue(reqType, out var resource))
            {
                return (false, $"Constraint Breach: Resource '{reqType}' is not registered or connected for tenant '{tenantId}'.");
            }

            // Invariant I32-U & I32-W: Check epistemic capacity reality
            if (resource.EpistemicReality is ResourceCapacityReality.Unknown or ResourceCapacityReality.NotConnected or ResourceCapacityReality.Stale)
            {
                return (false, $"Constraint Breach: Resource '{reqType}' has epistemic state '{resource.EpistemicReality}'. Unmeasured or uninstrumented capacity cannot be allocated.");
            }

            if (resource.EpistemicReality == ResourceCapacityReality.Simulated)
            {
                return (false, $"Constraint Breach: Resource '{reqType}' has Simulated capacity. Simulated capacity cannot be consumed for real allocation (I32-W).");
            }

            // Invariant I32-G: Max allocation per demand ceiling
            if (reqAmount > resource.MaxAllocationPerDemand)
            {
                return (false, $"Constraint Breach: Requested amount {reqAmount} exceeds max per-demand ceiling ({resource.MaxAllocationPerDemand}) for '{reqType}'.");
            }

            // Invariant I32-H: Hard budget / liquidity ceilings
            if (resource.HardFloorCapacity > 0)
            {
                double remaining = resource.AvailableCapacity - reqAmount;
                if (remaining < resource.HardFloorCapacity)
                {
                    if (!demand.AllowsPartialAllocation || (resource.AvailableCapacity - resource.HardFloorCapacity <= 0))
                    {
                        return (false, $"Constraint Breach: Allocation would breach hard floor capacity of {resource.HardFloorCapacity} for '{reqType}'.");
                    }
                }
            }

            if (reqType == OrganizationalResourceType.LiquidityBuffer)
            {
                double remainingAfterAllocation = resource.AvailableCapacity - reqAmount;
                double minFloor = resource.TotalCapacity * 0.20; // 20% inviolable liquidity floor
                if (remainingAfterAllocation < minFloor)
                {
                    return (false, $"Constraint Breach: Liquidity allocation of {reqAmount:C0} would breach mandatory 20% liquidity floor ({minFloor:C0}).");
                }
            }

            if (reqType == OrganizationalResourceType.Budget)
            {
                if (reqAmount > resource.AvailableCapacity)
                {
                    return (false, $"Constraint Breach: Requested budget ({reqAmount:C0}) exceeds hard available budget ceiling ({resource.AvailableCapacity:C0}).");
                }
            }

            // Invariant I32-E: Total capacity checks
            if (reqAmount > resource.AvailableCapacity)
            {
                if (!demand.AllowsPartialAllocation)
                {
                    return (false, $"Capacity Shortfall: Requested {reqAmount} {resource.Unit} of '{reqType}', but only {resource.AvailableCapacity} is available.");
                }
            }
        }

        return (true, null);
    }
}
