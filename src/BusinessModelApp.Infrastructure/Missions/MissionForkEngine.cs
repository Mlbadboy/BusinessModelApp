using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Missions;

namespace BusinessModelApp.Infrastructure.Missions
{
    public class MissionForkEngine : IMissionForkEngine
    {
        private readonly IMissionEventStore _eventStore;

        public MissionForkEngine(IMissionEventStore eventStore)
        {
            _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        }

        public async Task<MissionForkResult> CreateForkAsync(Guid parentMissionId, CounterfactualScenario scenario)
        {
            if (scenario == null) throw new ArgumentNullException(nameof(scenario));

            var projection = await _eventStore.ReplayMissionStateAsync(parentMissionId);

            decimal targetRevenue = projection.TargetRevenueINR > 0 ? projection.TargetRevenueINR : 5000000m;
            decimal baseBudget = projection.TotalAllocatedBudgetINR > 0 ? projection.TotalAllocatedBudgetINR : 50000m;

            // Counterfactual calculations
            decimal effectiveDealSize = (decimal)scenario.DealSizeMultiplier;
            decimal effectiveConversion = (decimal)scenario.ConversionRateMultiplier;
            decimal simulatedRevenue = Math.Round(targetRevenue * effectiveDealSize * effectiveConversion, 2);
            decimal simulatedGap = Math.Max(0m, targetRevenue - simulatedRevenue);

            double requiredOutreachMultiplier = scenario.ConversionRateMultiplier > 0
                ? Math.Round(1.0 / scenario.ConversionRateMultiplier, 2)
                : 10.0;

            // Probability model: Base 65% scaled by conversion and budget multipliers
            double rawProb = 0.65 * scenario.ConversionRateMultiplier * Math.Min(1.0, scenario.BudgetMultiplier);
            double estimatedProb = Math.Clamp(Math.Round(rawProb, 4), 0.05, 0.99);

            bool isFeasible = scenario.BudgetMultiplier >= 0.50 && scenario.ConversionRateMultiplier >= 0.50;

            return new MissionForkResult
            {
                ForkId = Guid.NewGuid(),
                ParentMissionId = parentMissionId,
                ForkedAtSequenceNumber = projection.LastSequenceNumber,
                Scenario = scenario,
                SimulatedRevenueINR = simulatedRevenue,
                SimulatedGapINR = simulatedGap,
                EstimatedSuccessProbability = estimatedProb,
                RequiredOutreachMultiplier = requiredOutreachMultiplier,
                IsFeasibleWithinBudget = isFeasible,
                CreatedAtUtc = DateTime.UtcNow
            };
        }

        public async Task<ForkComparisonReport> CompareForksAsync(Guid parentMissionId, List<CounterfactualScenario> scenarios)
        {
            if (scenarios == null) throw new ArgumentNullException(nameof(scenarios));

            var projection = await _eventStore.ReplayMissionStateAsync(parentMissionId);
            var forks = new List<MissionForkResult>();

            foreach (var scenario in scenarios)
            {
                var fork = await CreateForkAsync(parentMissionId, scenario);
                forks.Add(fork);
            }

            var ranked = forks.OrderByDescending(f => f.EstimatedSuccessProbability).ToList();
            var best = ranked.FirstOrDefault(f => f.IsFeasibleWithinBudget) ?? ranked.FirstOrDefault();

            string recommendation = best != null
                ? $"Scenario '{best.Scenario.Name}' selected with {best.EstimatedSuccessProbability:P1} probability and INR {best.SimulatedRevenueINR:N0} simulated revenue."
                : "No feasible counterfactual scenario identified.";

            return new ForkComparisonReport
            {
                ParentMissionId = parentMissionId,
                BaselineTargetRevenueINR = projection.TargetRevenueINR,
                EvaluatedForks = ranked,
                RecommendationSummary = recommendation,
                GeneratedAtUtc = DateTime.UtcNow
            };
        }
    }
}
