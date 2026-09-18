using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContinuousWatchtowerController : ControllerBase
    {
        private readonly IContinuousWatchtowerService _watchtowerService;

        public ContinuousWatchtowerController(IContinuousWatchtowerService watchtowerService)
        {
            _watchtowerService = watchtowerService ?? throw new ArgumentNullException(nameof(watchtowerService));
        }

        private string GetTenantId() =>
            Request.Headers.TryGetValue("X-Tenant-ID", out var tenant) && !string.IsNullOrWhiteSpace(tenant)
                ? tenant.ToString()
                : "default-tenant";

        [HttpPost("events")]
        public async Task<IActionResult> IngestEvent([FromBody] IngestEventRequest request, CancellationToken ct)
        {
            var tenantId = GetTenantId();
            if (request == null) return BadRequest(new { error = "Request payload is required." });

            var (accepted, signal, reason) = await _watchtowerService.IngestEventAsync(
                tenantId, request.SourceSystem, request.EventType, request.EntityId,
                request.SourceRecordId, request.EvidenceHash, request.ObservedValue,
                request.BaselineValue, request.MetricName ?? "DefaultMetric", null, ct);

            if (!accepted)
            {
                return BadRequest(new { error = reason });
            }

            return Ok(new { accepted = true, signal, reason });
        }

        [HttpGet("signals/active")]
        public async Task<IActionResult> ListActiveSignals(CancellationToken ct)
        {
            var tenantId = GetTenantId();
            var signals = await _watchtowerService.ListActiveSignalsAsync(tenantId, ct);
            return Ok(signals);
        }

        [HttpGet("signals/{signalId}")]
        public async Task<IActionResult> GetSignal(string signalId, CancellationToken ct)
        {
            var tenantId = GetTenantId();
            if (string.IsNullOrWhiteSpace(signalId)) return BadRequest(new { error = "SignalId is required." });

            var signal = await _watchtowerService.GetSignalAsync(tenantId, signalId, ct);
            if (signal == null)
            {
                return NotFound(new { error = $"Signal '{signalId}' not found." });
            }

            return Ok(signal);
        }

        [HttpGet("signals/{signalId}/why")]
        public async Task<IActionResult> GetWhyTrace(string signalId, CancellationToken ct)
        {
            var tenantId = GetTenantId();
            if (string.IsNullOrWhiteSpace(signalId)) return BadRequest(new { error = "SignalId is required." });

            var whyTrace = await _watchtowerService.GetWhyExplanationAsync(tenantId, signalId, ct);
            if (whyTrace == null)
            {
                return NotFound(new { error = $"Why trace for signal '{signalId}' not found." });
            }

            return Ok(whyTrace);
        }

        [HttpGet("conditions")]
        public async Task<IActionResult> ListConditions(CancellationToken ct)
        {
            var tenantId = GetTenantId();
            var conditions = await _watchtowerService.ListConditionsAsync(tenantId, ct);
            return Ok(conditions);
        }

        [HttpPost("signals/{signalId}/propose-work")]
        public async Task<IActionResult> ProposeWork(string signalId, CancellationToken ct)
        {
            var tenantId = GetTenantId();
            if (string.IsNullOrWhiteSpace(signalId)) return BadRequest(new { error = "SignalId is required." });

            var (generated, proposalId, error) = await _watchtowerService.ProposeWorkFromSignalAsync(tenantId, signalId, ct);
            if (!generated)
            {
                return BadRequest(new { error });
            }

            return Ok(new { workProposalId = proposalId });
        }

        public class IngestEventRequest
        {
            public string SourceSystem { get; set; } = string.Empty;
            public string EventType { get; set; } = string.Empty;
            public string EntityId { get; set; } = string.Empty;
            public string SourceRecordId { get; set; } = string.Empty;
            public string EvidenceHash { get; set; } = string.Empty;
            public double ObservedValue { get; set; } = 0.0;
            public double BaselineValue { get; set; } = 0.0;
            public string? MetricName { get; set; }
        }
    }
}
