using System;

namespace BusinessModelApp.Core.DTOs.Expense
{
    public class ExpenseOptimizationDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public OptimizationType Type { get; set; }
        public string Category { get; set; }  // Cost Reduction, Process Improvement, etc.
        
        // Financial Impact
        public decimal CurrentCost { get; set; }
        public decimal ProjectedSavings { get; set; }
        public decimal ImplementationCost { get; set; }
        public decimal ROI { get; set; }
        public int PaybackPeriodMonths { get; set; }
        
        // Implementation Details
        public string ImplementationStrategy { get; set; }
        public string[] RequiredResources { get; set; }
        public string[] Prerequisites { get; set; }
        public string[] Stakeholders { get; set; }
        public decimal ImplementationProgress { get; set; }  // 0-100
        
        // Feasibility
        public decimal FeasibilityScore { get; set; }  // 0-100
        public string[] Constraints { get; set; }
        public string[] Risks { get; set; }
        public decimal SuccessProbability { get; set; }  // 0-1
        
        // Timeline
        public DateTime? PlannedStartDate { get; set; }
        public DateTime? ExpectedCompletionDate { get; set; }
        public string[] Milestones { get; set; }
        public string[] Dependencies { get; set; }
        
        // Impact Areas
        public string[] AffectedDepartments { get; set; }
        public string[] AffectedProcesses { get; set; }
        public string[] BusinessImpact { get; set; }
        
        // Monitoring and Metrics
        public string[] SuccessMetrics { get; set; }
        public string[] MonitoringMethods { get; set; }
        public string ReviewFrequency { get; set; }  // Weekly, Monthly, Quarterly
        
        // Status
        public string Status { get; set; }  // Proposed, Approved, In Progress, Completed
        public string[] Achievements { get; set; }
        public string[] Challenges { get; set; }
        public decimal ActualSavingsAchieved { get; set; }
        
        // Meta Information
        public string Owner { get; set; }
        public int Priority { get; set; }  // 1-5
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
    }

    public enum OptimizationType
    {
        CostReduction,
        ProcessImprovement,
        ResourceOptimization,
        VendorConsolidation,
        AutomationInitiative,
        DigitalTransformation,
        OutsourcingStrategy,
        RenegotiationOpportunity
    }
}
