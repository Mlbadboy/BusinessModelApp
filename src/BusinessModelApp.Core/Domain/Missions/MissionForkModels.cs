using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessModelApp.Core.Domain.Missions
{
    public class CounterfactualScenario
    {
        public string ScenarioId { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = string.Empty;
        public double ConversionRateMultiplier { get; set; } = 1.0; // 0.8 = -20%
        public double DealSizeMultiplier { get; set; } = 1.0;        // 1.2 = +20%
        public double BudgetMultiplier { get; set; } = 1.0;          // 0.7 = -30%
        public List<string> ChangedAssumptions { get; set; } = new();
    }

    public class MissionForkResult
    {
        public Guid ForkId { get; set; } = Guid.NewGuid();
        public Guid ParentMissionId { get; set; }
        public long ForkedAtSequenceNumber { get; set; }
        public CounterfactualScenario Scenario { get; set; } = new();
        public decimal SimulatedRevenueINR { get; set; }
        public decimal SimulatedGapINR { get; set; }
        public double EstimatedSuccessProbability { get; set; }
        public double RequiredOutreachMultiplier { get; set; }
        public bool IsFeasibleWithinBudget { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public class ForkComparisonReport
    {
        public Guid ParentMissionId { get; set; }
        public decimal BaselineTargetRevenueINR { get; set; }
        public List<MissionForkResult> EvaluatedForks { get; set; } = new();
        public string RecommendationSummary { get; set; } = string.Empty;
        public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public interface IMissionForkEngine
    {
        Task<MissionForkResult> CreateForkAsync(Guid parentMissionId, CounterfactualScenario scenario);
        Task<ForkComparisonReport> CompareForksAsync(Guid parentMissionId, List<CounterfactualScenario> scenarios);
    }
}
