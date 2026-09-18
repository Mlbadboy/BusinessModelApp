using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Operations;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Operations;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Operations
{
    public sealed class InMemoryContinuousBusinessOperatingKernelStore : IContinuousBusinessOperatingKernelStore
    {
        private readonly ConcurrentDictionary<string, BusinessCycle> _cycles = new();

        public Task SaveCycleAsync(BusinessCycle cycle, CancellationToken cancellationToken = default)
        {
            _cycles[cycle.CycleId] = cycle;
            return Task.CompletedTask;
        }

        public Task<BusinessCycle?> GetCycleAsync(string cycleId, CancellationToken cancellationToken = default)
        {
            _cycles.TryGetValue(cycleId, out var cycle);
            return Task.FromResult(cycle);
        }

        public Task<BusinessCycle?> GetActiveCycleForObjectiveAsync(string tenantId, string businessObjectiveId, CancellationToken cancellationToken = default)
        {
            var active = _cycles.Values.FirstOrDefault(c =>
                c.TenantId == tenantId &&
                c.BusinessObjectiveId == businessObjectiveId &&
                c.Status == BusinessCycleStatus.Running);

            return Task.FromResult(active);
        }

        public Task<IReadOnlyList<BusinessCycle>> ListCyclesAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            var list = _cycles.Values.Where(c => c.TenantId == tenantId).ToList();
            return Task.FromResult<IReadOnlyList<BusinessCycle>>(list);
        }
    }

    public sealed class ContinuousBusinessOperatingKernelService : IContinuousBusinessOperatingKernelService
    {
        private readonly IContinuousBusinessOperatingKernelStore _store;

        public ContinuousBusinessOperatingKernelService(IContinuousBusinessOperatingKernelStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<BusinessCycle> StartNewCycleAsync(
            string tenantId,
            string businessObjectiveId,
            string growthObjectiveId,
            TimeSpan? maxDuration = null,
            decimal? maxBudgetINR = null,
            CancellationToken cancellationToken = default)
        {
            // Constitutional Law I42-K: Exactly one active business cycle may operate per business objective
            var existingActive = await _store.GetActiveCycleForObjectiveAsync(tenantId, businessObjectiveId, cancellationToken);
            if (existingActive != null)
            {
                throw new InvalidOperationException(
                    $"Objective '{businessObjectiveId}' already has an active running cycle '{existingActive.CycleId}'. Conclude or terminate it before launching a new cycle (Law I42-K).");
            }

            var previousCycles = await _store.ListCyclesAsync(tenantId, cancellationToken);
            int nextCycleNumber = previousCycles.Count(c => c.BusinessObjectiveId == businessObjectiveId) + 1;

            var cycle = new BusinessCycle
            {
                TenantId = tenantId,
                BusinessObjectiveId = businessObjectiveId,
                GrowthObjectiveId = growthObjectiveId,
                CycleNumber = nextCycleNumber,
                MaxDuration = maxDuration ?? TimeSpan.FromHours(4),
                MaxBudgetINR = maxBudgetINR ?? 500_000m
            };

            cycle.Start();
            await _store.SaveCycleAsync(cycle, cancellationToken);
            return cycle;
        }

        public async Task<BusinessCycle> AdvanceStageAsync(
            string cycleId,
            BusinessCycleStage stage,
            CancellationToken cancellationToken = default)
        {
            var cycle = await _store.GetCycleAsync(cycleId, cancellationToken);
            if (cycle == null) throw new KeyNotFoundException($"Cycle '{cycleId}' not found.");

            cycle.AdvanceStage(stage);
            await _store.SaveCycleAsync(cycle, cancellationToken);
            return cycle;
        }

        public async Task<BusinessCycle> CheckpointCycleAsync(
            string cycleId,
            string stateDigest,
            CancellationToken cancellationToken = default)
        {
            var cycle = await _store.GetCycleAsync(cycleId, cancellationToken);
            if (cycle == null) throw new KeyNotFoundException($"Cycle '{cycleId}' not found.");

            cycle.AdvanceStage(cycle.CurrentStage);
            await _store.SaveCycleAsync(cycle, cancellationToken);
            return cycle;
        }

        public async Task<BusinessCycle> RecoverCycleAfterCrashAsync(
            string cycleId,
            string recoveryNotes,
            CancellationToken cancellationToken = default)
        {
            var cycle = await _store.GetCycleAsync(cycleId, cancellationToken);
            if (cycle == null) throw new KeyNotFoundException($"Cycle '{cycleId}' not found.");

            cycle.RecoverFromCrash(recoveryNotes);
            await _store.SaveCycleAsync(cycle, cancellationToken);
            return cycle;
        }

        public async Task<BusinessCycle> ConcludeCycleAsync(
            string cycleId,
            BusinessCycleOutcome outcome,
            CancellationToken cancellationToken = default)
        {
            var cycle = await _store.GetCycleAsync(cycleId, cancellationToken);
            if (cycle == null) throw new KeyNotFoundException($"Cycle '{cycleId}' not found.");

            cycle.SetOutcome(outcome);
            cycle.AdvanceStage(BusinessCycleStage.Concluded);
            await _store.SaveCycleAsync(cycle, cancellationToken);
            return cycle;
        }
    }
}
