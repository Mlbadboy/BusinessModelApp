using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime;

namespace BusinessModelApp.Core.Interfaces.Runtime
{
    public enum ScheduledJobStatus
    {
        Scheduled = 1,
        Running = 2,
        Paused = 3,
        Completed = 4,
        Cancelled = 5,
        Failed = 6,
        TimedOut = 7,
        DeadLettered = 8
    }

    public readonly record struct ScheduledJobId
    {
        public Guid Value { get; }

        public ScheduledJobId(Guid value)
        {
            if (value == Guid.Empty) throw new ArgumentException("ScheduledJobId cannot be Guid.Empty.", nameof(value));
            Value = value;
        }

        public static ScheduledJobId New() => new(Guid.NewGuid());
        public override string ToString() => Value.ToString();
    }

    public record ScheduledJobRequest
    {
        public ScheduledJobId JobId { get; init; } = ScheduledJobId.New();
        public Guid WorkspaceId { get; init; }
        public string JobName { get; init; } = string.Empty;
        public TimeSpan InitialDelay { get; init; } = TimeSpan.Zero;
        public TimeSpan? Interval { get; init; }
        public int Priority { get; init; } = 5; // 1 (Highest) to 10 (Lowest)
        public int MaxRetries { get; init; } = 3;
        public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(5);
        public string? IdempotencyKey { get; init; }
        public Func<CancellationToken, Task>? ExecutionHandler { get; init; }

        public ScheduledJobRequest(Guid workspaceId, string jobName, Func<CancellationToken, Task>? handler = null)
        {
            if (workspaceId == Guid.Empty) throw new ArgumentException("WorkspaceId cannot be empty.", nameof(workspaceId));
            if (string.IsNullOrWhiteSpace(jobName)) throw new ArgumentException("JobName cannot be empty.", nameof(jobName));

            WorkspaceId = workspaceId;
            JobName = jobName;
            ExecutionHandler = handler;
        }
    }

    public record ScheduledJobInfo
    {
        public ScheduledJobId JobId { get; init; }
        public Guid WorkspaceId { get; init; }
        public string JobName { get; init; } = string.Empty;
        public ScheduledJobStatus Status { get; init; }
        public int Priority { get; init; }
        public int RetryCount { get; init; }
        public DateTime ScheduledAtUtc { get; init; }
        public DateTime? LastRunAtUtc { get; init; }
        public DateTime? NextRunAtUtc { get; init; }
        public string? FailureReason { get; init; }
    }

    public interface ISchedulerEngine
    {
        Task<ScheduledJobId> ScheduleAsync(
            ScheduledJobRequest request,
            CancellationToken cancellationToken = default);

        Task<bool> PauseAsync(
            ScheduledJobId jobId,
            Guid callerWorkspaceId,
            CancellationToken cancellationToken = default);

        Task<bool> ResumeAsync(
            ScheduledJobId jobId,
            Guid callerWorkspaceId,
            CancellationToken cancellationToken = default);

        Task<bool> CancelAsync(
            ScheduledJobId jobId,
            Guid callerWorkspaceId,
            string reason,
            CancellationToken cancellationToken = default);

        Task<ScheduledJobInfo?> GetStatusAsync(
            ScheduledJobId jobId,
            Guid callerWorkspaceId,
            CancellationToken cancellationToken = default);

        int GetActiveJobCount(Guid workspaceId);
    }
}
