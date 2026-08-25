using System;

namespace BusinessModelApp.Core.DTOs.Strategy
{
    public class StrategyOpportunityDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public OpportunityType Type { get; set; }
        public string Category { get; set; }  // Market, Technology, Partnership, etc.
        
        public decimal PotentialRevenue { get; set; }
        public decimal EstimatedCost { get; set; }
        public decimal ROIEstimate { get; set; }
        public int TimeToRealizationMonths { get; set; }
        
        public decimal PriorityScore { get; set; }  // 0-100
        public decimal FeasibilityScore { get; set; }  // 0-100
        public decimal ImpactScore { get; set; }  // 0-100
        
        public string[] RequiredResources { get; set; }
        public string[] Prerequisites { get; set; }
        public string[] Stakeholders { get; set; }
        
        public string Status { get; set; }  // "Identified", "Under Review", "Approved", "In Progress", "Completed"
        public string Implementation { get; set; }
        public decimal ProgressPercentage { get; set; }
        
        public DateTime IdentificationDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? CompletionDate { get; set; }
        
        public string Owner { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
    }

    public enum OpportunityType
    {
        Growth,
        Optimization,
        Innovation,
        Partnership,
        Market,
        Technology,
        Operational
    }
}
