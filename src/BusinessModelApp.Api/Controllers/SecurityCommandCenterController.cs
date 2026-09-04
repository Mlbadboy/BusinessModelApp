using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BusinessModelApp.Core.Domain.Security;
using BusinessModelApp.Core.Interfaces;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/security")]
    [Authorize]
    public class SecurityCommandCenterController : ControllerBase
    {
        private readonly ISecurityCommandCenterService _securityService;
        private readonly IUserContextService _userContext;
        private readonly ILogger<SecurityCommandCenterController> _logger;

        public SecurityCommandCenterController(
            ISecurityCommandCenterService securityService,
            IUserContextService userContext,
            ILogger<SecurityCommandCenterController> logger)
        {
            _securityService = securityService ?? throw new ArgumentNullException(nameof(securityService));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("posture")]
        public async Task<IActionResult> GetSecurityPosture(CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var posture = await _securityService.CalculatePostureScoreAsync(workspaceId, ct);
            var killSwitch = await _securityService.GetKillSwitchStatusAsync(ct);

            return Ok(new
            {
                Posture = posture,
                KillSwitch = killSwitch
            });
        }

        [HttpGet("findings")]
        public async Task<IActionResult> ListFindings([FromQuery] VulnerabilitySeverity? minSeverity, CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var findings = await _securityService.ListFindingsAsync(workspaceId, minSeverity, ct);
            return Ok(findings);
        }

        [HttpGet("findings/{id:guid}")]
        public async Task<IActionResult> GetFinding(Guid id, CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var finding = await _securityService.GetFindingAsync(id, workspaceId, ct);
            if (finding == null) return NotFound(new { error = $"Finding {id} not found." });
            return Ok(finding);
        }

        [HttpGet("targets")]
        public async Task<IActionResult> ListTargets(CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var targets = await _securityService.ListTargetsAsync(workspaceId, ct);
            return Ok(targets);
        }

        [HttpPost("targets")]
        public async Task<IActionResult> RegisterTarget([FromBody] SecurityTargetRegistration target, CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            target.WorkspaceId = workspaceId;
            var registered = await _securityService.RegisterTargetAsync(target, ct);
            return Ok(registered);
        }

        [HttpGet("campaigns")]
        public async Task<IActionResult> ListCampaigns(CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var campaigns = await _securityService.ListCampaignsAsync(workspaceId, ct);
            return Ok(campaigns);
        }

        [HttpPost("campaigns/start")]
        public async Task<IActionResult> StartCampaign([FromBody] RedTeamCampaign campaign, CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            campaign.WorkspaceId = workspaceId;
            var started = await _securityService.StartCampaignAsync(campaign, ct);
            return Ok(started);
        }

        [HttpGet("kill-switch")]
        public async Task<IActionResult> GetKillSwitch(CancellationToken ct)
        {
            var status = await _securityService.GetKillSwitchStatusAsync(ct);
            return Ok(status);
        }

        [HttpPost("kill-switch/trigger")]
        public async Task<IActionResult> TriggerKillSwitch([FromBody] KillSwitchRequest request, CancellationToken ct)
        {
            var status = await _securityService.TriggerKillSwitchAsync(
                request?.Reason ?? "Emergency Stop",
                User.Identity?.Name ?? "SecurityAdministrator",
                ct);

            _logger.LogCritical("[SecurityCommandCenter] EMERGENCY KILL SWITCH TRIGGERED: {Reason}", status.Reason);
            return Ok(status);
        }

        [HttpPost("kill-switch/reset")]
        public async Task<IActionResult> ResetKillSwitch(CancellationToken ct)
        {
            var status = await _securityService.ResetKillSwitchAsync(
                User.Identity?.Name ?? "SecurityAdministrator",
                ct);

            _logger.LogWarning("[SecurityCommandCenter] Kill Switch disarmed by {Admin}", status.InitiatedBy);
            return Ok(status);
        }

        [HttpGet("remediations")]
        public async Task<IActionResult> ListRemediations(CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var remediations = await _securityService.ListRemediationsAsync(workspaceId, ct);
            return Ok(remediations);
        }

        [HttpPost("remediations/{id:guid}/apply")]
        public async Task<IActionResult> ApplyRemediation(Guid id, [FromQuery] bool isGovernanceApproved, CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var applied = await _securityService.ApplyRemediationAsync(id, workspaceId, isGovernanceApproved, ct);
            return Ok(applied);
        }
    }

    public class KillSwitchRequest
    {
        public string Reason { get; set; } = "Emergency security stop initiated by operator.";
    }
}
