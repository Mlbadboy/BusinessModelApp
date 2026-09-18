using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Experiments;

namespace BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Experiments
{
    public interface IGrowthExperimentStore
    {
        Task SaveExperimentAsync(GrowthExperiment experiment, CancellationToken cancellationToken = default);
        Task<GrowthExperiment?> GetExperimentAsync(string experimentId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<GrowthExperiment>> ListExperimentsForTenantAsync(string tenantId, CancellationToken cancellationToken = default);
    }

    public interface IGrowthExperimentService
    {
        Task<GrowthExperiment> CreateExperimentAsync(
            string tenantId,
            string title,
            string hypothesis,
            GrowthExperimentType type,
            decimal baselineConversionRate,
            decimal targetConversionRate,
            decimal budgetAllocatedINR,
            decimal riskCeiling = 0.20m,
            CancellationToken cancellationToken = default);

        Task<GrowthExperiment> StartExperimentAsync(string experimentId, CancellationToken cancellationToken = default);

        Task<GrowthExperiment> RecordObservationsAsync(
            string experimentId,
            int controlSamples,
            int controlSuccesses,
            int variantSamples,
            int variantSuccesses,
            decimal spendINR,
            CancellationToken cancellationToken = default);

        Task<GrowthExperiment> ConcludeExperimentAsync(
            string experimentId,
            int minimumSamplesPerArm = 100,
            CancellationToken cancellationToken = default);
    }
}
