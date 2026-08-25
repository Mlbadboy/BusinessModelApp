using System;

namespace BusinessModelApp.Core.DTOs.Revenue
{
    public class RevenueRiskDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public RiskType Type { get; set; }
        public RiskSeverity Severity { get; set; }
        
        // Risk Assessment
        public decimal PotentialImpact { get; set; }  // In currency value
        public decimal Probability { get; set; }  // 0-1
        public decimal RiskScore { get; set; }  // Calculated from Impact and Probability
        public string Category { get; set; }  // Market, Competition, Operation, etc.
        
        // Risk Details
        public string[] AffectedRevenueSources { get; set; }
        public string[] AffectedCustomerSegments { get; set; }
        public decimal PercentageOfRevenueAtRisk { get; set; }
        public string TimeHorizon { get; set; }  // Short-term, Medium-term, Long-term
        
        // Mitigation
        public string MitigationStrategy { get; set; }
        public decimal MitigationCost { get; set; }
        public string MitigationStatus { get; set; }  // Not Started, In Progress, Completed
        public DateTime? MitigationDeadline { get; set; }
        
        // Monitoring
        public string[] EarlyWarningIndicators { get; set; }
        public string[] MonitoringMetrics { get; set; }
        public decimal CurrentStatusScore { get; set; }  // 0-100
        
        // Meta Information
        public string Owner { get; set; }
        public DateTime IdentificationDate { get; set; }
        public DateTime LastAssessmentDate { get; set; }
        public DateTime NextReviewDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
    }

    public enum RiskType
    {
        Market,
        Competition,
        Operational,
        Financial,
        Regulatory,
        Technology,
        Strategic,
        External
    }

    public enum RiskSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }
}
