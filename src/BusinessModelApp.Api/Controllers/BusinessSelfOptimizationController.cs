using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Optimization;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Optimization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/enterprise/optimization")]
    public sealed class BusinessSelfOptimizationController : ControllerBase
    {
        private readonly IBusinessSelfOptimizationService _service;
        private readonly IBusinessSelfOptimizationStore _store;

        public BusinessSelfOptimizationController(
            IBusinessSelfOptimizationService service,
            IBusinessSelfOptimizationStore store)
        {
            _service = service;
            _store = store;
        }

        public record ProposeAdaptationRequest(
            string TenantId,
            OptimizationDomain Domain,
            string TargetParameter,
            decimal CurrentValue,
            decimal ProposedValue,
            decimal CausalConfidenceScore,
            int EmpiricalObservationsCount,
            decimal SafetyCeilingMin,
            decimal SafetyCeilingMax);

        [HttpPost("proposals")]
        public async Task<ActionResult<AdaptationProposal>> ProposeAdaptation([FromBody] ProposeAdaptationRequest request, CancellationToken cancellationToken)
        {
            var prop = await _service.ProposeAdaptationAsync(
                request.TenantId,
                request.Domain,
                request.TargetParameter,
                request.CurrentValue,
                request.ProposedValue,
                request.CausalConfidenceScore,
                request.EmpiricalObservationsCount,
                request.SafetyCeilingMin,
                request.SafetyCeilingMax,
                cancellationToken);

            return Ok(prop);
        }

        [HttpGet("proposals")]
        public async Task<ActionResult<IReadOnlyList<AdaptationProposal>>> ListProposals([FromQuery] string tenantId, CancellationToken cancellationToken)
        {
            var list = await _store.ListProposalsForTenantAsync(tenantId, cancellationToken);
            return Ok(list);
        }

        [HttpPost("proposals/{proposalId}/evaluate")]
        public async Task<ActionResult<AdaptationProposal>> EvaluateProposal(
            string proposalId,
            [FromQuery] decimal minConfidence = 0.80m,
            [FromQuery] int minObs = 50,
            CancellationToken cancellationToken = default)
        {
            var prop = await _service.EvaluateAndCertifyAsync(proposalId, minConfidence, minObs, cancellationToken);
            return Ok(prop);
        }

        [HttpPost("proposals/{proposalId}/apply")]
        public async Task<ActionResult<AdaptationProposal>> ApplyProposal(string proposalId, CancellationToken cancellationToken)
        {
            var prop = await _service.ApplyAdaptationAsync(proposalId, cancellationToken);
            return Ok(prop);
        }
    }
}
