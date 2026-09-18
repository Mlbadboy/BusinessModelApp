using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Operations;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Operations;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Operations
{
    public sealed class InMemoryProductionRealityLedgerStore : IProductionRealityLedgerStore
    {
        private readonly List<ProductionRealityEvent> _events = new();
        private readonly object _lock = new();

        public Task AppendEventAsync(ProductionRealityEvent evt, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                _events.Add(evt);
            }
            return Task.CompletedTask;
        }

        public Task<ProductionRealityEvent?> GetEventAsync(string eventId, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                var evt = _events.FirstOrDefault(e => e.EventId == eventId);
                return Task.FromResult(evt);
            }
        }

        public Task<IReadOnlyList<ProductionRealityEvent>> ListEventsAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                var list = _events.Where(e => e.TenantId == tenantId).ToList();
                return Task.FromResult<IReadOnlyList<ProductionRealityEvent>>(list);
            }
        }

        public Task<IReadOnlyList<ProductionRealityEvent>> ListEventsForObjectAsync(string businessObjectId, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                var list = _events.Where(e => e.BusinessObjectId == businessObjectId).ToList();
                return Task.FromResult<IReadOnlyList<ProductionRealityEvent>>(list);
            }
        }
    }

    public sealed class ProductionRealityLedgerService : IProductionRealityLedgerService
    {
        private readonly IProductionRealityLedgerStore _store;

        public ProductionRealityLedgerService(IProductionRealityLedgerStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<ProductionRealityEvent> RecordExternalEventAsync(
            string tenantId,
            string businessObjectiveId,
            string growthObjectiveId,
            string businessObjectId,
            ProductionRealityEventType eventType,
            EpistemicEvidenceLevel evidenceLevel,
            string source,
            string connectorName,
            string externalProvider,
            string externalReferenceId,
            string rawPayloadForDigest,
            decimal revenueImpact = 0m,
            decimal costImpact = 0m,
            string counterpartyReference = "",
            CancellationToken cancellationToken = default)
        {
            string digest = ProductionRealityEvent.ComputeDigest(rawPayloadForDigest);

            var evt = new ProductionRealityEvent
            {
                TenantId = tenantId,
                BusinessObjectiveId = businessObjectiveId,
                GrowthObjectiveId = growthObjectiveId,
                BusinessObjectId = businessObjectId,
                EventType = eventType,
                EvidenceLevel = evidenceLevel,
                Source = source,
                ConnectorName = connectorName,
                ExternalProvider = externalProvider,
                ExternalReferenceId = externalReferenceId,
                EvidenceDigestSha256 = digest,
                FinancialImpactINR = revenueImpact - costImpact,
                RevenueImpactINR = revenueImpact,
                CostImpactINR = costImpact,
                CounterpartyReference = counterpartyReference
            };

            await _store.AppendEventAsync(evt, cancellationToken);
            return evt;
        }

        public async Task<ProductionRealityEvent> ReconcileEventAsync(
            string eventId,
            string authority,
            ExternalReconciliationStatus status,
            string auditNotes = "",
            CancellationToken cancellationToken = default)
        {
            var evt = await _store.GetEventAsync(eventId, cancellationToken);
            if (evt == null) throw new KeyNotFoundException($"Production reality event '{eventId}' not found.");

            evt.Reconcile(authority, status, auditNotes);
            return evt;
        }

        public async Task<bool> VerifyRealityEvidenceIntegrityAsync(
            string eventId,
            CancellationToken cancellationToken = default)
        {
            var evt = await _store.GetEventAsync(eventId, cancellationToken);
            if (evt == null) return false;

            // Strict Epistemic Grounding: Only BankVerifiedCash / RealizedRevenue with non-empty digest and Reconciled status passes
            return evt.EvidenceLevel >= EpistemicEvidenceLevel.BankVerifiedCash &&
                   evt.VerificationStatus == ExternalReconciliationStatus.Reconciled &&
                   !string.IsNullOrWhiteSpace(evt.EvidenceDigestSha256) &&
                   !string.IsNullOrWhiteSpace(evt.ExternalReferenceId);
        }
    }
}
