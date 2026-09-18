using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Delivery;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Delivery;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/enterprise/delivery")]
    public sealed class CustomerDeliveryRuntimeController : ControllerBase
    {
        private readonly ICustomerDeliveryRuntimeService _service;
        private readonly ICustomerDeliveryRuntimeStore _store;

        public CustomerDeliveryRuntimeController(
            ICustomerDeliveryRuntimeService service,
            ICustomerDeliveryRuntimeStore store)
        {
            _service = service;
            _store = store;
        }

        public record CreateProjectRequest(
            string TenantId,
            string CustomerId,
            string ContractId,
            List<DeliveryMilestone> Milestones,
            List<ValueRealizationMetric> ValueMetrics);

        public record AdvanceStatusRequest(
            DeliveryProjectStatus NextStatus,
            string? SignoffSha256);

        public record RecordMetricRequest(
            decimal MeasuredValue,
            EpistemicEvidenceLevel Level,
            string ProofSha256);

        [HttpPost("projects")]
        public async Task<ActionResult<DeliveryProject>> CreateProject(
            [FromBody] CreateProjectRequest request,
            CancellationToken cancellationToken)
        {
            var project = await _service.CreateProjectAsync(
                request.TenantId,
                request.CustomerId,
                request.ContractId,
                request.Milestones,
                request.ValueMetrics,
                cancellationToken);

            return Ok(project);
        }

        [HttpGet("projects/{projectId}")]
        public async Task<ActionResult<DeliveryProject>> GetProject(
            string projectId,
            CancellationToken cancellationToken)
        {
            var project = await _store.GetProjectAsync(projectId, cancellationToken);
            if (project == null) return NotFound();
            return Ok(project);
        }

        [HttpPost("projects/{projectId}/advance")]
        public async Task<ActionResult<DeliveryProject>> AdvanceStatus(
            string projectId,
            [FromBody] AdvanceStatusRequest request,
            CancellationToken cancellationToken)
        {
            var project = await _service.AdvanceProjectStatusAsync(
                projectId,
                request.NextStatus,
                request.SignoffSha256,
                cancellationToken);

            return Ok(project);
        }

        [HttpPost("projects/{projectId}/metrics/{metricId}")]
        public async Task<ActionResult<DeliveryProject>> RecordMetric(
            string projectId,
            string metricId,
            [FromBody] RecordMetricRequest request,
            CancellationToken cancellationToken)
        {
            var project = await _service.RecordMetricMeasurementAsync(
                projectId,
                metricId,
                request.MeasuredValue,
                request.Level,
                request.ProofSha256,
                cancellationToken);

            return Ok(project);
        }
    }
}
