using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Watchtower;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Watchtower
{
    public class InMemoryWatchtowerStore : IWatchtowerStore
    {
        private readonly ConcurrentDictionary<string, BusinessEvent> _events = new();
        private readonly ConcurrentDictionary<string, List<BusinessEvent>> _correlationEvents = new();
        private readonly ConcurrentDictionary<string, PersistentCondition> _conditions = new();
        private readonly ConcurrentDictionary<string, WatchtowerSignal> _signals = new();

        private static string Key(string tenantId, string id) => $"{tenantId}::{id}";

        public Task SaveEventAsync(BusinessEvent evt, CancellationToken ct = default)
        {
            if (evt == null) throw new ArgumentNullException(nameof(evt));
            if (string.IsNullOrWhiteSpace(evt.TenantId)) throw new ArgumentException("TenantId is required.");
            if (string.IsNullOrWhiteSpace(evt.EventFingerprint)) throw new ArgumentException("EventFingerprint is required.");

            _events[Key(evt.TenantId, evt.EventFingerprint)] = evt;

            if (!string.IsNullOrWhiteSpace(evt.CorrelationKey))
            {
                var corrKey = Key(evt.TenantId, evt.CorrelationKey);
                _correlationEvents.AddOrUpdate(
                    corrKey,
                    _ => new List<BusinessEvent> { evt },
                    (_, list) =>
                    {
                        lock (list)
                        {
                            list.Add(evt);
                        }
                        return list;
                    });
            }

            return Task.CompletedTask;
        }

        public Task<BusinessEvent?> GetEventByFingerprintAsync(string tenantId, string fingerprint, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(fingerprint))
                return Task.FromResult<BusinessEvent?>(null);

            _events.TryGetValue(Key(tenantId, fingerprint), out var evt);
            return Task.FromResult(evt);
        }

        public Task<IReadOnlyList<BusinessEvent>> ListEventsForCorrelationAsync(string tenantId, string correlationKey, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(correlationKey))
                return Task.FromResult<IReadOnlyList<BusinessEvent>>(Array.Empty<BusinessEvent>());

            if (_correlationEvents.TryGetValue(Key(tenantId, correlationKey), out var list))
            {
                lock (list)
                {
                    return Task.FromResult<IReadOnlyList<BusinessEvent>>(list.OrderBy(e => e.ObservedAt).ToList());
                }
            }

            return Task.FromResult<IReadOnlyList<BusinessEvent>>(Array.Empty<BusinessEvent>());
        }

        public Task SaveConditionAsync(PersistentCondition condition, CancellationToken ct = default)
        {
            if (condition == null) throw new ArgumentNullException(nameof(condition));
            if (string.IsNullOrWhiteSpace(condition.TenantId)) throw new ArgumentException("TenantId is required.");
            if (string.IsNullOrWhiteSpace(condition.ConditionId)) throw new ArgumentException("ConditionId is required.");

            _conditions[Key(condition.TenantId, condition.ConditionId)] = condition;
            return Task.CompletedTask;
        }

        public Task<PersistentCondition?> GetConditionAsync(string tenantId, string conditionId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(conditionId))
                return Task.FromResult<PersistentCondition?>(null);

            _conditions.TryGetValue(Key(tenantId, conditionId), out var condition);
            return Task.FromResult(condition);
        }

        public Task<IReadOnlyList<PersistentCondition>> ListConditionsAsync(string tenantId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                return Task.FromResult<IReadOnlyList<PersistentCondition>>(Array.Empty<PersistentCondition>());

            var results = _conditions.Values
                .Where(c => c.TenantId == tenantId)
                .OrderByDescending(c => c.LastObservedUtc)
                .ToList();

            return Task.FromResult<IReadOnlyList<PersistentCondition>>(results);
        }

        public Task SaveSignalAsync(WatchtowerSignal signal, CancellationToken ct = default)
        {
            if (signal == null) throw new ArgumentNullException(nameof(signal));
            if (string.IsNullOrWhiteSpace(signal.TenantId)) throw new ArgumentException("TenantId is required.");
            if (string.IsNullOrWhiteSpace(signal.SignalId)) throw new ArgumentException("SignalId is required.");

            _signals[Key(signal.TenantId, signal.SignalId)] = signal;
            return Task.CompletedTask;
        }

        public Task<WatchtowerSignal?> GetSignalAsync(string tenantId, string signalId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(signalId))
                return Task.FromResult<WatchtowerSignal?>(null);

            _signals.TryGetValue(Key(tenantId, signalId), out var signal);
            return Task.FromResult(signal);
        }

        public Task<IReadOnlyList<WatchtowerSignal>> ListActiveSignalsAsync(string tenantId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                return Task.FromResult<IReadOnlyList<WatchtowerSignal>>(Array.Empty<WatchtowerSignal>());

            var results = _signals.Values
                .Where(s => s.TenantId == tenantId && s.Status == SignalStatus.Active)
                .OrderByDescending(s => s.Breakdown.CompositeAttentionScore)
                .ToList();

            return Task.FromResult<IReadOnlyList<WatchtowerSignal>>(results);
        }
    }
}
