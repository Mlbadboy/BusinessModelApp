using System;

namespace BusinessModelApp.Core.DTOs.Strategy
{
    public class StrategyPerformanceTrendDto
    {
        public DateTime Date { get; set; }
        public decimal KpiValue { get; set; }
        public string KpiName { get; set; } = string.Empty;
    }
}
