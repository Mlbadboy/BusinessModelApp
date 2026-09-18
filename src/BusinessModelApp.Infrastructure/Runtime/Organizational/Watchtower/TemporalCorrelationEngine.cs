using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Watchtower;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Watchtower
{
    public class TemporalCorrelationEngine : IEventCorrelationEngine
    {
        private readonly IWatchtowerStore _store;

        public TemporalCorrelationEngine(IWatchtowerStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<string> CorrelateEventAsync(
            string tenantId,
            BusinessEvent evt,
            int windowMinutes,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId is required.");
            if (evt == null) throw new ArgumentNullException(nameof(evt));

            // Default correlation key is SourceSystem::EntityId
            if (string.IsNullOrWhiteSpace(evt.CorrelationKey))
            {
                evt.CorrelationKey = $"{evt.SourceSystem.ToUpperInvariant()}::{evt.EntityId}";
            }

            await _store.SaveEventAsync(evt, ct);
            return evt.CorrelationKey;
        }

        public async Task<IReadOnlyList<BusinessEvent>> GetCorrelatedEventsAsync(
            string tenantId,
            string correlationKey,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(correlationKey))
                return Array.Empty<BusinessEvent>();

            var events = await _store.ListEventsForCorrelationAsync(tenantId, correlationKey, ct);
            return events;
        }
    }
}
