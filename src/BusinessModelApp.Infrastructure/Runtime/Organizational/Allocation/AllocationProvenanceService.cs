using BusinessModelApp.Core.Domain.Runtime.Organizational.Allocation;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Allocation;

/// <summary>
/// Provenance service explaining "Why Allocated?" and "Why Not More?".
/// </summary>
public class AllocationProvenanceService : IAllocationProvenanceService
{
    public WhyAllocationTrace ExplainAllocation(
        AllocationRecord record,
        AllocationCandidate candidate,
        IReadOnlyList<AllocationCandidate> competingCandidates)
    {
        if (record == null) throw new ArgumentNullException(nameof(record));
        if (candidate == null) throw new ArgumentNullException(nameof(candidate));

        var ladderPasses = new List<string>
        {
            "Stage 1: Passed Tenant Boundary Isolation",
            "Stage 2: Passed 3.5 Hard Business Constraints & Liquidity Floors",
            "Stage 3: Passed Safety & Statutory Compliance Bounds",
            $"Stage 4: Aligned with Strategic Regime (Regime Score: {candidate.StrategicRegimeScore:F2})",
            $"Stage 5: Evaluated Priority Tier {candidate.Demand.PriorityTier}",
            $"Stage 8: Expected Outcome Impact {candidate.Demand.ExpectedOutcomeImpact:P0}"
        };

        if (candidate.Demand.IsReadinessDebtRemediation)
        {
            ladderPasses.Add("Stage 9: Mitigates acute 3.9.6 Readiness Debt exposure");
        }

        if (candidate.AgingBonus > 0)
        {
            ladderPasses.Add($"Stage 12: Applied Anti-Starvation Aging Bonus (+{candidate.AgingBonus:P0})");
        }

        string whyAllocated = candidate.RecommendedPosture switch
        {
            AllocationDecisionPosture.Allocate => 
                $"Fully allocated across all requested dimensions with arbitration score {candidate.FinalArbitrationScore:F2}.",
            AllocationDecisionPosture.AllocatePartial => 
                $"Partially allocated to provide minimum viable operational progress without exhausting scarce shared capacity.",
            AllocationDecisionPosture.WaitForCapacity => 
                $"Queued with high priority pending completion of running missions.",
            _ => $"Candidate evaluated under 14-stage lexicographic arbitration."
        };

        string whyNotMore;
        string limitingConstraint = "None - Request Fully Satisfied";

        if (candidate.RecommendedPosture == AllocationDecisionPosture.Allocate)
        {
            whyNotMore = "The allocation fully met 100% of the requested capacity across all dimensions; no further allocation was requested.";
        }
        else if (candidate.RecommendedPosture == AllocationDecisionPosture.AllocatePartial)
        {
            var constrainedTypes = candidate.Demand.RequestedCapacities
                .Where(kv => candidate.GrantedCapacities.TryGetValue(kv.Key, out var granted) && granted < kv.Value)
                .Select(kv => kv.Key.ToString())
                .ToList();

            limitingConstraint = string.Join(", ", constrainedTypes);
            whyNotMore = $"Allocation was capped at partial viability because available capacity for [{limitingConstraint}] was insufficient to grant 100% without breaching shared buffer limits.";
        }
        else
        {
            limitingConstraint = candidate.ConstraintBreachReason ?? "Competing Priority Depletion";
            whyNotMore = $"No capacity could be granted during this epoch due to: {limitingConstraint}.";
        }

        var competingTitles = competingCandidates?
            .Where(c => c.Demand.DemandId != candidate.Demand.DemandId)
            .Take(5)
            .Select(c => $"'{c.Demand.Title}' (Score: {c.FinalArbitrationScore:F2})")
            .ToList() ?? new List<string>();

        var shortfalls = new Dictionary<OrganizationalResourceType, double>();
        foreach (var (type, requested) in candidate.Demand.RequestedCapacities)
        {
            candidate.GrantedCapacities.TryGetValue(type, out var granted);
            if (granted < requested)
            {
                shortfalls[type] = Math.Round(requested - granted, 2);
            }
        }

        return new WhyAllocationTrace
        {
            AllocationId = record.AllocationId,
            Posture = record.Posture,
            WhyAllocated = whyAllocated,
            WhyNotMore = whyNotMore,
            LimitingConstraint = limitingConstraint,
            UpstreamLadderPasses = ladderPasses,
            CompetingAlternativesConsidered = competingTitles,
            CapacityShortfallsEncountered = shortfalls,
            SnapshotHash = record.SnapshotHash,
            EvaluatedUtc = DateTime.UtcNow
        };
    }
}
