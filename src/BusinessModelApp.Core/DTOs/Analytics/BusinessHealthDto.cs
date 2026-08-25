using System;

namespace BusinessModelApp.Core.DTOs.Analytics
{
    public class BusinessHealthDto
    {
        public string OverallHealth { get; set; }
        public decimal FinancialHealthScore { get; set; }
        public decimal OperationalHealthScore { get; set; }
        public decimal MarketHealthScore { get; set; }
        public decimal CustomerHealthScore { get; set; }
        public decimal GrowthHealthScore { get; set; }
        
        public RiskLevel RiskAssessment { get; set; }
        public string[] Warnings { get; set; }
        public string[] Recommendations { get; set; }
        
        public decimal CashRunway { get; set; } // Months of operation possible with current cash
        public decimal BurnRate { get; set; }
        public decimal CustomerRetentionRate { get; set; }
        public decimal CustomerAcquisitionRate { get; set; }
        public decimal MarketSharePercentage { get; set; }
        public decimal EmployeeProductivity { get; set; }
        public decimal ResourceUtilization { get; set; }
        
        public DateTime AssessmentDate { get; set; }
        public string AssessedBy { get; set; }
        public string[] ImprovementAreas { get; set; }
        public Dictionary<string, decimal> KeyMetrics { get; set; }
    }

    public enum RiskLevel
    {
        Low,
        Moderate,
        High,
        Critical
    }
}
