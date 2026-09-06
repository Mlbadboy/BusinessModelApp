using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Domain.Runtime.Fleet;
using BusinessModelApp.Core.Interfaces.Runtime.Fleet;

namespace BusinessModelApp.Infrastructure.Runtime.Fleet
{
    public class WorkerLeaseCoordinator : IWorkerLeaseCoordinator
    {
        private class ActiveNodeLease
        {
            public RuntimeLease Lease { get; init; } = null!;
            public MissionGraphVersion BoundGraphVersion { get; init; }
            public WorkerProcessId WorkerId { get; init; }
            public AgentInstanceId AgentId { get; init; }
            public ExecutionAttemptId AttemptId { get; init; }
            public FenceToken Token { get; init; }
            public DateTime ExpiresAtUtc { get; set; }
            public bool IsReleased { get; set; }
        }

        // Key: $"{graphId}:{nodeId}"
        private readonly ConcurrentDictionary<string, ActiveNodeLease> _activeLeases = new(StringComparer.Ordinal);
        private long _globalFenceSequence = 1000;

        public Task<LeaseGrantResult> AcquireNodeLeaseAsync(
            Guid workspaceId,
            MissionGraphId graphId,
            MissionGraphVersion graphVersion,
            MissionNodeId nodeId,
            WorkerProcessId workerId,
            AgentInstanceId agentId,
            TimeSpan duration,
            CancellationToken ct = default)
        {
            var key = $"{graphId}:{nodeId.Value}";
            var now = DateTime.UtcNow;

            while (true)
            {
                if (_activeLeases.TryGetValue(key, out var current))
                {
                    // If lease is not expired and not released, deny
                    if (!current.IsReleased && current.ExpiresAtUtc > now)
                    {
                        return Task.FromResult(LeaseGrantResult.Denied(
                            $"Node '{nodeId.Value}' is already actively leased to worker '{current.WorkerId}' until {current.ExpiresAtUtc:O}."));
                    }
                }

                var tokenVal = Interlocked.Increment(ref _globalFenceSequence);
                var fenceToken = new FenceToken(tokenVal);
                var leaseId = LeaseId.New();
                var attemptId = ExecutionAttemptId.New();
                var expiresAt = now.Add(duration);

                var runtimeLease = new RuntimeLease(
                    leaseId,
                    RuntimeRunId.New(),
                    attemptId,
                    workerId.ToString(),
                    workspaceId,
                    expiresAt,
                    fenceToken);

                var newActiveLease = new ActiveNodeLease
                {
                    Lease = runtimeLease,
                    BoundGraphVersion = graphVersion,
                    WorkerId = workerId,
                    AgentId = agentId,
                    AttemptId = attemptId,
                    Token = fenceToken,
                    ExpiresAtUtc = expiresAt,
                    IsReleased = false
                };

                if (current == null)
                {
                    if (_activeLeases.TryAdd(key, newActiveLease))
                    {
                        return Task.FromResult(LeaseGrantResult.Granted(runtimeLease, fenceToken, attemptId));
                    }
                }
                else
                {
                    if (_activeLeases.TryUpdate(key, newActiveLease, current))
                    {
                        return Task.FromResult(LeaseGrantResult.Granted(runtimeLease, fenceToken, attemptId));
                    }
                }
            }
        }

        public Task<FencingValidationResult> ValidateEnvelopeAsync(
            FencingEnvelope envelope,
            MissionGraph currentGraph,
            CancellationToken ct = default)
        {
            if (envelope == null)
            {
                return Task.FromResult(FencingValidationResult.Invalid("Envelope cannot be null."));
            }

            if (currentGraph == null)
            {
                return Task.FromResult(FencingValidationResult.Invalid("Current graph cannot be null."));
            }

            // 1. Workspace match
            if (envelope.WorkspaceId != currentGraph.WorkspaceId)
            {
                return Task.FromResult(FencingValidationResult.Invalid(
                    $"Workspace mismatch: envelope workspace '{envelope.WorkspaceId}' does not match graph '{currentGraph.WorkspaceId}'."));
            }

            // 2. Graph version match (stale graph version rejection)
            if (envelope.GraphVersion != currentGraph.Version)
            {
                return Task.FromResult(FencingValidationResult.StaleGraph(
                    $"Stale graph version: attempt was executed under version {envelope.GraphVersion}, but current graph is {currentGraph.Version}."));
            }

            var key = $"{envelope.MissionGraphId}:{envelope.MissionNodeId.Value}";
            if (!_activeLeases.TryGetValue(key, out var activeLease))
            {
                return Task.FromResult(FencingValidationResult.Invalid($"No active lease found for node '{envelope.MissionNodeId.Value}'."));
            }

            // 3. Lease release/expiration check
            if (activeLease.IsReleased)
            {
                return Task.FromResult(FencingValidationResult.Expired($"Lease for node '{envelope.MissionNodeId.Value}' was already released."));
            }

            if (DateTime.UtcNow > activeLease.ExpiresAtUtc)
            {
                return Task.FromResult(FencingValidationResult.Expired(
                    $"Lease expired at {activeLease.ExpiresAtUtc:O}, outcome submitted at {DateTime.UtcNow:O}."));
            }

            // 4. Fence token match
            if (envelope.FenceToken.Value != activeLease.Token.Value)
            {
                return Task.FromResult(FencingValidationResult.StaleToken(
                    $"Stale fencing token: envelope token {envelope.FenceToken.Value} does not match active token {activeLease.Token.Value}."));
            }

            // 5. Worker and attempt match
            if (envelope.WorkerId != activeLease.WorkerId)
            {
                return Task.FromResult(FencingValidationResult.Invalid(
                    $"Worker mismatch: envelope worker {envelope.WorkerId} does not match lease holder {activeLease.WorkerId}."));
            }

            if (envelope.AttemptId != activeLease.AttemptId)
            {
                return Task.FromResult(FencingValidationResult.Invalid(
                    $"Attempt mismatch: envelope attempt {envelope.AttemptId} does not match lease attempt {activeLease.AttemptId}."));
            }

            return Task.FromResult(FencingValidationResult.Valid());
        }

        public Task<bool> ReleaseLeaseAsync(LeaseId leaseId, FenceToken token, CancellationToken ct = default)
        {
            foreach (var kvp in _activeLeases)
            {
                if (kvp.Value.Lease.LeaseId == leaseId && kvp.Value.Token.Value == token.Value)
                {
                    kvp.Value.IsReleased = true;
                    return Task.FromResult(true);
                }
            }
            return Task.FromResult(false);
        }
    }
}
