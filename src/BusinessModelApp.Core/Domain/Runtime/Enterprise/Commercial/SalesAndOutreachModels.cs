using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Commercial
{
    public class CommercialAccountStrategy
    {
        public string StrategyId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
        public string OpportunityId { get; set; } = string.Empty;
        public string ProblemStatement { get; set; } = string.Empty;
        public List<string> EvidenceIds { get; set; } = new();
        public string BusinessImpactSummary { get; set; } = string.Empty;
        public string DecisionMakersSummary { get; set; } = string.Empty;
        public string ValueHypothesis { get; set; } = string.Empty;
        public string ProposedSolutionDescription { get; set; } = string.Empty;
        public string CompetitiveContext { get; set; } = string.Empty;
        public List<string> AnticipatedObjections { get; set; } = new();
        public List<string> KeyRisks { get; set; } = new();
        public string RecommendedNextAction { get; set; } = string.Empty;
        public string CreatedByAgentId { get; set; } = string.Empty;
        public bool IsAgentSelfAuthorized => false; // Law I40: Agent recommends, never self-authorizes
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public enum CommercialOutreachChannel
    {
        EMAIL = 1,
        WHATSAPP = 2,
        LINKEDIN = 3,
        CALENDAR = 4,
        CRM = 5,
        WEB = 6,
        VOICE = 7
    }

    public class CommercialCommunicationIntent
    {
        public string IntentId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string OpportunityId { get; set; } = string.Empty;
        public string InitiatingAgentId { get; set; } = string.Empty;
        public CommercialOutreachChannel Channel { get; set; } = CommercialOutreachChannel.EMAIL;
        public string RecipientAddress { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string BodyContent { get; set; } = string.Empty;
        public int RiskTier { get; set; } = 1; // 1 to 5
        public bool RequiresHumanApproval => RiskTier >= 3; // R3+ mandates PRG-1 human gate
        public bool IsHumanApproved { get; set; } = false;
        public string? HumanSignoffId { get; set; }
        public bool IsBatch6Authorized { get; set; } = false;
        public string? Batch6PermitId { get; set; }
        public bool IsDispatched { get; set; } = false;
        public string? ExternalEffectId { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
