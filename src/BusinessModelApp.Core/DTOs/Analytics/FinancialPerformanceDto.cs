using System;

namespace BusinessModelApp.Core.DTOs.Analytics
{
    public class FinancialPerformanceDto
    {
        public decimal Revenue { get; set; }
        public decimal Expenses { get; set; }
        public decimal NetProfit { get; set; }
        public decimal GrossMargin { get; set; }
        public decimal OperatingMargin { get; set; }
        public decimal ProfitMargin { get; set; }
        public decimal CashFlow { get; set; }
        public decimal ROI { get; set; }
        public decimal ROCE { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public decimal YearOverYearGrowth { get; set; }
        public decimal QuarterOverQuarterGrowth { get; set; }
        public string TrendDirection { get; set; }
        public string HealthIndicator { get; set; }
        public decimal BreakEvenPoint { get; set; }
        public int PaybackPeriodMonths { get; set; }
    }
}
