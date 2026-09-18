using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Workforce;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/continuous-operations")]
    public class ContinuousOperationsController : ControllerBase
    {
        private readonly IContinuousBusinessOperationsCoordinator _coordinator;

        public ContinuousOperationsController(IContinuousBusinessOperationsCoordinator coordinator)
        {
            _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        }

        private string GetTenantId() =>
            Request.Headers.TryGetValue("X-Tenant-ID", out var t) && !string.IsNullOrWhiteSpace(t) ? t.ToString() : "tenant-default";

        [HttpPost("cycles/trigger")]
        public async Task<IActionResult> TriggerCycle([FromBody] TriggerCycleRequest request)
        {
            var trigger = new BusinessCycleTrigger
            {
                TriggerType = request.TriggerType,
                SourcePayloadJson = request.PayloadJson ?? "{}"
            };
            var cycle = await _coordinator.TriggerCycleAsync(GetTenantId(), trigger, request.Budget);
            return Ok(cycle);
        }

        [HttpGet("cycles/{cycleId}")]
        public async Task<IActionResult> GetCycle(string cycleId)
        {
            var cycle = await _coordinator.GetCycleAsync(GetTenantId(), cycleId);
            if (cycle == null) return NotFound();
            return Ok(cycle);
        }

        [HttpGet("cycles")]
        public async Task<IActionResult> ListCycles()
        {
            var list = await _coordinator.ListCyclesAsync(GetTenantId());
            return Ok(list);
        }

        [HttpPost("cycles/{cycleId}/checkpoint")]
        public async Task<IActionResult> AdvanceCheckpoint(string cycleId, [FromBody] AdvanceCheckpointRequest request)
        {
            var success = await _coordinator.AdvanceCheckpointAsync(GetTenantId(), cycleId, request.NextState, request.IncrementalCost);
            if (!success) return BadRequest(new { error = "Cannot advance checkpoint (timed out, over budget, or completed)." });
            return Ok(new { success });
        }

        [HttpPost("cycles/{cycleId}/complete")]
        public async Task<IActionResult> CompleteCycle(string cycleId, [FromBody] CompleteCycleRequest request)
        {
            var success = await _coordinator.CompleteCycleAsync(GetTenantId(), cycleId, request.OutcomeSummary ?? "Completed successfully");
            if (!success) return NotFound();
            return Ok(new { success });
        }

        [HttpPost("recover")]
        public async Task<IActionResult> RecoverLastCheckpoint()
        {
            var cycle = await _coordinator.RecoverLastCheckpointAsync(GetTenantId());
            return Ok(new { recoveredCycle = cycle });
        }
    }

    public class TriggerCycleRequest
    {
        public BusinessCycleTriggerType TriggerType { get; set; } = BusinessCycleTriggerType.SCHEDULED_TIMER;
        public string? PayloadJson { get; set; }
        public decimal Budget { get; set; } = 100m;
    }

    public class AdvanceCheckpointRequest
    {
        public string NextState { get; set; } = string.Empty;
        public decimal IncrementalCost { get; set; } = 0m;
    }

    public class CompleteCycleRequest
    {
        public string? OutcomeSummary { get; set; }
    }
}
