using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Causal;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Kernel;
using BusinessModelApp.Core.Interfaces.Runtime.Intelligence.Causal;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/intelligence/causal")]
    public class CausalIntelligenceController : ControllerBase
    {
        private readonly ICausalIntelligenceEngine _causalEngine;

        public CausalIntelligenceController(ICausalIntelligenceEngine causalEngine)
        {
            _causalEngine = causalEngine ?? throw new ArgumentNullException(nameof(causalEngine));
        }

        [HttpGet("graph/{tenantId}")]
        public async Task<ActionResult<CausalGraph>> GetGraph(string tenantId, CancellationToken ct)
        {
            var graph = await _causalEngine.Graph.GetGraphAsync(tenantId, ct);
            return Ok(graph);
        }

        [HttpPost("graph/nodes")]
        public async Task<ActionResult<CausalNode>> AddNode([FromBody] AddNodeRequest request, CancellationToken ct)
        {
            var node = new CausalNode(
                NodeId: request.NodeId,
                TenantId: request.TenantId,
                MetricId: request.MetricId,
                Label: request.Label,
                NodeType: request.NodeType,
                EpistemicKind: request.EpistemicKind,
                CreatedAtUtc: DateTime.UtcNow);

            var created = await _causalEngine.Graph.AddNodeAsync(node, ct);
            return Ok(created);
        }

        [HttpPost("graph/edges")]
        public async Task<ActionResult<CausalEdge>> AddEdge([FromBody] AddEdgeRequest request, CancellationToken ct)
        {
            var edge = new CausalEdge(
                EdgeId: request.EdgeId ?? $"edge-{Guid.NewGuid():N}",
                TenantId: request.TenantId,
                SourceNodeId: request.SourceNodeId,
                TargetNodeId: request.TargetNodeId,
                EstimatedStrength: request.EstimatedStrength,
                MechanismDescription: request.MechanismDescription,
                Status: request.Status,
                Confidence: request.Confidence,
                IsBackdoorPath: request.IsBackdoorPath);

            var created = await _causalEngine.Graph.AddEdgeAsync(edge, ct);
            return Ok(created);
        }

        [HttpGet("graph/{tenantId}/topological-sort")]
        public async Task<ActionResult<IReadOnlyList<string>>> GetTopologicalSort(string tenantId, CancellationToken ct)
        {
            var order = await _causalEngine.Graph.GetTopologicalSortAsync(tenantId, ct);
            return Ok(order);
        }

        [HttpPost("hypotheses")]
        public async Task<ActionResult<CausalHypothesis>> FormulateHypothesis([FromBody] FormulateHypothesisRequest request, CancellationToken ct)
        {
            var hyp = await _causalEngine.Hypotheses.FormulateHypothesisAsync(
                request.TenantId,
                request.CauseMetricId,
                request.EffectMetricId,
                request.Mechanism,
                request.StatedAssumptions,
                request.AlternativeExplanationIds,
                ct);

            return Ok(hyp);
        }

        [HttpGet("hypotheses/{tenantId}")]
        public async Task<ActionResult<IReadOnlyList<CausalHypothesis>>> GetAllHypotheses(string tenantId, CancellationToken ct)
        {
            var list = await _causalEngine.Hypotheses.GetAllHypothesesAsync(tenantId, ct);
            return Ok(list);
        }

        [HttpGet("hypotheses/{tenantId}/{metricId}")]
        public async Task<ActionResult<IReadOnlyList<CausalHypothesis>>> GetHypothesesForMetric(string tenantId, string metricId, CancellationToken ct)
        {
            var list = await _causalEngine.Hypotheses.GetHypothesesForMetricAsync(tenantId, metricId, ct);
            return Ok(list);
        }

        [HttpPost("confounders/assess")]
        public async Task<ActionResult<ConfounderAssessment>> AssessConfounders([FromBody] ConfounderAssessRequest request, CancellationToken ct)
        {
            var result = await _causalEngine.Confounders.AssessConfoundersAsync(request.TenantId, request.CauseMetricId, request.EffectMetricId, ct);
            return Ok(result);
        }

        [HttpPost("temporal/investigate")]
        public async Task<ActionResult<TemporalPrecedenceResult>> InvestigateTemporalPrecedence([FromBody] TemporalInvestigateRequest request, CancellationToken ct)
        {
            var result = await _causalEngine.Temporal.InvestigateTemporalPrecedenceAsync(request.TenantId, request.MetricA, request.MetricB, null, ct);
            return Ok(result);
        }

        [HttpPost("interventions/simulate")]
        public async Task<ActionResult<InterventionSimulationResult>> SimulateIntervention([FromBody] InterventionSpec spec, CancellationToken ct)
        {
            var result = await _causalEngine.Interventions.SimulateInterventionAsync(spec, ct);
            return Ok(result);
        }

        [HttpPost("evidence/scorecard")]
        public async Task<ActionResult<CausalEvidenceScorecard>> EvaluateEvidenceScorecard([FromBody] EvaluateScorecardRequest request, CancellationToken ct)
        {
            var scorecard = await _causalEngine.Evaluator.EvaluateEvidenceAsync(request.TenantId, request.HypothesisId, ct);
            return Ok(scorecard);
        }

        [HttpPost("explain")]
        public async Task<ActionResult<CausalHypothesis>> ExplainCausalChain([FromBody] ExplainCausalRequest request, CancellationToken ct)
        {
            var explanation = await _causalEngine.EvaluateFullCausalExplanationAsync(
                request.TenantId, request.CauseMetricId, request.EffectMetricId, request.InitialMechanism, ct);
            return Ok(explanation);
        }
    }

    public record AddNodeRequest(string NodeId, string TenantId, string MetricId, string Label, CausalNodeType NodeType, EpistemicKind EpistemicKind);
    public record AddEdgeRequest(string? EdgeId, string TenantId, string SourceNodeId, string TargetNodeId, decimal EstimatedStrength, string MechanismDescription, CausalEdgeStatus Status, decimal Confidence, bool IsBackdoorPath = false);
    public record FormulateHypothesisRequest(string TenantId, string CauseMetricId, string EffectMetricId, string Mechanism, List<string>? StatedAssumptions = null, List<string>? AlternativeExplanationIds = null);
    public record ConfounderAssessRequest(string TenantId, string CauseMetricId, string EffectMetricId);
    public record TemporalInvestigateRequest(string TenantId, string MetricA, string MetricB);
    public record EvaluateScorecardRequest(string TenantId, string HypothesisId);
    public record ExplainCausalRequest(string TenantId, string CauseMetricId, string EffectMetricId, string InitialMechanism);
}
