using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce
{
    public class AgentCostBreakdown
    {
        public decimal ModelInferenceCost { get; set; } = 0m;
        public decimal ToolUsageCost { get; set; } = 0m;
        public decimal BrowserComputeCost { get; set; } = 0m;
        public decimal ApiCost { get; set; } = 0m;
        public decimal InfrastructureCost { get; set; } = 0m;
        public decimal StorageCost { get; set; } = 0m;
        public decimal HumanAttentionCost { get; set; } = 0m;
        public decimal ExternalServicesCost { get; set; } = 0m;

        public decimal TotalOperatingCost =>
            ModelInferenceCost + ToolUsageCost + BrowserComputeCost + ApiCost +
            InfrastructureCost + StorageCost + HumanAttentionCost + ExternalServicesCost;
    }

    public class CostAllocationRecord
    {
        public string AllocationId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string AgentId { get; set; } = string.Empty;
        public string MissionId { get; set; } = string.Empty;
        public string OpportunityId { get; set; } = string.Empty;
        public string CostType { get; set; } = "ModelInference"; // ModelInference, ToolUsage, BrowserCompute, HumanAttention, etc.
        public decimal Amount { get; set; }
        public DateTime IncurredAt { get; set; } = DateTime.UtcNow;
    }

    public class RevenueAttributionRecord
    {
        public string AttributionId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string RevenueRecordId { get; set; } = string.Empty;
        public string OpportunityId { get; set; } = string.Empty;
        public string MissionId { get; set; } = string.Empty;
        public string AgentId { get; set; } = string.Empty;
        public decimal AttributedCollectedRevenue { get; set; }
        public decimal AttributedDeliveryCost { get; set; }
        public decimal AttributedAgentCost { get; set; }
        public decimal AttributedAcquisitionCost { get; set; }
        public decimal AttributedPaymentCost { get; set; }

        public decimal NetContribution =>
            AttributedCollectedRevenue - AttributedDeliveryCost - AttributedAgentCost - AttributedAcquisitionCost - AttributedPaymentCost;

        public decimal ContributionRatio =>
            AttributedAgentCost > 0 ? (NetContribution / AttributedAgentCost) : (NetContribution > 0 ? 1.0m : 0.0m);

        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    }

    public class AgentMetrologyPerformance
    {
        public string AgentId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public decimal ResearchAccuracyPercent { get; set; } = 95.0m;
        public decimal QualificationRatePercent { get; set; } = 80.0m;
        public decimal CostPerQualifiedLead { get; set; } = 150m;
        public decimal CostPerMeeting { get; set; } = 400m;
        public decimal CostPerWonDeal { get; set; } = 1500m;
        public double AverageLatencyMs { get; set; } = 320.0;
        public int RiskIncidentsCount { get; set; } = 0;
        public decimal RoutingScore { get; set; } = 0.92m; // Feeds task assignment/routing only
        public DateTime LastEvaluatedAt { get; set; } = DateTime.UtcNow;
    }

    public class EconomicOutcomeRecord
    {
        public string OutcomeId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string ObjectiveId { get; set; } = string.Empty;
        public decimal TotalCollectedRevenue { get; set; }
        public decimal TotalDeliveryCost { get; set; }
        public decimal TotalWorkforceCost { get; set; }
        public decimal NetGrossMargin { get; set; }
        public decimal RealizedROI { get; set; } // (CollectedRevenue - TotalCosts) / TotalCosts
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    }
}
