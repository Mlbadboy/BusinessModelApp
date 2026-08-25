using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.DTOs.Analytics
{
    public class RevenueAnalysisDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal TargetRevenue { get; set; }
        public decimal RevenueVariance { get; set; }
        public decimal GrowthRate { get; set; }
        public decimal MonthlyGrowthRate { get; set; }
        public decimal QuarterlyGrowthRate { get; set; }
        public decimal YearlyGrowthRate { get; set; }
        public Dictionary<string, decimal> RevenueBySource { get; set; } = new();
        public Dictionary<string, decimal> RevenueByChannel { get; set; } = new();
        public Dictionary<string, decimal> RevenueByCustomerSegment { get; set; } = new();
        public IEnumerable<RevenueTrendDto> RevenueTrends { get; set; } = new List<RevenueTrendDto>();
        public IEnumerable<RevenueSourcePerformanceDto> SourcePerformance { get; set; } = new List<RevenueSourcePerformanceDto>();
    }
}
