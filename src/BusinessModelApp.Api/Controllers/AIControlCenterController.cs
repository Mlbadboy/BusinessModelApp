using System;
using System.Linq;
using System.Threading.Tasks;
using BusinessModelApp.Core.AI.Governance;
using BusinessModelApp.Core.Interfaces;
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

        public AIControlCenterController(
            AppDbContext context,
            IUserContextService userContext,
            IOmniRouteClient omniClient,
            IBudgetReservationService budgetService,
            IApprovalService approvalService)
        {
            _context = context;
            _userContext = userContext;
            _omniClient = omniClient;
            _budgetService = budgetService;
            _approvalService = approvalService;
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

            // AI ROI Attribution (Attributed Opportunity Value)
            var wonRevenue = await _context.Opportunities
                .Where(o => o.WorkspaceId == targetWorkspaceId && o.Stage == Core.Domain.Commercial.OpportunityStage.ClosedWon)
                .SumAsync(o => (decimal?)o.EstimatedValue) ?? 0m;

            var aiRoiRatio = totalSpent > 0 ? (double)(wonRevenue / totalSpent) : 0.0;

            return Ok(new
            {
                gatewayStatus = isHealthy ? "Healthy" : "Degraded",
                monthlySpend = totalSpent,
                monthlyBudgetCap = policy.MonthlyBudgetCap,
                budgetPercentConsumed = policy.MonthlyBudgetCap > 0 ? (totalSpent / policy.MonthlyBudgetCap) * 100m : 0m,
                totalRequests = totalRequests > 0 ? totalRequests : await _context.AICallRecords.CountAsync(r => r.WorkspaceId == targetWorkspaceId),
                totalTokens,
                averageLatencyMs = avgLatency,
                fallbackCount,
                cacheHits,
                cacheSavings = cacheHits * 0.05m,
                attributedWonRevenue = wonRevenue,
                aiRoiRatio
            });
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

    public class DecideApprovalDto
    {
        public bool IsApproved { get; set; }
        public string? DecisionNote { get; set; }
    }
}
