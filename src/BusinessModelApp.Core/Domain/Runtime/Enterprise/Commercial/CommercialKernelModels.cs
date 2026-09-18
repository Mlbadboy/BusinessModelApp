using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Commercial
{
    public enum CommercialStage
    {
        LEAD = 0,
        QUALIFYING = 1,
        QUALIFIED = 2,
        ENGAGED = 3,
        MEETING_REQUESTED = 4,
        MEETING_CONFIRMED = 5,
        DISCOVERY_COMPLETE = 6,
        SOLUTION_PROPOSED = 7,
        PROPOSAL_SUBMITTED = 8,
        NEGOTIATION = 9,
        COMMERCIAL_APPROVAL = 10,
        DEAL_WON = 11,
        CONTRACT_VERIFIED = 12,
        DELIVERY = 13,
        ACCEPTANCE = 14,
        INVOICED = 15,
        PAYMENT_PENDING = 16,
        PAYMENT_VERIFIED = 17,
        REVENUE_REALIZED = 18
    }

    public class CommercialStageAuditRecord
    {
        public string AuditId { get; set; } = Guid.NewGuid().ToString("N");
        public string CommercialEntityId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public CommercialStage FromStage { get; set; }
        public CommercialStage ToStage { get; set; }
        public string InitiatedByAgentId { get; set; } = string.Empty;
        public string AuthoritativeEvidenceId { get; set; } = string.Empty;
        public string EvidenceDigest { get; set; } = string.Empty;
        public string? HumanSignoffId { get; set; }
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        public string PreviousBlockHash { get; set; } = "GENESIS";
        public string CurrentBlockHash { get; set; } = string.Empty;

        public string ComputeHash()
        {
            var raw = $"{PreviousBlockHash}|{TenantId}|{CommercialEntityId}|{FromStage}|{ToStage}|{InitiatedByAgentId}|{AuthoritativeEvidenceId}|{EvidenceDigest}|{HumanSignoffId}|{TimestampUtc:O}";
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }
    }

    public class CommercialStageRecord
    {
        public string CommercialEntityId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string OpportunityId { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
        public CommercialStage CurrentStage { get; set; } = CommercialStage.LEAD;
        public decimal MonetaryExposureINR { get; set; } = 0m;
        public decimal RealizedCashINR { get; set; } = 0m;
        public List<string> CorroboratedEvidenceIds { get; set; } = new();
        public List<CommercialStageAuditRecord> History { get; set; } = new();
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
        public string LatestBlockHash { get; set; } = "GENESIS";
    }

    public class CommercialTransitionRequest
    {
        public string CommercialEntityId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public CommercialStage TargetStage { get; set; }
        public string InitiatingAgentId { get; set; } = string.Empty;
        public string AuthoritativeEvidenceId { get; set; } = string.Empty;
        public string EvidenceDigest { get; set; } = string.Empty;
        public string? HumanSignoffId { get; set; }
        public string TransitionRationale { get; set; } = string.Empty;
        public bool IsBatch6Authorized { get; set; } = false;
        public string? Batch6PermitId { get; set; }
    }

    public class CommercialTransitionResult
    {
        public bool IsSuccess { get; set; }
        public CommercialStage CurrentStage { get; set; }
        public string? RejectionReason { get; set; }
        public List<string> ConstitutionalViolations { get; set; } = new();
        public CommercialStageAuditRecord? AuditRecord { get; set; }
    }
}
