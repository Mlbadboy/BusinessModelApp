using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce
{
    public enum BuyingCenterRole
    {
        UNKNOWN = 0,
        DECISION_MAKER = 1,
        INFLUENCER = 2,
        CHAMPION = 3,
        PROCUREMENT = 4,
        END_USER = 5,
        BLOCKER = 6
    }

    public class BuyingCenterPersona
    {
        public string PersonaId { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public BuyingCenterRole Role { get; set; } = BuyingCenterRole.UNKNOWN;
        public int InfluenceScore { get; set; } = 5; // 1 to 10
        public string PainPointDescription { get; set; } = string.Empty;
        public List<string> CorroboratingEvidenceIds { get; set; } = new();
        public bool IsGroundedInEvidence => CorroboratingEvidenceIds.Count > 0;
    }

    public class AccountGraph
    {
        public string AccountId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public string Geography { get; set; } = string.Empty;
        public List<BuyingCenterPersona> Personas { get; set; } = new();
        public List<string> IdentifiedPainPoints { get; set; } = new();
        public DateTime LastAuditedAt { get; set; } = DateTime.UtcNow;
    }

    public class GovernedCrmMutation
    {
        public string MutationId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string AgentId { get; set; } = string.Empty;
        public string EntityType { get; set; } = "Opportunity"; // Lead, Account, Contact, Opportunity, Deal, Customer
        public string MutationType { get; set; } = "CREATE"; // CREATE, UPDATE, STATUS_CHANGE
        public string EntityId { get; set; } = string.Empty;
        public string PayloadJson { get; set; } = "{}";
        public bool IsBatch6Authorized { get; set; } = false;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class ImmutableExternalEffect
    {
        public string EffectId { get; set; } = Guid.NewGuid().ToString("N");
        public string Connector { get; set; } = string.Empty; // e.g. "EmailConnector", "CrmConnector", "StripeConnector"
        public string Provider { get; set; } = string.Empty;
        public string RequestHash { get; set; } = string.Empty;
        public string IdempotencyKey { get; set; } = string.Empty;
        public string ExecutionAttemptId { get; set; } = string.Empty;
        public DateTime ObservedAt { get; set; } = DateTime.UtcNow;
        public string ProviderReference { get; set; } = string.Empty;
        public string EffectState { get; set; } = "SUCCEEDED"; // SUCCEEDED, UNKNOWN_EFFECT, FAILED
        public string VerificationEvidence { get; set; } = string.Empty;
    }

    public class CommercialLineageNode
    {
        public string NodeId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string BusinessObjectiveId { get; set; } = string.Empty;
        public string RevenueObjectiveId { get; set; } = string.Empty;
        public string OpportunityId { get; set; } = string.Empty;
        public string ResponsibilityId { get; set; } = string.Empty;
        public string WorkProposalId { get; set; } = string.Empty;
        public string MissionId { get; set; } = string.Empty;
        public string ActionId { get; set; } = string.Empty;
        public string ExternalEffectId { get; set; } = string.Empty;
        public string OutcomeId { get; set; } = string.Empty;
        public string DealId { get; set; } = string.Empty;
        public string InvoiceId { get; set; } = string.Empty;
        public string PaymentId { get; set; } = string.Empty;
        public string RevenueRecordId { get; set; } = string.Empty;

        public string PreviousBlockHash { get; set; } = "GENESIS";
        public string CurrentBlockHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string ComputeHash()
        {
            var raw = $"{PreviousBlockHash}|{TenantId}|{BusinessObjectiveId}|{RevenueObjectiveId}|{OpportunityId}|{MissionId}|{ExternalEffectId}|{DealId}|{InvoiceId}|{PaymentId}|{RevenueRecordId}";
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(bytes);
        }
    }
}
