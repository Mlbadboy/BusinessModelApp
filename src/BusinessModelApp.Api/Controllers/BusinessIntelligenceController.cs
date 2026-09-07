using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Kernel;
using BusinessModelApp.Core.Interfaces.Runtime.Intelligence.Kernel;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/intelligence/kernel")]
    public sealed class BusinessIntelligenceController : ControllerBase
    {
        private readonly IBusinessIntelligenceKernel _kernel;

        public BusinessIntelligenceController(IBusinessIntelligenceKernel kernel)
        {
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        }

        [HttpGet("kpis")]
        public async Task<IActionResult> GetKpis([FromQuery] string tenantId = "default-tenant", CancellationToken ct = default)
        {
            var list = await _kernel.Registry.GetAllKpisAsync(tenantId, ct);
            return Ok(list);
        }

        [HttpPost("kpis")]
        public async Task<IActionResult> RegisterKpi([FromBody] KpiDefinition definition, CancellationToken ct = default)
        {
            if (definition == null) return BadRequest("Definition is required.");
            await _kernel.Registry.RegisterKpiAsync(definition, ct);
            return Ok(new { status = "REGISTERED", metricId = definition.MetricId });
        }

        [HttpPost("observations")]
        public async Task<IActionResult> IngestObservation([FromBody] KpiObservation observation, CancellationToken ct = default)
        {
            if (observation == null) return BadRequest("Observation is required.");
            await _kernel.IngestObservationAsync(observation, ct);
            return Ok(new { status = "INGESTED", observationId = observation.ObservationId });
        }

        [HttpGet("anomalies")]
        public async Task<IActionResult> GetAnomalies([FromQuery] string tenantId = "default-tenant", CancellationToken ct = default)
        {
            var list = await _kernel.Anomalies.GetAllActiveAnomaliesAsync(tenantId, ct);
            return Ok(list);
        }

        [HttpGet("trends")]
        public async Task<IActionResult> GetTrend(
            [FromQuery] string metricId,
            [FromQuery] string tenantId = "default-tenant",
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(metricId)) return BadRequest("metricId is required.");
            var trend = await _kernel.Trends.AnalyzeTrendAsync(tenantId, metricId, ct);
            return Ok(trend);
        }

        [HttpGet("relationships")]
        public async Task<IActionResult> GetRelationship(
            [FromQuery] string metricA,
            [FromQuery] string metricB,
            [FromQuery] string tenantId = "default-tenant",
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(metricA) || string.IsNullOrWhiteSpace(metricB))
            {
                return BadRequest("metricA and metricB are required.");
            }
            var rel = await _kernel.Relationships.AnalyzeRelationshipAsync(tenantId, metricA, metricB, ct);
            return Ok(rel);
        }

        [HttpPost("state/evaluate")]
        public async Task<IActionResult> EvaluateState(
            [FromBody] ObservedBusinessState observedState,
            [FromQuery] string tenantId = "default-tenant",
            CancellationToken ct = default)
        {
            if (observedState == null) return BadRequest("Observed state is required.");
            var evaluated = await _kernel.EvaluateEnterpriseStateAsync(tenantId, observedState, ct);
            return Ok(evaluated);
        }

        [HttpGet("insights")]
        public async Task<IActionResult> GetInsights([FromQuery] string tenantId = "default-tenant", CancellationToken ct = default)
        {
            var list = await _kernel.Explainer.GetInsightsAsync(tenantId, ct);
            return Ok(list);
        }
    }
}
