using System;

namespace BusinessModelApp.Core.Domain.Runtime
{
    public enum LeaseStatus
    {
        Acquired = 1,
        Renewed = 2,
        Conflict = 3,
        Expired = 4,
        Fenced = 5,
        Released = 6
    }

    public class RuntimeLease
    {
        public LeaseId LeaseId { get; init; }
        public RuntimeRunId RunId { get; init; }
        public ExecutionAttemptId AttemptId { get; init; }
        public string OwnerId { get; init; }
        public Guid WorkspaceId { get; init; }
        public DateTime IssuedAtUtc { get; init; } = DateTime.UtcNow;
        public DateTime ExpiresAtUtc { get; set; }
        public FenceToken Token { get; init; }
        public bool IsActive => DateTime.UtcNow < ExpiresAtUtc;

        public RuntimeLease(
            LeaseId leaseId,
            RuntimeRunId runId,
            ExecutionAttemptId attemptId,
            string ownerId,
            Guid workspaceId,
            DateTime expiresAtUtc,
            FenceToken token)
        {
            if (string.IsNullOrWhiteSpace(ownerId))
                throw new ArgumentException("OwnerId cannot be empty.", nameof(ownerId));
            if (workspaceId == Guid.Empty)
                throw new ArgumentException("WorkspaceId cannot be empty.", nameof(workspaceId));

            LeaseId = leaseId;
            RunId = runId;
            AttemptId = attemptId;
            OwnerId = ownerId;
            WorkspaceId = workspaceId;
            ExpiresAtUtc = expiresAtUtc;
            Token = token;
        }
    }

    public record LeaseAcquisitionResult
    {
        public LeaseStatus Status { get; init; }
        public RuntimeLease? Lease { get; init; }
        public FenceToken Token { get; init; }
        public string? FailureReason { get; init; }

        public static LeaseAcquisitionResult Acquired(RuntimeLease lease) => new()
        {
            Status = LeaseStatus.Acquired,
            Lease = lease,
            Token = lease.Token
        };

        public static LeaseAcquisitionResult Conflict(string reason, FenceToken currentToken) => new()
        {
            Status = LeaseStatus.Conflict,
            FailureReason = reason,
            Token = currentToken
        };

        public static LeaseAcquisitionResult Fenced(string reason, FenceToken currentToken) => new()
        {
            Status = LeaseStatus.Fenced,
            FailureReason = reason,
            Token = currentToken
        };

        public static LeaseAcquisitionResult Expired(string reason, FenceToken currentToken) => new()
        {
            Status = LeaseStatus.Expired,
            FailureReason = reason,
            Token = currentToken
        };
    }
}
