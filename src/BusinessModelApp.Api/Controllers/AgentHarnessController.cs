using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Workforce;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/agent-harness")]
    public class AgentHarnessController : ControllerBase
    {
        private readonly IAgentHarnessService _harnessService;

        public AgentHarnessController(IAgentHarnessService harnessService)
        {
            _harnessService = harnessService ?? throw new ArgumentNullException(nameof(harnessService));
        }

        private string GetTenantId()
        {
            if (Request.Headers.TryGetValue("X-Tenant-ID", out var tenantId) && !string.IsNullOrWhiteSpace(tenantId))
            {
                return tenantId.ToString();
            }
            return "tenant-default";
        }

        [HttpPost("sessions")]
        public async Task<IActionResult> StartSession([FromBody] StartSessionRequest request)
        {
            var session = await _harnessService.StartSessionAsync(GetTenantId(), request.AgentId, request.MissionId);
            return Ok(session);
        }

        [HttpGet("sessions/{sessionId}")]
        public async Task<IActionResult> GetSession(string sessionId)
        {
            var session = await _harnessService.GetSessionAsync(GetTenantId(), sessionId);
            if (session == null) return NotFound();
            return Ok(session);
        }

        [HttpPost("sessions/{sessionId}/end")]
        public async Task<IActionResult> EndSession(string sessionId)
        {
            var success = await _harnessService.EndSessionAsync(GetTenantId(), sessionId);
            if (!success) return NotFound();
            return Ok(new { success = true });
        }

        [HttpPost("sessions/{sessionId}/trajectory")]
        public async Task<IActionResult> RecordTrajectoryStep(string sessionId, [FromBody] AgentTrajectoryStep step)
        {
            var traj = await _harnessService.RecordTrajectoryStepAsync(GetTenantId(), sessionId, step);
            return Ok(traj);
        }

        [HttpGet("sessions/{sessionId}/trajectory")]
        public async Task<IActionResult> GetTrajectory(string sessionId)
        {
            var traj = await _harnessService.GetTrajectoryAsync(GetTenantId(), sessionId);
            if (traj == null) return NotFound();
            return Ok(traj);
        }

        [HttpPost("spawn")]
        public async Task<IActionResult> SpawnSubAgent([FromBody] AgentSpawnRequest request)
        {
            try
            {
                var instance = await _harnessService.SpawnSubAgentAsync(GetTenantId(), request);
                return Ok(instance);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("instances/active")]
        public async Task<IActionResult> ListActiveInstances()
        {
            var list = await _harnessService.ListActiveInstancesAsync(GetTenantId());
            return Ok(list);
        }
    }

    public class StartSessionRequest
    {
        public string AgentId { get; set; } = string.Empty;
        public string MissionId { get; set; } = string.Empty;
    }
}
