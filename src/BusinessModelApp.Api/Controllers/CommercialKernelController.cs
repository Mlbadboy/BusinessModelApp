using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Commercial;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Commercial;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/commercial/kernel")]
    public class CommercialKernelController : ControllerBase
    {
        private readonly ICommercialKernelService _kernelService;

        public CommercialKernelController(ICommercialKernelService kernelService)
        {
            _kernelService = kernelService ?? throw new ArgumentNullException(nameof(kernelService));
        }

        private string GetTenantId()
        {
            if (Request?.Headers?.TryGetValue("X-Tenant-ID", out var tenantId) == true && !string.IsNullOrWhiteSpace(tenantId))
                return tenantId.ToString();
            return "tenant-default";
        }

        [HttpGet("invariants")]
        public IActionResult GetInvariants()
        {
            return Ok(new
            {
                Axiom = CommercialConstitutionalInvariants.Axiom,
                Laws = CommercialConstitutionalInvariants.AllLaws
            });
        }

        [HttpPost("entities")]
        public async Task<IActionResult> InitializeEntity([FromBody] InitializeEntityRequest request)
        {
            var tenantId = GetTenantId();
            var record = await _kernelService.InitializeCommercialEntityAsync(tenantId, request.OpportunityId, request.AccountId);
            return Ok(record);
        }

        [HttpGet("entities/{id}")]
        public async Task<IActionResult> GetEntity(string id)
        {
            var tenantId = GetTenantId();
            var record = await _kernelService.GetCommercialEntityAsync(tenantId, id);
            if (record == null) return NotFound();
            return Ok(record);
        }

        [HttpGet("entities")]
        public async Task<IActionResult> ListEntities()
        {
            var tenantId = GetTenantId();
            var list = await _kernelService.ListCommercialEntitiesAsync(tenantId);
            return Ok(list);
        }

        [HttpPost("transition")]
        public async Task<IActionResult> AttemptTransition([FromBody] CommercialTransitionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TenantId))
                request.TenantId = GetTenantId();

            var result = await _kernelService.AttemptTransitionAsync(request);
            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("entities/{id}/integrity")]
        public async Task<IActionResult> VerifyIntegrity(string id)
        {
            var tenantId = GetTenantId();
            var isValid = await _kernelService.VerifyAuditTrailIntegrityAsync(tenantId, id);
            return Ok(new { CommercialEntityId = id, IsValid = isValid });
        }
    }

    public class InitializeEntityRequest
    {
        public string OpportunityId { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
    }
}
