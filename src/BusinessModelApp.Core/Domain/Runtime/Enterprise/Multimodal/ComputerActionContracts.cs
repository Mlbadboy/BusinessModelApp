using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Multimodal
{
    public record ActionPrecondition(string ConditionName, string ExpectedKey, string ExpectedValue)
    {
        public bool IsSatisfied(IReadOnlyDictionary<string, string> currentState)
        {
            if (currentState != null && currentState.TryGetValue(ExpectedKey, out var actualVal))
            {
                return string.Equals(actualVal, ExpectedValue, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }
    }

    public record ActionPostcondition(string ConditionName, string ExpectedStateDescription, int VerificationTimeoutMs = 5000);

    public class ComputerActionProposal
    {
        public string ProposalId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public string EnvironmentSnapshotId { get; set; } = string.Empty;
        public string IntentId { get; set; } = string.Empty;
        public ProposedActionType ActionType { get; set; }
        public string? TargetElementId { get; set; }
        public (int X, int Y)? TargetCoordinates { get; set; }
        public Dictionary<string, string> Parameters { get; set; } = new();
        public ActionRiskTier RiskTier { get; set; } = ActionRiskTier.R0_Observation;
        public string ExpectedEffect { get; set; } = string.Empty;
        public bool IsReversible { get; set; } = true;
        public List<ActionPrecondition> Preconditions { get; set; } = new();
        public List<ActionPostcondition> Postconditions { get; set; } = new();
        public string Evidence { get; set; } = string.Empty;
        public string ModelVersion { get; set; } = "vlm-governed-1.0";
        public string PolicySnapshotHash { get; set; } = string.Empty;
        public string CapabilityId { get; set; } = string.Empty;
        public string ProposalHash { get; set; } = string.Empty;
        public bool RequiresHumanApproval => RiskTier >= ActionRiskTier.R4_CommercialConsequential;

        public static string ComputeSha256(string content)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content ?? string.Empty));
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        public string ComputeProposalHash()
        {
            var raw = $"{TenantId}:{SessionId}:{EnvironmentSnapshotId}:{IntentId}:{ActionType}:{TargetElementId}:{RiskTier}:{CapabilityId}:{ExpectedEffect}";
            return ComputeSha256(raw);
        }
    }

    public enum ActionExecutionStatus
    {
        Admitted = 1,
        Executing = 2,
        Completed = 3,
        Failed = 4,
        TargetMismatch = 5,
        EnvironmentChanged = 6,
        BlockedByGovernance = 7,
        UnknownEffect = 8,
        Quarantined = 9
    }

    public class ComputerActionExecutionAttempt
    {
        public string AttemptId { get; set; } = Guid.NewGuid().ToString("N");
        public string ProposalId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public DateTime AdmittedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAtUtc { get; set; }
        public ActionExecutionStatus Status { get; set; } = ActionExecutionStatus.Admitted;
        public string? ErrorMessage { get; set; }
        public string ExternalEffect { get; set; } = "None";
        public bool HasSideEffects => Status == ActionExecutionStatus.Completed && ExternalEffect != "None";
    }

    public class ComputerActionResult
    {
        public bool IsSuccess { get; set; }
        public ActionExecutionStatus Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool RequiresReconciliation => Status == ActionExecutionStatus.UnknownEffect;
        public ComputerActionExecutionAttempt? Attempt { get; set; }
        public string ResultHash { get; set; } = string.Empty;
    }
}
