using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce
{
    public enum CommercialTruthState
    {
        PROSPECTED = 0,
        QUALIFIED = 1,
        VERIFIED = 2,
        ENGAGED = 3,
        MEETING_CONFIRMED = 4,
        PROPOSAL_SUBMITTED = 5,
        NEGOTIATION_ACTIVE = 6,
        DEAL_VERIFIED = 7,
        CONTRACT_VERIFIED = 8,
        DELIVERY_ACCEPTED = 9,
        INVOICE_VERIFIED = 10,
        PAYMENT_VERIFIED = 11,
        REVENUE_REALIZED = 12
    }

    public class CommercialEvidenceItem
    {
        public string EvidenceId { get; set; } = Guid.NewGuid().ToString("N");
        public string EvidenceType { get; set; } = string.Empty; // EmailHeader, CrmRecord, SignedPdf, PaymentGatewayWebhook, BankStatement
        public string SourceSystem { get; set; } = string.Empty;
        public string PayloadHash { get; set; } = string.Empty;
        public decimal CorroborationConfidence { get; set; } = 1.0m;
        public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;
        public string VerifiedBy { get; set; } = "ExternalSystem";
    }

    public class CommercialFact
    {
        public string FactId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string OpportunityId { get; set; } = string.Empty;
        public CommercialTruthState State { get; set; } = CommercialTruthState.PROSPECTED;
        public string ClaimedDescription { get; set; } = string.Empty;
        public bool IsVerified { get; set; } = false;
        public List<CommercialEvidenceItem> CorroboratingEvidence { get; set; } = new();
        public DateTime StateUpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class BusinessSopStage
    {
        public int StageOrder { get; set; }
        public string StageName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> RequiredTools { get; set; } = new();
        public List<string> RequiredEvidenceTypes { get; set; } = new();
        public int RiskLevel { get; set; } = 1;
    }

    public class BusinessSopDefinition
    {
        public string SopId { get; set; } = string.Empty; // "LEAD_QUALIFICATION_SOP", "SALES_SOP", "DELIVERY_SOP", "COLLECTION_SOP"
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<BusinessSopStage> Stages { get; set; } = new();
        public bool IsCertified { get; set; } = true;
    }

    public class CompiledWorkProposal
    {
        public string ProposalId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string SopId { get; set; } = string.Empty;
        public string OpportunityId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public List<string> PlannedMissionTitles { get; set; } = new();
        public decimal EstimatedTotalCost { get; set; }
        public int MaxRiskLevel { get; set; }
        public bool RequiresHumanApproval => MaxRiskLevel >= 3;
        public DateTime CompiledAt { get; set; } = DateTime.UtcNow;
    }
}
