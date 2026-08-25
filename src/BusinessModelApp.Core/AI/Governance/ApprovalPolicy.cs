using System;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessModelApp.Core.AI.Governance
{
    public enum ApprovalRiskLevel
    {
        Low = 1,      // Automatic (e.g. Executive Brief, Metric Explanation)
        Medium = 2,   // Deal analysis / opportunity coach (Auto-analyzed, flagged for human review)
        High = 3,     // Outbound customer message, price commitment (Mandatory human approval)
        Critical = 4  // Routing policy reconfiguration, tenant budget alterations (Mandatory admin approval)
    }

    public enum ApprovalStatus
    {
        Pending = 1,
        Approved = 2,
        Rejected = 3,
        AutoApproved = 4,
        Expired = 5
    }

    public class ApprovalRequest
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrganizationId { get; set; }
        public Guid WorkspaceId { get; set; }
        public Guid RequesterUserId { get; set; }
        public string RequesterName { get; set; } = "System";
        public string ActionType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ContextDataJson { get; set; } = "{}";
        public ApprovalRiskLevel RiskLevel { get; set; } = ApprovalRiskLevel.Medium;
        public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
        public Guid? DecidedByUserId { get; set; }
        public string? DecidedByName { get; set; }
        public string? DecisionNote { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DecidedAt { get; set; }
    }

    public interface IApprovalService
    {
        Task<ApprovalRequest> SubmitApprovalRequestAsync(
            Guid organizationId,
            Guid workspaceId,
            Guid userId,
            string requesterName,
            string actionType,
            string title,
            string contextDataJson,
            ApprovalRiskLevel riskLevel,
            CancellationToken ct = default);

        Task<ApprovalRequest> DecideApprovalRequestAsync(
            Guid requestId,
            Guid deciderUserId,
            string deciderName,
            bool isApproved,
            string decisionNote,
            CancellationToken ct = default);

        ApprovalRiskLevel EvaluateTaskRiskLevel(AITaskType taskType, string? actionDetails = null);
    }
}
