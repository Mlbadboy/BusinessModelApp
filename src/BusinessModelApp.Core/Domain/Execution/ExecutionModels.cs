using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using System.Text;

namespace BusinessModelApp.Core.Domain.Execution
{
    public enum ExecutionRiskTier
    {
        R0_ReadData = 0,               // Autonomous telemetry / search
        R1_InternalReversible = 1,     // Autonomous if delegated (drafts, cache, sandbox)
        R2_CustomerCommunication = 2,  // Policy controlled, rate-limited outreach
        R3_FinancialCommitment = 3,    // Budget-delegated threshold or human approval
        R4_LegalContract = 4,          // Binding agreements, human sign-off mandatory
        R5_DestructiveOrHighImpact = 5 // Destructive actions, multi-party human sign-off
    }

    public enum ExecutionActionTier
    {
        L0_Observe = 0,
        L1_Analyze = 1,
        L2_Recommend = 2,
        L3_Prepare = 3,
        L4_ReversibleAction = 4,
        L5_ConsequentialAction = 5
    }

    public enum ExecutionStatus
    {
        Requested = 0,
        Validating = 1,
        Permitted = 2,
        Dispatched = 3,
        Running = 4,
        Succeeded = 5,
        Failed = 6,
        TimedOut = 7,
        CompensationRequired = 8,
        Compensated = 9,
        ManualIntervention = 10,
        Denied = 11
    }

    public class ExecutionRequest
    {
        public Guid RequestId { get; set; } = Guid.NewGuid();
        public Guid WorkspaceId { get; set; }
        public Guid? OrganizationId { get; set; }
        public Guid MissionId { get; set; }
        public string AgentId { get; set; } = string.Empty;
        public string AgentRole { get; set; } = string.Empty;
        public string CapabilityId { get; set; } = string.Empty;
        public ExecutionActionTier ActionTier { get; set; } = ExecutionActionTier.L5_ConsequentialAction;
        public decimal MonetaryImpactINR { get; set; } = 0m;
        public string PayloadJson { get; set; } = "{}";
        public string IdempotencyKey { get; set; } = string.Empty;
        public string AuditContext { get; set; } = string.Empty;
        public Dictionary<string, string> Preconditions { get; set; } = new();
        public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;

        public string ComputePayloadDigest()
        {
            using var sha = SHA256.Create();
            var raw = $"{WorkspaceId}:{CapabilityId}:{MonetaryImpactINR}:{PayloadJson}:{IdempotencyKey}";
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }

    public class ExecutionPermit
    {
        public Guid PermitId { get; set; } = Guid.NewGuid();
        public Guid RequestId { get; set; }
        public Guid WorkspaceId { get; set; }
        public string CapabilityId { get; set; } = string.Empty;
        public string PayloadDigest { get; set; } = string.Empty;
        public ExecutionRiskTier RiskTier { get; set; }
        public decimal ApprovedBudgetINR { get; set; }
        public string PermitToken { get; set; } = string.Empty;
        public DateTime IssuedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAtUtc { get; set; } = DateTime.UtcNow.AddMinutes(2); // 120s TTL
        public bool IsConsumed { get; set; } = false;

        public static string GeneratePermitToken(Guid permitId, Guid workspaceId, string capabilityId, string payloadDigest, string secretKey)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
            var raw = $"{permitId}:{workspaceId}:{capabilityId}:{payloadDigest}";
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        public bool ValidatePermitToken(string secretKey)
        {
            var expected = GeneratePermitToken(PermitId, WorkspaceId, CapabilityId, PayloadDigest, secretKey);
            return string.Equals(expected, PermitToken, StringComparison.OrdinalIgnoreCase);
        }
    }

    public class ExecutionDenial
    {
        public Guid RequestId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string ViolatingPillar { get; set; } = string.Empty;
        public ExecutionRiskTier EvaluatedRisk { get; set; }
        public DateTime DeniedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public class ExecutionDecision
    {
        public bool IsPermitted { get; set; }
        public ExecutionPermit? Permit { get; set; }
        public ExecutionDenial? Denial { get; set; }
        public bool RequiresHumanApproval { get; set; }
        public Guid? ApprovalRequestId { get; set; }
    }

    public class ExecutionReceipt
    {
        public Guid ReceiptId { get; set; } = Guid.NewGuid();
        public Guid RequestId { get; set; }
        public Guid PermitId { get; set; }
        public Guid WorkspaceId { get; set; }
        public string CapabilityId { get; set; } = string.Empty;
        public ExecutionStatus Status { get; set; }
        public string IdempotencyKey { get; set; } = string.Empty;
        public string ResultPayloadJson { get; set; } = "{}";
        public string ResultHash { get; set; } = string.Empty;
        public DateTime ExecutedAtUtc { get; set; } = DateTime.UtcNow;
        public int DurationMs { get; set; }
        public string? ErrorDetails { get; set; }
    }

    [Table("ExecutionLedgerEntries")]
    public class ExecutionLedgerEntry
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid WorkspaceId { get; set; }

        public Guid? OrganizationId { get; set; }
        public Guid MissionId { get; set; }
        public Guid RequestId { get; set; }
        public Guid? PermitId { get; set; }

        [Required]
        [MaxLength(200)]
        public string IdempotencyKey { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string CapabilityId { get; set; } = string.Empty;

        public ExecutionActionTier ActionTier { get; set; }
        public ExecutionRiskTier RiskTier { get; set; }
        public ExecutionStatus Status { get; set; } = ExecutionStatus.Permitted;

        public decimal MonetaryImpactINR { get; set; }

        [Required]
        [MaxLength(64)]
        public string RequestHash { get; set; } = string.Empty;

        [MaxLength(64)]
        public string ResultHash { get; set; } = string.Empty;

        public string RequestPayloadJson { get; set; } = "{}";
        public string ResultPayloadJson { get; set; } = "{}";

        public DateTime ExecutedAtUtc { get; set; } = DateTime.UtcNow;
        public int DurationMs { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
