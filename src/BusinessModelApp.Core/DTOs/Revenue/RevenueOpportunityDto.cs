using System;

namespace BusinessModelApp.Core.DTOs.Revenue
{
    public class RevenueOpportunityDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public OpportunityType Type { get; set; }
        public string Category { get; set; }  // Market Expansion, Product Innovation, etc.
        
        // Financial Impact
        public decimal PotentialRevenue { get; set; }
        public decimal InitialInvestment { get; set; }
        public decimal OngoingCosts { get; set; }
        public decimal ExpectedROI { get; set; }
        public decimal PaybackPeriodMonths { get; set; }
        
        // Implementation
        public string ImplementationStrategy { get; set; }
        public string[] RequiredResources { get; set; }
        public string[] Prerequisites { get; set; }
        public decimal ImplementationProgress { get; set; }  // 0-100
        public string Status { get; set; }  // Identified, Under Review, Approved, In Progress
        
        // Risk Assessment
        public decimal SuccessProbability { get; set; }  // 0-1
        public string[] RiskFactors { get; set; }
        public decimal RiskScore { get; set; }  // 0-100
        
        // Market Analysis
        public string[] TargetMarkets { get; set; }
        public int EstimatedCustomerReach { get; set; }
        public decimal MarketSize { get; set; }
        public decimal MarketSharePotential { get; set; }  // 0-1
        
        // Timeline
        public DateTime? PlannedStartDate { get; set; }
        public DateTime? ExpectedCompletionDate { get; set; }
        public int EstimatedTimeToMarketMonths { get; set; }
        public string[] Milestones { get; set; }
        
        // Stakeholders
        public string Owner { get; set; }
        public string[] KeyStakeholders { get; set; }
        public string[] RequiredApprovals { get; set; }
        
        // Meta Information
        public bool IsActive { get; set; }
        public int Priority { get; set; }  // 1-5
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
    }

    public enum OpportunityType
    {
        MarketExpansion,
        ProductInnovation,
        ChannelDevelopment,
        CustomerSegment,
        PricingOptimization,
        StrategicPartnership,
        DigitalTransformation,
        ServiceEnhancement
    }
}
