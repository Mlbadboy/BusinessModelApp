using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Organizational;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Watchtower;

namespace BusinessModelApp.Core.Interfaces.Runtime.Organizational
{
    public interface IEventFingerprintService
    {
        string ComputeFingerprint(string sourceSystem, string eventType, string entityId, string sourceRecordId);
        string ComputeProvenanceHash(string tenantId, string fingerprint, DateTime observedAt, string evidenceHash);
    }

    public interface IEventNormalizer
    {
        Task<(bool Valid, BusinessEvent? Event, string? ErrorReason)> NormalizeEventAsync(
            string tenantId,
            string sourceSystem,
            string eventType,
            string entityId,
            string sourceRecordId,
            string evidenceHash,
            EpistemicStatus epistemicKind = EpistemicStatus.ObservedFact,
            CancellationToken ct = default);
    }

    public interface IEventCorrelationEngine
    {
        Task<string> CorrelateEventAsync(
            string tenantId,
            BusinessEvent evt,
            int windowMinutes,
            CancellationToken ct = default);

        Task<IReadOnlyList<BusinessEvent>> GetCorrelatedEventsAsync(
            string tenantId,
            string correlationKey,
            CancellationToken ct = default);
    }

    public interface IPersistentConditionTracker
    {
        Task<PersistentCondition> UpdateConditionAsync(
            string tenantId,
            string correlationKey,
            string entityId,
            string metricName,
            double observedValue,
            double baselineValue,
            int persistenceThreshold,
            CancellationToken ct = default);

        Task<PersistentCondition?> GetConditionAsync(
            string tenantId,
            string conditionId,
            CancellationToken ct = default);

        Task<IReadOnlyList<PersistentCondition>> ListConditionsAsync(
            string tenantId,
            CancellationToken ct = default);
    }

    public interface IStormSuppressionEngine
    {
        Task<(bool Suppressed, string? Reason, EventRelationType Relation)> EvaluateStormAsync(
            string tenantId,
            BusinessEvent evt,
            WatchtowerAttentionPolicy policy,
            CancellationToken ct = default);

        Task<int> GetAggregatedEventCountAsync(
            string tenantId,
            string fingerprint,
            CancellationToken ct = default);
    }

    public interface IAttentionScoringEngine
    {
        AttentionScoreBreakdown ScoreCondition(
            PersistentCondition condition,
            double exposureScore = 0.5,
            double confidenceScore = 1.0);
    }

    public interface IWatchtowerStore
    {
        Task SaveEventAsync(BusinessEvent evt, CancellationToken ct = default);
        Task<BusinessEvent?> GetEventByFingerprintAsync(string tenantId, string fingerprint, CancellationToken ct = default);
        Task<IReadOnlyList<BusinessEvent>> ListEventsForCorrelationAsync(string tenantId, string correlationKey, CancellationToken ct = default);
        Task SaveConditionAsync(PersistentCondition condition, CancellationToken ct = default);
        Task<PersistentCondition?> GetConditionAsync(string tenantId, string conditionId, CancellationToken ct = default);
        Task<IReadOnlyList<PersistentCondition>> ListConditionsAsync(string tenantId, CancellationToken ct = default);
        Task SaveSignalAsync(WatchtowerSignal signal, CancellationToken ct = default);
        Task<WatchtowerSignal?> GetSignalAsync(string tenantId, string signalId, CancellationToken ct = default);
        Task<IReadOnlyList<WatchtowerSignal>> ListActiveSignalsAsync(string tenantId, CancellationToken ct = default);
    }

    public interface IContinuousWatchtowerService
    {
        Task<(bool Accepted, WatchtowerSignal? Signal, string? RejectionReason)> IngestEventAsync(
            string tenantId,
            string sourceSystem,
            string eventType,
            string entityId,
            string sourceRecordId,
            string evidenceHash,
            double observedValue,
            double baselineValue = 0.0,
            string metricName = "DefaultMetric",
            WatchtowerAttentionPolicy? policyOverride = null,
            CancellationToken ct = default);

        Task<IReadOnlyList<WatchtowerSignal>> ListActiveSignalsAsync(string tenantId, CancellationToken ct = default);
        Task<WatchtowerSignal?> GetSignalAsync(string tenantId, string signalId, CancellationToken ct = default);
        Task<WhyExplanationTrace?> GetWhyExplanationAsync(string tenantId, string signalId, CancellationToken ct = default);
        Task<IReadOnlyList<PersistentCondition>> ListConditionsAsync(string tenantId, CancellationToken ct = default);

        Task<(bool Generated, string? WorkProposalId, string? ErrorReason)> ProposeWorkFromSignalAsync(
            string tenantId,
            string signalId,
            CancellationToken ct = default);
    }
}
