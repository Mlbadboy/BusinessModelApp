using System.Security.Cryptography;
using System.Text;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Portfolio;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Portfolio;

/// <summary>
/// Epistemically stamped read-only simulation sandbox for What-If scenario analysis.
/// Invariant I33-K: Simulation cannot mutate production portfolio.
/// Invariant I33-W: TruthClassification = Simulation. Read-only snapshots, zero write capability.
/// </summary>
public class PortfolioSimulator : IPortfolioSimulator
{
    public Task<PortfolioSimulationResult> SimulateScenariosAsync(
        string tenantId,
        OrganizationalPortfolio portfolioSnapshot,
        IReadOnlyList<PortfolioSimulationScenario> scenarios)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (portfolioSnapshot == null) throw new ArgumentNullException(nameof(portfolioSnapshot));

        string inputHash = ComputeSnapshotHash(portfolioSnapshot);

        var outcomes = new List<SimulatedScenarioOutcome>();

        if (scenarios != null)
        {
            foreach (var scenario in scenarios)
            {
                // Deterministic simulation projections
                double weightStrategic = scenario.CategoryAllocationWeights.TryGetValue(PortfolioWorkCategory.Strategic, out var ws) ? ws : 0.4;
                double weightOps = scenario.CategoryAllocationWeights.TryGetValue(PortfolioWorkCategory.Operational, out var wo) ? wo : 0.4;
                double weightObligatory = scenario.CategoryAllocationWeights.TryGetValue(PortfolioWorkCategory.Obligatory, out var wb) ? wb : 0.2;

                double basePortfolioValue = portfolioSnapshot.WorkItems.Count > 0 ? portfolioSnapshot.WorkItems.Sum(i => i.ExpectedValue) : 0.0;
                double baseRisk = portfolioSnapshot.WorkItems.Count > 0 ? portfolioSnapshot.WorkItems.Average(i => i.RiskScore) : 0.2;

                double projectedValue = Math.Round(basePortfolioValue * (1.0 + (weightStrategic * 0.3) - (weightOps * 0.05)), 2);
                double projectedRisk = Math.Round(baseRisk * (1.0 + (weightStrategic * 0.2) - (weightObligatory * 0.1)), 3);
                double bufferPreservation = scenario.SimulatedRegime == "CashPreservation" ? 0.45 : 0.20;

                int completedCount = (int)Math.Round(portfolioSnapshot.WorkItems.Count * (0.6 + (weightOps * 0.3)));
                int deferredCount = portfolioSnapshot.WorkItems.Count - completedCount;

                outcomes.Add(new SimulatedScenarioOutcome
                {
                    ScenarioName = scenario.Name,
                    ProjectedPortfolioValue = projectedValue,
                    ProjectedRiskExposure = Math.Clamp(projectedRisk, 0.0, 1.0),
                    ProjectedBufferPreservation = bufferPreservation,
                    CompletedInitiativesEstimate = completedCount,
                    DeferredInitiativesCount = Math.Max(0, deferredCount)
                });
            }
        }

        string outputHash = ComputeOutcomesHash(outcomes);

        var result = new PortfolioSimulationResult
        {
            InputSnapshotHash = inputHash,
            OutputSnapshotHash = outputHash,
            ProviderId = "DeterministicScenarioEngine",
            ScenarioOutcomes = outcomes,
            EstimatedUncertainty = 0.18,
            SimulatedUtc = DateTime.UtcNow
        };

        return Task.FromResult(result);
    }

    private static string ComputeSnapshotHash(OrganizationalPortfolio portfolio)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(portfolio.PortfolioDecisionHash + ":" + portfolio.PortfolioId)));
    }

    private static string ComputeOutcomesHash(IReadOnlyList<SimulatedScenarioOutcome> outcomes)
    {
        var sb = new StringBuilder();
        foreach (var o in outcomes.OrderBy(x => x.ScenarioName))
        {
            sb.Append($"{o.ScenarioName}:{o.ProjectedPortfolioValue:F2}:{o.ProjectedRiskExposure:F3};");
        }
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString())));
    }
}
