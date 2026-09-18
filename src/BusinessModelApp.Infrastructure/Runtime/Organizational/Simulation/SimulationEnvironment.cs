using BusinessModelApp.Core.Domain.Runtime.Organizational.Simulation;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational.Simulation;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Simulation;

public sealed class SimulationEnvironment : ISimulationEnvironment
{
    public Task ApplyEnvironmentEvolutionAsync(
        SimulationWorldSnapshot snapshot,
        SimulationScenario scenario,
        int stepIndex)
    {
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
        if (scenario == null) throw new ArgumentNullException(nameof(scenario));

        // Evaluate decision variables and apply to simulated world snapshot
        foreach (var variable in scenario.DecisionVariables)
        {
            if (variable.Name.Contains("Price", StringComparison.OrdinalIgnoreCase))
            {
                double priceDeltaPct = (variable.SimulatedValue - variable.OriginalValue) / Math.Max(1.0, variable.OriginalValue);
                // Elasticity: higher price -> slightly higher churn, higher revenue per unit
                snapshot.Customers.AverageChurnRate = Math.Clamp(
                    snapshot.Customers.AverageChurnRate * (1.0 + (priceDeltaPct * snapshot.Customers.PriceSensitivityIndex)),
                    0.005,
                    0.25);
            }
            else if (variable.Name.Contains("Headcount", StringComparison.OrdinalIgnoreCase) ||
                     variable.Name.Contains("Staff", StringComparison.OrdinalIgnoreCase))
            {
                snapshot.Workforce.TotalAgentsAndStaff = (int)Math.Round(variable.SimulatedValue);
            }
            else if (variable.Name.Contains("Compute", StringComparison.OrdinalIgnoreCase))
            {
                snapshot.Resources.ComputeCapacityUnits = variable.SimulatedValue;
            }
        }

        // Competitor response rule
        foreach (var rule in scenario.EnvironmentRules)
        {
            if (rule.RuleType == "CompetitorReaction")
            {
                snapshot.Competitors.AggressivenessIndex = Math.Clamp(
                    snapshot.Competitors.AggressivenessIndex * (1.0 + (0.02 * rule.SensitivityMultiplier)),
                    0.1,
                    1.0);
            }
        }

        return Task.CompletedTask;
    }
}
