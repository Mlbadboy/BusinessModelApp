using System;
using System.Security.Cryptography;
using System.Text;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime.Capabilities;

namespace BusinessModelApp.Core.Domain.Runtime.Workers
{
    /// <summary>
    /// Worker Action Proposal: Represents a prepared external action constructed by a worker.
    /// Under Invariant I16-C, workers cannot directly invoke consequential external actions.
    /// They MUST package their request as an ActionProposal and submit to Runtime Admission
    /// and the Batch 6 Execution Firewall.
    /// </summary>
    public class WorkerActionProposal
    {
        public Guid ProposalId { get; init; } = Guid.NewGuid();
        public Guid WorkspaceId { get; init; }
        public MissionId MissionId { get; init; }
        public MissionNodeId? NodeId { get; init; }
        public WorkerAttemptId AttemptId { get; init; }
        public WorkerInstanceId WorkerInstanceId { get; init; }
        public WorkerModality Modality { get; init; }
        public CapabilityId CapabilityId { get; init; }
        public string CapabilityVersion { get; init; } = "1.0.0";
        public string TargetSystem { get; init; } = string.Empty;
        public string ActionType { get; init; } = string.Empty;
        public string ParametersJson { get; init; } = "{}";
        public string ExpectedEffect { get; init; } = string.Empty;
        public string EvidencePayload { get; init; } = string.Empty;
        public string IdempotencyKey { get; init; } = string.Empty;
        public string PayloadDigest { get; set; } = string.Empty;
        public string PolicyVersion { get; init; } = "1.0";
        public string ConstraintSnapshotHash { get; init; } = string.Empty;
        public string TraceId { get; init; } = Guid.NewGuid().ToString("N");
        public DateTimeOffset ProposedAt { get; init; } = DateTimeOffset.UtcNow;

        public static string ComputePayloadDigest(WorkerActionProposal proposal)
        {
            if (proposal == null) return string.Empty;
            var raw = $"{proposal.WorkspaceId}:{proposal.CapabilityId}:{proposal.TargetSystem}:{proposal.ActionType}:{proposal.ParametersJson}:{proposal.IdempotencyKey}";
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }
    }

    /// <summary>
    /// Structural validation result emitted before forwarding to Runtime Admission / Batch 6.
    /// </summary>
    public class ActionProposalValidationResult
    {
        public bool IsAdmissible { get; init; }
        public string? RejectionReason { get; init; }
        public bool IsConsequential { get; init; }
        public bool DigestValid { get; init; }
        public DateTimeOffset ValidatedAt { get; init; } = DateTimeOffset.UtcNow;

        public static ActionProposalValidationResult Success(bool isConsequential) =>
            new() { IsAdmissible = true, IsConsequential = isConsequential, DigestValid = true };

        public static ActionProposalValidationResult Reject(string reason) =>
            new() { IsAdmissible = false, RejectionReason = reason, DigestValid = false };
    }
}
