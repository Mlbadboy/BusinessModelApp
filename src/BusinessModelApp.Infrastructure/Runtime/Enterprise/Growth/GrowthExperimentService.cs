using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Growth;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Growth
{
    public sealed class InMemoryGrowthExperimentStore : IGrowthExperimentStore
    {
        private readonly ConcurrentDictionary<string, GrowthExperimentRecord> _experiments = new();
        private readonly ConcurrentDictionary<string, GrowthMissionPlan> _missions = new();

        public Task SaveExperimentAsync(GrowthExperimentRecord experiment, CancellationToken cancellationToken = default)
        {
            _experiments[experiment.ExperimentId] = experiment;
            return Task.CompletedTask;
        }

        public Task<GrowthExperimentRecord?> GetExperimentAsync(string experimentId, CancellationToken cancellationToken = default)
        {
            _experiments.TryGetValue(experimentId, out var exp);
            return Task.FromResult(exp);
        }

        public Task<IReadOnlyList<GrowthExperimentRecord>> ListExperimentsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<GrowthExperimentRecord>>(_experiments.Values.ToList());
        }

        public Task SaveMissionAsync(GrowthMissionPlan mission, CancellationToken cancellationToken = default)
        {
            _missions[mission.MissionId] = mission;
            return Task.CompletedTask;
        }

        public Task<GrowthMissionPlan?> GetMissionAsync(string missionId, CancellationToken cancellationToken = default)
        {
            _missions.TryGetValue(missionId, out var mission);
            return Task.FromResult(mission);
        }

        public Task<IReadOnlyList<GrowthMissionPlan>> ListMissionsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<GrowthMissionPlan>>(_missions.Values.ToList());
        }
    }

    public sealed class GrowthExperimentService : IGrowthExperimentService
    {
        private readonly IGrowthExperimentStore _store;

        public GrowthExperimentService(IGrowthExperimentStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<GrowthExperimentRecord> CreateExperimentAsync(
            string hypothesisStatement,
            string primaryMetricName,
            decimal baselineMetricValue,
            decimal minimumDetectableEffectPercent,
            int requiredSampleSize,
            decimal allocatedBudget,
            string governanceSignoffId,
            CancellationToken cancellationToken = default)
        {
            // Enforces Law I41-J: Scientific experimentation with governance signoff
            var exp = new GrowthExperimentRecord(
                hypothesisStatement,
                primaryMetricName,
                baselineMetricValue,
                minimumDetectableEffectPercent,
                requiredSampleSize,
                allocatedBudget,
                governanceSignoffId);

            await _store.SaveExperimentAsync(exp, cancellationToken);
            return exp;
        }

        public async Task<GrowthExperimentRecord> RecordExperimentTelemetryAsync(
            string experimentId,
            int sampleCount,
            decimal controlValue,
            decimal variantValue,
            decimal pValue,
            CancellationToken cancellationToken = default)
        {
            var exp = await _store.GetExperimentAsync(experimentId, cancellationToken);
            if (exp == null)
                throw new KeyNotFoundException($"Experiment '{experimentId}' not found.");

            exp.RecordObservations(sampleCount, controlValue, variantValue, pValue);
            await _store.SaveExperimentAsync(exp, cancellationToken);
            return exp;
        }

        public async Task<GrowthExperimentRecord> ConcludeExperimentAsync(
            string experimentId,
            GrowthExperimentStatus finalStatus,
            string summary,
            CancellationToken cancellationToken = default)
        {
            var exp = await _store.GetExperimentAsync(experimentId, cancellationToken);
            if (exp == null)
                throw new KeyNotFoundException($"Experiment '{experimentId}' not found.");

            exp.ConcludeExperiment(finalStatus, summary);
            await _store.SaveExperimentAsync(exp, cancellationToken);
            return exp;
        }

        public async Task<GrowthMissionPlan> CreateGrowthMissionAsync(
            string growthObjectiveId,
            string missionName,
            string targetIcpDescription,
            decimal budgetLimit,
            decimal minTargetLtvToCac = 3.0m,
            decimal minTargetMarginPercent = 35.0m,
            CancellationToken cancellationToken = default)
        {
            // Enforces Law I41-Y (parent link) and Law I41-N/G (LTV:CAC and margin floors)
            var mission = new GrowthMissionPlan(
                growthObjectiveId,
                missionName,
                targetIcpDescription,
                budgetLimit,
                minTargetLtvToCac,
                minTargetMarginPercent);

            await _store.SaveMissionAsync(mission, cancellationToken);
            return mission;
        }

        public async Task<GrowthMissionPlan> ActivateMissionAsync(
            string missionId,
            IEnumerable<string> agentRoles,
            CancellationToken cancellationToken = default)
        {
            var mission = await _store.GetMissionAsync(missionId, cancellationToken);
            if (mission == null)
                throw new KeyNotFoundException($"Growth mission '{missionId}' not found.");

            if (agentRoles != null)
            {
                foreach (var role in agentRoles)
                    mission.AssignAgent(role);
            }

            mission.Activate();
            await _store.SaveMissionAsync(mission, cancellationToken);
            return mission;
        }

        public async Task<GrowthMissionPlan> RecordMissionProgressAsync(
            string missionId,
            decimal addedSpend,
            decimal addedRealizedRevenue,
            CancellationToken cancellationToken = default)
        {
            var mission = await _store.GetMissionAsync(missionId, cancellationToken);
            if (mission == null)
                throw new KeyNotFoundException($"Growth mission '{missionId}' not found.");

            mission.UpdateProgress(addedSpend, addedRealizedRevenue);
            await _store.SaveMissionAsync(mission, cancellationToken);
            return mission;
        }
    }
}
