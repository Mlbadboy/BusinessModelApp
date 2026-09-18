using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Workforce;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/brain-space")]
    public class BrainSpaceController : ControllerBase
    {
        private readonly IBrainSpaceTelemetryService _telemetryService;

        public BrainSpaceController(IBrainSpaceTelemetryService telemetryService)
        {
            _telemetryService = telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
        }

        private string GetTenantId() =>
            Request.Headers.TryGetValue("X-Tenant-ID", out var t) && !string.IsNullOrWhiteSpace(t) ? t.ToString() : "tenant-default";

        [HttpGet("telemetry")]
        public async Task<IActionResult> GetTelemetrySnapshot()
        {
            var snapshot = await _telemetryService.CaptureSnapshotAsync(GetTenantId());
            return Ok(snapshot);
        }
    }
}
