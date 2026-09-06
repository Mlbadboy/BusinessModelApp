using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Responsibilities;
using BusinessModelApp.Core.Interfaces.Ambient;

namespace BusinessModelApp.Infrastructure.Runtime.Ambient
{
    public class AmbientEventIngestionService : IAmbientEventNormalizer, IAmbientEventSource
    {
        private readonly ConcurrentDictionary<string, bool> _idempotencyKeys = new();
        private readonly IResponsibilityDetector _detector;
        private readonly IResponsibilityCorrelationEngine _correlationEngine;

        public AmbientEventIngestionService(
            IResponsibilityDetector detector,
            IResponsibilityCorrelationEngine correlationEngine)
        {
            _detector = detector ?? throw new ArgumentNullException(nameof(detector));
            _correlationEngine = correlationEngine ?? throw new ArgumentNullException(nameof(correlationEngine));
        }

        public AmbientBusinessEvent NormalizeEvent(
            Guid workspaceId,
            string source,
            string eventType,
            string entityType,
            string entityId,
            decimal metricValue,
            string unit,
            Dictionary<string, object>? payload = null,
            double sourceTrust = 1.0)
        {
            if (workspaceId == Guid.Empty)
                throw new ArgumentException("Tenant isolation violation: WorkspaceId cannot be empty.", nameof(workspaceId));

            return new AmbientBusinessEvent
            {
                EventId = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                Source = source?.Trim().ToLowerInvariant() ?? "unknown",
                EventType = eventType?.Trim() ?? "metric_observed",
                EntityType = entityType?.Trim() ?? "system",
                EntityId = entityId?.Trim() ?? "default",
                MetricValue = metricValue,
                Unit = unit?.Trim() ?? string.Empty,
                Payload = payload ?? new Dictionary<string, object>(),
                OccurredAtUtc = DateTime.UtcNow,
                ReceivedAtUtc = DateTime.UtcNow,
                SourceTrustLevel = Math.Clamp(sourceTrust, 0.0, 1.0),
                IdempotencyKey = $"{workspaceId}:{source}:{eventType}:{entityId}:{metricValue}"
            };
        }

        public async Task<bool> IngestEventAsync(AmbientBusinessEvent businessEvent, CancellationToken cancellationToken = default)
        {
            if (businessEvent == null) throw new ArgumentNullException(nameof(businessEvent));

            // Tenant Isolation Guard: Fail-closed on missing/empty tenant
            if (businessEvent.WorkspaceId == Guid.Empty)
                return false;

            // Idempotency Deduplication: Don't process exact duplicate payload twice
            if (!string.IsNullOrEmpty(businessEvent.IdempotencyKey))
            {
                if (!_idempotencyKeys.TryAdd(businessEvent.IdempotencyKey, true))
                {
                    // Duplicate event suppressed
                    return false;
                }
            }

            // Record event for multi-signal correlation
            await _correlationEngine.RecordEventForCorrelationAsync(businessEvent, cancellationToken);

            // Forward to responsibility detection engine
            await _detector.DetectResponsibilitiesAsync(businessEvent, cancellationToken);
            return true;
        }
    }
}
