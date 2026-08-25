using System;
using System.Linq;
using System.Threading.Tasks;
using BusinessModelApp.Core.AI.Governance;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Core.Services;
using BusinessModelApp.Infrastructure.AI.OmniRoute;
using BusinessModelApp.Infrastructure.Data;
using BusinessModelApp.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/ai-control-center")]
    [Authorize]
    public class AIControlCenterController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IUserContextService _userContext;
        private readonly IOmniRouteClient _omniClient;
        private readonly IBudgetReservationService _budgetService;
        private readonly IApprovalService _approvalService;
        private readonly IAIROIService _roiService;

        public AIControlCenterController(
            AppDbContext context,
            IUserContextService userContext,
            IOmniRouteClient omniClient,
            IBudgetReservationService budgetService,
            IApprovalService approvalService,
            IAIROIService roiService)
        {
            _context = context;
            _userContext = userContext;
            _omniClient = omniClient;
            _budgetService = budgetService;
            _approvalService = approvalService;
            _roiService = roiService;
        }

        [HttpGet("summary")]
        public async Task<ActionResult<object>> GetSummary([FromQuery] Guid? workspaceId)
        {
            var targetWorkspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(workspaceId);
            var orgId = await _userContext.GetCurrentOrganizationIdAsync();

            var isHealthy = await _omniClient.CheckHealthAsync();
            var policy = await _budgetService.GetOrCreateBudgetPolicyAsync(orgId, targetWorkspaceId);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var firstDayOfMonth = new DateOnly(today.Year, today.Month, 1);

            var mtdUsage = await _context.AIUsageDailies
                .Where(u => u.WorkspaceId == targetWorkspaceId && u.Date >= firstDayOfMonth && u.Date <= today)
                .ToListAsync();

            var totalRequests = mtdUsage.Sum(u => u.RequestCount);
            var totalTokens = mtdUsage.Sum(u => u.TotalTokens);
            var totalSpent = mtdUsage.Sum(u => u.EstimatedCost);
            var totalLatency = mtdUsage.Sum(u => u.TotalLatencyMs);
            var fallbackCount = mtdUsage.Sum(u => u.FallbackCount);
            var cacheHits = mtdUsage.Sum(u => u.CacheHits);
            var avgLatency = totalRequests > 0 ? (double)totalLatency / totalRequests : 850.0;

            // Deterministic AI ROI Attribution via Commercial Journey Linkage
            var aiCalls = await _context.AICallRecords
                .Where(r => r.WorkspaceId == targetWorkspaceId)
                .ToListAsync();

            var opps = await _context.Opportunities
                .Where(o => o.WorkspaceId == targetWorkspaceId)
                .ToListAsync();

            var leads = await _context.Leads
                .Where(l => l.WorkspaceId == targetWorkspaceId)
                .ToListAsync();

            var roiResult = _roiService.CalculateDeterministicRoi(targetWorkspaceId, aiCalls, opps, leads);

            // Traffic status
            var trafficPolicy = await _context.AITrafficControlPolicies
                .FirstOrDefaultAsync(p => p.OrganizationId == orgId && (p.WorkspaceId == targetWorkspaceId || p.WorkspaceId == null));

            var trafficStatus = trafficPolicy?.Status.ToString() ?? "Enabled";

            return Ok(new
            {
                gatewayStatus = isHealthy ? "Healthy" : "Degraded",
                trafficStatus = trafficStatus,
                monthlySpend = totalSpent,
                monthlyBudgetCap = policy.MonthlyBudgetCap,
                budgetPercentConsumed = policy.MonthlyBudgetCap > 0 ? (totalSpent / policy.MonthlyBudgetCap) * 100m : 0m,
                totalRequests = totalRequests > 0 ? totalRequests : aiCalls.Count,
                totalTokens,
                averageLatencyMs = avgLatency,
                fallbackCount,
                cacheHits,
                cacheSavings = cacheHits * 0.05m,
                
                // Deterministic AI ROI & Attribution Metrics
                attributedWonRevenue = roiResult.AttributedClosedWonRevenue,
                attributedAISpend = roiResult.AttributedAISpend,
                aiRoiRatio = roiResult.NetAIRoiRatio,
                attributionStatus = roiResult.AttributionStatus.ToString(),
                attributionSummary = roiResult.AttributionSummary
            });
        }

        [HttpPost("traffic-status")]
        public async Task<ActionResult<AITrafficControlPolicy>> UpdateTrafficStatus(
            [FromBody] UpdateTrafficStatusDto dto,
            [FromQuery] Guid? workspaceId)
        {
            var targetWorkspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(workspaceId);
            var orgId = await _userContext.GetCurrentOrganizationIdAsync();

            var policy = await _context.AITrafficControlPolicies
                .FirstOrDefaultAsync(p => p.OrganizationId == orgId && p.WorkspaceId == targetWorkspaceId);

            if (policy == null)
            {
                policy = new AITrafficControlPolicy
                {
                    OrganizationId = orgId,
                    WorkspaceId = targetWorkspaceId,
                    Status = dto.Status,
                    DisabledReason = dto.Reason,
                    UpdatedByName = User.Identity?.Name ?? "Admin"
                };
                _context.AITrafficControlPolicies.Add(policy);
            }
            else
            {
                policy.Status = dto.Status;
                policy.DisabledReason = dto.Reason;
                policy.UpdatedByName = User.Identity?.Name ?? "Admin";
                policy.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(policy);
        }

        [HttpGet("telemetry")]
        public async Task<ActionResult<object>> GetTelemetry([FromQuery] Guid? workspaceId, [FromQuery] int limit = 25)
        {
            var targetWorkspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(workspaceId);
            var records = await _context.AICallRecords
                .Where(r => r.WorkspaceId == targetWorkspaceId)
                .OrderByDescending(r => r.CreatedAt)
                .Take(Math.Min(100, Math.Max(1, limit)))
                .Select(r => new
                {
                    r.Id,
                    taskType = r.TaskType.ToString(),
                    r.Provider,
                    r.Model,
                    r.PromptTokens,
                    r.CompletionTokens,
                    r.TotalTokens,
                    r.EstimatedCost,
                    r.LatencyMs,
                    r.CacheHit,
                    r.FallbackAttempts,
                    r.RequestCorrelationId,
                    r.LeadId,
                    r.OpportunityId,
                    r.CreatedAt
                })
                .ToListAsync();

            return Ok(records);
        }

        [HttpGet("budgets")]
        public async Task<ActionResult<AIBudgetPolicy>> GetBudgetPolicy([FromQuery] Guid? workspaceId)
        {
            var targetWorkspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(workspaceId);
            var orgId = await _userContext.GetCurrentOrganizationIdAsync();
            var policy = await _budgetService.GetOrCreateBudgetPolicyAsync(orgId, targetWorkspaceId);
            return Ok(policy);
        }

        [HttpGet("approvals")]
        public async Task<ActionResult<object>> GetApprovals([FromQuery] Guid? workspaceId)
        {
            var targetWorkspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(workspaceId);
            var requests = await _context.ApprovalRequests
                .Where(r => r.WorkspaceId == targetWorkspaceId)
                .OrderByDescending(r => r.CreatedAt)
                .Take(50)
                .ToListAsync();

            return Ok(requests);
        }

        [HttpPost("approvals/{id}/decide")]
        public async Task<ActionResult<ApprovalRequest>> DecideApproval(
            Guid id,
            [FromBody] DecideApprovalDto dto)
        {
            var userId = await _userContext.GetCurrentUserIdAsync();
            var result = await _approvalService.DecideApprovalRequestAsync(
                id, userId, User.Identity?.Name ?? "Admin", dto.IsApproved, dto.DecisionNote ?? string.Empty);

            return Ok(result);
        }
    }

    public class UpdateTrafficStatusDto
    {
        public AITrafficStatus Status { get; set; }
        public string? Reason { get; set; }
    }

    public class DecideApprovalDto
    {
        public bool IsApproved { get; set; }
        public string? DecisionNote { get; set; }
    }
}
