using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Workforce;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Workforce
{
    public interface IContinuousOperationsStore
    {
        Task SaveCycleAsync(string tenantId, ContinuousBusinessCycle cycle);
        Task<ContinuousBusinessCycle?> GetCycleAsync(string tenantId, string cycleId);
        Task<IReadOnlyList<ContinuousBusinessCycle>> ListCyclesAsync(string tenantId);
        Task<ContinuousBusinessCycle?> GetCycleByIdempotencyKeyAsync(string tenantId, string idempotencyKey);
    }

    public class InMemoryContinuousOperationsStore : IContinuousOperationsStore
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ContinuousBusinessCycle>> _cyclesByTenant = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _idempotencyMap = new(); // tenant -> key -> cycleId

        public Task SaveCycleAsync(string tenantId, ContinuousBusinessCycle cycle)
        {
            var map = _cyclesByTenant.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, ContinuousBusinessCycle>());
            map[cycle.CycleId] = cycle;

            if (!string.IsNullOrWhiteSpace(cycle.IdempotencyKey))
            {
                var idMap = _idempotencyMap.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, string>());
                idMap[cycle.IdempotencyKey] = cycle.CycleId;
            }

            return Task.CompletedTask;
        }

        public Task<ContinuousBusinessCycle?> GetCycleAsync(string tenantId, string cycleId)
        {
            if (_cyclesByTenant.TryGetValue(tenantId, out var map) && map.TryGetValue(cycleId, out var cycle))
            {
                return Task.FromResult<ContinuousBusinessCycle?>(cycle);
            }
            return Task.FromResult<ContinuousBusinessCycle?>(null);
        }

        public Task<IReadOnlyList<ContinuousBusinessCycle>> ListCyclesAsync(string tenantId)
        {
            if (_cyclesByTenant.TryGetValue(tenantId, out var map))
            {
                return Task.FromResult<IReadOnlyList<ContinuousBusinessCycle>>(map.Values.OrderBy(c => c.SequenceNumber).ToList());
            }
            return Task.FromResult<IReadOnlyList<ContinuousBusinessCycle>>(Array.Empty<ContinuousBusinessCycle>());
        }

        public Task<ContinuousBusinessCycle?> GetCycleByIdempotencyKeyAsync(string tenantId, string idempotencyKey)
        {
            if (_idempotencyMap.TryGetValue(tenantId, out var idMap) && idMap.TryGetValue(idempotencyKey, out var cycleId))
            {
                return GetCycleAsync(tenantId, cycleId);
            }
            return Task.FromResult<ContinuousBusinessCycle?>(null);
        }
    }

    public class ContinuousBusinessOperationsCoordinator : IContinuousBusinessOperationsCoordinator
    {
        private readonly IContinuousOperationsStore _store;

        public ContinuousBusinessOperationsCoordinator(IContinuousOperationsStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<ContinuousBusinessCycle> TriggerCycleAsync(string tenantId, BusinessCycleTrigger trigger, decimal budget = 100m)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId required", nameof(tenantId));
            if (trigger == null) throw new ArgumentNullException(nameof(trigger));

            var idempotencyKey = ComputeTriggerIdempotencyKey(tenantId, trigger);
            var existing = await _store.GetCycleByIdempotencyKeyAsync(tenantId, idempotencyKey);
            if (existing != null)
            {
                return existing; // Idempotent deduplication
            }

            var existingCycles = await _store.ListCyclesAsync(tenantId);
            long nextSeq = existingCycles.Count + 1;

            var cycle = new ContinuousBusinessCycle
            {
                TenantId = tenantId,
                TriggerId = trigger.TriggerId,
                SequenceNumber = nextSeq,
                BrainSnapshotHash = ComputeHash($"Brain-{nextSeq}-{tenantId}"),
                PolicySnapshotHash = ComputeHash($"Policy-{nextSeq}-{tenantId}"),
                AllocatedBudget = budget,
                ActualSpent = 0m,
                StartedAt = DateTime.UtcNow,
                Deadline = DateTime.UtcNow.AddMinutes(30), // Bounded finite cycle
                CheckpointState = "INITIALIZED",
                IdempotencyKey = idempotencyKey
            };

            await _store.SaveCycleAsync(tenantId, cycle);
            return cycle;
        }

        public Task<ContinuousBusinessCycle?> GetCycleAsync(string tenantId, string cycleId)
        {
            return _store.GetCycleAsync(tenantId, cycleId);
        }

        public Task<IReadOnlyList<ContinuousBusinessCycle>> ListCyclesAsync(string tenantId)
        {
            return _store.ListCyclesAsync(tenantId);
        }

        public async Task<bool> AdvanceCheckpointAsync(string tenantId, string cycleId, string nextState, decimal incrementalCost = 0m)
        {
            var cycle = await _store.GetCycleAsync(tenantId, cycleId);
            if (cycle == null || cycle.IsCompleted) return false;

            if (cycle.IsTimedOut)
            {
                cycle.CheckpointState = "FAILED";
                cycle.OutcomeSummary = "Cycle deadline exceeded (Timeout).";
                await _store.SaveCycleAsync(tenantId, cycle);
                return false;
            }

            cycle.ActualSpent += incrementalCost;
            if (cycle.ActualSpent > cycle.AllocatedBudget)
            {
                cycle.CheckpointState = "FAILED";
                cycle.OutcomeSummary = "Cycle budget exceeded.";
                await _store.SaveCycleAsync(tenantId, cycle);
                return false;
            }

            cycle.CheckpointState = nextState;
            await _store.SaveCycleAsync(tenantId, cycle);
            return true;
        }

        public async Task<bool> CompleteCycleAsync(string tenantId, string cycleId, string outcomeSummary)
        {
            var cycle = await _store.GetCycleAsync(tenantId, cycleId);
            if (cycle == null) return false;

            cycle.CheckpointState = "COMPLETED";
            cycle.OutcomeSummary = outcomeSummary;
            await _store.SaveCycleAsync(tenantId, cycle);
            return true;
        }

        public async Task<ContinuousBusinessCycle?> RecoverLastCheckpointAsync(string tenantId)
        {
            var cycles = await _store.ListCyclesAsync(tenantId);
            return cycles.LastOrDefault(c => !c.IsCompleted);
        }

        private static string ComputeTriggerIdempotencyKey(string tenantId, BusinessCycleTrigger trigger)
        {
            var raw = $"{tenantId}|{trigger.TriggerType}|{trigger.TriggerId}|{trigger.SourcePayloadJson}";
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }

        private static string ComputeHash(string input)
        {
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(input)));
        }
    }
}
