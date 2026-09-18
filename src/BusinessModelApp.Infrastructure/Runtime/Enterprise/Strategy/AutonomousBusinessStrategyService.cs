using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Strategy;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Strategy;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Strategy
{
    public sealed class InMemoryAutonomousBusinessStrategyStore : IAutonomousBusinessStrategyStore
    {
        private readonly ConcurrentDictionary<string, StrategicObjective> _objectives = new();
        private readonly ConcurrentDictionary<string, StrategicInitiative> _initiatives = new();

        public Task SaveObjectiveAsync(StrategicObjective objective, CancellationToken cancellationToken = default)
        {
            _objectives[objective.ObjectiveId] = objective;
            return Task.CompletedTask;
        }

        public Task<StrategicObjective?> GetObjectiveAsync(string objectiveId, CancellationToken cancellationToken = default)
        {
            _objectives.TryGetValue(objectiveId, out var obj);
            return Task.FromResult(obj);
        }

        public Task<IReadOnlyList<StrategicObjective>> ListObjectivesForTenantAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            var list = _objectives.Values.Where(o => o.TenantId == tenantId).ToList();
            return Task.FromResult<IReadOnlyList<StrategicObjective>>(list);
        }

        public Task SaveInitiativeAsync(StrategicInitiative initiative, CancellationToken cancellationToken = default)
        {
            _initiatives[initiative.InitiativeId] = initiative;
            return Task.CompletedTask;
        }

        public Task<StrategicInitiative?> GetInitiativeAsync(string initiativeId, CancellationToken cancellationToken = default)
        {
            _initiatives.TryGetValue(initiativeId, out var init);
            return Task.FromResult(init);
        }

        public Task<IReadOnlyList<StrategicInitiative>> ListInitiativesForObjectiveAsync(string objectiveId, CancellationToken cancellationToken = default)
        {
            var list = _initiatives.Values.Where(i => i.ObjectiveId == objectiveId).ToList();
            return Task.FromResult<IReadOnlyList<StrategicInitiative>>(list);
        }
    }

    public sealed class AutonomousBusinessStrategyService : IAutonomousBusinessStrategyService
    {
        private readonly IAutonomousBusinessStrategyStore _store;

        public AutonomousBusinessStrategyService(IAutonomousBusinessStrategyStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<StrategicObjective> CreateStrategicObjectiveAsync(
            string tenantId,
            string title,
            StrategicTimeHorizon horizon,
            decimal targetRevenueINR,
            decimal allocatedCapitalINR,
            decimal maxRiskToleranceScore = 0.30m,
            CancellationToken cancellationToken = default)
        {
            if (allocatedCapitalINR <= 0) throw new ArgumentOutOfRangeException(nameof(allocatedCapitalINR), "Allocated capital must be positive.");

            var obj = new StrategicObjective
            {
                TenantId = tenantId,
                Title = title,
                Horizon = horizon,
                TargetRevenueINR = targetRevenueINR,
                AllocatedCapitalINR = allocatedCapitalINR,
                MaxRiskToleranceScore = maxRiskToleranceScore
            };

            await _store.SaveObjectiveAsync(obj, cancellationToken);
            return obj;
        }

        public async Task<StrategicInitiative> ProposeInitiativeAsync(
            string tenantId,
            string objectiveId,
            string title,
            string description,
            decimal strategicValueScore,
            decimal requiredCapitalINR,
            CancellationToken cancellationToken = default)
        {
            var objective = await _store.GetObjectiveAsync(objectiveId, cancellationToken);
            if (objective == null) throw new KeyNotFoundException($"Strategic objective '{objectiveId}' not found.");

            var existingInitiatives = await _store.ListInitiativesForObjectiveAsync(objectiveId, cancellationToken);
            var committedCapital = existingInitiatives.Where(i => i.Status != StrategicInitiativeStatus.Abandoned).Sum(i => i.RequiredCapitalINR);

            if (committedCapital + requiredCapitalINR > objective.AllocatedCapitalINR)
            {
                throw new InvalidOperationException($"Strategic initiative capital requirement (₹{requiredCapitalINR:N0}) exceeds available objective budget (Available: ₹{(objective.AllocatedCapitalINR - committedCapital):N0}).");
            }

            var initiative = new StrategicInitiative
            {
                TenantId = tenantId,
                ObjectiveId = objectiveId,
                Title = title,
                Description = description,
                StrategicValueScore = Math.Clamp(strategicValueScore, 1m, 100m),
                RequiredCapitalINR = requiredCapitalINR
            };

            await _store.SaveInitiativeAsync(initiative, cancellationToken);
            return initiative;
        }

        public async Task<StrategicInitiative> ApproveInitiativeWithPRG1Async(
            string initiativeId,
            string prg1SignoffSha256,
            CancellationToken cancellationToken = default)
        {
            var initiative = await _store.GetInitiativeAsync(initiativeId, cancellationToken);
            if (initiative == null) throw new KeyNotFoundException($"Initiative '{initiativeId}' not found.");

            initiative.ApprovePRG1(prg1SignoffSha256);
            await _store.SaveInitiativeAsync(initiative, cancellationToken);
            return initiative;
        }

        public async Task<StrategicInitiative> LaunchInitiativeExecutionAsync(
            string initiativeId,
            CancellationToken cancellationToken = default)
        {
            var initiative = await _store.GetInitiativeAsync(initiativeId, cancellationToken);
            if (initiative == null) throw new KeyNotFoundException($"Initiative '{initiativeId}' not found.");

            initiative.StartExecution();
            await _store.SaveInitiativeAsync(initiative, cancellationToken);
            return initiative;
        }
    }
}
