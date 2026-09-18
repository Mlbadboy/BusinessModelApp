using BusinessModelApp.Core.Domain.Runtime.Organizational.Portfolio;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Portfolio;

/// <summary>
/// Provenance engine providing "Why Rebalance?" and "Why Stop?" explanations.
/// Invariant I33-O: Every rebalance must be explainable.
/// </summary>
public class PortfolioProvenanceService : IPortfolioProvenanceService
{
    public WhyRebalanceTrace ExplainRebalanceAction(
        PortfolioChangeProposal proposal,
        PortfolioItemRebalanceAction action,
        MaterialityDelta delta)
    {
        if (proposal == null) throw new ArgumentNullException(nameof(proposal));
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (delta == null) throw new ArgumentNullException(nameof(delta));

        string whyAction = action.Action switch
        {
            PortfolioItemAction.Stop =>
                $"Work item was proposed for governed termination because acute reality shift ({delta.RealityDelta:P0}) degraded its viable return below organizational hurdle rates.",
            PortfolioItemAction.Supersede =>
                $"Work item is being superseded by a higher-yield initiative newly enabled by current capabilities.",
            PortfolioItemAction.Reduce =>
                $"Allocation was curtailed to mitigate shared constraint pressure ({delta.ConstraintDelta:P0}) while preserving core progress.",
            PortfolioItemAction.Increase =>
                $"Allocation was expanded due to validated positive performance signals ({delta.OutcomeDelta:P0}).",
            PortfolioItemAction.Pause =>
                $"Execution was paused to safeguard against potential downstream exposure under elevated constraint conditions.",
            PortfolioItemAction.Start =>
                $"Newly staged for work plane admission to capture immediate high-value strategic momentum.",
            _ => "Execution continues on schedule as parameters remain within nominal bounds."
        };

        string materialitySummary =
            $"Materiality Score: {proposal.ComputedMaterialityScore:F4} (Threshold: {proposal.MaterialityThreshold:F2}). " +
            $"Factors: Reality Δ={delta.RealityDelta:P0}, Constraint Δ={delta.ConstraintDelta:P0}, Regime Δ={delta.StrategicRegimeDelta:P0}, Outcome Δ={delta.OutcomeDelta:P0}.";

        string opportunityCost = action.Action == PortfolioItemAction.Stop || action.Action == PortfolioItemAction.Reduce
            ? "Frees critical capacity to prevent starvation of top-priority statutory and core operational obligations."
            : "Consumes capacity within agreed OARA envelope with acceptable opportunity trade-offs.";

        string capacitySummary = string.Join(", ", action.TargetCapacities.Select(kv => $"{kv.Key}: {kv.Value}"));
        if (string.IsNullOrEmpty(capacitySummary)) capacitySummary = "Zero allocation (stopped/cleared).";

        return new WhyRebalanceTrace
        {
            ProposalId = proposal.ProposalId,
            WorkItemId = action.WorkItemId,
            Action = action.Action,
            WhyActionTaken = whyAction,
            TriggeringMaterialityFactors = materialitySummary,
            OpportunityCostComparison = opportunityCost,
            CapacityImpactSummary = capacitySummary,
            EvaluatedUtc = DateTime.UtcNow
        };
    }
}
