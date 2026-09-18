using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Watchtower;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Watchtower
{
    public class StormSuppressionEngine : IStormSuppressionEngine
    {
        private readonly IWatchtowerStore _store;
        private readonly ConcurrentDictionary<string, int> _aggregatedCounts = new();
        private readonly ConcurrentDictionary<string, (DateTime WindowStart, int Count)> _sourceRateLimits = new();

        public StormSuppressionEngine(IWatchtowerStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        private static string AggKey(string tenantId, string fingerprint) => $"{tenantId}::{fingerprint}";
        private static string RateKey(string tenantId, string sourceSystem) => $"{tenantId}::{sourceSystem.ToUpperInvariant()}";

        public Task<int> GetAggregatedEventCountAsync(string tenantId, string fingerprint, CancellationToken ct = default)
        {
            _aggregatedCounts.TryGetValue(AggKey(tenantId, fingerprint), out var count);
            return Task.FromResult(count > 0 ? count : 1);
        }

        public async Task<(bool Suppressed, string? Reason, EventRelationType Relation)> EvaluateStormAsync(
            string tenantId,
            BusinessEvent evt,
            WatchtowerAttentionPolicy policy,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId is required.");
            if (evt == null) throw new ArgumentNullException(nameof(evt));

            var now = DateTime.UtcNow;

            // 1. Source Rate Limiting
            var rateKey = RateKey(tenantId, evt.SourceSystem);
            var rateInfo = _sourceRateLimits.AddOrUpdate(
                rateKey,
                _ => (now, 1),
                (_, current) =>
                {
                    if ((now - current.WindowStart).TotalSeconds < 60)
                    {
                        return (current.WindowStart, current.Count + 1);
                    }
                    return (now, 1);
                });

            if (rateInfo.Count > policy.RateLimitEventsPerMinutePerSource)
            {
                return (true, $"Rate limit ({policy.RateLimitEventsPerMinutePerSource}/min) exceeded for source '{evt.SourceSystem}'. Storm suppressed.", EventRelationType.Duplicate);
            }

            // 2. Invariant I30-E: Fingerprint Deduplication
            var existing = await _store.GetEventByFingerprintAsync(tenantId, evt.EventFingerprint, ct);
            if (existing != null)
            {
                // Increment aggregated count
                var aggKey = AggKey(tenantId, evt.EventFingerprint);
                _aggregatedCounts.AddOrUpdate(aggKey, 2, (_, c) => c + 1);

                return (true, "Duplicate event fingerprint detected; aggregated into existing observation without inflating signal count per Invariant I30-E.", EventRelationType.Duplicate);
            }

            // 3. Correlation Check
            var correlated = await _store.ListEventsForCorrelationAsync(tenantId, evt.CorrelationKey, ct);
            if (correlated.Count > 0)
            {
                return (false, null, EventRelationType.Correlated);
            }

            return (false, null, EventRelationType.Independent);
        }
    }
}
