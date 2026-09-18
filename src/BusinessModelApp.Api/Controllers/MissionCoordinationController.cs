using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime.Missions;
using BusinessModelApp.Core.Domain.Runtime.Organizational;
using BusinessModelApp.Core.Interfaces.Runtime.Missions;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MissionCoordinationController : ControllerBase
    {
        private readonly IMissionOrchestrator _orchestrator;
        private readonly IMissionResourceArbiter _arbiter;

        public MissionCoordinationController(
            IMissionOrchestrator orchestrator,
            IMissionResourceArbiter arbiter)
        {
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            _arbiter = arbiter ?? throw new ArgumentNullException(nameof(arbiter));
        }

        private string GetTenantId() =>
            Request.Headers.TryGetValue("X-Tenant-ID", out var tenant) && !string.IsNullOrWhiteSpace(tenant)
                ? tenant.ToString()
                : "default-tenant";

        private string GetActor() =>
            User?.Identity?.Name ?? "system-coordinator";

        [HttpPost("admit")]
        public async Task<IActionResult> AdmitMission(
            [FromBody] AdmitMissionRequest request,
            CancellationToken ct)
        {
            var tenantId = GetTenantId();
            if (request?.Proposal == null || string.IsNullOrWhiteSpace(request.WorkId))
            {
                return BadRequest(new { error = "Proposal and WorkId are required." });
            }

            var (admitted, ticket, reason) = await _orchestrator.AdmitMissionProposalAsync(
                tenantId,
                request.Proposal,
                request.WorkId,
                request.RiskTier,
                ct);

            if (!admitted && ticket?.Decision == AdmissionDecisionStatus.Rejected)
            {
                return StatusCode(429, new { error = reason, ticket });
            }

            return Ok(new { admitted, ticket, reason });
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActiveState(CancellationToken ct)
        {
            var tenantId = GetTenantId();
            var state = await _orchestrator.GetActiveStateAsync(tenantId, ct);
            return Ok(state);
        }

        [HttpPost("cancel/{missionId}")]
        public async Task<IActionResult> CancelMission(
            string missionId,
            [FromQuery] string workId,
            [FromQuery] string reason,
            CancellationToken ct)
        {
            var tenantId = GetTenantId();
            var actor = GetActor();

            var receipt = await _orchestrator.CancelMissionCascadingAsync(
                tenantId,
                missionId,
                workId ?? string.Empty,
                reason ?? "User cancelled",
                actor,
                ct);

            return Ok(receipt);
        }

        [HttpPost("locks/acquire")]
        public async Task<IActionResult> AcquireLock(
            [FromBody] AcquireLockRequest request,
            CancellationToken ct)
        {
            var tenantId = GetTenantId();
            if (request == null || string.IsNullOrWhiteSpace(request.ResourceId))
            {
                return BadRequest(new { error = "ResourceId is required." });
            }

            var (acquired, lockItem, reason) = await _arbiter.AcquireLockAsync(
                tenantId,
                request.ResourceNamespace ?? "Default",
                request.ResourceType ?? "Entity",
                request.ResourceId,
                request.MissionId,
                request.NodeId,
                request.TtlSeconds.HasValue ? TimeSpan.FromSeconds(request.TtlSeconds.Value) : null,
                ct);

            if (!acquired)
            {
                return Conflict(new { error = reason });
            }

            return Ok(lockItem);
        }

        [HttpPost("locks/release")]
        public async Task<IActionResult> ReleaseLock(
            [FromBody] ReleaseLockRequest request,
            CancellationToken ct)
        {
            var tenantId = GetTenantId();
            if (request == null || string.IsNullOrWhiteSpace(request.ResourceId))
            {
                return BadRequest(new { error = "ResourceId is required." });
            }

            var released = await _arbiter.ReleaseLockAsync(
                tenantId,
                request.ResourceNamespace ?? "Default",
                request.ResourceType ?? "Entity",
                request.ResourceId,
                request.FencingToken,
                ct);

            if (!released)
            {
                return StatusCode(412, new { error = "Fencing token mismatch or lock expired." });
            }

            return Ok(new { released = true });
        }
    }

    public record AdmitMissionRequest
    {
        public MissionGraphProposal Proposal { get; init; } = null!;
        public string WorkId { get; init; } = string.Empty;
        public WorkRiskTier RiskTier { get; init; } = WorkRiskTier.R1_InternalReversible;
    }

    public record AcquireLockRequest
    {
        public string ResourceNamespace { get; init; } = "Default";
        public string ResourceType { get; init; } = "Entity";
        public string ResourceId { get; init; } = string.Empty;
        public string MissionId { get; init; } = string.Empty;
        public string NodeId { get; init; } = string.Empty;
        public int? TtlSeconds { get; init; }
    }

    public record ReleaseLockRequest
    {
        public string ResourceNamespace { get; init; } = "Default";
        public string ResourceType { get; init; } = "Entity";
        public string ResourceId { get; init; } = string.Empty;
        public long FencingToken { get; init; }
    }
}
