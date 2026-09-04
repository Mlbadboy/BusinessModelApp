using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessModelApp.Core.Domain.Execution
{
    public enum ApprovalStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2,
        Expired = 3,
        Invalidated = 4
    }

    [Table("ExecutionApprovalRequests")]
    public class ExecutionApprovalRequestEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid WorkspaceId { get; set; }

        public Guid? OrganizationId { get; set; }
        public Guid RequestId { get; set; }
        public Guid MissionId { get; set; }

        [Required]
        [MaxLength(100)]
        public string CapabilityId { get; set; } = string.Empty;

        public ExecutionRiskTier RiskTier { get; set; } = ExecutionRiskTier.R3_FinancialCommitment;
        public decimal MonetaryImpactINR { get; set; }

        [Required]
        [MaxLength(64)]
        public string PayloadDigest { get; set; } = string.Empty;

        public string PayloadSummary { get; set; } = string.Empty;
        public string RequestingAgentId { get; set; } = string.Empty;

        public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

        public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAtUtc { get; set; } = DateTime.UtcNow.AddHours(24);

        public Guid? DecidedByUserId { get; set; }
        public DateTime? DecidedAtUtc { get; set; }
        public string? DecisionReason { get; set; }

        [MaxLength(128)]
        public string? DigitalSignature { get; set; }

        public bool IsValidForExecution(string currentPayloadDigest)
        {
            if (Status != ApprovalStatus.Approved) return false;
            if (DateTime.UtcNow > ExpiresAtUtc) return false;
            // Payload tamper check - if the payload changed after approval, it is invalidated
            return string.Equals(PayloadDigest, currentPayloadDigest, StringComparison.OrdinalIgnoreCase);
        }
    }
}
