using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Forecasting;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Kernel;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Radar;
using BusinessModelApp.Core.Interfaces.Runtime.Intelligence.Radar;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/intelligence/radar")]
    public class OpportunityThreatRadarController : ControllerBase
    {
        private readonly IOpportunityThreatRadarOrchestrator _orchestrator;
        private readonly IRadarSignalStore _store;
        private readonly ISignificanceScorer _scorer;

        public OpportunityThreatRadarController(
            IOpportunityThreatRadarOrchestrator orchestrator,
            IRadarSignalStore store,
            ISignificanceScorer scorer)
        {
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _scorer = scorer ?? throw new ArgumentNullException(nameof(scorer));
        }

        [HttpGet("{tenantId}/opportunities")]
        public async Task<ActionResult<IReadOnlyList<RadarSignal>>> GetOpportunities(string tenantId, CancellationToken ct)
        {
            var list = await _store.GetSignalsAsync(tenantId, RadarSignalType.Opportunity, null, ct);
            return Ok(list);
        }

        [HttpGet("{tenantId}/threats")]
        public async Task<ActionResult<IReadOnlyList<RadarSignal>>> GetThreats(string tenantId, CancellationToken ct)
        {
            var list = await _store.GetSignalsAsync(tenantId, RadarSignalType.Threat, null, ct);
            return Ok(list);
        }

        [HttpGet("{tenantId}/signals/{id}")]
        public async Task<ActionResult<RadarSignal>> GetSignal(string tenantId, string id, CancellationToken ct)
        {
            var signal = await _store.GetSignalAsync(tenantId, id, ct);
            if (signal == null) return NotFound(new { message = $"Signal {id} not found for tenant {tenantId}" });
            return Ok(signal);
        }

        [HttpPost("detect")]
        public async Task<ActionResult<RadarScanResult>> TriggerRadarScan([FromBody] TriggerRadarScanRequest request, CancellationToken ct)
        {
            var result = await _orchestrator.ExecuteRadarScanAsync(
                request.TenantId,
                request.Observations ?? new List<KpiObservation>(),
                request.Forecasts ?? new List<ForecastOutput>(),
                request.RegimeAssessments,
                ct);
            return Ok(result);
        }

        [HttpPost("signals/{id}/status")]
        public async Task<ActionResult> UpdateSignalStatus(string id, [FromBody] UpdateSignalStatusRequest request, CancellationToken ct)
        {
            var success = await _store.UpdateLifecycleStateAsync(request.TenantId, id, request.NewState, request.Reason, ct);
            if (!success) return NotFound(new { message = $"Signal {id} not found or transition invalid" });
            return Ok(new { message = "Status updated successfully", signalId = id, newState = request.NewState.ToString() });
        }

        [HttpGet("{tenantId}/policy")]
        public ActionResult<RadarSignificancePolicy> GetPolicy(string tenantId)
        {
            var policy = _scorer.GetActivePolicy(tenantId);
            return Ok(policy);
        }

        [HttpPost("policy")]
        public ActionResult RegisterPolicy([FromBody] RadarSignificancePolicy policy)
        {
            _scorer.RegisterPolicy(policy);
            return Ok(policy);
        }
    }

    public class TriggerRadarScanRequest
    {
        public string TenantId { get; set; } = string.Empty;
        public List<KpiObservation>? Observations { get; set; }
        public List<ForecastOutput>? Forecasts { get; set; }
        public List<RegimeAssessment>? RegimeAssessments { get; set; }
    }

    public class UpdateSignalStatusRequest
    {
        public string TenantId { get; set; } = string.Empty;
        public RadarSignalLifecycleState NewState { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
