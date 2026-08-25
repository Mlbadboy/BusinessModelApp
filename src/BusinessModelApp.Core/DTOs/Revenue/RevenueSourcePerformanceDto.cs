using System;

namespace BusinessModelApp.Core.DTOs.Revenue
{
    public class RevenueSourcePerformanceDto
    {
        public int Id { get; set; }
        public string SourceName { get; set; }
        public string Name { get => SourceName; set => SourceName = value; }
        public string Category { get; set; }
        
        // Performance Metrics
        public decimal CurrentRevenue { get; set; }
        public decimal Revenue { get => CurrentRevenue; set => CurrentRevenue = value; }
        public decimal PreviousRevenue { get; set; }
        public decimal Target { get; set; }
        public decimal GrowthRate { get; set; }
        public decimal ContributionPercentage { get; set; }
        
        // Performance Indicators
        public decimal ProfitMargin { get; set; }
        public decimal CustomerRetentionRate { get; set; }
        public decimal CustomerAcquisitionCost { get; set; }
        public decimal AverageRevenuePerCustomer { get; set; }
        
        // Health Metrics
        public string PerformanceStatus { get; set; }  // "Strong", "Stable", "Declining"
        public string Status { get => PerformanceStatus; set => PerformanceStatus = value; }
        public decimal HealthScore { get; set; }  // 0-100
        public string[] Strengths { get; set; }
        public string[] WeakPoints { get; set; }
        
        // Trends
        public string TrendDirection { get; set; }
        public decimal MonthlyGrowthRate { get; set; }
        public decimal QuarterlyGrowthRate { get; set; }
        public decimal YearlyGrowthRate { get; set; }
        
        // Forecasting
        public decimal ProjectedRevenue { get; set; }
        public decimal ProjectedGrowth { get; set; }
        public decimal ForecastConfidence { get; set; }  // 0-100
        
        // Period Information
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string TimePeriod { get; set; }  // "Monthly", "Quarterly", "Yearly"
        
        // Meta Information
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
    }
}
