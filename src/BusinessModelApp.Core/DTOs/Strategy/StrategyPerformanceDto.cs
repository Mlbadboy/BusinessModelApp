using System.Collections.Generic;

namespace BusinessModelApp.Core.DTOs.Strategy
{
    public class StrategyPerformanceDto
    {
        public string GoalName { get; set; }
        public decimal Progress { get; set; }
        public List<StrategyMetricDto> KeyMetrics { get; set; }
    }
}
