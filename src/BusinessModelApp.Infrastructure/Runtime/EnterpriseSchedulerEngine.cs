using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Interfaces.Runtime;

namespace BusinessModelApp.Infrastructure.Runtime
{
    public class EnterpriseSchedulerEngine : ISchedulerEngine
    {
        private class JobInternalState
        {
            public ScheduledJobRequest Request { get; init; } = null!;
            public ScheduledJobStatus Status { get; set; } = ScheduledJobStatus.Scheduled;
            public int RetryCount { get; set; } = 0;
            public DateTime ScheduledAtUtc { get; init; } = DateTime.UtcNow;
            public DateTime? LastRunAtUtc { get; set; }
            public DateTime? NextRunAtUtc { get; set; }
            public string? FailureReason { get; set; }
            public CancellationTokenSource? Cts { get; set; }
            public object Lock { get; } = new();
        }

        private readonly ConcurrentDictionary<Guid, JobInternalState> _jobs = new();
        private readonly ConcurrentDictionary<(Guid WorkspaceId, string IdempotencyKey), Guid> _idempotentKeys = new();
        private readonly ConcurrentDictionary<Guid, int> _tenantRunningCounts = new();
        public const int MaxConcurrentJobsPerTenant = 5;

        public async Task<ScheduledJobId> ScheduleAsync(
            ScheduledJobRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            // Check deduplication
            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                var dedupeKey = (request.WorkspaceId, request.IdempotencyKey);
                if (_idempotentKeys.TryGetValue(dedupeKey, out var existingId))
                {
                    if (_jobs.TryGetValue(existingId, out var existingJob) &&
                        existingJob.Status != ScheduledJobStatus.Cancelled &&
                        existingJob.Status != ScheduledJobStatus.Failed)
                    {
                        return new ScheduledJobId(existingId);
                    }
                }
            }

            var internalState = new JobInternalState
            {
                Request = request,
                ScheduledAtUtc = DateTime.UtcNow,
                NextRunAtUtc = DateTime.UtcNow.Add(request.InitialDelay),
                Status = ScheduledJobStatus.Scheduled
            };

            _jobs[request.JobId.Value] = internalState;

            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                _idempotentKeys[(request.WorkspaceId, request.IdempotencyKey)] = request.JobId.Value;
            }

            // If initial delay is zero or minimal, dispatch trigger
            if (request.ExecutionHandler != null)
            {
                _ = ExecuteJobAsync(request.JobId.Value, request.InitialDelay);
            }

            return await Task.FromResult(request.JobId);
        }

        private async Task ExecuteJobAsync(Guid jobId, TimeSpan delay)
        {
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay);
            }

            if (!_jobs.TryGetValue(jobId, out var state))
                return;

            lock (state.Lock)
            {
                if (state.Status == ScheduledJobStatus.Cancelled || state.Status == ScheduledJobStatus.Paused)
                    return;

                // Check tenant concurrency ceiling
                int running = _tenantRunningCounts.GetOrAdd(state.Request.WorkspaceId, 0);
                if (running >= MaxConcurrentJobsPerTenant)
                {
                    state.Status = ScheduledJobStatus.Failed;
                    state.FailureReason = $"Tenant concurrency limit ({MaxConcurrentJobsPerTenant}) exceeded.";
                    return;
                }

                _tenantRunningCounts.AddOrUpdate(state.Request.WorkspaceId, 1, (_, c) => c + 1);
                state.Status = ScheduledJobStatus.Running;
                state.LastRunAtUtc = DateTime.UtcNow;
                state.Cts = new CancellationTokenSource(state.Request.Timeout);
            }

            try
            {
                if (state.Request.ExecutionHandler != null)
                {
                    await state.Request.ExecutionHandler(state.Cts.Token);
                }

                lock (state.Lock)
                {
                    state.Status = ScheduledJobStatus.Completed;
                }
            }
            catch (OperationCanceledException)
            {
                lock (state.Lock)
                {
                    state.Status = ScheduledJobStatus.TimedOut;
                    state.FailureReason = "Execution timed out.";
                }
            }
            catch (Exception ex)
            {
                lock (state.Lock)
                {
                    state.RetryCount++;
                    if (state.RetryCount <= state.Request.MaxRetries)
                    {
                        state.Status = ScheduledJobStatus.Scheduled;
                        state.FailureReason = $"Retry {state.RetryCount}: {ex.Message}";
                        _ = ExecuteJobAsync(jobId, TimeSpan.FromMilliseconds(50 * state.RetryCount)); // Backoff
                    }
                    else
                    {
                        state.Status = ScheduledJobStatus.DeadLettered;
                        state.FailureReason = $"Exhausted max retries ({state.Request.MaxRetries}): {ex.Message}";
                    }
                }
            }
            finally
            {
                _tenantRunningCounts.AddOrUpdate(state.Request.WorkspaceId, 0, (_, c) => Math.Max(0, c - 1));
            }
        }

        public Task<bool> PauseAsync(ScheduledJobId jobId, Guid callerWorkspaceId, CancellationToken cancellationToken = default)
        {
            if (!_jobs.TryGetValue(jobId.Value, out var state))
                return Task.FromResult(false);

            if (state.Request.WorkspaceId != callerWorkspaceId)
                throw new InvalidOperationException("Cross-tenant operation rejected: Caller workspace does not own this scheduled job.");

            lock (state.Lock)
            {
                if (state.Status == ScheduledJobStatus.Scheduled || state.Status == ScheduledJobStatus.Running)
                {
                    state.Status = ScheduledJobStatus.Paused;
                    state.Cts?.Cancel();
                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(false);
        }

        public Task<bool> ResumeAsync(ScheduledJobId jobId, Guid callerWorkspaceId, CancellationToken cancellationToken = default)
        {
            if (!_jobs.TryGetValue(jobId.Value, out var state))
                return Task.FromResult(false);

            if (state.Request.WorkspaceId != callerWorkspaceId)
                throw new InvalidOperationException("Cross-tenant operation rejected: Caller workspace does not own this scheduled job.");

            lock (state.Lock)
            {
                if (state.Status == ScheduledJobStatus.Paused)
                {
                    state.Status = ScheduledJobStatus.Scheduled;
                    _ = ExecuteJobAsync(jobId.Value, TimeSpan.Zero);
                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(false);
        }

        public Task<bool> CancelAsync(ScheduledJobId jobId, Guid callerWorkspaceId, string reason, CancellationToken cancellationToken = default)
        {
            if (!_jobs.TryGetValue(jobId.Value, out var state))
                return Task.FromResult(false);

            if (state.Request.WorkspaceId != callerWorkspaceId)
                throw new InvalidOperationException("Cross-tenant operation rejected: Caller workspace does not own this scheduled job.");

            lock (state.Lock)
            {
                state.Status = ScheduledJobStatus.Cancelled;
                state.FailureReason = reason;
                state.Cts?.Cancel();
                return Task.FromResult(true);
            }
        }

        public Task<ScheduledJobInfo?> GetStatusAsync(ScheduledJobId jobId, Guid callerWorkspaceId, CancellationToken cancellationToken = default)
        {
            if (!_jobs.TryGetValue(jobId.Value, out var state))
                return Task.FromResult<ScheduledJobInfo?>(null);

            if (state.Request.WorkspaceId != callerWorkspaceId)
                throw new InvalidOperationException("Cross-tenant operation rejected: Caller workspace does not own this scheduled job.");

            lock (state.Lock)
            {
                var info = new ScheduledJobInfo
                {
                    JobId = state.Request.JobId,
                    WorkspaceId = state.Request.WorkspaceId,
                    JobName = state.Request.JobName,
                    Status = state.Status,
                    Priority = state.Request.Priority,
                    RetryCount = state.RetryCount,
                    ScheduledAtUtc = state.ScheduledAtUtc,
                    LastRunAtUtc = state.LastRunAtUtc,
                    NextRunAtUtc = state.NextRunAtUtc,
                    FailureReason = state.FailureReason
                };
                return Task.FromResult<ScheduledJobInfo?>(info);
            }
        }

        public int GetActiveJobCount(Guid workspaceId)
        {
            return _tenantRunningCounts.TryGetValue(workspaceId, out var count) ? count : 0;
        }
    }
}
