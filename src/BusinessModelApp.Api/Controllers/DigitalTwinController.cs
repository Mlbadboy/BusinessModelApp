using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.DigitalTwin;
using BusinessModelApp.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/digital-twin")]
    [Authorize]
    public class DigitalTwinController : ControllerBase
    {
        private readonly ICompanyDigitalTwinService _digitalTwinService;
        private readonly IUserContextService _userContext;
        private readonly ILogger<DigitalTwinController> _logger;

        public DigitalTwinController(
            ICompanyDigitalTwinService digitalTwinService,
            IUserContextService userContext,
            ILogger<DigitalTwinController> logger)
        {
            _digitalTwinService = digitalTwinService ?? throw new ArgumentNullException(nameof(digitalTwinService));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("state")]
        public async Task<IActionResult> GetCurrentState(CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var state = await _digitalTwinService.GetCurrentStateAsync(workspaceId, ct);
            return Ok(state);
        }

        [HttpGet("dimension/{dimension}")]
        public async Task<IActionResult> GetDimension(DigitalTwinDimension dimension, CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var dimState = await _digitalTwinService.GetDimensionAsync(workspaceId, dimension, ct);
            return Ok(dimState);
        }

        [HttpGet("field")]
        public async Task<IActionResult> GetField([FromQuery] string path, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return BadRequest(new { message = "Field path parameter is required." });
            }

            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var field = await _digitalTwinService.GetFieldAsync(workspaceId, path, ct);
            if (field == null)
            {
                return NotFound(new { message = $"Field '{path}' not found in Digital Twin." });
            }

            return Ok(field);
        }

        [HttpGet("conflicts")]
        public async Task<IActionResult> GetConflicts(CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var conflicts = await _digitalTwinService.GetConflictsAsync(workspaceId, ct);
            return Ok(conflicts);
        }

        [HttpGet("stale")]
        public async Task<IActionResult> GetStaleData(CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var staleFields = await _digitalTwinService.GetStaleDataAsync(workspaceId, ct);
            return Ok(staleFields);
        }

        [HttpGet("unknowns")]
        public async Task<IActionResult> GetUnknowns(CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var unknowns = await _digitalTwinService.GetUnknownsAsync(workspaceId, ct);
            return Ok(unknowns);
        }

        [HttpGet("evidence")]
        public async Task<IActionResult> GetEvidence([FromQuery] string fieldPath, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(fieldPath))
            {
                return BadRequest(new { message = "Field path parameter is required." });
            }

            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var records = await _digitalTwinService.GetEvidenceForFieldAsync(workspaceId, fieldPath, ct);
            return Ok(records);
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory(CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var history = await _digitalTwinService.GetHistoryAsync(workspaceId, ct);
            return Ok(history);
        }

        [HttpPost("snapshot")]
        public async Task<IActionResult> CreateSnapshot(CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var snapshot = await _digitalTwinService.CreateSnapshotAsync(workspaceId, ct);
            return Ok(new
            {
                snapshot.Id,
                snapshot.WorkspaceId,
                snapshot.CreatedAt,
                snapshot.SourceVersion,
                snapshot.RealityVersion,
                snapshot.EvidenceVersion,
                snapshot.TwinVersion,
                snapshot.IntegrityHash
            });
        }

        [HttpGet("diff")]
        public async Task<IActionResult> CompareSnapshots([FromQuery] Guid snapshotAId, [FromQuery] Guid snapshotBId, CancellationToken ct)
        {
            if (snapshotAId == Guid.Empty || snapshotBId == Guid.Empty)
            {
                return BadRequest(new { message = "Both snapshotAId and snapshotBId are required." });
            }

            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            try
            {
                var diff = await _digitalTwinService.CompareSnapshotsAsync(workspaceId, snapshotAId, snapshotBId, ct);
                return Ok(diff);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("health")]
        public async Task<IActionResult> GetHealth(CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var health = await _digitalTwinService.GetHealthReportAsync(workspaceId, ct);
            return Ok(health);
        }
    }
}
