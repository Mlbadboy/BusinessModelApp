using System;

namespace BusinessModelApp.Core.DTOs.Strategy
{
    public class StrategyRiskDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public RiskSeverity Severity { get; set; }
        public RiskProbability Probability { get; set; }
        public decimal ImpactScore { get; set; }  // 0-100
        public string Category { get; set; }  // e.g., "Financial", "Operational", "Market", etc.
        
        public string MitigationStrategy { get; set; }
        public decimal MitigationCost { get; set; }
        public string MitigationStatus { get; set; }  // "Not Started", "In Progress", "Completed"
        
        public DateTime IdentificationDate { get; set; }
        public DateTime? ResolutionDate { get; set; }
        public string Owner { get; set; }
        
        public bool IsActive { get; set; }
        public string[] AffectedAreas { get; set; }
        public decimal RiskScore { get; set; }  // Calculated based on Severity and Probability
        
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
    }

    public enum RiskSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum RiskProbability
    {
        VeryLow,
        Low,
        Medium,
        High,
        VeryHigh
    }
}
