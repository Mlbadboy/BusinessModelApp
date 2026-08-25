using System;

namespace BusinessModelApp.Core.DTOs.Revenue
{
    public class RevenueTrendDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }  // e.g., "Product Revenue", "Service Revenue", etc.
        
        // Trend Data Points
        public DateTime Date { get; set; }
        public decimal Value { get; set; }
        public decimal ChangePercentage { get; set; }
        public string TrendDirection { get; set; }  // "Up", "Down", "Stable"
        
        // Comparative Metrics
        public decimal PreviousPeriodValue { get; set; }
        public decimal YearAgoValue { get; set; }
        public decimal Variance { get; set; }
        public decimal VariancePercentage { get; set; }
        
        // Analysis
        public string Insight { get; set; }
        public decimal Confidence { get; set; }  // 0-100
        public string[] ContributingFactors { get; set; }
        public string[] RecommendedActions { get; set; }
        
        // Seasonality
        public bool IsSeasonalTrend { get; set; }
        public string SeasonalPattern { get; set; }
        public decimal SeasonalityImpact { get; set; }  // 0-1
        
        // Period Information
        public string TimePeriod { get; set; }  // "Daily", "Weekly", "Monthly", "Quarterly", "Yearly"
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        
        // Meta Information
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
    }
}
