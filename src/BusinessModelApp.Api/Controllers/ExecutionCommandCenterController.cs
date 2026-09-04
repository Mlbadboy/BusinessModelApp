using System;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BusinessModelApp.Core.Domain.Execution;
using BusinessModelApp.Core.Interfaces;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/execution")]
    [Authorize]
    public class ExecutionCommandCenterController : ControllerBase
    {
        private readonly IExecutionFirewall _firewall;
        private readonly IBudgetGuardService _budgetService;
        private readonly IApprovalGateway _approvalGateway;
        private readonly IExecutionLedgerService _ledgerService;
        private readonly IAuthorityDelegationService _authorityService;
        private readonly IExecutionKillSwitchService _killSwitchService;
        private readonly IUserContextService _userContext;
        private readonly ILogger<ExecutionCommandCenterController> _logger;

        public ExecutionCommandCenterController(
            IExecutionFirewall firewall,
            IBudgetGuardService budgetService,
            IApprovalGateway approvalGateway,
            IExecutionLedgerService ledgerService,
            IAuthorityDelegationService authorityService,
            IExecutionKillSwitchService killSwitchService,
            IUserContextService userContext,
            ILogger<ExecutionCommandCenterController> logger)
        {
            _firewall = firewall;
            _budgetService = budgetService;
            _approvalGateway = approvalGateway;
            _ledgerService = ledgerService;
            _authorityService = authorityService;
            _killSwitchService = killSwitchService;
            _userContext = userContext;
            _logger = logger;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetExecutionSummary(CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var wallet = await _budgetService.GetOrCreateWalletAsync(workspaceId, ct);
            var pendingApprovals = await _approvalGateway.GetPendingApprovalsAsync(workspaceId, ct);
            var activeKillSwitches = await _killSwitchService.GetActiveKillSwitchesAsync(workspaceId, ct);
            var delegations = await _authorityService.GetActiveDelegationsAsync(workspaceId, ct);

            return Ok(new
            {
                WorkspaceId = workspaceId,
                Wallet = wallet,
                PendingApprovalsCount = pendingApprovals.Count,
                ActiveKillSwitchesCount = activeKillSwitches.Count,
                ActiveDelegationsCount = delegations.Count,
                IsHalted = activeKillSwitches.Count > 0
            });
        }

        [HttpGet("ledger")]
        public async Task<IActionResult> GetRecentLedgerEntries([FromQuery] int limit = 50, CancellationToken ct = default)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var entries = await _ledgerService.GetRecentEntriesAsync(workspaceId, limit, ct);
            return Ok(entries);
        }

        [HttpGet("approvals")]
        public async Task<IActionResult> GetPendingApprovals(CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var approvals = await _approvalGateway.GetPendingApprovalsAsync(workspaceId, ct);
            return Ok(approvals);
        }

        public class DecideApprovalDto
        {
            public bool Approve { get; set; }
            public string Reason { get; set; } = string.Empty;
            public string? DigitalSignature { get; set; }
        }

        [HttpPost("approvals/{id:guid}/decide")]
        public async Task<IActionResult> DecideApproval(Guid id, [FromBody] DecideApprovalDto dto, CancellationToken ct)
        {
            Guid userId = await _userContext.GetCurrentUserIdAsync(ct);
            var result = await _approvalGateway.SubmitDecisionAsync(id, dto.Approve, userId, dto.Reason, dto.DigitalSignature, ct);
            return Ok(result);
        }

        [HttpGet("wallet")]
        public async Task<IActionResult> GetWallet(CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var wallet = await _budgetService.GetOrCreateWalletAsync(workspaceId, ct);
            return Ok(wallet);
        }

        public class UpdateBudgetLimitsDto
        {
            public decimal DailyCapINR { get; set; }
            public decimal AllocatedBudgetINR { get; set; }
        }

        [HttpPost("wallet/limits")]
        public async Task<IActionResult> UpdateWalletLimits([FromBody] UpdateBudgetLimitsDto dto, CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            Guid userId = await _userContext.GetCurrentUserIdAsync(ct);

            var wallet = await _budgetService.UpdateBudgetLimitsByHumanAsync(workspaceId, dto.DailyCapINR, dto.AllocatedBudgetINR, userId, ct);
            return Ok(wallet);
        }

        [HttpGet("delegations")]
        public async Task<IActionResult> GetActiveDelegations(CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var delegations = await _authorityService.GetActiveDelegationsAsync(workspaceId, ct);
            return Ok(delegations);
        }

        public class TriggerKillSwitchDto
        {
            public ExecutionKillSwitchTier Tier { get; set; }
            public string? TargetIdentifier { get; set; }
            public string Reason { get; set; } = string.Empty;
        }

        [HttpPost("kill-switch/trigger")]
        public async Task<IActionResult> TriggerKillSwitch([FromBody] TriggerKillSwitchDto dto, CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            Guid userId = await _userContext.GetCurrentUserIdAsync(ct);

            var result = await _killSwitchService.TriggerKillSwitchAsync(dto.Tier, workspaceId, dto.TargetIdentifier, dto.Reason, userId, ct);
            return Ok(result);
        }

        [HttpPost("kill-switch/{id:guid}/deactivate")]
        public async Task<IActionResult> DeactivateKillSwitch(Guid id, CancellationToken ct)
        {
            Guid userId = await _userContext.GetCurrentUserIdAsync(ct);
            var success = await _killSwitchService.DeactivateKillSwitchAsync(id, userId, ct);
            if (!success) return NotFound(new { error = "Kill switch not found or already deactivated." });
            return Ok(new { success = true });
        }
    }
}
