using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Organizational;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Watchtower;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Watchtower
{
    public class EventNormalizer : IEventNormalizer
    {
        private readonly IEventFingerprintService _fingerprintService;

        public EventNormalizer(IEventFingerprintService fingerprintService)
        {
            _fingerprintService = fingerprintService ?? throw new ArgumentNullException(nameof(fingerprintService));
        }

        public Task<(bool Valid, BusinessEvent? Event, string? ErrorReason)> NormalizeEventAsync(
            string tenantId,
            string sourceSystem,
            string eventType,
            string entityId,
            string sourceRecordId,
            string evidenceHash,
            EpistemicStatus epistemicKind = EpistemicStatus.ObservedFact,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) return Task.FromResult<(bool, BusinessEvent?, string?)>((false, null, "TenantId is required."));
            if (string.IsNullOrWhiteSpace(sourceSystem)) return Task.FromResult<(bool, BusinessEvent?, string?)>((false, null, "SourceSystem is required."));
            if (string.IsNullOrWhiteSpace(eventType)) return Task.FromResult<(bool, BusinessEvent?, string?)>((false, null, "EventType is required."));
            if (string.IsNullOrWhiteSpace(entityId)) return Task.FromResult<(bool, BusinessEvent?, string?)>((false, null, "EntityId is required."));
            if (string.IsNullOrWhiteSpace(sourceRecordId)) return Task.FromResult<(bool, BusinessEvent?, string?)>((false, null, "SourceRecordId is required."));

            // Invariant I30-K: UNKNOWN Preservation - if evidence is missing, cannot claim VerifiedTruth/ObservedFact
            var effectiveEpistemic = epistemicKind;
            if (string.IsNullOrWhiteSpace(evidenceHash) &&
                (epistemicKind == EpistemicStatus.VerifiedTruth || epistemicKind == EpistemicStatus.ObservedFact))
            {
                effectiveEpistemic = EpistemicStatus.Unknown;
            }

            var now = DateTime.UtcNow;
            var fingerprint = _fingerprintService.ComputeFingerprint(sourceSystem, eventType, entityId, sourceRecordId);
            var provenance = _fingerprintService.ComputeProvenanceHash(tenantId, fingerprint, now, evidenceHash ?? string.Empty);

            var businessEvent = new BusinessEvent
            {
                EventId = $"EVT-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                TenantId = tenantId.Trim(),
                SourceSystem = sourceSystem.Trim(),
                EventType = eventType.Trim(),
                EntityId = entityId.Trim(),
                SourceRecordId = sourceRecordId.Trim(),
                ObservedAt = now,
                ReceivedAt = now,
                EvidenceHash = evidenceHash?.Trim() ?? string.Empty,
                EventFingerprint = fingerprint,
                CorrelationKey = $"{sourceSystem.Trim().ToUpperInvariant()}::{entityId.Trim()}",
                EpistemicKind = effectiveEpistemic,
                Freshness = FreshnessStatus.Fresh,
                ProvenanceHash = provenance
            };

            return Task.FromResult<(bool, BusinessEvent?, string?)>((true, businessEvent, null));
        }
    }
}
