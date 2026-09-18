using BusinessModelApp.Core.Domain.Runtime.Enterprise.Brain;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Brain;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AutonomousBusinessBrainController : ControllerBase
{
    private readonly IAutonomousBusinessBrainService _brainService;

    public AutonomousBusinessBrainController(IAutonomousBusinessBrainService brainService)
    {
        _brainService = brainService ?? throw new ArgumentNullException(nameof(brainService));
    }

    private string ResolveTenantId()
    {
        if (Request.Headers.TryGetValue("X-Tenant-ID", out var tenantVal) && !string.IsNullOrWhiteSpace(tenantVal))
        {
            return tenantVal.ToString();
        }
        return "default";
    }

    [HttpGet("state")]
    public async Task<IActionResult> GetCurrentState()
    {
        var tenantId = ResolveTenantId();
        var state = await _brainService.GetCurrentCognitiveStateAsync(tenantId);
        return Ok(state);
    }

    [HttpPost("synthesize")]
    public async Task<IActionResult> ForceSynthesis()
    {
        var tenantId = ResolveTenantId();
        var state = await _brainService.ForceCognitiveSynthesisAsync(tenantId);
        return Ok(state);
    }

    [HttpGet("deltas")]
    public async Task<IActionResult> GetRecentDeltas()
    {
        var tenantId = ResolveTenantId();
        var deltas = await _brainService.GetRecentDeltasAsync(tenantId);
        return Ok(deltas);
    }

    [HttpGet("priorities")]
    public async Task<IActionResult> GetAttentionPriorities()
    {
        var tenantId = ResolveTenantId();
        var priorities = await _brainService.GetAttentionPrioritiesAsync(tenantId);
        return Ok(priorities);
    }

    [HttpGet("unknowns")]
    public async Task<IActionResult> GetEpistemicGaps()
    {
        var tenantId = ResolveTenantId();
        var unknowns = await _brainService.GetEpistemicGapsAsync(tenantId);
        return Ok(unknowns);
    }

    [HttpGet("contradictions")]
    public async Task<IActionResult> GetCognitiveContradictions()
    {
        var tenantId = ResolveTenantId();
        var contradictions = await _brainService.GetCognitiveContradictionsAsync(tenantId);
        return Ok(contradictions);
    }

    [HttpGet("escalations")]
    public async Task<IActionResult> GetExecutiveEscalations()
    {
        var tenantId = ResolveTenantId();
        var escalations = await _brainService.GetExecutiveEscalationsAsync(tenantId);
        return Ok(escalations);
    }

    [HttpGet("snapshots/{id}")]
    public async Task<IActionResult> GetSnapshot(string id)
    {
        var tenantId = ResolveTenantId();
        try
        {
            var snapshot = await _brainService.GetSnapshotByIdAsync(tenantId, id);
            if (snapshot == null) return NotFound(new { error = $"Snapshot '{id}' not found." });
            return Ok(snapshot);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
    }

    [HttpGet("capabilities")]
    public IActionResult GetCapabilities()
    {
        return Ok(new
        {
            batch = "Batch 4.0 Autonomous Business Brain",
            primaryInvariant = AutonomousBusinessBrainInvariants.PrimaryInvariant,
            nonExecutionPrinciple = AutonomousBusinessBrainInvariants.I36_I_NonExecutionPrinciple,
            explicitUnknowns = AutonomousBusinessBrainInvariants.I36_G_ExplicitUnknowns,
            contradictionResolution = AutonomousBusinessBrainInvariants.I36_Q_ContradictionResolution,
            prg1EscalationQueue = AutonomousBusinessBrainInvariants.I36_U_PRG1EscalationQueue,
            multiTenantIsolation = AutonomousBusinessBrainInvariants.I36_H_MultiTenantCognitiveIsolation
        });
    }
}
