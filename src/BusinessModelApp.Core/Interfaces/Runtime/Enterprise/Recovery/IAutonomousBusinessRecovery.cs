using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Recovery;

namespace BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Recovery
{
    public interface IAutonomousBusinessRecoveryStore
    {
        Task SaveIncidentAsync(BusinessIncident incident, CancellationToken cancellationToken = default);
        Task<BusinessIncident?> GetIncidentAsync(string incidentId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<BusinessIncident>> ListIncidentsForTenantAsync(string tenantId, CancellationToken cancellationToken = default);
    }

    public interface IAutonomousBusinessRecoveryService
    {
        Task<BusinessIncident> ReportIncidentAsync(
            string tenantId,
            string sourceComponent,
            string errorCode,
            IncidentSeverity severity,
            string rootCause,
            string unknownEffectToken = "",
            CancellationToken cancellationToken = default);

        Task<BusinessIncident> ReconcileUnknownEffectAsync(
            string incidentId,
            bool externalEffectConfirmedExecuted,
            string providerConfirmationRef,
            CancellationToken cancellationToken = default);

        Task<BusinessIncident> ExecuteCompensationAsync(
            string incidentId,
            string compensationActionDescription,
            CancellationToken cancellationToken = default);

        Task<BusinessIncident> CloseIncidentAsync(
            string incidentId,
            string resolutionSummary,
            CancellationToken cancellationToken = default);
    }
}
