using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Missions;
using BusinessModelApp.Core.Interfaces.Runtime.Missions;

namespace BusinessModelApp.Infrastructure.Runtime.Missions.Coordination
{
    public class MissionResourceArbiter : IMissionResourceArbiter
    {
        private readonly IMissionCoordinationStore _store;
        private readonly TenantMissionConcurrencyPolicy _systemPolicy;
        private readonly object _lockSync = new();
        private long _fencingSequence = 1000;

        public MissionResourceArbiter(
            IMissionCoordinationStore store,
            TenantMissionConcurrencyPolicy? systemPolicy = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _systemPolicy = systemPolicy ?? new TenantMissionConcurrencyPolicy();
        }

        public async Task<(bool Acquired, MissionResourceLock? Lock, string? Reason)> AcquireLockAsync(
            string tenantId,
            string resourceNamespace,
            string resourceType,
            string resourceId,
            string missionId,
            string nodeId,
            TimeSpan? ttl = null,
            CancellationToken ct = default)
        {
            var resTuple = new List<(string Namespace, string Type, string ResourceId)>
            {
                (resourceNamespace, resourceType, resourceId)
            };

            var (allAcquired, locks, reason) = await AcquireMultipleLocksCanonicalAsync(
                tenantId,
                resTuple,
                missionId,
                nodeId,
                ttl,
                ct);

            if (allAcquired && locks.Count > 0)
            {
                return (true, locks[0], null);
            }
            return (false, null, reason);
        }

        public async Task<(bool AllAcquired, IReadOnlyList<MissionResourceLock> Locks, string? Reason)> AcquireMultipleLocksCanonicalAsync(
            string tenantId,
            IReadOnlyList<(string Namespace, string Type, string ResourceId)> resources,
            string missionId,
            string nodeId,
            TimeSpan? ttl = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId is required.", nameof(tenantId));
            if (resources == null || resources.Count == 0) return (true, Array.Empty<MissionResourceLock>(), null);

            var effectiveTtl = ttl ?? _systemPolicy.DefaultLockTtl;
            var now = DateTime.UtcNow;

            // Invariant I27-C: Sort into canonical order to prevent deadlocks
            var canonicalMap = resources.ToDictionary(
                r => CanonicalResourceOrdering.GetCanonicalKey(r.Namespace, r.Type, tenantId, r.ResourceId),
                r => r);

            var sortedKeys = CanonicalResourceOrdering.SortCanonicalKeys(canonicalMap.Keys);
            var acquiredLocks = new List<MissionResourceLock>();

            lock (_lockSync)
            {
                foreach (var key in sortedKeys)
                {
                    var existing = _store.GetLockAsync(tenantId, key, ct).GetAwaiter().GetResult();

                    // If existing lock is valid and not expired, cannot acquire
                    if (existing != null && !existing.IsExpired(now))
                    {
                        // Check if same mission/node is already holder
                        if (existing.HolderMissionId != missionId || existing.HolderNodeId != nodeId)
                        {
                            // Rollback acquired locks in this batch to avoid partial lock deadlock
                            foreach (var rollbackLock in acquiredLocks)
                            {
                                _store.DeleteLockAsync(tenantId, rollbackLock.CanonicalKey, ct).GetAwaiter().GetResult();
                            }
                            return (false, Array.Empty<MissionResourceLock>(), $"Resource contention on '{key}'. Held by Mission '{existing.HolderMissionId}' until {existing.ExpiresUtc:O}.");
                        }
                    }

                    // Stale holder or new lock: increment fencing token
                    long newFencing = Interlocked.Increment(ref _fencingSequence);
                    long nextVersion = (existing?.LockVersion ?? 0) + 1;
                    var (rNs, rType, rId) = canonicalMap[key];

                    var newLock = new MissionResourceLock
                    {
                        LockId = $"LCK-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                        TenantId = tenantId,
                        ResourceNamespace = rNs,
                        ResourceType = rType,
                        ResourceId = rId,
                        LockVersion = nextVersion,
                        HolderMissionId = missionId,
                        HolderNodeId = nodeId,
                        AcquiredUtc = now,
                        ExpiresUtc = now.Add(effectiveTtl),
                        FencingToken = newFencing
                    };

                    _store.SaveLockAsync(newLock, ct).GetAwaiter().GetResult();
                    acquiredLocks.Add(newLock);
                }
            }

            return (true, acquiredLocks, null);
        }

        public async Task<bool> ReleaseLockAsync(
            string tenantId,
            string resourceNamespace,
            string resourceType,
            string resourceId,
            long fencingToken,
            CancellationToken ct = default)
        {
            var key = CanonicalResourceOrdering.GetCanonicalKey(resourceNamespace, resourceType, tenantId, resourceId);

            lock (_lockSync)
            {
                var existing = _store.GetLockAsync(tenantId, key, ct).GetAwaiter().GetResult();
                if (existing == null) return true; // Already free

                // Stale holder cannot release newer lock
                if (existing.FencingToken != fencingToken)
                {
                    return false; // Fencing token mismatch (stale holder rejected)
                }

                _store.DeleteLockAsync(tenantId, key, ct).GetAwaiter().GetResult();
                return true;
            }
        }

        public async Task<bool> ValidateFencingTokenAsync(
            string tenantId,
            string resourceNamespace,
            string resourceType,
            string resourceId,
            long fencingToken,
            CancellationToken ct = default)
        {
            var key = CanonicalResourceOrdering.GetCanonicalKey(resourceNamespace, resourceType, tenantId, resourceId);
            var existing = await _store.GetLockAsync(tenantId, key, ct);
            if (existing == null) return false;

            if (existing.IsExpired(DateTime.UtcNow)) return false;

            return existing.FencingToken == fencingToken;
        }
    }
}
