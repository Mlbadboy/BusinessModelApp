using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MultiAgentCollaborationController : ControllerBase
    {
        private readonly IMultiAgentCollaborationService _collaborationService;

        public MultiAgentCollaborationController(IMultiAgentCollaborationService collaborationService)
        {
            _collaborationService = collaborationService ?? throw new ArgumentNullException(nameof(collaborationService));
        }

        private string GetTenantId() =>
            Request.Headers.TryGetValue("X-Tenant-ID", out var tenant) && !string.IsNullOrWhiteSpace(tenant)
                ? tenant.ToString()
                : "default-tenant";

        [HttpGet("charters/{charterId}")]
        public async Task<IActionResult> GetCharter(string charterId, CancellationToken ct)
        {
            var tenantId = GetTenantId();
            if (string.IsNullOrWhiteSpace(charterId)) return BadRequest(new { error = "CharterId is required." });

            var charter = await _collaborationService.GetCharterAsync(tenantId, charterId, ct);
            if (charter == null)
            {
                return NotFound(new { error = $"Charter '{charterId}' not found." });
            }

            return Ok(charter);
        }

        [HttpGet("charters/active")]
        public async Task<IActionResult> ListActiveCharters(CancellationToken ct)
        {
            var tenantId = GetTenantId();
            var active = await _collaborationService.ListActiveChartersAsync(tenantId, ct);
            return Ok(active);
        }

        [HttpGet("charters/{charterId}/messages")]
        public async Task<IActionResult> GetMessages(string charterId, CancellationToken ct)
        {
            var tenantId = GetTenantId();
            if (string.IsNullOrWhiteSpace(charterId)) return BadRequest(new { error = "CharterId is required." });

            var messages = await _collaborationService.GetMessagesAsync(tenantId, charterId, ct);
            return Ok(messages);
        }

        [HttpGet("charters/{charterId}/disputes")]
        public async Task<IActionResult> GetDisputes(string charterId, CancellationToken ct)
        {
            var tenantId = GetTenantId();
            if (string.IsNullOrWhiteSpace(charterId)) return BadRequest(new { error = "CharterId is required." });

            var disputes = await _collaborationService.GetDisputesAsync(tenantId, charterId, ct);
            return Ok(disputes);
        }

        [HttpPost("charters/{charterId}/dissolve")]
        public async Task<IActionResult> DissolveCharter(string charterId, [FromBody] DissolveRequest? request, CancellationToken ct)
        {
            var tenantId = GetTenantId();
            if (string.IsNullOrWhiteSpace(charterId)) return BadRequest(new { error = "CharterId is required." });

            var reason = request?.Reason ?? "Manual dissolution request.";
            var (dissolved, error) = await _collaborationService.DissolveTeamAsync(tenantId, charterId, reason, ct);
            if (!dissolved)
            {
                return BadRequest(new { error });
            }

            return Ok(new { status = "Dissolved", reason });
        }

        public class DissolveRequest
        {
            public string? Reason { get; set; }
        }
    }
}
