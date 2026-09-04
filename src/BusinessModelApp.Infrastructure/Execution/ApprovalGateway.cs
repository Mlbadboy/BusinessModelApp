using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BusinessModelApp.Core.Domain.Execution;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Infrastructure.Data;

namespace BusinessModelApp.Infrastructure.Execution
{
    public class ApprovalGateway : IApprovalGateway
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ApprovalGateway> _logger;

        public ApprovalGateway(AppDbContext context, ILogger<ApprovalGateway> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ExecutionApprovalRequestEntity> CreateApprovalRequestAsync(
            ExecutionRequest request,
            ExecutionRiskTier riskTier,
            string summary,
            CancellationToken cancellationToken = default)
        {
            var approval = new ExecutionApprovalRequestEntity
            {
                WorkspaceId = request.WorkspaceId,
                OrganizationId = request.OrganizationId,
                RequestId = request.RequestId,
                MissionId = request.MissionId,
                CapabilityId = request.CapabilityId,
                RiskTier = riskTier,
                MonetaryImpactINR = request.MonetaryImpactINR,
                PayloadDigest = request.ComputePayloadDigest(),
                PayloadSummary = summary,
                RequestingAgentId = request.AgentId,
                Status = ApprovalStatus.Pending,
                RequestedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddHours(24)
            };

            _context.ExecutionApprovalRequests.Add(approval);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created Human Approval Request {Id} for request {RequestId}, risk {RiskTier}",
                approval.Id, request.RequestId, riskTier);

            return approval;
        }

        public async Task<ExecutionApprovalRequestEntity> SubmitDecisionAsync(
            Guid approvalId,
            bool approve,
            Guid deciderUserId,
            string reason,
            string? digitalSignature = null,
            CancellationToken cancellationToken = default)
        {
            var approval = await _context.ExecutionApprovalRequests.FindAsync(new object[] { approvalId }, cancellationToken);
            if (approval == null)
            {
                throw new KeyNotFoundException($"Approval request {approvalId} not found.");
            }

            if (approval.Status != ApprovalStatus.Pending)
            {
                throw new InvalidOperationException($"Approval request {approvalId} is already decided (Status: {approval.Status}).");
            }

            approval.Status = approve ? ApprovalStatus.Approved : ApprovalStatus.Rejected;
            approval.DecidedByUserId = deciderUserId;
            approval.DecidedAtUtc = DateTime.UtcNow;
            approval.DecisionReason = reason;
            approval.DigitalSignature = digitalSignature ?? GenerateSignature(approvalId, deciderUserId, approve);

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Human Decision recorded for Approval {Id}: {Status} by User {UserId}",
                approvalId, approval.Status, deciderUserId);

            return approval;
        }

        public async Task<bool> ValidateApprovalAsync(
            Guid requestId,
            string currentPayloadDigest,
            CancellationToken cancellationToken = default)
        {
            var approval = await _context.ExecutionApprovalRequests
                .FirstOrDefaultAsync(a => a.RequestId == requestId, cancellationToken);

            if (approval == null)
                return false;

            // Check validity and tamper detection
            if (!approval.IsValidForExecution(currentPayloadDigest))
            {
                if (approval.Status == ApprovalStatus.Approved && !string.Equals(approval.PayloadDigest, currentPayloadDigest, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogCritical("TAMPER DETECTED: Payload digest changed after approval grant for request {RequestId}! Automatically invalidating approval.", requestId);
                    approval.Status = ApprovalStatus.Invalidated;
                    await _context.SaveChangesAsync(cancellationToken);
                }
                return false;
            }

            return true;
        }

        public async Task<List<ExecutionApprovalRequestEntity>> GetPendingApprovalsAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        {
            return await _context.ExecutionApprovalRequests
                .Where(a => a.WorkspaceId == workspaceId && a.Status == ApprovalStatus.Pending)
                .OrderByDescending(a => a.RequestedAtUtc)
                .ToListAsync(cancellationToken);
        }

        private string GenerateSignature(Guid approvalId, Guid userId, bool approved)
        {
            using var sha = SHA256.Create();
            var raw = $"{approvalId}:{userId}:{approved}:{DateTime.UtcNow:yyyyMMddHH}";
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
        }
    }
}
