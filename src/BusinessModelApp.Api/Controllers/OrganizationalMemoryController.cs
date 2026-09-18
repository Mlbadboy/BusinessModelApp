using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Organizational;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrganizationalMemoryController : ControllerBase
    {
        private readonly IOrganizationalMemoryService _memoryService;

        public OrganizationalMemoryController(IOrganizationalMemoryService memoryService)
        {
            _memoryService = memoryService ?? throw new ArgumentNullException(nameof(memoryService));
        }

        private string GetTenantId() =>
            Request.Headers.TryGetValue("X-Tenant-ID", out var tenant) && !string.IsNullOrWhiteSpace(tenant)
                ? tenant.ToString()
                : "default-tenant";

        [HttpGet("context/work/{workId}")]
        public async Task<IActionResult> GetContextForWork(string workId, CancellationToken ct)
        {
            var tenantId = GetTenantId();
            if (string.IsNullOrWhiteSpace(workId))
            {
                return BadRequest(new { error = "WorkId is required." });
            }

            var snapshot = await _memoryService.GetContextForWorkAsync(tenantId, workId, null, ct);
            return Ok(snapshot);
        }

        [HttpGet("trajectory/{workId}")]
        public async Task<IActionResult> GetTrajectoryForWork(string workId, CancellationToken ct)
        {
            var tenantId = GetTenantId();
            if (string.IsNullOrWhiteSpace(workId))
            {
                return BadRequest(new { error = "WorkId is required." });
            }

            var trajectory = await _memoryService.GetTrajectoryForWorkAsync(tenantId, workId, ct);
            if (trajectory == null)
            {
                return NotFound(new { error = $"Trajectory for work '{workId}' not found." });
            }
            return Ok(trajectory);
        }

        [HttpGet("antipatterns")]
        public async Task<IActionResult> GetAntiPatterns([FromQuery] string? domain, CancellationToken ct)
        {
            var tenantId = GetTenantId();
            var list = await _memoryService.GetAntiPatternsForDomainAsync(tenantId, domain ?? string.Empty, ct);
            return Ok(list);
        }

        [HttpPost("freshness/evaluate")]
        public async Task<IActionResult> EvaluateFreshness(
            [FromBody] EvaluateFreshnessRequest request,
            CancellationToken ct)
        {
            var tenantId = GetTenantId();
            if (request == null || string.IsNullOrWhiteSpace(request.PrecedentId))
            {
                return BadRequest(new { error = "PrecedentId is required." });
            }

            var age = TimeSpan.FromDays(request.AgeInDays);
            var result = await _memoryService.EvaluateMemoryFreshnessAsync(
                tenantId,
                request.PrecedentId,
                age,
                request.CurrentRegime,
                ct);

            return Ok(result);
        }

        [HttpGet("{memoryId}/provenance")]
        public async Task<IActionResult> GetProvenance(string memoryId, CancellationToken ct)
        {
            var tenantId = GetTenantId();
            if (string.IsNullOrWhiteSpace(memoryId))
            {
                return BadRequest(new { error = "MemoryId is required." });
            }

            var lineage = await _memoryService.GetMemoryProvenanceAsync(tenantId, memoryId, ct);
            if (lineage == null)
            {
                return NotFound(new { error = $"Provenance lineage for memory '{memoryId}' not found." });
            }
            return Ok(lineage);
        }
    }

    public record EvaluateFreshnessRequest
    {
        public string PrecedentId { get; init; } = string.Empty;
        public double AgeInDays { get; init; } = 0;
        public string? CurrentRegime { get; init; }
    }
}
