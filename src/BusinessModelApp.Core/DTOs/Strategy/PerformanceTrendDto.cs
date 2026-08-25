using System;

namespace BusinessModelApp.Core.DTOs.Strategy
{
    public class PerformanceTrendDto
    {
        public int Id { get; set; }
        public string MetricName { get; set; }
        public DateTime Date { get; set; }
        public decimal Value { get; set; }
        public string TrendDirection { get; set; }  // "Up", "Down", "Stable"
        public decimal PercentageChange { get; set; }
        public string Period { get; set; }  // "Daily", "Weekly", "Monthly", "Quarterly", "Yearly"
        public decimal Benchmark { get; set; }
        public decimal VarianceFromBenchmark { get; set; }
        public string Analysis { get; set; }
        public int Confidence { get; set; }  // 0-100
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
    }
}
