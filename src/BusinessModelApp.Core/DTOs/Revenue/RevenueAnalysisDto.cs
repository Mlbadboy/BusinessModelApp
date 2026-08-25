using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.DTOs.Revenue
{
    public class RevenueAnalysisDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime AnalysisDate { get; set; }
        
        // Revenue Overview
        public decimal TotalRevenue { get; set; }
        public decimal RecurringRevenue { get; set; }
        public decimal OneTimeRevenue { get; set; }
        public decimal ProjectedRevenue { get; set; }
        
        // Growth Metrics
        public decimal YearOverYearGrowth { get; set; }
        public decimal QuarterOverQuarterGrowth { get; set; }
        public decimal MonthOverMonthGrowth { get; set; }
        
        // Revenue Sources
        public ICollection<RevenueSourcePerformanceDto> SourcePerformance { get; set; }
        public IDictionary<string, decimal> RevenueBreakdown { get; set; }
        public ICollection<RevenueTrendDto> RevenueTrends { get; set; }
        public IDictionary<string, decimal> RevenueBySource { get; set; }
        
        // Customer Metrics
        public decimal AverageRevenuePerUser { get; set; }
        public decimal CustomerLifetimeValue { get; set; }
        public decimal ChurnRate { get; set; }
        
        // Financial Health
        public decimal ProfitMargin { get; set; }
        public decimal GrossMargin { get; set; }
        public decimal OperatingMargin { get; set; }
        
        // Trends and Forecasts
        public ICollection<RevenueTrendDto> Trends { get; set; }
        public decimal ForecastAccuracy { get; set; }
        public IDictionary<string, decimal> Forecasts { get; set; }
        
        // Risk Analysis
        public ICollection<RevenueRiskDto> Risks { get; set; }
        public decimal RiskScore { get; set; }
        
        // Opportunities
        public ICollection<RevenueOpportunityDto> Opportunities { get; set; }
        public decimal OpportunityValue { get; set; }
        
        // Meta Information
        public string AnalyzedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string Status { get; set; }
    }
}
