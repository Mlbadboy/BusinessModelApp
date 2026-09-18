using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Executive;
using BusinessModelApp.Core.Interfaces.Runtime.Intelligence.Decision;
using BusinessModelApp.Core.Interfaces.Runtime.Intelligence.Executive;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/intelligence/executive")]
    public sealed class ExecutiveIntelligenceController : ControllerBase
    {
        private readonly IExecutiveBriefOrchestrator _orchestrator;
        private readonly IExecutiveBriefStore _briefStore;
        private readonly IExecutiveClaimValidator _claimValidator;
        private readonly IExecutiveBriefSnapshotStore _snapshotStore;
        private readonly IDecisionStore _decisionStore;

        public ExecutiveIntelligenceController(
            IExecutiveBriefOrchestrator orchestrator,
            IExecutiveBriefStore briefStore,
            IExecutiveClaimValidator claimValidator,
            IExecutiveBriefSnapshotStore snapshotStore,
            IDecisionStore decisionStore)
        {
            _orchestrator = orchestrator;
            _briefStore = briefStore;
            _claimValidator = claimValidator;
            _snapshotStore = snapshotStore;
            _decisionStore = decisionStore;
        }

        public sealed record GenerateBriefRequest(
            string TenantId,
            ExecutiveAudience Audience,
            ExecutiveMaterialityPolicy? Policy);

        public sealed record ValidateClaimRequest(
            ExecutiveClaim Claim,
            string SnapshotId,
            ExecutiveMaterialityPolicy? Policy);

        public sealed record AcknowledgeBriefRequest(
            string TenantId,
            string AcknowledgedBy);

        [HttpPost("briefs/generate")]
        public async Task<IActionResult> GenerateBrief([FromBody] GenerateBriefRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TenantId))
                return BadRequest("TenantId is required.");

            var brief = await _orchestrator.GenerateExecutiveBriefAsync(request.TenantId, request.Audience, request.Policy);
            return Ok(brief);
        }

        [HttpPost("briefs/validate")]
        public async Task<IActionResult> ValidateClaim([FromBody] ValidateClaimRequest request)
        {
            if (request.Claim == null)
                return BadRequest("Claim is required.");

            ExecutiveInputSnapshot? snapshot = null;
            if (!string.IsNullOrWhiteSpace(request.SnapshotId))
            {
                snapshot = await _snapshotStore.GetSnapshotByIdAsync(request.SnapshotId);
            }

            var policy = request.Policy ?? new ExecutiveMaterialityPolicy();
            var validated = _claimValidator.ValidateClaim(request.Claim, snapshot ?? new ExecutiveInputSnapshot(), policy);
            return Ok(validated);
        }

        [HttpGet("briefs/{tenantId}/{briefId}")]
        public async Task<IActionResult> GetBriefById(string tenantId, string briefId)
        {
            var brief = await _briefStore.GetBriefByIdAsync(tenantId, briefId);
            if (brief == null)
                return NotFound($"Executive brief '{briefId}' not found for tenant '{tenantId}'.");

            return Ok(brief);
        }

        [HttpGet("briefs/{tenantId}/latest")]
        public async Task<IActionResult> GetLatestBrief(string tenantId, [FromQuery] ExecutiveAudience audience = ExecutiveAudience.CEO)
        {
            var brief = await _briefStore.GetLatestBriefAsync(tenantId, audience);
            if (brief == null)
                return NotFound($"No executive brief found for tenant '{tenantId}' with audience '{audience}'.");

            return Ok(brief);
        }

        [HttpGet("attention/{tenantId}")]
        public async Task<IActionResult> GetAttentionQueue(string tenantId, [FromQuery] ExecutiveAudience audience = ExecutiveAudience.CEO)
        {
            var brief = await _briefStore.GetLatestBriefAsync(tenantId, audience);
            if (brief == null)
                return Ok(Array.Empty<ExecutiveAttentionItem>());

            return Ok(brief.AttentionQueue);
        }

        [HttpGet("governance/{tenantId}")]
        public async Task<IActionResult> GetGovernanceQueue(string tenantId, [FromQuery] ExecutiveAudience audience = ExecutiveAudience.CEO)
        {
            var brief = await _briefStore.GetLatestBriefAsync(tenantId, audience);
            if (brief == null)
                return Ok(Array.Empty<ExecutiveGovernanceItem>());

            return Ok(brief.GovernanceQueue);
        }

        [HttpGet("decisions/{tenantId}")]
        public async Task<IActionResult> GetDecisionsAwaitingReview(string tenantId)
        {
            var decisions = await _decisionStore.ListCandidatesAsync(tenantId);
            return Ok(decisions);
        }

        [HttpPost("briefs/{briefId}/acknowledge")]
        public async Task<IActionResult> AcknowledgeBrief(string briefId, [FromBody] AcknowledgeBriefRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TenantId))
                return BadRequest("TenantId is required.");

            // INVARIANT: Acknowledgment records leadership visibility; does NOT grant execution authority.
            bool acked = await _orchestrator.ValidateAndAcknowledgeBriefAsync(request.TenantId, briefId, request.AcknowledgedBy);
            if (!acked)
                return NotFound($"Brief '{briefId}' not found or tenant mismatch.");

            return Ok(new { BriefId = briefId, Status = "Acknowledged", BoundaryNotice = "Consequential actions remain subject to PRG-1 and Batch 6 Execution Firewall." });
        }
    }
}
