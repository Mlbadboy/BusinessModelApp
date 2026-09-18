using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Recovery;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Recovery;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Recovery
{
    public sealed class InMemoryAutonomousBusinessRecoveryStore : IAutonomousBusinessRecoveryStore
    {
        private readonly ConcurrentDictionary<string, BusinessIncident> _incidents = new();

        public Task SaveIncidentAsync(BusinessIncident incident, CancellationToken cancellationToken = default)
        {
            _incidents[incident.IncidentId] = incident;
            return Task.CompletedTask;
        }

        public Task<BusinessIncident?> GetIncidentAsync(string incidentId, CancellationToken cancellationToken = default)
        {
            _incidents.TryGetValue(incidentId, out var inc);
            return Task.FromResult(inc);
        }

        public Task<IReadOnlyList<BusinessIncident>> ListIncidentsForTenantAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            var list = _incidents.Values.Where(i => i.TenantId == tenantId).ToList();
            return Task.FromResult<IReadOnlyList<BusinessIncident>>(list);
        }
    }

    public sealed class AutonomousBusinessRecoveryService : IAutonomousBusinessRecoveryService
    {
        private readonly IAutonomousBusinessRecoveryStore _store;

        public AutonomousBusinessRecoveryService(IAutonomousBusinessRecoveryStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<BusinessIncident> ReportIncidentAsync(
            string tenantId,
            string sourceComponent,
            string errorCode,
            IncidentSeverity severity,
            string rootCause,
            string unknownEffectToken = "",
            CancellationToken cancellationToken = default)
        {
            var incident = new BusinessIncident
            {
                TenantId = tenantId,
                SourceComponent = sourceComponent,
                ErrorCode = errorCode,
                Severity = severity,
                RootCause = rootCause,
                UnknownEffectToken = unknownEffectToken
            };

            // Determine strategy
            if (!string.IsNullOrWhiteSpace(unknownEffectToken))
            {
                incident.AssignStrategy(RecoveryStrategy.ReconcileExternalEffect);
            }
            else if (severity >= IncidentSeverity.Major)
            {
                incident.AssignStrategy(RecoveryStrategy.CompensateAndRollback);
            }
            else
            {
                incident.AssignStrategy(RecoveryStrategy.RetryWithBackoff);
            }

            await _store.SaveIncidentAsync(incident, cancellationToken);
            return incident;
        }

        public async Task<BusinessIncident> ReconcileUnknownEffectAsync(
            string incidentId,
            bool externalEffectConfirmedExecuted,
            string providerConfirmationRef,
            CancellationToken cancellationToken = default)
        {
            var incident = await _store.GetIncidentAsync(incidentId, cancellationToken);
            if (incident == null) throw new KeyNotFoundException($"Incident '{incidentId}' not found.");

            if (string.IsNullOrWhiteSpace(incident.UnknownEffectToken))
            {
                throw new InvalidOperationException("Incident does not contain an UnknownEffect token to reconcile.");
            }

            var statusDesc = externalEffectConfirmedExecuted
                ? $"External effect confirmed executed on provider side (Ref: {providerConfirmationRef}). Replay aborted to prevent duplicate transaction."
                : $"External effect confirmed NOT executed on provider side. Safe for idempotent re-execution.";

            incident.MarkReconciled(statusDesc);
            await _store.SaveIncidentAsync(incident, cancellationToken);
            return incident;
        }

        public async Task<BusinessIncident> ExecuteCompensationAsync(
            string incidentId,
            string compensationActionDescription,
            CancellationToken cancellationToken = default)
        {
            var incident = await _store.GetIncidentAsync(incidentId, cancellationToken);
            if (incident == null) throw new KeyNotFoundException($"Incident '{incidentId}' not found.");

            incident.MarkCompensated(compensationActionDescription);
            await _store.SaveIncidentAsync(incident, cancellationToken);
            return incident;
        }

        public async Task<BusinessIncident> CloseIncidentAsync(
            string incidentId,
            string resolutionSummary,
            CancellationToken cancellationToken = default)
        {
            var incident = await _store.GetIncidentAsync(incidentId, cancellationToken);
            if (incident == null) throw new KeyNotFoundException($"Incident '{incidentId}' not found.");

            incident.Resolve(resolutionSummary);
            await _store.SaveIncidentAsync(incident, cancellationToken);
            return incident;
        }
    }
}
