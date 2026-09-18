using BusinessModelApp.Core.Domain.Runtime.Enterprise.Responsibility;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Responsibility;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DurableResponsibilityController : ControllerBase
{
    private readonly IDurableResponsibilityService _responsibilityService;

    public DurableResponsibilityController(IDurableResponsibilityService responsibilityService)
    {
        _responsibilityService = responsibilityService ?? throw new ArgumentNullException(nameof(responsibilityService));
    }

    private string ResolveTenantId()
    {
        if (Request.Headers.TryGetValue("X-Tenant-ID", out var tenantVal) && !string.IsNullOrWhiteSpace(tenantVal))
        {
            return tenantVal.ToString();
        }
        return "default";
    }

    [HttpPost("cycles/step")]
    public async Task<IActionResult> ExecuteCycleStep([FromBody] ExecuteCycleStepRequest? request)
    {
        var tenantId = ResolveTenantId();
        var reason = request?.TriggerReason ?? CycleTriggerReason.ScheduledHeartbeat;
        var cycle = await _responsibilityService.ExecuteSingleCycleAsync(tenantId, reason);
        return Ok(cycle);
    }

    [HttpGet("cycles/latest")]
    public async Task<IActionResult> GetLatestCycle()
    {
        var tenantId = ResolveTenantId();
        var cycle = await _responsibilityService.GetLatestCycleAsync(tenantId);
        if (cycle == null) return NotFound(new { error = "No responsibility cycles found for tenant." });
        return Ok(cycle);
    }

    [HttpGet("cycles")]
    public async Task<IActionResult> ListCycles([FromQuery] int limit = 50)
    {
        var tenantId = ResolveTenantId();
        var cycles = await _responsibilityService.ListCyclesAsync(tenantId, limit);
        return Ok(cycles);
    }

    [HttpGet("cycles/{id}")]
    public async Task<IActionResult> GetCycle(string id)
    {
        var tenantId = ResolveTenantId();
        try
        {
            var cycle = await _responsibilityService.GetCycleByIdAsync(tenantId, id);
            if (cycle == null) return NotFound(new { error = $"Cycle '{id}' not found." });
            return Ok(cycle);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
    }

    [HttpGet("checkpoints")]
    public async Task<IActionResult> ListCheckpoints([FromQuery] int limit = 50)
    {
        var tenantId = ResolveTenantId();
        var checkpoints = await _responsibilityService.ListCheckpointsAsync(tenantId, limit);
        return Ok(checkpoints);
    }

    [HttpPost("pause")]
    public async Task<IActionResult> Pause([FromBody] PauseCycleRequest request)
    {
        var tenantId = ResolveTenantId();
        await _responsibilityService.PauseCyclesAsync(tenantId, request.HumanSupervisorId, request.Reason);
        return Ok(new { status = "Paused", tenantId, supervisor = request.HumanSupervisorId });
    }

    [HttpPost("resume")]
    public async Task<IActionResult> Resume([FromBody] ResumeCycleRequest request)
    {
        var tenantId = ResolveTenantId();
        await _responsibilityService.ResumeCyclesAsync(tenantId, request.HumanSupervisorId);
        return Ok(new { status = "Resumed", tenantId, supervisor = request.HumanSupervisorId });
    }

    [HttpGet("capabilities")]
    public IActionResult GetCapabilities()
    {
        return Ok(new
        {
            batch = "Batch 4.1 Continuous Responsibility & Mission Loop",
            primaryInvariant = DurableResponsibilityInvariants.PrimaryInvariant,
            boundedReasoning = DurableResponsibilityInvariants.I37_C_BoundedReasoningBudget,
            governanceGate = DurableResponsibilityInvariants.I37_E_GovernanceGateEnforcement,
            nonExecution = DurableResponsibilityInvariants.I37_F_NonExecutionSovereignty,
            checkpointImmutability = DurableResponsibilityInvariants.I37_J_CheckpointImmutability,
            humanPauseOverride = DurableResponsibilityInvariants.I37_I_HumanPauseOverride
        });
    }
}

public sealed class ExecuteCycleStepRequest
{
    public CycleTriggerReason TriggerReason { get; set; } = CycleTriggerReason.ScheduledHeartbeat;
}

public sealed class PauseCycleRequest
{
    public string HumanSupervisorId { get; set; } = "ExecutiveUser";
    public string Reason { get; set; } = "Manual inspection requested";
}

public sealed class ResumeCycleRequest
{
    public string HumanSupervisorId { get; set; } = "ExecutiveUser";
}
