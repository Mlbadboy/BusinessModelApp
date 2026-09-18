using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Workforce;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/workforce-constitution")]
    public class WorkforceConstitutionController : ControllerBase
    {
        private readonly IWorkforceConstitutionService _service;

        public WorkforceConstitutionController(IWorkforceConstitutionService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        private string GetTenantId()
        {
            if (Request.Headers.TryGetValue("X-Tenant-ID", out var tenantId) && !string.IsNullOrWhiteSpace(tenantId))
            {
                return tenantId.ToString();
            }
            return "tenant-default";
        }

        [HttpGet("invariants")]
        public IActionResult GetConstitutionalInvariants()
        {
            return Ok(new
            {
                axiom = WorkforceConstitutionalInvariants.Axiom,
                laws = WorkforceConstitutionalInvariants.AllLaws
            });
        }

        [HttpPost("objectives")]
        public async Task<IActionResult> CreateObjective([FromBody] BusinessObjective objective)
        {
            var created = await _service.CreateObjectiveAsync(GetTenantId(), objective);
            return Ok(created);
        }

        [HttpPost("objectives/revenue")]
        public async Task<IActionResult> CreateRevenueObjective([FromBody] RevenueObjective revenueObjective)
        {
            var created = await _service.CreateRevenueObjectiveAsync(GetTenantId(), revenueObjective);
            return Ok(created);
        }

        [HttpGet("objectives")]
        public async Task<IActionResult> ListObjectives()
        {
            var objectives = await _service.ListObjectivesAsync(GetTenantId());
            return Ok(objectives);
        }

        [HttpPost("objectives/{objectiveId}/approve")]
        public async Task<IActionResult> ApproveObjective(string objectiveId, [FromBody] ApproveObjectiveRequest request)
        {
            var success = await _service.ApproveObjectiveAsync(GetTenantId(), objectiveId, request.ApprovedBy ?? "HumanSupervisor");
            if (!success) return NotFound();
            return Ok(new { success = true });
        }

        [HttpGet("organization")]
        public async Task<IActionResult> GetOrganization()
        {
            var org = await _service.GetOrganizationAsync(GetTenantId());
            return Ok(org);
        }

        [HttpPost("contracts/hire")]
        public async Task<IActionResult> HireAgent([FromBody] AgentEmploymentContract contract)
        {
            var created = await _service.HireAgentAsync(GetTenantId(), contract);
            return Ok(created);
        }

        [HttpGet("contracts")]
        public async Task<IActionResult> ListContracts()
        {
            var contracts = await _service.ListContractsAsync(GetTenantId());
            return Ok(contracts);
        }

        [HttpPost("contracts/{agentId}/promote")]
        public async Task<IActionResult> PromoteAgent(string agentId, [FromBody] PromoteAgentRequest request)
        {
            try
            {
                var success = await _service.PromoteAgentAsync(GetTenantId(), agentId, request.TargetStatus, request.PromotedBy);
                if (!success) return NotFound();
                return Ok(new { success = true });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("contracts/{agentId}/quarantine")]
        public async Task<IActionResult> QuarantineAgent(string agentId, [FromBody] QuarantineAgentRequest request)
        {
            var success = await _service.QuarantineAgentAsync(GetTenantId(), agentId, request.Reason ?? "Security Violation");
            if (!success) return NotFound();
            return Ok(new { success = true });
        }
    }

    public class ApproveObjectiveRequest
    {
        public string? ApprovedBy { get; set; }
    }

    public class PromoteAgentRequest
    {
        public AgentLifecycleStatus TargetStatus { get; set; }
        public string PromotedBy { get; set; } = string.Empty;
    }

    public class QuarantineAgentRequest
    {
        public string? Reason { get; set; }
    }
}
