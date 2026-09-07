using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Reality
{
    /// <summary>
    /// Explicit 13-state lifecycle for human consequential action governance.
    /// </summary>
    public enum ApprovalState
    {
        Draft,
        Requested,
        UnderReview,
        Approved,
        Rejected,
        ChangesRequested,
        Expired,
        Invalidated,
        Executing,
        Executed,
        Failed,
        UnknownEffect,
        CompensationRequired
    }

    /// <summary>
    /// Governed risk classification for human approval gating.
    /// </summary>
    public enum ApprovalRiskTier
    {
        R0_Trivial,
        R1_Informational,
        R2_Operational,
        R3_Commercial,
        R4_Strategic,
        R5_Existential
    }

    /// <summary>
    /// Immutable record of an approval request presented to the CEO.
    /// Payload is cryptographic and protected by SHA-256 digest.
    /// </summary>
    public sealed record ApprovalRequest(
        string ApprovalId,
        string TenantId,
        string MissionId,
        string MissionName,
        string NodeId,
        string WorkerId,
        string TargetSystem,
        string Capability,
        ApprovalRiskTier Risk,
        string ActionDescription,
        string ProposedEffect,
        string EvidenceSummary,
        string ConstraintStatus,
        string StrategicRegime,
        decimal FinancialExposure,
        string Currency,
        string PayloadJson,
        string PayloadDigest,
        bool IsReversible,
        DateTimeOffset RequestedAt,
        DateTimeOffset ExpiresAt,
        ApprovalState State,
        string? ReviewerId = null,
        DateTimeOffset? DecidedAt = null,
        string? DecisionNotes = null,
        string? RejectionReason = null,
        string? ChangesRequestedNotes = null,
        string? PermitToken = null
    )
    {
        public bool IsExpired(DateTimeOffset now) => State == ApprovalState.Requested && now > ExpiresAt;
    }

    /// <summary>
    /// Human approval decision submission.
    /// </summary>
    public sealed record ApprovalDecisionSubmission(
        string ApprovalId,
        string TenantId,
        string ReviewerId,
        string DecisionType, // "Approve", "Reject", "RequestChanges"
        string? Notes,
        string? ExpectedPayloadDigest = null
    );

    /// <summary>
    /// Cryptographic execution permit generated upon CEO approval.
    /// Submitted to Batch 6 Execution Firewall; does NOT bypass the firewall.
    /// </summary>
    public sealed record ExecutionPermit(
        string PermitId,
        string ApprovalId,
        string TenantId,
        string MissionId,
        string NodeId,
        string TargetSystem,
        string PayloadDigest,
        DateTimeOffset IssuedAt,
        DateTimeOffset ExpiresAt,
        string ApprovedBy,
        string AuthoritySignature
    );

    /// <summary>
    /// Immutable audit entry for approval lifecycle events.
    /// </summary>
    public sealed record ApprovalAuditEntry(
        string AuditId,
        string ApprovalId,
        string TenantId,
        ApprovalState PreviousState,
        ApprovalState NewState,
        string InitiatedBy,
        DateTimeOffset Timestamp,
        string Details,
        string? IntegrityHash
    );
}
