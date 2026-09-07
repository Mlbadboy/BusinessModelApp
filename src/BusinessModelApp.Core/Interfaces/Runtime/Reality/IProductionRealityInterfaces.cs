using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Reality;

namespace BusinessModelApp.Core.Interfaces.Runtime.Reality
{
    /// <summary>
    /// Governs production data authenticity, provenance envelopes, and epistemic classification.
    /// </summary>
    public interface IProductionRealityService
    {
        RealityEnvelope<T> Wrap<T>(
            T? value,
            RealityStatus status,
            TruthClassification classification,
            string tenantId,
            string source,
            string? sourceRecordId = null,
            string? provenanceId = null,
            DateTimeOffset? observedAt = null
        );

        MetricValue<T> CreateMetric<T>(
            T? value,
            RealityStatus status,
            TruthClassification classification,
            string metricKey,
            string label,
            string unit,
            string tenantId,
            string source,
            TimeSpan freshnessSla,
            string? sourceRecordId = null,
            string? provenanceId = null,
            string? epistemicRationale = null,
            DateTimeOffset? observedAt = null
        );

        void AssertProductionIntegrity(bool isProductionEnvironment, bool hasMockProvider);

        RealityMode GetCurrentMode(string tenantId);

        SystemRealityOverview GetSystemOverview(string tenantId);
    }

    /// <summary>
    /// Manages the CEO human approval center, payload cryptographic validation,
    /// SLA expiration, and execution permit issuance.
    /// </summary>
    public interface IHumanApprovalManager
    {
        Task<ApprovalRequest> SubmitApprovalRequestAsync(ApprovalRequest request);

        Task<ApprovalRequest?> GetApprovalRequestAsync(string tenantId, string approvalId);

        Task<IReadOnlyList<ApprovalRequest>> GetPendingApprovalsAsync(string tenantId);

        Task<IReadOnlyList<ApprovalRequest>> GetApprovalHistoryAsync(string tenantId);

        Task<ExecutionPermit> ApproveRequestAsync(
            string tenantId,
            string approvalId,
            string reviewerId,
            string? expectedPayloadDigest = null,
            string? notes = null
        );

        Task<ApprovalRequest> RejectRequestAsync(
            string tenantId,
            string approvalId,
            string reviewerId,
            string reason
        );

        Task<ApprovalRequest> RequestChangesAsync(
            string tenantId,
            string approvalId,
            string reviewerId,
            string changesRequested
        );

        Task<int> CheckAndExpireApprovalsAsync(string tenantId);

        Task<IReadOnlyList<ApprovalAuditEntry>> GetAuditTrailAsync(string tenantId, string approvalId);
    }

    /// <summary>
    /// Aggregates live operational work across missions, nodes, and workers.
    /// </summary>
    public interface IWorkControlCenter
    {
        Task<WorkProgress> GetWorkProgressAsync(string tenantId);

        Task<MissionWorkLedger?> GetMissionWorkLedgerAsync(string tenantId, string missionId);

        Task<IReadOnlyList<MissionWorkLedger>> GetAllMissionLedgersAsync(string tenantId);

        Task RecordMissionStepAsync(string tenantId, string missionId, MissionWorkLedgerEntry entry);

        Task RegisterMissionLedgerAsync(MissionWorkLedger ledger);
    }

    /// <summary>
    /// Tracks expected vs. verified business value.
    /// Enforces the separation of work execution from realized value.
    /// </summary>
    public interface IValueRealizationEngine
    {
        Task<ValueRealization?> GetValueRealizationAsync(string tenantId, string missionId);

        Task<IReadOnlyList<ValueRealization>> GetAllValueRealizationsAsync(string tenantId);

        Task RecordExpectedValueAsync(
            string tenantId,
            string missionId,
            string missionName,
            decimal expected,
            decimal authorized,
            decimal actual,
            string currency = "INR"
        );

        Task<ValueRealization> RecordVerifiedOutcomeAsync(
            string tenantId,
            string missionId,
            decimal verifiedRevenue,
            decimal verifiedMargin,
            string outcomeLedgerRef,
            string evidenceSource
        );
    }

    /// <summary>
    /// Monitors health of external business connectors.
    /// </summary>
    public interface IConnectorHealthService
    {
        Task<IReadOnlyList<ConnectorHealthRecord>> GetConnectorHealthAsync(string tenantId);

        Task<ConnectorHealthRecord> GetConnectorHealthAsync(string tenantId, ConnectorType type);

        Task UpdateConnectorHealthAsync(ConnectorHealthRecord record);
    }
}
