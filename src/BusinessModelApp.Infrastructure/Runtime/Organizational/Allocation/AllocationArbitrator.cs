using System.Security.Cryptography;
using System.Text;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Allocation;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Allocation;

/// <summary>
/// 14-Stage Lexicographic Decision Ladder Arbitrator.
/// Invariant I32-E: Scarcity Sovereignty.
/// Invariant I32-F: No Resource Monopolization (Starvation protection & aging).
/// Invariant I32-R: Deterministic Arbitration Non-Compensation.
/// Invariant I32-S: Strategic Regime Sovereignty.
/// Invariant I32-M: Allocation Atomicity.
/// </summary>
public class AllocationArbitrator : IAllocationArbitrator
{
    private readonly IAllocationConstraintEvaluator _constraintEvaluator;

    public AllocationArbitrator(IAllocationConstraintEvaluator constraintEvaluator)
    {
        _constraintEvaluator = constraintEvaluator ?? throw new ArgumentNullException(nameof(constraintEvaluator));
    }

    public AllocationPlan ArbitrateDemands(
        string tenantId,
        IReadOnlyList<AllocationDemand> demands,
        IReadOnlyList<OrganizationalResource> resources,
        string activeStrategicRegime = "GrowthAndResilience")
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));

        var plan = new AllocationPlan
        {
            PlanId = Guid.NewGuid().ToString("N"),
            TenantId = tenantId,
            GeneratedUtc = DateTime.UtcNow,
            ActiveStrategicRegime = activeStrategicRegime
        };

        if (demands == null || demands.Count == 0) return plan;

        // Working pool of available capacities for this arbitration epoch
        var workingCapacities = resources?.ToDictionary(r => r.ResourceType, r => r.AvailableCapacity)
            ?? new Dictionary<OrganizationalResourceType, double>();

        var candidates = new List<AllocationCandidate>();

        // Phase 1: Upstream Ladder Stages (Stages 1 to 4: Filtering & Regime Scoring)
        foreach (var demand in demands)
        {
            var candidate = new AllocationCandidate { Demand = demand };

            // Stage 1: Tenant Isolation Check
            if (demand.TenantId != tenantId)
            {
                candidate.PassesHardConstraints = false;
                candidate.ConstraintBreachReason = "Cross-tenant allocation attempt rejected (I32-L).";
                candidate.RecommendedPosture = AllocationDecisionPosture.Reject;
                candidates.Add(candidate);
                continue;
            }

            // Stage 2: Hard Business Constraints (Liquidity floors, budget ceilings) [I32-G, I32-R]
            var (isAdmissible, rejectionReason) = _constraintEvaluator.EvaluateHardConstraints(tenantId, demand, resources ?? Array.Empty<OrganizationalResource>());
            if (!isAdmissible)
            {
                candidate.PassesHardConstraints = false;
                candidate.ConstraintBreachReason = rejectionReason ?? "Hard constraint breach.";
                candidate.RecommendedPosture = AllocationDecisionPosture.Reject;
                candidates.Add(candidate);
                continue; // Non-compensation (I32-R): downstream value cannot rescue constraint failure
            }

            // Stage 3: Safety & Statutory Compliance
            if (demand.RiskScore > 0.85 && demand.PriorityTier < 3)
            {
                candidate.PassesSafetyAndCompliance = false;
                candidate.ConstraintBreachReason = "Uncompensated operational risk without high governance priority.";
                candidate.RecommendedPosture = AllocationDecisionPosture.Reject;
                candidates.Add(candidate);
                continue;
            }

            // Stage 4: Strategic Regime Alignment (Consumed from 3.5 authority) [I32-S]
            candidate.StrategicRegimeScore = EvaluateRegimeAlignment(demand, activeStrategicRegime);

            // Calculate composite ranking score across downstream stages (Stages 5 - 13)
            // Stage 5: Priority Tier (0.25)
            double priorityScore = demand.PriorityTier * 0.25;
            // Stage 7: Urgency (inverse decay hours)
            double urgencyScore = Math.Clamp(1.0 - (demand.TimeToDecayHours / 168.0), 0.0, 1.0) * 0.15;
            // Stage 8: Impact Magnitude (0.20)
            double impactScore = demand.ExpectedOutcomeImpact * 0.20;
            // Stage 9: Readiness Debt Exposure Mitigation (0.15 bonus if mitigating 3.9.6 debt)
            double readinessDebtBonus = demand.IsReadinessDebtRemediation ? 0.15 : 0.0;
            // Stage 10: Expected Value / ROI (0.15)
            double roiScore = Math.Clamp(demand.ExpectedRoiPerUnit * 0.10, 0.0, 0.15);
            // Stage 11: Risk-Adjusted Value (discounted by risk, scaled by reversibility)
            double riskAdjustedScore = (demand.ReversibilityScore * 0.05) - (demand.RiskScore * 0.05);
            // Stage 12: Anti-Monopolization Aging Bonus (I32-F)
            int waitDays = (int)(DateTime.UtcNow - demand.SubmittedUtc).TotalDays;
            candidate.AgingBonus = Math.Min(0.30, waitDays * 0.05); // Up to +30% for aged/starved requests

            candidate.FinalArbitrationScore = Math.Round(
                priorityScore + urgencyScore + impactScore + readinessDebtBonus + roiScore + riskAdjustedScore + candidate.AgingBonus + (candidate.StrategicRegimeScore * 0.10),
                4);

            candidates.Add(candidate);
        }

        // Phase 2: Lexicographic Ranking & Downstream Arbitration
        var sortedCandidates = candidates
            .Where(c => c.PassesHardConstraints && c.PassesSafetyAndCompliance)
            .OrderByDescending(c => c.Demand.PriorityTier) // Stage 5: Primary lexicographic ladder
            .ThenByDescending(c => c.FinalArbitrationScore) // Stages 6-13
            .ThenBy(c => GetCanonicalHash(c.Demand.DemandId)) // Stage 14: Deterministic Cryptographic Tie-Break
            .ToList();

        // Phase 3: Capacity Allocation & Decision Postures
        foreach (var candidate in sortedCandidates)
        {
            var demand = candidate.Demand;
            bool canFulfillFully = true;
            bool canFulfillPartially = demand.AllowsPartialAllocation;
            double lowestFraction = 1.0;

            foreach (var (type, requested) in demand.RequestedCapacities)
            {
                if (requested <= 0) continue;
                workingCapacities.TryGetValue(type, out var available);

                if (available < requested)
                {
                    canFulfillFully = false;
                    double minRequired = demand.MinimumViableCapacities.TryGetValue(type, out var mv) 
                        ? mv 
                        : (requested * demand.MinimumViableFraction);

                    if (canFulfillPartially && available >= minRequired && available > 0)
                    {
                        double fraction = available / requested;
                        lowestFraction = Math.Min(lowestFraction, fraction);
                    }
                    else
                    {
                        canFulfillPartially = false;
                    }
                }
            }

            if (canFulfillFully)
            {
                candidate.RecommendedPosture = AllocationDecisionPosture.Allocate;
                foreach (var (type, requested) in demand.RequestedCapacities)
                {
                    candidate.GrantedCapacities[type] = requested;
                    workingCapacities[type] = Math.Max(0.0, workingCapacities[type] - requested);
                }
                CreateAndAdmitRecord(plan, candidate, tenantId);
            }
            else if (canFulfillPartially)
            {
                candidate.RecommendedPosture = AllocationDecisionPosture.AllocatePartial;
                foreach (var (type, requested) in demand.RequestedCapacities)
                {
                    double granted = Math.Round(requested * lowestFraction, 2);
                    candidate.GrantedCapacities[type] = granted;
                    workingCapacities[type] = Math.Max(0.0, workingCapacities[type] - granted);
                }
                CreateAndAdmitRecord(plan, candidate, tenantId);
            }
            else
            {
                // Invariant I32-M: Multi-resource all-or-nothing atomicity.
                // If capacity is unavailable and cannot be partially allocated: WaitForCapacity or Defer
                candidate.RecommendedPosture = AllocationDecisionPosture.WaitForCapacity;

                // Record Resource Allocation Debt for legitimate unfulfilled demand
                foreach (var (type, requested) in demand.RequestedCapacities)
                {
                    double shortfall = requested;
                    if (shortfall > 0)
                    {
                        plan.AccumulatedDebts.Add(new ResourceDebtRecord
                        {
                            DebtId = Guid.NewGuid().ToString("N"),
                            TenantId = tenantId,
                            DebtType = MapResourceTypeToDebt(type),
                            ShortfallAmount = shortfall,
                            Description = $"Capacity shortfall of {shortfall} for demand '{demand.Title}' in epoch {plan.PlanId[..8]}.",
                            FirstDeferredUtc = DateTime.UtcNow,
                            LastEvaluatedUtc = DateTime.UtcNow,
                            IsSatisfied = false
                        });
                    }
                }
            }
        }

        plan.EvaluatedCandidates = candidates;
        return plan;
    }

    private void CreateAndAdmitRecord(AllocationPlan plan, AllocationCandidate candidate, string tenantId)
    {
        var record = new AllocationRecord
        {
            AllocationId = Guid.NewGuid().ToString("N"),
            TenantId = tenantId,
            DemandId = candidate.Demand.DemandId,
            TargetResponsibilityId = candidate.Demand.TargetResponsibilityId,
            TargetProposalId = candidate.Demand.TargetProposalId,
            AllocatedCapacities = new Dictionary<OrganizationalResourceType, double>(candidate.GrantedCapacities),
            State = AllocationLifecycleState.ArbitrationAdmitted, // Invariant I32-V: ArbitrationAdmitted != HumanApproved
            Posture = candidate.RecommendedPosture,
            ValidFromUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(24), // Invariant I32-N: 24h deterministic reservation TTL
            ReasonSummary = $"Admitted via 14-stage lexicographic arbitration with score {candidate.FinalArbitrationScore:F2}."
        };

        record.ComputeSnapshotHash(
            resourceSnapshot: string.Join(",", candidate.GrantedCapacities.Select(kv => $"{kv.Key}:{kv.Value}")),
            demandSnapshot: candidate.Demand.DemandId,
            constraintSnapshot: "ConstraintsPass",
            regimeSnapshot: plan.ActiveStrategicRegime,
            policySnapshot: "StandardGovernancePolicy",
            readinessSnapshot: candidate.Demand.IsReadinessDebtRemediation ? "RemediatesReadinessDebt" : "Standard"
        );

        plan.AdmittedAllocations.Add(record);
    }

    private double EvaluateRegimeAlignment(AllocationDemand demand, string activeStrategicRegime)
    {
        // 3.5 Strategic Regime alignment scoring
        return activeStrategicRegime switch
        {
            "CashPreservation" => demand.RequestedCapacities.ContainsKey(OrganizationalResourceType.LiquidityBuffer) ? 0.3 : 1.0,
            "Growth" => demand.ExpectedOutcomeImpact >= 0.7 ? 1.2 : 1.0,
            "Resilience" => demand.IsReadinessDebtRemediation ? 1.3 : 1.0,
            _ => 1.0
        };
    }

    private static string GetCanonicalHash(string input)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(input)));
    }

    private static ResourceDebtType MapResourceTypeToDebt(OrganizationalResourceType type)
    {
        return type switch
        {
            OrganizationalResourceType.AgentBandwidth => ResourceDebtType.AgentBandwidthDebt,
            OrganizationalResourceType.Compute => ResourceDebtType.ComputeDebt,
            OrganizationalResourceType.HumanAttention => ResourceDebtType.AttentionDebt,
            OrganizationalResourceType.OperationsCapacity => ResourceDebtType.OperationsDebt,
            _ => ResourceDebtType.LiquidityBufferDebt
        };
    }
}
