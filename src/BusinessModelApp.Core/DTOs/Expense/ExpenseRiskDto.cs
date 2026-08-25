using System;

namespace BusinessModelApp.Core.DTOs.Expense
{
    public class ExpenseRiskDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public RiskType Type { get; set; }
        public RiskSeverity Severity { get; set; }
        public string Category { get; set; }  // Cost Overrun, Vendor, Compliance, etc.
        
        // Risk Metrics
        public decimal PotentialImpact { get; set; }  // In currency value
        public decimal Probability { get; set; }  // 0-1
        public decimal RiskScore { get; set; }  // Calculated from Impact and Probability
        public string ImpactArea { get; set; }  // Budget, Operations, Compliance
        
        // Affected Areas
        public string[] AffectedCategories { get; set; }
        public string[] AffectedDepartments { get; set; }
        public decimal PercentageOfBudgetAtRisk { get; set; }
        
        // Mitigation
        public string MitigationStrategy { get; set; }
        public decimal MitigationCost { get; set; }
        public string MitigationStatus { get; set; }  // "Not Started", "In Progress", "Completed"
        public DateTime? MitigationDeadline { get; set; }
        public string[] PreventiveMeasures { get; set; }
        
        // Monitoring
        public string[] EarlyWarningIndicators { get; set; }
        public string[] ControlMeasures { get; set; }
        public decimal ThresholdValue { get; set; }
        public string MonitoringFrequency { get; set; }  // Daily, Weekly, Monthly
        
        // Time Frame
        public DateTime IdentificationDate { get; set; }
        public DateTime? ExpectedResolutionDate { get; set; }
        public string TimeHorizon { get; set; }  // Short-term, Medium-term, Long-term
        
        // Status and Tracking
        public bool IsActive { get; set; }
        public string CurrentStatus { get; set; }
        public decimal CompletionPercentage { get; set; }
        public string[] Stakeholders { get; set; }
        
        // Meta Information
        public string Owner { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
    }

    public enum RiskType
    {
        CostOverrun,
        VendorRelated,
        Compliance,
        Operational,
        Strategic,
        Financial,
        External,
        Internal
    }

    public enum RiskSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }
}
