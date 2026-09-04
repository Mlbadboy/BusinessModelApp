using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Execution;

namespace BusinessModelApp.Core.Interfaces
{
    public interface IApprovalGateway
    {
        Task<ExecutionApprovalRequestEntity> CreateApprovalRequestAsync(
            ExecutionRequest request,
            ExecutionRiskTier riskTier,
            string summary,
            CancellationToken cancellationToken = default);

        Task<ExecutionApprovalRequestEntity> SubmitDecisionAsync(
            Guid approvalId,
            bool approve,
            Guid deciderUserId,
            string reason,
            string? digitalSignature = null,
            CancellationToken cancellationToken = default);

        Task<bool> ValidateApprovalAsync(
            Guid requestId,
            string currentPayloadDigest,
            CancellationToken cancellationToken = default);

        Task<List<ExecutionApprovalRequestEntity>> GetPendingApprovalsAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    }
}
