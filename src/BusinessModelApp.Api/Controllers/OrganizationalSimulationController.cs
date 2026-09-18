using BusinessModelApp.Core.Domain.Runtime.Organizational.Simulation;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational.Simulation;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class OrganizationalSimulationController : ControllerBase
{
    private readonly IOrganizationalSimulationService _simulationService;

    public OrganizationalSimulationController(IOrganizationalSimulationService simulationService)
    {
        _simulationService = simulationService ?? throw new ArgumentNullException(nameof(simulationService));
    }

    private string ResolveTenantId()
    {
        if (Request.Headers.TryGetValue("X-Tenant-ID", out var tenantVal) && !string.IsNullOrWhiteSpace(tenantVal))
        {
            return tenantVal.ToString();
        }
        return "default";
    }

    [HttpPost("scenarios")]
    public async Task<IActionResult> CreateScenario([FromBody] SimulationScenario scenario)
    {
        var tenantId = ResolveTenantId();
        var created = await _simulationService.CreateScenarioAsync(tenantId, scenario);
        return CreatedAtAction(nameof(GetScenario), new { id = created.ScenarioId }, created);
    }

    [HttpGet("scenarios")]
    public async Task<IActionResult> ListScenarios()
    {
        var tenantId = ResolveTenantId();
        var scenarios = await _simulationService.ListScenariosAsync(tenantId);
        return Ok(scenarios);
    }

    [HttpGet("scenarios/{id}")]
    public async Task<IActionResult> GetScenario(string id)
    {
        var tenantId = ResolveTenantId();
        var scenario = await _simulationService.GetScenarioAsync(tenantId, id);
        if (scenario == null) return NotFound(new { error = $"Scenario '{id}' not found." });
        return Ok(scenario);
    }

    [HttpPost("scenarios/{id}/branch")]
    public async Task<IActionResult> BranchScenario(string id, [FromBody] SimulationBranchScenarioRequest request)
    {
        var tenantId = ResolveTenantId();
        var branch = await _simulationService.BranchScenarioAsync(tenantId, id, request.Hypothesis);
        return Ok(branch);
    }

    [HttpPost("runs")]
    public async Task<IActionResult> LaunchRun([FromBody] LaunchSimulationRunRequest request)
    {
        var tenantId = ResolveTenantId();
        var run = await _simulationService.LaunchSimulationRunAsync(tenantId, request.ScenarioId, request.ProviderId);
        return Ok(run);
    }

    [HttpGet("runs/{id}")]
    public async Task<IActionResult> GetRun(string id)
    {
        var tenantId = ResolveTenantId();
        var status = await _simulationService.GetRunStatusAsync(tenantId, id);
        if (status == null) return NotFound(new { error = $"Simulation run '{id}' not found." });
        return Ok(status);
    }

    [HttpGet("runs/{id}/status")]
    public async Task<IActionResult> GetRunStatus(string id)
    {
        return await GetRun(id);
    }

    [HttpGet("runs/{id}/outcomes")]
    public async Task<IActionResult> GetRunOutcomes(string id)
    {
        var tenantId = ResolveTenantId();
        var outcomes = await _simulationService.GetRunOutcomesAsync(tenantId, id);
        return Ok(outcomes);
    }

    [HttpGet("runs/{id}/provenance")]
    public async Task<IActionResult> GetRunProvenance(string id)
    {
        var tenantId = ResolveTenantId();
        try
        {
            var trace = await _simulationService.GetRunProvenanceAsync(tenantId, id);
            return Ok(trace);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost("compare")]
    public async Task<IActionResult> CompareScenarios([FromBody] SimulationCompareScenariosRequest request)
    {
        var tenantId = ResolveTenantId();
        var comparison = await _simulationService.CompareScenariosAsync(tenantId, request.ScenarioIds);
        return Ok(comparison);
    }

    [HttpPost("calibrate")]
    public async Task<IActionResult> Calibrate([FromBody] SimulationCalibrateOutcomeRequest request)
    {
        var tenantId = ResolveTenantId();
        var record = await _simulationService.CalibrateOutcomeAsync(
            tenantId,
            request.ScenarioType,
            request.PredictedValue,
            request.ActualValue);
        return Ok(record);
    }

    [HttpGet("providers")]
    public async Task<IActionResult> ListProviders()
    {
        var providers = await _simulationService.ListProvidersAsync();
        return Ok(providers.Select(p => new
        {
            p.ProviderId,
            p.ProviderName,
            p.ProviderVersion,
            p.SupportsMultiAgent
        }));
    }

    [HttpGet("capabilities")]
    public IActionResult GetCapabilities()
    {
        return Ok(new
        {
            PrimaryInvariant = OrganizationalSimulationInvariants.PrimaryInvariant,
            TruthClassification = OrganizationalSimulationInvariants.TruthClassificationSimulation,
            ExecutionFirewalled = true,
            SupportsCounterfactualBranching = true,
            SupportsMultiAgent = true,
            BatchSovereignty = "Batch 3.9.9 Certified"
        });
    }
}

public sealed class SimulationBranchScenarioRequest
{
    public string Hypothesis { get; set; } = string.Empty;
}

public sealed class LaunchSimulationRunRequest
{
    public string ScenarioId { get; set; } = string.Empty;
    public string? ProviderId { get; set; }
}

public sealed class SimulationCompareScenariosRequest
{
    public List<string> ScenarioIds { get; set; } = new();
}

public sealed class SimulationCalibrateOutcomeRequest
{
    public string ScenarioType { get; set; } = "General";
    public double PredictedValue { get; set; }
    public double ActualValue { get; set; }
}
