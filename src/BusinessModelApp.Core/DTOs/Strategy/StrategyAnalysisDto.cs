using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.DTOs.Strategy
{
    public class StrategyAnalysisDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime AnalysisDate { get; set; }
        public string Status { get; set; }
        
        // Strategy Performance Metrics
        public decimal ImplementationProgress { get; set; }
        public decimal SuccessRate { get; set; }
        public decimal ROI { get; set; }
        public decimal MarketImpact { get; set; }
        public decimal CustomerImpact { get; set; }
        public decimal GoalCompletionRate { get; set; }
        public decimal ActionCompletionRate { get; set; }
        public decimal ResourceUtilization { get; set; }
        public decimal ImpactScore { get; set; }
        
        // Trends and Patterns
        public ICollection<PerformanceTrendDto> Trends { get; set; }
        public ICollection<PerformanceTrendDto> PerformanceTrends { get; set; }
        public ICollection<string> KeyFindings { get; set; }
        
        // Risk and Opportunity Assessment
        public ICollection<StrategyRiskDto> Risks { get; set; }
        public ICollection<StrategyOpportunityDto> Opportunities { get; set; }
        
        // Resource Allocation
        public IDictionary<string, decimal> ResourceAllocation { get; set; }
        public decimal BudgetUtilization { get; set; }
        
        // Timeline and Milestones
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public ICollection<string> CompletedMilestones { get; set; }
        public ICollection<string> UpcomingMilestones { get; set; }
        
        // Recommendations
        public ICollection<string> Recommendations { get; set; }
        public ICollection<string> ActionItems { get; set; }
        
        // Meta Information
        public string AnalyzedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
