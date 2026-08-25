using System;

namespace BusinessModelApp.Core.DTOs.Expense
{
    public class ExpenseTrendDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ExpenseType Category { get; set; }
        
        // Trend Data Points
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public decimal ChangePercentage { get; set; }
        public string TrendDirection { get; set; }  // "Increasing", "Decreasing", "Stable"
        
        // Comparative Analysis
        public decimal PreviousPeriodAmount { get; set; }
        public decimal YearAgoAmount { get; set; }
        public decimal BudgetedAmount { get; set; }
        public decimal Variance { get; set; }
        
        // Pattern Analysis
        public bool IsRecurring { get; set; }
        public string Frequency { get; set; }  // "Daily", "Weekly", "Monthly", etc.
        public bool IsSeasonalTrend { get; set; }
        public string SeasonalPattern { get; set; }
        
        // Forecasting
        public decimal ProjectedAmount { get; set; }
        public decimal ForecastAccuracy { get; set; }  // Percentage
        public string[] InfluencingFactors { get; set; }
        public decimal ConfidenceLevel { get; set; }  // 0-100
        
        // Cost Management
        public string CostDriver { get; set; }
        public decimal CostPerUnit { get; set; }
        public decimal VolumeMetric { get; set; }
        public string[] CostReductionOpportunities { get; set; }
        
        // Period Information
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public string TimePeriod { get; set; }  // "Daily", "Weekly", "Monthly", "Quarterly", "Yearly"
        
        // Analysis and Insights
        public string[] KeyObservations { get; set; }
        public string[] RecommendedActions { get; set; }
        public decimal ImpactScore { get; set; }  // 0-100
        
        // Meta Information
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
    }
}
