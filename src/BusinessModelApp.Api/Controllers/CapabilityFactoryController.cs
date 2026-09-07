using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Domain.Runtime.Capabilities;
using BusinessModelApp.Core.Domain.Runtime.Capabilities.Factory;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Core.Interfaces.Runtime.Capabilities.Factory;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/capabilities/factory")]
    public class CapabilityFactoryController : ControllerBase
    {
        private readonly IAutonomousCapabilityFactory _factory;
        private readonly ICapabilityLifecycleRegistry _registry;
        private readonly ICapabilityGapDetector _gapDetector;
        private readonly IUserContextService _userContext;
        private readonly ILogger<CapabilityFactoryController> _logger;

        public CapabilityFactoryController(
            IAutonomousCapabilityFactory factory,
            ICapabilityLifecycleRegistry registry,
            ICapabilityGapDetector gapDetector,
            IUserContextService userContext,
            ILogger<CapabilityFactoryController> logger)
        {
            _factory = factory;
            _registry = registry;
            _gapDetector = gapDetector;
            _userContext = userContext;
            _logger = logger;
        }

        private async Task<string> GetTenantIdAsync(CancellationToken ct)
        {
            try
            {
                var workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
                return workspaceId.ToString();
            }
            catch
            {
                return "default-tenant";
            }
        }

        [HttpGet("gaps")]
        public async Task<IActionResult> GetGaps(CancellationToken ct)
        {
            var tenantId = await GetTenantIdAsync(ct);
            var gaps = await _gapDetector.DetectGapsAsync(tenantId, ct);
            return Ok(gaps);
        }

        public class SynthesizeGapDto
        {
            public string BusinessNeed { get; set; } = string.Empty;
            public string RequiredCapabilityName { get; set; } = string.Empty;
            public string? ExpectedInputDescription { get; set; }
            public string? ExpectedOutputDescription { get; set; }
        }

        [HttpPost("synthesize")]
        public async Task<IActionResult> SynthesizeCapability([FromBody] SynthesizeGapDto dto, CancellationToken ct)
        {
            var tenantId = await GetTenantIdAsync(ct);

            var gap = new CapabilityGap
            {
                WorkspaceId = Guid.NewGuid(),
                TenantId = tenantId,
                BusinessNeed = dto.BusinessNeed,
                RequiredCapabilityName = dto.RequiredCapabilityName,
                ExpectedInputDescription = dto.ExpectedInputDescription ?? "Target payload",
                ExpectedOutputDescription = dto.ExpectedOutputDescription ?? "Processed outcome",
            };

            try
            {
                var result = await _factory.SynthesizeCapabilityPipelineAsync(gap, ct);
                return Ok(new
                {
                    Status = "Synthesized",
                    CapabilityId = result.CapabilityId.ToString(),
                    Version = result.Version,
                    LifecycleState = result.State.ToString(),
                    CertificateId = result.Certificate.CertificateId,
                    SignatureHex = result.Signature.SignatureHex,
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Capability synthesis rejected");
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("registry")]
        public async Task<IActionResult> GetRegistry(CancellationToken ct)
        {
            var tenantId = await GetTenantIdAsync(ct);
            var records = await _registry.GetAllCapabilitiesAsync(tenantId, ct);
            return Ok(records);
        }

        [HttpGet("{capabilityId}/{version}")]
        public async Task<IActionResult> GetCapability(string capabilityId, string version, CancellationToken ct)
        {
            var record = await _registry.GetCapabilityAsync(new CapabilityId(capabilityId, version), version, ct);
            if (record == null) return NotFound(new { Message = $"Capability {capabilityId}@{version} not found." });
            return Ok(record);
        }

        public class PromoteDto
        {
            public string TargetState { get; set; } = "Shadow"; // Shadow, Probation, Active, Quarantined, Revoked
        }

        [HttpPost("{capabilityId}/{version}/promote")]
        public async Task<IActionResult> PromoteCapability(string capabilityId, string version, [FromBody] PromoteDto dto, CancellationToken ct)
        {
            if (!Enum.TryParse<CapabilityLifecycleState>(dto.TargetState, true, out var state))
            {
                return BadRequest(new { Message = $"Invalid target state: {dto.TargetState}" });
            }

            try
            {
                var record = await _factory.PromoteCapabilityAsync(new CapabilityId(capabilityId, version), version, state, ct);
                return Ok(new
                {
                    Status = "Promoted",
                    CapabilityId = record.CapabilityId.ToString(),
                    Version = record.Version,
                    CurrentState = record.State.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Promotion failed for {CapabilityId}@{Version}", capabilityId, version);
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("{capabilityId}/{version}/quarantine")]
        public async Task<IActionResult> QuarantineCapability(string capabilityId, string version, [FromBody] string? reason, CancellationToken ct)
        {
            try
            {
                await _registry.QuarantineCapabilityAsync(new CapabilityId(capabilityId, version), version, reason ?? "Manual quarantine", ct);
                return Ok(new { Status = "Quarantined", CapabilityId = capabilityId, Version = version });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("{capabilityId}/{version}/revoke")]
        public async Task<IActionResult> RevokeCapability(string capabilityId, string version, [FromBody] string? reason, CancellationToken ct)
        {
            try
            {
                await _registry.RevokeCapabilityAsync(new CapabilityId(capabilityId, version), version, reason ?? "Manual revocation", ct);
                return Ok(new { Status = "Revoked", CapabilityId = capabilityId, Version = version });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
