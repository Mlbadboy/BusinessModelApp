using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Watchtower;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Watchtower
{
    public class ContinuousWatchtowerService : IContinuousWatchtowerService
    {
        private readonly IWatchtowerStore _store;
        private readonly IEventNormalizer _normalizer;
        private readonly IEventCorrelationEngine _correlationEngine;
        private readonly IStormSuppressionEngine _stormEngine;
        private readonly IPersistentConditionTracker _conditionTracker;
        private readonly IAttentionScoringEngine _scoringEngine;
        private readonly WatchtowerAttentionPolicy _systemPolicy;

        public ContinuousWatchtowerService(
            IWatchtowerStore store,
            IEventNormalizer normalizer,
            IEventCorrelationEngine correlationEngine,
            IStormSuppressionEngine stormEngine,
            IPersistentConditionTracker conditionTracker,
            IAttentionScoringEngine scoringEngine,
            WatchtowerAttentionPolicy? systemPolicy = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _normalizer = normalizer ?? throw new ArgumentNullException(nameof(normalizer));
            _correlationEngine = correlationEngine ?? throw new ArgumentNullException(nameof(correlationEngine));
            _stormEngine = stormEngine ?? throw new ArgumentNullException(nameof(stormEngine));
            _conditionTracker = conditionTracker ?? throw new ArgumentNullException(nameof(conditionTracker));
            _scoringEngine = scoringEngine ?? throw new ArgumentNullException(nameof(scoringEngine));
            _systemPolicy = systemPolicy ?? new WatchtowerAttentionPolicy();
        }

        public async Task<(bool Accepted, WatchtowerSignal? Signal, string? RejectionReason)> IngestEventAsync(
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
            CancellationToken ct = default)
        {
            var policy = WatchtowerAttentionPolicy.ResolveEffective(_systemPolicy, policyOverride);

            // 1. Normalize
            var (valid, evt, normError) = await _normalizer.NormalizeEventAsync(
                tenantId, sourceSystem, eventType, entityId, sourceRecordId, evidenceHash,
                BusinessModelApp.Core.Domain.Runtime.Organizational.EpistemicStatus.ObservedFact, ct);

            if (!valid || evt == null)
            {
                return (false, null, normError);
            }

            // 2. Storm & Rate-limit Evaluation
            var (suppressed, reason, relation) = await _stormEngine.EvaluateStormAsync(tenantId, evt, policy, ct);
            if (suppressed)
            {
                return (true, null, reason); // Accepted into aggregation, signal generation suppressed
            }

            // 3. Temporal Correlation
            var correlationKey = await _correlationEngine.CorrelateEventAsync(tenantId, evt, policy.CorrelationWindowMinutes, ct);
            var correlatedEvents = await _correlationEngine.GetCorrelatedEventsAsync(tenantId, correlationKey, ct);

            // 4. Persistence Analysis
            var condition = await _conditionTracker.UpdateConditionAsync(
                tenantId, correlationKey, entityId, metricName, observedValue, baselineValue,
                policy.PersistenceObservationThreshold, ct);

            // 5. Attention Scoring
            double confidence = string.IsNullOrWhiteSpace(evidenceHash) ? 0.3 : 1.0;
            var breakdown = _scoringEngine.ScoreCondition(condition, 0.6, confidence);

            // 6. Build Why Explanation
            var whyTrace = new WhyExplanationTrace
            {
                SourceSystem = sourceSystem,
                EventFingerprint = evt.EventFingerprint,
                EvidenceHash = evt.EvidenceHash,
                CorrelationKey = correlationKey,
                CorrelatedEventIds = correlatedEvents.Select(e => e.EventId).ToList(),
                Trajectory = condition.Trajectory,
                AttentionBreakdown = breakdown,
                PolicyRuleTriggered = $"Rule::{condition.Trajectory}::AttentionLevel::{breakdown.ResolvedAttentionLevel}",
                ExplanationText = $"Condition '{condition.MetricName}' on '{condition.EntityId}' shows {condition.Trajectory} with variance {condition.Variance:+0.0%;-0.0%}. Composite attention score: {breakdown.CompositeAttentionScore:F2} ({breakdown.ResolvedAttentionLevel})."
            };

            // 7. Emit Signal
            var signal = new WatchtowerSignal
            {
                SignalId = $"SIG-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                TenantId = tenantId,
                ConditionId = condition.ConditionId,
                Title = $"{condition.MetricName} {condition.Trajectory}",
                Summary = whyTrace.ExplanationText,
                AttentionLevel = breakdown.ResolvedAttentionLevel,
                Status = SignalStatus.Active,
                Breakdown = breakdown,
                WhyTrace = whyTrace,
                EmittedUtc = DateTime.UtcNow,
                SourceEvidenceRefs = !string.IsNullOrWhiteSpace(evt.EvidenceHash) ? new List<string> { evt.EvidenceHash } : new List<string>()
            };

            signal.ComputeSignalHash();
            await _store.SaveSignalAsync(signal, ct);

            return (true, signal, null);
        }

        public Task<IReadOnlyList<WatchtowerSignal>> ListActiveSignalsAsync(string tenantId, CancellationToken ct = default)
        {
            return _store.ListActiveSignalsAsync(tenantId, ct);
        }

        public Task<WatchtowerSignal?> GetSignalAsync(string tenantId, string signalId, CancellationToken ct = default)
        {
            return _store.GetSignalAsync(tenantId, signalId, ct);
        }

        public async Task<WhyExplanationTrace?> GetWhyExplanationAsync(string tenantId, string signalId, CancellationToken ct = default)
        {
            var signal = await _store.GetSignalAsync(tenantId, signalId, ct);
            return signal?.WhyTrace;
        }

        public Task<IReadOnlyList<PersistentCondition>> ListConditionsAsync(string tenantId, CancellationToken ct = default)
        {
            return _store.ListConditionsAsync(tenantId, ct);
        }

        public async Task<(bool Generated, string? WorkProposalId, string? ErrorReason)> ProposeWorkFromSignalAsync(
            string tenantId,
            string signalId,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) return (false, null, "TenantId is required.");
            if (string.IsNullOrWhiteSpace(signalId)) return (false, null, "SignalId is required.");

            var signal = await _store.GetSignalAsync(tenantId, signalId, ct);
            if (signal == null)
            {
                return (false, null, $"Signal '{signalId}' not found for tenant.");
            }

            // Invariant I30-A: Signal produces a WorkProposal to 3.9.0 Organizational Work Control Plane
            var proposalId = $"PROP-CBW-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
            signal.WorkProposalId = proposalId;
            signal.Status = SignalStatus.Escalated;
            await _store.SaveSignalAsync(signal, ct);

            return (true, proposalId, null);
        }
    }
}
