using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Interfaces.Runtime;

namespace BusinessModelApp.Infrastructure.Runtime
{
    public class InMemoryRuntimeLeaseManager : IRuntimeLeaseManager
    {
        private class RunLeaseState
        {
            public RuntimeLease? ActiveLease { get; set; }
            public FenceToken CurrentFenceToken { get; set; } = FenceToken.Initial;
            public object SyncLock { get; } = new();
        }

        private readonly ConcurrentDictionary<Guid, RunLeaseState> _runStates = new();

        public Task<LeaseAcquisitionResult> AcquireLeaseAsync(
            RuntimeRunId runId,
            ExecutionAttemptId attemptId,
            string workerId,
            Guid workspaceId,
            TimeSpan duration,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(workerId)) throw new ArgumentException("WorkerId cannot be empty.", nameof(workerId));
            if (workspaceId == Guid.Empty) throw new ArgumentException("WorkspaceId cannot be empty.", nameof(workspaceId));

            var state = _runStates.GetOrAdd(runId.Value, _ => new RunLeaseState());

            lock (state.SyncLock)
            {
                var now = DateTime.UtcNow;

                // Check if active lease exists and is not expired
                if (state.ActiveLease != null && state.ActiveLease.ExpiresAtUtc > now)
                {
                    if (state.ActiveLease.OwnerId != workerId)
                    {
                        return Task.FromResult(LeaseAcquisitionResult.Conflict(
                            $"Run '{runId}' is currently leased by worker '{state.ActiveLease.OwnerId}' until {state.ActiveLease.ExpiresAtUtc:O}.",
                            state.CurrentFenceToken));
                    }

                    // Same worker re-acquiring/extending
                    state.ActiveLease.ExpiresAtUtc = now.Add(duration);
                    return Task.FromResult(LeaseAcquisitionResult.Acquired(state.ActiveLease));
                }

                // Previous lease expired or new run: increment fence token
                if (state.ActiveLease != null)
                {
                    state.CurrentFenceToken = state.CurrentFenceToken.Next();
                }

                var newLease = new RuntimeLease(
                    LeaseId.New(),
                    runId,
                    attemptId,
                    workerId,
                    workspaceId,
                    now.Add(duration),
                    state.CurrentFenceToken);

                state.ActiveLease = newLease;
                return Task.FromResult(LeaseAcquisitionResult.Acquired(newLease));
            }
        }

        public Task<LeaseAcquisitionResult> RenewLeaseAsync(
            RuntimeRunId runId,
            LeaseId leaseId,
            FenceToken token,
            TimeSpan duration,
            CancellationToken cancellationToken = default)
        {
            if (!_runStates.TryGetValue(runId.Value, out var state))
            {
                return Task.FromResult(LeaseAcquisitionResult.Expired("No active lease found for run.", FenceToken.Initial));
            }

            lock (state.SyncLock)
            {
                if (token < state.CurrentFenceToken)
                {
                    return Task.FromResult(LeaseAcquisitionResult.Fenced(
                        $"Worker held stale FenceToken {token.Value}; current token is {state.CurrentFenceToken.Value}.",
                        state.CurrentFenceToken));
                }

                if (state.ActiveLease == null || state.ActiveLease.LeaseId != leaseId)
                {
                    return Task.FromResult(LeaseAcquisitionResult.Expired("Lease ID mismatch or lease not found.", state.CurrentFenceToken));
                }

                if (state.ActiveLease.ExpiresAtUtc <= DateTime.UtcNow)
                {
                    return Task.FromResult(LeaseAcquisitionResult.Expired("Lease has already expired.", state.CurrentFenceToken));
                }

                state.ActiveLease.ExpiresAtUtc = DateTime.UtcNow.Add(duration);
                return Task.FromResult(LeaseAcquisitionResult.Acquired(state.ActiveLease));
            }
        }

        public Task ReleaseLeaseAsync(
            RuntimeRunId runId,
            LeaseId leaseId,
            FenceToken token,
            CancellationToken cancellationToken = default)
        {
            if (_runStates.TryGetValue(runId.Value, out var state))
            {
                lock (state.SyncLock)
                {
                    if (state.ActiveLease != null && state.ActiveLease.LeaseId == leaseId && token == state.CurrentFenceToken)
                    {
                        state.ActiveLease = null;
                    }
                }
            }

            return Task.CompletedTask;
        }

        public Task<bool> ValidateFenceTokenAsync(
            RuntimeRunId runId,
            FenceToken token,
            CancellationToken cancellationToken = default)
        {
            if (!_runStates.TryGetValue(runId.Value, out var state))
                return Task.FromResult(token == FenceToken.Initial);

            lock (state.SyncLock)
            {
                return Task.FromResult(token == state.CurrentFenceToken);
            }
        }

        public Task<FenceToken> GetCurrentFenceTokenAsync(
            RuntimeRunId runId,
            CancellationToken cancellationToken = default)
        {
            if (!_runStates.TryGetValue(runId.Value, out var state))
                return Task.FromResult(FenceToken.Initial);

            lock (state.SyncLock)
            {
                return Task.FromResult(state.CurrentFenceToken);
            }
        }
    }
}
