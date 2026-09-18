using BusinessModelApp.Core.Domain.Runtime.Organizational.Allocation;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers;

[ApiController]
[Route("api/allocation")]
public class OrganizationalAllocationController : ControllerBase
{
    private readonly IOrganizationalAllocationService _allocationService;
    private readonly IResourceRegistry _resourceRegistry;
    private readonly IAllocationLedger _ledger;

    public OrganizationalAllocationController(
        IOrganizationalAllocationService allocationService,
        IResourceRegistry resourceRegistry,
        IAllocationLedger ledger)
    {
        _allocationService = allocationService ?? throw new ArgumentNullException(nameof(allocationService));
        _resourceRegistry = resourceRegistry ?? throw new ArgumentNullException(nameof(resourceRegistry));
        _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
    }

    private string GetTenantId() =>
        Request.Headers.TryGetValue("X-Tenant-ID", out var tenant) && !string.IsNullOrWhiteSpace(tenant)
            ? tenant.ToString()
            : "default-tenant";

    [HttpPost("plan")]
    public async Task<IActionResult> GeneratePlan([FromBody] GeneratePlanRequest request)
    {
        var tenantId = GetTenantId();
        if (request?.Demands == null)
        {
            return BadRequest(new { error = "Demands array is required." });
        }

        foreach (var demand in request.Demands)
        {
            demand.TenantId = tenantId;
        }

        var plan = await _allocationService.GenerateAllocationPlanAsync(tenantId, request.Demands);
        return Ok(plan);
    }

    [HttpGet("resources")]
    public async Task<IActionResult> GetResources()
    {
        var tenantId = GetTenantId();
        var resources = await _resourceRegistry.ListResourcesAsync(tenantId);
        return Ok(resources);
    }

    [HttpPost("resources")]
    public async Task<IActionResult> RegisterResource([FromBody] OrganizationalResource resource)
    {
        var tenantId = GetTenantId();
        if (resource == null) return BadRequest(new { error = "Resource payload is required." });

        resource.TenantId = tenantId;
        await _resourceRegistry.RegisterResourceAsync(tenantId, resource);
        return Ok(resource);
    }

    [HttpGet("records")]
    public async Task<IActionResult> ListRecords()
    {
        var tenantId = GetTenantId();
        var records = await _ledger.ListRecordsAsync(tenantId);
        return Ok(records);
    }

    [HttpGet("records/{id}")]
    public async Task<IActionResult> GetRecord(string id)
    {
        var tenantId = GetTenantId();
        var record = await _ledger.GetRecordAsync(tenantId, id);
        if (record == null) return NotFound(new { error = $"Allocation record '{id}' not found." });
        return Ok(record);
    }

    [HttpGet("records/{id}/why")]
    public async Task<IActionResult> GetWhyTrace(string id)
    {
        var tenantId = GetTenantId();
        try
        {
            var trace = await _allocationService.GetWhyAllocationTraceAsync(tenantId, id);
            return Ok(trace);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = $"Allocation record '{id}' not found." });
        }
    }

    [HttpPost("records/{id}/release")]
    public async Task<IActionResult> ReleaseReservation(string id)
    {
        var tenantId = GetTenantId();
        var success = await _allocationService.ReleaseAllocationReservationAsync(tenantId, id);
        if (!success) return NotFound(new { error = $"Allocation record '{id}' could not be released or is already released/expired." });
        return Ok(new { status = "Released", allocationId = id });
    }

    [HttpGet("debt")]
    public async Task<IActionResult> GetResourceDebt()
    {
        var tenantId = GetTenantId();
        var debts = await _allocationService.GetTenantResourceDebtsAsync(tenantId);
        return Ok(debts);
    }
}

public class GeneratePlanRequest
{
    public List<AllocationDemand> Demands { get; set; } = new();
}
