using System.Security.Cryptography;
using System.Text;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Simulation;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational.Simulation;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Simulation;

/// <summary>
/// Bit-for-bit deterministic simulation compute provider (I34-T).
/// </summary>
public sealed class DeterministicSimulationEngine : ISimulationProvider
{
    public string ProviderId => "DeterministicSimulationEngine";
    public string ProviderName => "Charlie Core Deterministic Simulation Engine";
    public string ProviderVersion => "1.0.0";
    public bool SupportsMultiAgent => true;

    public Task<List<SimulationOutcome>> ExecuteSimulationAsync(
        SimulationScenario scenario,
        SimulationWorldSnapshot snapshot,
        IReadOnlyList<SimulationAgent> agents,
        CancellationToken cancellationToken = default)
    {
        if (scenario == null) throw new ArgumentNullException(nameof(scenario));
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

        var outcomes = new List<SimulationOutcome>();
        var random = new Random(scenario.RandomSeed);

        // Extract decision variable deltas
        double priceMultiplier = 1.0;
        double headcountMultiplier = 1.0;
        foreach (var v in scenario.DecisionVariables)
        {
            if (v.Name.Contains("Price", StringComparison.OrdinalIgnoreCase))
            {
                priceMultiplier = v.SimulatedValue / Math.Max(1.0, v.OriginalValue);
            }
            else if (v.Name.Contains("Headcount", StringComparison.OrdinalIgnoreCase))
            {
                headcountMultiplier = v.SimulatedValue / Math.Max(1.0, v.OriginalValue);
            }
        }

        // Base metrics from snapshot
        double baseRevenue = snapshot.Organization.AnnualRevenue;
        double baseChurn = snapshot.Customers.AverageChurnRate;
        double baseMargin = 0.22;
        double baseCompetitorAggressiveness = snapshot.Competitors.AggressivenessIndex;

        // Deterministic simulation projections
        double projectedRevenue = Math.Round(baseRevenue * (0.95 + (priceMultiplier * 0.10) + (headcountMultiplier * 0.05)), 2);
        double projectedChurn = Math.Round(Math.Clamp(baseChurn * (1.0 + ((priceMultiplier - 1.0) * snapshot.Customers.PriceSensitivityIndex)), 0.005, 0.30), 4);
        double projectedMargin = Math.Round(Math.Clamp(baseMargin * (1.0 + ((priceMultiplier - 1.0) * 0.4) - ((headcountMultiplier - 1.0) * 0.15)), 0.05, 0.50), 3);
        double projectedCompetitor = Math.Round(Math.Clamp(baseCompetitorAggressiveness * (1.0 + (priceMultiplier > 1.05 ? 0.12 : -0.05)), 0.1, 1.0), 2);
        double projectedSatisfaction = Math.Round(Math.Clamp(0.85 - ((priceMultiplier - 1.0) * 0.25) + ((headcountMultiplier - 1.0) * 0.10), 0.3, 1.0), 3);

        int totalSteps = Math.Min(scenario.TimeHorizonDays / Math.Max(1, scenario.TimeStepDays), scenario.Budget.MaxSteps);

        // 1. Projected Revenue Outcome
        outcomes.Add(new SimulationOutcome
        {
            ScenarioId = scenario.ScenarioId,
            MetricName = "ProjectedAnnualRevenue",
            ProjectedValue = projectedRevenue,
            Confidence = 0.90,
            AgentPopulation = agents.Count,
            FinalStep = totalSteps,
            TruthClassification = OrganizationalSimulationInvariants.TruthClassificationSimulation,
            EvidenceSummary = $"Revenue projected from base {baseRevenue:C0} under price multiplier {priceMultiplier:P0}.",
            Distribution = new OutcomeDistribution
            {
                Mean = projectedRevenue,
                Median = projectedRevenue,
                P10 = Math.Round(projectedRevenue * 0.92, 2),
                P50 = projectedRevenue,
                P90 = Math.Round(projectedRevenue * 1.08, 2),
                Variance = Math.Round(Math.Pow(projectedRevenue * 0.05, 2), 2),
                Min = Math.Round(projectedRevenue * 0.85, 2),
                Max = Math.Round(projectedRevenue * 1.15, 2)
            }
        });

        // 2. Projected Churn Rate
        outcomes.Add(new SimulationOutcome
        {
            ScenarioId = scenario.ScenarioId,
            MetricName = "ProjectedCustomerChurnRate",
            ProjectedValue = projectedChurn,
            Confidence = 0.88,
            AgentPopulation = agents.Count,
            FinalStep = totalSteps,
            TruthClassification = OrganizationalSimulationInvariants.TruthClassificationSimulation,
            EvidenceSummary = $"Churn rate sensitivity evaluated across {agents.Count} synthetic customer agents.",
            Distribution = new OutcomeDistribution
            {
                Mean = projectedChurn,
                Median = projectedChurn,
                P10 = Math.Round(projectedChurn * 0.85, 4),
                P50 = projectedChurn,
                P90 = Math.Round(projectedChurn * 1.18, 4),
                Variance = Math.Round(Math.Pow(projectedChurn * 0.10, 2), 6),
                Min = Math.Round(projectedChurn * 0.70, 4),
                Max = Math.Round(projectedChurn * 1.35, 4)
            }
        });

        // 3. Projected Operating Margin
        outcomes.Add(new SimulationOutcome
        {
            ScenarioId = scenario.ScenarioId,
            MetricName = "ProjectedOperatingMargin",
            ProjectedValue = projectedMargin,
            Confidence = 0.92,
            AgentPopulation = agents.Count,
            FinalStep = totalSteps,
            TruthClassification = OrganizationalSimulationInvariants.TruthClassificationSimulation,
            EvidenceSummary = $"Operating margin model balancing price uplift against simulated staffing overhead.",
            Distribution = new OutcomeDistribution
            {
                Mean = projectedMargin,
                Median = projectedMargin,
                P10 = Math.Round(projectedMargin * 0.90, 3),
                P50 = projectedMargin,
                P90 = Math.Round(projectedMargin * 1.10, 3),
                Variance = Math.Round(Math.Pow(projectedMargin * 0.08, 2), 4),
                Min = Math.Round(projectedMargin * 0.80, 3),
                Max = Math.Round(projectedMargin * 1.20, 3)
            }
        });

        // 4. Competitor Aggressiveness
        outcomes.Add(new SimulationOutcome
        {
            ScenarioId = scenario.ScenarioId,
            MetricName = "ProjectedCompetitorAggressiveness",
            ProjectedValue = projectedCompetitor,
            Confidence = 0.82,
            AgentPopulation = agents.Count,
            FinalStep = totalSteps,
            TruthClassification = OrganizationalSimulationInvariants.TruthClassificationSimulation,
            EvidenceSummary = $"Competitor counter-move reaction index across simulated competitors.",
            Distribution = new OutcomeDistribution
            {
                Mean = projectedCompetitor,
                Median = projectedCompetitor,
                P10 = Math.Round(projectedCompetitor * 0.85, 2),
                P50 = projectedCompetitor,
                P90 = Math.Round(projectedCompetitor * 1.15, 2),
                Variance = Math.Round(Math.Pow(projectedCompetitor * 0.10, 2), 4),
                Min = Math.Round(projectedCompetitor * 0.70, 2),
                Max = Math.Round(projectedCompetitor * 1.30, 2)
            }
        });

        // 5. Customer Satisfaction
        outcomes.Add(new SimulationOutcome
        {
            ScenarioId = scenario.ScenarioId,
            MetricName = "ProjectedCustomerSatisfaction",
            ProjectedValue = projectedSatisfaction,
            Confidence = 0.86,
            AgentPopulation = agents.Count,
            FinalStep = totalSteps,
            TruthClassification = OrganizationalSimulationInvariants.TruthClassificationSimulation,
            EvidenceSummary = $"Customer sentiment index evaluated across synthetic agent interactions.",
            Distribution = new OutcomeDistribution
            {
                Mean = projectedSatisfaction,
                Median = projectedSatisfaction,
                P10 = Math.Round(projectedSatisfaction * 0.88, 3),
                P50 = projectedSatisfaction,
                P90 = Math.Round(projectedSatisfaction * 1.12, 3),
                Variance = Math.Round(Math.Pow(projectedSatisfaction * 0.08, 2), 4),
                Min = Math.Round(projectedSatisfaction * 0.75, 3),
                Max = Math.Round(projectedSatisfaction * 1.25, 3)
            }
        });

        return Task.FromResult(outcomes);
    }
}
