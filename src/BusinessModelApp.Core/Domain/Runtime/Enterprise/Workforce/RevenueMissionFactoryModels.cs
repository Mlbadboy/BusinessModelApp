using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce
{
    public enum RevenueMissionTemplateType
    {
        QUALIFY_ACCOUNT = 1,
        RESEARCH_BUYING_CENTER = 2,
        PREPARE_OUTREACH = 3,
        FOLLOW_UP = 4,
        BOOK_MEETING = 5,
        PREPARE_PROPOSAL = 6,
        NEGOTIATE = 7,
        CUSTOMER_ONBOARDING = 8,
        DELIVER_PROJECT = 9,
        INVOICE = 10,
        COLLECTION = 11,
        CUSTOMER_SUCCESS = 12
    }

    public class RevenueMissionSpecification
    {
        public string MissionId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string OpportunityId { get; set; } = string.Empty;
        public RevenueMissionTemplateType TemplateType { get; set; }
        public string Title { get; set; } = string.Empty;
        public string AssignedRoleTitle { get; set; } = "RevenueProspector";
        public List<string> RequiredCapabilities { get; set; } = new();
        public List<string> RequiredTools { get; set; } = new();
        public int RiskTier { get; set; } = 2;
        public decimal AllocatedBudget { get; set; } = 50m;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class RevenueControlPlaneState
    {
        public string TenantId { get; set; } = string.Empty;
        public string ActiveRevenueObjectiveId { get; set; } = string.Empty;
        public decimal TargetRevenue { get; set; }
        public decimal QualifiedPipelineAmount { get; set; }
        public decimal WeightedPipelineAmount { get; set; }
        public decimal ClosedWonRevenue { get; set; }
        public decimal InvoicedRevenue { get; set; }
        public decimal CollectedCashRevenue { get; set; }
        public decimal RealizedGrossMargin { get; set; }
        public int ActiveCommercialMissionsCount { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
