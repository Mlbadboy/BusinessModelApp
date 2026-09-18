using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Acquisition;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Acquisition;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/acquisition/runtime")]
    public sealed class CustomerAcquisitionRuntimeController : ControllerBase
    {
        private readonly ICustomerAcquisitionRuntimeService _acquisitionService;

        public CustomerAcquisitionRuntimeController(ICustomerAcquisitionRuntimeService acquisitionService)
        {
            _acquisitionService = acquisitionService ?? throw new ArgumentNullException(nameof(acquisitionService));
        }

        [HttpPost("prepare")]
        public async Task<IActionResult> PrepareOutreach([FromBody] PrepareOutreachDto dto)
        {
            var task = await _acquisitionService.PrepareOutreachAsync(
                dto.TenantId, dto.ProspectId, dto.ContactEmail, dto.SubjectLine, dto.MessageContent, dto.LastContactedUtc);
            return Ok(task);
        }

        [HttpPost("execute")]
        public async Task<IActionResult> ExecuteOutreach([FromBody] ExecuteOutreachDto dto)
        {
            var task = await _acquisitionService.ExecuteGovernedOutreachAsync(
                dto.TaskId, dto.Batch6PermitId, dto.ExternalProviderRef);
            return Ok(task);
        }
    }

    public sealed class PrepareOutreachDto
    {
        public string TenantId { get; set; } = string.Empty;
        public string ProspectId { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string SubjectLine { get; set; } = string.Empty;
        public string MessageContent { get; set; } = string.Empty;
        public DateTime? LastContactedUtc { get; set; }
    }

    public sealed class ExecuteOutreachDto
    {
        public string TaskId { get; set; } = string.Empty;
        public string Batch6PermitId { get; set; } = string.Empty;
        public string ExternalProviderRef { get; set; } = string.Empty;
    }
}
