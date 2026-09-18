using BusinessModelApp.Core.Domain.Runtime.Organizational.Portfolio;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Portfolio;

/// <summary>
/// Dynamic Portfolio Rebalance Engine.
/// Invariant I33-Q: Mathematical Materiality Metric gates rebalancing proposals.
/// Invariant I33-U: Proposed intents, not direct Mission.Cancel() calls.
/// Invariant I33-J: In-flight work with UnknownEffect cannot be force-preempted without safe drain.
/// </summary>
public class PortfolioRebalanceEngine : IPortfolioRebalanceEngine
{
    private const double DefaultMaterialityThreshold = 0.15;

    public Task<PortfolioChangeProposal?> EvaluateRebalanceAsync(
        string tenantId,
        OrganizationalPortfolio currentPortfolio,
        MaterialityDelta delta,
        IReadOnlyList<PortfolioWorkItem> incomingCandidates)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (currentPortfolio == null) throw new ArgumentNullException(nameof(currentPortfolio));
        if (delta == null) throw new ArgumentNullException(nameof(delta));

        double score = delta.ComputeScore();

        // Anti-Churn Gate (I33-Q): If materiality is below threshold, return null (NO PROPOSAL)
        if (score < DefaultMaterialityThreshold)
        {
            return Task.FromResult<PortfolioChangeProposal?>(null);
        }

        var proposal = new PortfolioChangeProposal
        {
            ProposalId = Guid.NewGuid().ToString("N"),
            TenantId = tenantId,
            SourcePortfolioId = currentPortfolio.PortfolioId,
            ComputedMaterialityScore = score,
            MaterialityThreshold = DefaultMaterialityThreshold,
            CreatedUtc = DateTime.UtcNow
        };

        var actions = new List<PortfolioItemRebalanceAction>();

        // 1. Evaluate current portfolio active items
        foreach (var item in currentPortfolio.WorkItems)
        {
            // Safeguard I33-J: UnknownEffect / in-flight uncertain state must be handled with Pause/Review rather than abrupt termination
            if (item.State == PortfolioLifecycleState.Evaluating || item.RiskScore >= 0.85 && delta.ConstraintDelta > 0.4)
            {
                actions.Add(new PortfolioItemRebalanceAction
                {
                    WorkItemId = item.WorkItemId,
                    Title = item.Title,
                    Action = PortfolioItemAction.Pause,
                    TargetCapacities = item.AllocatedCapacities,
                    Rationale = $"Paused due to acute constraint elevation ({delta.ConstraintDelta:P0}) pending safety verification.",
                    StatusQuoImpact = "Halts resource drain without aborting uncommitted state."
                });
                continue;
            }

            // Detect STOP condition: Value drop, regime obsolescence, or falsified assumptions
            if (delta.RealityDelta > 0.5 && item.Category == PortfolioWorkCategory.Strategic && item.ExpectedValue < 0.3)
            {
                actions.Add(new PortfolioItemRebalanceAction
                {
                    WorkItemId = item.WorkItemId,
                    Title = item.Title,
                    Action = PortfolioItemAction.Stop,
                    TargetCapacities = new(),
                    Rationale = $"Proposed for governed termination: Reality shift ({delta.RealityDelta:P0}) significantly degraded expected business value.",
                    StatusQuoImpact = "Frees scarce allocated capacity back to corporate pool."
                });
                continue;
            }

            // Detect REDUCE condition: Capacity crunch or constraint shift
            if (delta.ConstraintDelta > 0.3 && item.AllocatedCapacities.Count > 0)
            {
                var reducedCapacities = item.AllocatedCapacities
                    .ToDictionary(kv => kv.Key, kv => Math.Round(kv.Value * 0.6, 2));

                actions.Add(new PortfolioItemRebalanceAction
                {
                    WorkItemId = item.WorkItemId,
                    Title = item.Title,
                    Action = PortfolioItemAction.Reduce,
                    TargetCapacities = reducedCapacities,
                    Rationale = $"Reduced allocation by 40% to preserve shared corporate buffer under constraint elevation.",
                    StatusQuoImpact = "Preserves minimum viable progress while relieving capacity strain."
                });
                continue;
            }

            // Detect INCREASE condition: Opportunity upside or high outcome validation
            if (delta.OutcomeDelta > 0.4 && item.StrategicAlignmentScore > 0.8)
            {
                var increasedCapacities = item.AllocatedCapacities
                    .ToDictionary(kv => kv.Key, kv => Math.Round(kv.Value * 1.25, 2));

                actions.Add(new PortfolioItemRebalanceAction
                {
                    WorkItemId = item.WorkItemId,
                    Title = item.Title,
                    Action = PortfolioItemAction.Increase,
                    TargetCapacities = increasedCapacities,
                    Rationale = $"Increased allocation by 25% due to positive outcome validation and high strategic alignment.",
                    StatusQuoImpact = "Accelerates delivery of validated high-yield opportunity."
                });
                continue;
            }

            // Default nominal condition: CONTINUE
            actions.Add(new PortfolioItemRebalanceAction
            {
                WorkItemId = item.WorkItemId,
                Title = item.Title,
                Action = PortfolioItemAction.Continue,
                TargetCapacities = item.AllocatedCapacities,
                Rationale = "Execution parameters remain viable and aligned with current reality.",
                StatusQuoImpact = "Maintains planned delivery schedule."
            });
        }

        // 2. Evaluate incoming candidate items: START
        if (incomingCandidates != null)
        {
            foreach (var candidate in incomingCandidates.Where(c => c.TenantId == tenantId))
            {
                if (candidate.Category == PortfolioWorkCategory.Obligatory || (candidate.ExpectedValue > 0.7 && delta.StrategicRegimeDelta > 0.2))
                {
                    actions.Add(new PortfolioItemRebalanceAction
                    {
                        WorkItemId = candidate.WorkItemId,
                        Title = candidate.Title,
                        Action = PortfolioItemAction.Start,
                        TargetCapacities = candidate.ResourceDemands,
                        Rationale = $"Admitted new candidate '{candidate.Title}' due to urgent strategic/obligatory alignment under changed regime.",
                        StatusQuoImpact = "Stages candidate for work plane admission."
                    });
                }
            }
        }

        proposal.ItemActions = actions;
        proposal.StrategicJustification = $"Material organizational change detected (Score: {score:F4} >= {DefaultMaterialityThreshold}). Rebalancing proposal generated with {actions.Count} actions.";
        proposal.ComputeHash();

        return Task.FromResult<PortfolioChangeProposal?>(proposal);
    }
}
