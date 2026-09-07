using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Reality
{
    /// <summary>
    /// Quantified operational work metrics for a mission or tenant fleet.
    /// </summary>
    public sealed record WorkProgress(
        string TenantId,
        int TotalMissions,
        int ActiveMissions,
        int WaitingApprovalMissions,
        int WaitingExternalMissions,
        int RunningNodes,
        int CompletedNodes,
        int BlockedNodes,
        int UnknownEffectNodes,
        int FailedNodes,
        int CompensatedNodes,
        DateTimeOffset MeasuredAt
    );

    /// <summary>
    /// Step in a mission's immutable work ledger timeline.
    /// </summary>
    public sealed record MissionWorkLedgerEntry(
        string StepId,
        string MissionId,
        string NodeId,
        string NodeName,
        string Stage, // "Responsibility", "Evidence", "Graph", "WorkerAssigned", "Research", "Strategy", "ConstraintCheck", "ActionPrepared", "HumanApproval", "FirewallPermit", "ConnectorExecuted", "OutcomeVerified"
        string Status, // "Completed", "Active", "WaitingApproval", "Blocked", "Failed", "UnknownEffect"
        string Description,
        string? WorkerId,
        DateTimeOffset Timestamp,
        string? ProofHash = null,
        string? Details = null
    );

    /// <summary>
    /// Full immutable work ledger for a specific mission.
    /// </summary>
    public sealed record MissionWorkLedger(
        string MissionId,
        string MissionName,
        string TenantId,
        string Objective,
        DateTimeOffset CreatedAt,
        string CurrentStatus,
        int TotalNodes,
        int CompletedNodes,
        IReadOnlyList<MissionWorkLedgerEntry> Entries
    );

    /// <summary>
    /// Business value realization accounting.
    /// Enforces the invariant: Work Completed != Business Value Realized.
    /// RealizedValue is strictly Unknown until verified by the empirical outcome ledger.
    /// </summary>
    public sealed record ValueRealization(
        string TenantId,
        string MissionId,
        string MissionName,
        decimal ExpectedValue,
        decimal AuthorizedExposure,
        decimal ActualSpend,
        decimal? VerifiedRevenue,
        decimal? VerifiedMargin,
        decimal? RealizedValue,
        RealityStatus RealizedValueStatus,
        string Currency,
        string? EvidenceSource,
        string? OutcomeLedgerReference,
        DateTimeOffset LastAssessedAt,
        string EpistemicNote
    )
    {
        public decimal? Variance => RealizedValue.HasValue ? RealizedValue.Value - ExpectedValue : null;

        public static ValueRealization CreateUnverified(
            string tenantId,
            string missionId,
            string missionName,
            decimal expectedValue,
            decimal authorizedExposure,
            decimal actualSpend,
            string currency = "INR",
            string note = "Realized value is UNKNOWN until outcome ledger confirmation.")
        {
            return new ValueRealization(
                TenantId: tenantId,
                MissionId: missionId,
                MissionName: missionName,
                ExpectedValue: expectedValue,
                AuthorizedExposure: authorizedExposure,
                ActualSpend: actualSpend,
                VerifiedRevenue: null,
                VerifiedMargin: null,
                RealizedValue: null,
                RealizedValueStatus: RealityStatus.Unknown,
                Currency: currency,
                EvidenceSource: null,
                OutcomeLedgerReference: null,
                LastAssessedAt: DateTimeOffset.UtcNow,
                EpistemicNote: note
            );
        }
    }
}
