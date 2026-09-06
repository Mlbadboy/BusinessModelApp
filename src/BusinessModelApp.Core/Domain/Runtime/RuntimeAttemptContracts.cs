using System;

namespace BusinessModelApp.Core.Domain.Runtime
{
    public class ExecutionOutcome
    {
        public ExecutionOutcomeStatus Status { get; init; } = ExecutionOutcomeStatus.NotStarted;
        public string ResultJson { get; init; } = string.Empty;
        public string VerificationEvidence { get; init; } = string.Empty;
        public double Confidence { get; init; } = 1.0;
        public string? FailureReason { get; init; }
        public bool IsUnknownEffect => Status == ExecutionOutcomeStatus.Unknown || Status == ExecutionOutcomeStatus.TimedOut;

        public static ExecutionOutcome Succeeded(string resultJson, string evidence, double confidence = 1.0) => new()
        {
            Status = ExecutionOutcomeStatus.Succeeded,
            ResultJson = resultJson,
            VerificationEvidence = evidence,
            Confidence = confidence
        };

        public static ExecutionOutcome Failed(string reason) => new()
        {
            Status = ExecutionOutcomeStatus.Failed,
            FailureReason = reason,
            Confidence = 0.0
        };

        public static ExecutionOutcome Unknown(string reason, string? partialEvidence = null) => new()
        {
            Status = ExecutionOutcomeStatus.Unknown,
            FailureReason = reason,
            VerificationEvidence = partialEvidence ?? "UNKNOWN_EFFECT: External effect cannot be verified; blind retry prohibited.",
            Confidence = 0.0
        };

        public static ExecutionOutcome TimedOut(string reason) => new()
        {
            Status = ExecutionOutcomeStatus.TimedOut,
            FailureReason = reason,
            VerificationEvidence = "UNKNOWN_EFFECT: Operation timed out in-flight; side-effect status uncertain.",
            Confidence = 0.0
        };
    }

    public class RuntimeAttempt
    {
        public ExecutionAttemptId AttemptId { get; init; }
        public RuntimeRunId RunId { get; init; }
        public int AttemptNumber { get; init; }
        public string WorkerId { get; init; }
        public LeaseId LeaseId { get; init; }
        public FenceToken FenceToken { get; init; }
        public DateTime StartedAtUtc { get; init; } = DateTime.UtcNow;
        public DateTime ExpiresAtUtc { get; set; }
        public AttemptState State { get; set; } = AttemptState.Claimed;
        public ExecutionOutcome? Outcome { get; set; }

        public RuntimeAttempt(
            ExecutionAttemptId attemptId,
            RuntimeRunId runId,
            int attemptNumber,
            string workerId,
            LeaseId leaseId,
            FenceToken fenceToken,
            DateTime expiresAtUtc)
        {
            if (string.IsNullOrWhiteSpace(workerId))
                throw new ArgumentException("WorkerId cannot be empty.", nameof(workerId));

            AttemptId = attemptId;
            RunId = runId;
            AttemptNumber = attemptNumber;
            WorkerId = workerId;
            LeaseId = leaseId;
            FenceToken = fenceToken;
            ExpiresAtUtc = expiresAtUtc;
        }
    }
}
