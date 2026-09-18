using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Experiments;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Experiments;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/enterprise/experiments")]
    public sealed class GrowthExperimentRuntimeController : ControllerBase
    {
        private readonly IGrowthExperimentService _service;
        private readonly IGrowthExperimentStore _store;

        public GrowthExperimentRuntimeController(
            IGrowthExperimentService service,
            IGrowthExperimentStore store)
        {
            _service = service;
            _store = store;
        }

        public record CreateExperimentRequest(
            string TenantId,
            string Title,
            string Hypothesis,
            GrowthExperimentType Type,
            decimal BaselineConversionRate,
            decimal TargetConversionRate,
            decimal BudgetAllocatedINR,
            decimal RiskCeiling = 0.20m);

        public record RecordObservationsRequest(
            int ControlSamples,
            int ControlSuccesses,
            int VariantSamples,
            int VariantSuccesses,
            decimal SpendINR);

        [HttpPost]
        public async Task<ActionResult<GrowthExperiment>> CreateExperiment([FromBody] CreateExperimentRequest request, CancellationToken cancellationToken)
        {
            var exp = await _service.CreateExperimentAsync(
                request.TenantId,
                request.Title,
                request.Hypothesis,
                request.Type,
                request.BaselineConversionRate,
                request.TargetConversionRate,
                request.BudgetAllocatedINR,
                request.RiskCeiling,
                cancellationToken);

            return Ok(exp);
        }

        [HttpGet("{experimentId}")]
        public async Task<ActionResult<GrowthExperiment>> GetExperiment(string experimentId, CancellationToken cancellationToken)
        {
            var exp = await _store.GetExperimentAsync(experimentId, cancellationToken);
            if (exp == null) return NotFound();
            return Ok(exp);
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<GrowthExperiment>>> ListExperiments([FromQuery] string tenantId, CancellationToken cancellationToken)
        {
            var list = await _store.ListExperimentsForTenantAsync(tenantId, cancellationToken);
            return Ok(list);
        }

        [HttpPost("{experimentId}/start")]
        public async Task<ActionResult<GrowthExperiment>> StartExperiment(string experimentId, CancellationToken cancellationToken)
        {
            var exp = await _service.StartExperimentAsync(experimentId, cancellationToken);
            return Ok(exp);
        }

        [HttpPost("{experimentId}/observations")]
        public async Task<ActionResult<GrowthExperiment>> RecordObservations(
            string experimentId,
            [FromBody] RecordObservationsRequest request,
            CancellationToken cancellationToken)
        {
            var exp = await _service.RecordObservationsAsync(
                experimentId,
                request.ControlSamples,
                request.ControlSuccesses,
                request.VariantSamples,
                request.VariantSuccesses,
                request.SpendINR,
                cancellationToken);

            return Ok(exp);
        }

        [HttpPost("{experimentId}/conclude")]
        public async Task<ActionResult<GrowthExperiment>> ConcludeExperiment(
            string experimentId,
            [FromQuery] int minSamples = 100,
            CancellationToken cancellationToken = default)
        {
            var exp = await _service.ConcludeExperimentAsync(experimentId, minSamples, cancellationToken);
            return Ok(exp);
        }
    }
}
