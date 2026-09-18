using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Decision;
using BusinessModelApp.Core.Interfaces.Runtime.Intelligence.Decision;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DecisionController : ControllerBase
    {
        private readonly IDecisionOrchestrator _orchestrator;
        private readonly IDecisionStore _store;

        public DecisionController(IDecisionOrchestrator orchestrator, IDecisionStore store)
        {
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        [HttpPost("rank")]
        public async Task<ActionResult<DecisionRankingResult>> RankDecisions(
            [FromBody] RankDecisionsRequest request,
            CancellationToken ct)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.TenantId) || request.Candidates == null || request.Candidates.Count == 0)
            {
                return BadRequest(new { message = "TenantId and at least one candidate (including DoNothing baseline) are required." });
            }

            var result = await _orchestrator.RankDecisionsAsync(request.TenantId, request.Candidates, request.Policy, ct);
            return Ok(result);
        }

        [HttpPost("from-signal/{signalId}")]
        public async Task<ActionResult<DecisionRankingResult>> SynthesizeAndRankFromSignal(
            string signalId,
            [FromQuery] string tenantId,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return BadRequest(new { message = "tenantId query parameter is required." });
            }

            var result = await _orchestrator.SynthesizeAndRankFromRadarAndScenarioAsync(tenantId, signalId, ct);
            return Ok(result);
        }

        [HttpGet("{tenantId}/latest")]
        public async Task<ActionResult<DecisionRankingResult>> GetLatestRanking(string tenantId, CancellationToken ct)
        {
            var ranking = await _store.GetLatestRankingAsync(tenantId, ct);
            if (ranking == null)
            {
                return NotFound(new { message = $"No decision ranking found for tenant {tenantId}." });
            }
            return Ok(ranking);
        }

        [HttpGet("{tenantId}/candidates")]
        public async Task<ActionResult<IReadOnlyList<DecisionCandidate>>> ListCandidates(
            string tenantId,
            [FromQuery] DecisionLifecycleState? state,
            CancellationToken ct)
        {
            var candidates = await _store.ListCandidatesAsync(tenantId, state, ct);
            return Ok(candidates);
        }

        [HttpGet("{tenantId}/candidates/{id}")]
        public async Task<ActionResult<DecisionCandidate>> GetCandidate(
            string tenantId,
            string id,
            CancellationToken ct)
        {
            var candidate = await _store.GetCandidateAsync(tenantId, id, ct);
            if (candidate == null)
            {
                return NotFound(new { message = $"Decision candidate {id} not found." });
            }
            return Ok(candidate);
        }

        [HttpGet("{tenantId}/candidates/{id}/provenance")]
        public async Task<ActionResult<DecisionProvenance>> GetProvenance(
            string tenantId,
            string id,
            CancellationToken ct)
        {
            var prov = await _store.GetProvenanceAsync(tenantId, id, ct);
            if (prov == null)
            {
                return NotFound(new { message = $"Provenance for decision {id} not found." });
            }
            return Ok(prov);
        }
    }

    public class RankDecisionsRequest
    {
        public string TenantId { get; set; } = string.Empty;
        public List<DecisionCandidate> Candidates { get; set; } = new();
        public DecisionPolicy? Policy { get; set; }
    }
}
