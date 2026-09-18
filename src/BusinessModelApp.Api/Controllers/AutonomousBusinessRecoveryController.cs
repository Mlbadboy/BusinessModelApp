using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Recovery;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Recovery;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/enterprise/recovery")]
    public sealed class AutonomousBusinessRecoveryController : ControllerBase
    {
        private readonly IAutonomousBusinessRecoveryService _service;
        private readonly IAutonomousBusinessRecoveryStore _store;

        public AutonomousBusinessRecoveryController(
            IAutonomousBusinessRecoveryService service,
            IAutonomousBusinessRecoveryStore store)
        {
            _service = service;
            _store = store;
        }

        public record ReportIncidentRequest(string TenantId, string SourceComponent, string ErrorCode, IncidentSeverity Severity, string RootCause, string UnknownEffectToken = "");
        public record ReconcileRequest(bool ExternalEffectConfirmedExecuted, string ProviderConfirmationRef);
        public record CompensateRequest(string CompensationActionDescription);
        public record CloseRequest(string ResolutionSummary);

        [HttpPost("incidents")]
        public async Task<ActionResult<BusinessIncident>> ReportIncident([FromBody] ReportIncidentRequest request, CancellationToken cancellationToken)
        {
            var inc = await _service.ReportIncidentAsync(request.TenantId, request.SourceComponent, request.ErrorCode, request.Severity, request.RootCause, request.UnknownEffectToken, cancellationToken);
            return Ok(inc);
        }

        [HttpGet("incidents")]
        public async Task<ActionResult<IReadOnlyList<BusinessIncident>>> ListIncidents([FromQuery] string tenantId, CancellationToken cancellationToken)
        {
            var list = await _store.ListIncidentsForTenantAsync(tenantId, cancellationToken);
            return Ok(list);
        }

        [HttpPost("incidents/{incidentId}/reconcile")]
        public async Task<ActionResult<BusinessIncident>> Reconcile(string incidentId, [FromBody] ReconcileRequest request, CancellationToken cancellationToken)
        {
            var inc = await _service.ReconcileUnknownEffectAsync(incidentId, request.ExternalEffectConfirmedExecuted, request.ProviderConfirmationRef, cancellationToken);
            return Ok(inc);
        }

        [HttpPost("incidents/{incidentId}/compensate")]
        public async Task<ActionResult<BusinessIncident>> Compensate(string incidentId, [FromBody] CompensateRequest request, CancellationToken cancellationToken)
        {
            var inc = await _service.ExecuteCompensationAsync(incidentId, request.CompensationActionDescription, cancellationToken);
            return Ok(inc);
        }

        [HttpPost("incidents/{incidentId}/close")]
        public async Task<ActionResult<BusinessIncident>> Close(string incidentId, [FromBody] CloseRequest request, CancellationToken cancellationToken)
        {
            var inc = await _service.CloseIncidentAsync(incidentId, request.ResolutionSummary, cancellationToken);
            return Ok(inc);
        }
    }
}
