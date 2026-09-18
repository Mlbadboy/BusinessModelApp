using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Production;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Production;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/production/activation")]
    public sealed class ProductionActivationController : ControllerBase
    {
        private readonly IProductionActivationKernelService _activationService;

        public ProductionActivationController(IProductionActivationKernelService activationService)
        {
            _activationService = activationService ?? throw new ArgumentNullException(nameof(activationService));
        }

        [HttpPost("request")]
        public async Task<IActionResult> RequestActivation([FromBody] RequestActivationDto dto)
        {
            var activation = await _activationService.RequestTenantActivationAsync(
                dto.TenantId, dto.BusinessObjectiveId, dto.LegalBusinessName);
            return Ok(activation);
        }

        [HttpPost("authorize")]
        public async Task<IActionResult> AuthorizeActivation([FromBody] AuthorizeActivationDto dto)
        {
            var activation = await _activationService.AuthorizeTenantActivationAsync(
                dto.TenantId, dto.HumanSignoffId);
            return Ok(activation);
        }

        [HttpPost("config/deploy")]
        public async Task<IActionResult> DeployConfig([FromBody] DeployConfigDto dto)
        {
            var version = await _activationService.DeployConfigVersionAsync(
                dto.TenantId, dto.ConfigPayload, dto.ApprovedBySignoffId);
            return Ok(version);
        }

        [HttpPost("config/rollback")]
        public async Task<IActionResult> RollbackConfig([FromBody] RollbackConfigDto dto)
        {
            var activation = await _activationService.RollbackTenantConfigAsync(
                dto.TenantId, dto.TargetVersionNumber, dto.HumanSignoffId);
            return Ok(activation);
        }
    }

    public sealed class RequestActivationDto
    {
        public string TenantId { get; set; } = string.Empty;
        public string BusinessObjectiveId { get; set; } = string.Empty;
        public string LegalBusinessName { get; set; } = string.Empty;
    }

    public sealed class AuthorizeActivationDto
    {
        public string TenantId { get; set; } = string.Empty;
        public string HumanSignoffId { get; set; } = string.Empty;
    }

    public sealed class DeployConfigDto
    {
        public string TenantId { get; set; } = string.Empty;
        public string ConfigPayload { get; set; } = string.Empty;
        public string ApprovedBySignoffId { get; set; } = string.Empty;
    }

    public sealed class RollbackConfigDto
    {
        public string TenantId { get; set; } = string.Empty;
        public int TargetVersionNumber { get; set; }
        public string HumanSignoffId { get; set; } = string.Empty;
    }
}
