using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Experiments;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Experiments;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Experiments
{
    public sealed class InMemoryGrowthExperimentStore : IGrowthExperimentStore
    {
        private readonly ConcurrentDictionary<string, GrowthExperiment> _experiments = new();

        public Task SaveExperimentAsync(GrowthExperiment experiment, CancellationToken cancellationToken = default)
        {
            _experiments[experiment.ExperimentId] = experiment;
            return Task.CompletedTask;
        }

        public Task<GrowthExperiment?> GetExperimentAsync(string experimentId, CancellationToken cancellationToken = default)
        {
            _experiments.TryGetValue(experimentId, out var exp);
            return Task.FromResult(exp);
        }

        public Task<IReadOnlyList<GrowthExperiment>> ListExperimentsForTenantAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            var list = _experiments.Values.Where(e => e.TenantId == tenantId).ToList();
            return Task.FromResult<IReadOnlyList<GrowthExperiment>>(list);
        }
    }

    public sealed class GrowthExperimentRuntimeService : IGrowthExperimentService
    {
        private readonly IGrowthExperimentStore _store;

        public GrowthExperimentRuntimeService(IGrowthExperimentStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<GrowthExperiment> CreateExperimentAsync(
            string tenantId,
            string title,
            string hypothesis,
            GrowthExperimentType type,
            decimal baselineConversionRate,
            decimal targetConversionRate,
            decimal budgetAllocatedINR,
            decimal riskCeiling = 0.20m,
            CancellationToken cancellationToken = default)
        {
            if (budgetAllocatedINR <= 0) throw new ArgumentOutOfRangeException(nameof(budgetAllocatedINR), "Experiment budget must be positive.");

            var exp = new GrowthExperiment
            {
                TenantId = tenantId,
                Title = title,
                Hypothesis = hypothesis,
                Type = type,
                BaselineConversionRate = baselineConversionRate,
                TargetConversionRate = targetConversionRate,
                BudgetAllocatedINR = budgetAllocatedINR,
                RiskCeiling = riskCeiling
            };

            await _store.SaveExperimentAsync(exp, cancellationToken);
            return exp;
        }

        public async Task<GrowthExperiment> StartExperimentAsync(string experimentId, CancellationToken cancellationToken = default)
        {
            var exp = await _store.GetExperimentAsync(experimentId, cancellationToken);
            if (exp == null) throw new KeyNotFoundException($"Experiment '{experimentId}' not found.");

            exp.Start();
            await _store.SaveExperimentAsync(exp, cancellationToken);
            return exp;
        }

        public async Task<GrowthExperiment> RecordObservationsAsync(
            string experimentId,
            int controlSamples,
            int controlSuccesses,
            int variantSamples,
            int variantSuccesses,
            decimal spendINR,
            CancellationToken cancellationToken = default)
        {
            var exp = await _store.GetExperimentAsync(experimentId, cancellationToken);
            if (exp == null) throw new KeyNotFoundException($"Experiment '{experimentId}' not found.");

            exp.RecordObservations(controlSamples, controlSuccesses, variantSamples, variantSuccesses, spendINR);
            await _store.SaveExperimentAsync(exp, cancellationToken);
            return exp;
        }

        public async Task<GrowthExperiment> ConcludeExperimentAsync(
            string experimentId,
            int minimumSamplesPerArm = 100,
            CancellationToken cancellationToken = default)
        {
            var exp = await _store.GetExperimentAsync(experimentId, cancellationToken);
            if (exp == null) throw new KeyNotFoundException($"Experiment '{experimentId}' not found.");

            exp.ConcludeAndEvaluate(minimumSamplesPerArm);
            await _store.SaveExperimentAsync(exp, cancellationToken);
            return exp;
        }
    }
}
