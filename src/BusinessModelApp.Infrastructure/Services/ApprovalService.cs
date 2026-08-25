using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.AI;
using BusinessModelApp.Core.AI.Governance;
using BusinessModelApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BusinessModelApp.Infrastructure.Services
{
    public class ApprovalService : IApprovalService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ApprovalService> _logger;

        public ApprovalService(AppDbContext context, ILogger<ApprovalService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public ApprovalRiskLevel EvaluateTaskRiskLevel(AITaskType taskType, string? actionDetails = null)
        {
            return taskType switch
            {
                AITaskType.ExecutiveBrief or AITaskType.BusinessHealthExplanation => ApprovalRiskLevel.Low,
                AITaskType.OpportunityAnalysis or AITaskType.LeadQualification => ApprovalRiskLevel.Medium,
                AITaskType.VoiceQualification => ApprovalRiskLevel.High,
                _ => ApprovalRiskLevel.Low
            };
        }

        public async Task<ApprovalRequest> SubmitApprovalRequestAsync(
            Guid organizationId,
            Guid workspaceId,
            Guid userId,
            string requesterName,
            string actionType,
            string title,
            string contextDataJson,
            ApprovalRiskLevel riskLevel,
            CancellationToken ct = default)
        {
            var request = new ApprovalRequest
            {
                OrganizationId = organizationId,
                WorkspaceId = workspaceId,
                RequesterUserId = userId,
                RequesterName = requesterName,
                ActionType = actionType,
                Title = title,
                ContextDataJson = contextDataJson,
                RiskLevel = riskLevel,
                Status = riskLevel == ApprovalRiskLevel.Low ? ApprovalStatus.AutoApproved : ApprovalStatus.Pending,
                DecidedAt = riskLevel == ApprovalRiskLevel.Low ? DateTime.UtcNow : null
            };

            _context.ApprovalRequests.Add(request);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Submitted approval request {RequestId} for action {ActionType} with risk {RiskLevel}",
                request.Id, actionType, riskLevel);

            return request;
        }

        public async Task<ApprovalRequest> DecideApprovalRequestAsync(
            Guid requestId,
            Guid deciderUserId,
            string deciderName,
            bool isApproved,
            string decisionNote,
            CancellationToken ct = default)
        {
            var request = await _context.ApprovalRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct);
            if (request == null)
            {
                throw new InvalidOperationException($"Approval request {requestId} not found.");
            }

            request.Status = isApproved ? ApprovalStatus.Approved : ApprovalStatus.Rejected;
            request.DecidedByUserId = deciderUserId;
            request.DecidedByName = deciderName;
            request.DecisionNote = decisionNote;
            request.DecidedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Approval request {RequestId} decided by {DeciderName}: {Status}",
                requestId, deciderName, request.Status);

            return request;
        }
    }
}
