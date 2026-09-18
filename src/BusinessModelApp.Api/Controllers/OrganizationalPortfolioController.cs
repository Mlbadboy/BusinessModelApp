using BusinessModelApp.Core.Domain.Runtime.Organizational.Allocation;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Portfolio;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers;

[ApiController]
[Route("api/portfolio")]
public class OrganizationalPortfolioController : ControllerBase
{
    private readonly IOrganizationalPortfolioService _portfolioService;
    private readonly IPortfolioRepository _repository;

    public OrganizationalPortfolioController(
        IOrganizationalPortfolioService portfolioService,
        IPortfolioRepository repository)
    {
        _portfolioService = portfolioService ?? throw new ArgumentNullException(nameof(portfolioService));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    private string GetTenantId() =>
        Request.Headers.TryGetValue("X-Tenant-ID", out var tenant) && !string.IsNullOrWhiteSpace(tenant)
            ? tenant.ToString()
            : "default-tenant";

    [HttpPost("plan")]
    public async Task<IActionResult> GeneratePlan([FromBody] GeneratePortfolioPlanRequest request)
    {
        var tenantId = GetTenantId();
        if (request?.CandidateItems == null || request.AllocationPlan == null)
        {
            return BadRequest(new { error = "CandidateItems array and AllocationPlan are required." });
        }

        foreach (var item in request.CandidateItems)
        {
            item.TenantId = tenantId;
        }

        var portfolio = await _portfolioService.GeneratePortfolioPlanAsync(
            tenantId,
            request.CandidateItems,
            request.AllocationPlan);

        return Ok(portfolio);
    }

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentPortfolio()
    {
        var tenantId = GetTenantId();
        var portfolio = await _repository.GetActivePortfolioAsync(tenantId);
        if (portfolio == null)
        {
            return NotFound(new { error = "No active portfolio found for tenant." });
        }
        return Ok(portfolio);
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetPortfolioHistory()
    {
        var tenantId = GetTenantId();
        var history = await _repository.ListPortfolioHistoryAsync(tenantId);
        return Ok(history);
    }

    [HttpPost("rebalance")]
    public async Task<IActionResult> RebalancePortfolio([FromBody] RebalancePortfolioRequest request)
    {
        var tenantId = GetTenantId();
        if (request?.Delta == null)
        {
            return BadRequest(new { error = "MaterialityDelta payload is required." });
        }

        var proposal = await _portfolioService.RebalancePortfolioAsync(
            tenantId,
            request.Delta,
            request.IncomingCandidates);

        if (proposal == null)
        {
            return Ok(new { status = "NoRebalanceNeeded", message = "Materiality delta was below threshold; no rebalance proposal generated (I33-Q)." });
        }

        return Ok(proposal);
    }

    [HttpGet("rebalance/{proposalId}/items/{workItemId}/why")]
    public async Task<IActionResult> GetRebalanceWhyTrace(string proposalId, string workItemId)
    {
        var tenantId = GetTenantId();
        try
        {
            var trace = await _portfolioService.GetRebalanceActionWhyTraceAsync(tenantId, proposalId, workItemId);
            return Ok(trace);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost("simulate")]
    public async Task<IActionResult> RunSimulation([FromBody] RunSimulationRequest request)
    {
        var tenantId = GetTenantId();
        if (request?.Scenarios == null || request.Scenarios.Count == 0)
        {
            return BadRequest(new { error = "Scenarios array is required." });
        }

        foreach (var sc in request.Scenarios)
        {
            sc.TenantId = tenantId;
        }

        var result = await _portfolioService.RunSimulationSandboxAsync(tenantId, request.Scenarios);
        return Ok(result);
    }

    [HttpPost("items/{workItemId}/stop")]
    public async Task<IActionResult> ProposeStop(string workItemId, [FromBody] StopItemRequest request)
    {
        var tenantId = GetTenantId();
        try
        {
            var proposal = await _portfolioService.ProposeItemStopOrSupersedeAsync(
                tenantId,
                workItemId,
                request?.Rationale ?? "Governed termination proposal.",
                request?.IsSupersede ?? false);

            return Ok(proposal);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}

public class GeneratePortfolioPlanRequest
{
    public List<PortfolioWorkItem> CandidateItems { get; set; } = new();
    public AllocationPlan AllocationPlan { get; set; } = new();
}

public class RebalancePortfolioRequest
{
    public MaterialityDelta Delta { get; set; } = new();
    public List<PortfolioWorkItem>? IncomingCandidates { get; set; }
}

public class RunSimulationRequest
{
    public List<PortfolioSimulationScenario> Scenarios { get; set; } = new();
}

public class StopItemRequest
{
    public string Rationale { get; set; } = string.Empty;
    public bool IsSupersede { get; set; } = false;
}
