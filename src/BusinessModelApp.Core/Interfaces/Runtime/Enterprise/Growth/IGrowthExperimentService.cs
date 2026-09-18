using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth;

namespace BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Growth
{
    public interface IGrowthExperimentStore
    {
        Task SaveExperimentAsync(GrowthExperimentRecord experiment, CancellationToken cancellationToken = default);
        Task<GrowthExperimentRecord?> GetExperimentAsync(string experimentId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<GrowthExperimentRecord>> ListExperimentsAsync(CancellationToken cancellationToken = default);

        Task SaveMissionAsync(GrowthMissionPlan mission, CancellationToken cancellationToken = default);
        Task<GrowthMissionPlan?> GetMissionAsync(string missionId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<GrowthMissionPlan>> ListMissionsAsync(CancellationToken cancellationToken = default);
    }

    public interface IGrowthExperimentService
    {
        Task<GrowthExperimentRecord> CreateExperimentAsync(
            string hypothesisStatement,
            string primaryMetricName,
            decimal baselineMetricValue,
            decimal minimumDetectableEffectPercent,
            int requiredSampleSize,
            decimal allocatedBudget,
            string governanceSignoffId,
            CancellationToken cancellationToken = default);

        Task<GrowthExperimentRecord> RecordExperimentTelemetryAsync(
            string experimentId,
            int sampleCount,
            decimal controlValue,
            decimal variantValue,
            decimal pValue,
            CancellationToken cancellationToken = default);

        Task<GrowthExperimentRecord> ConcludeExperimentAsync(
            string experimentId,
            GrowthExperimentStatus finalStatus,
            string summary,
            CancellationToken cancellationToken = default);

        Task<GrowthMissionPlan> CreateGrowthMissionAsync(
            string growthObjectiveId,
            string missionName,
            string targetIcpDescription,
            decimal budgetLimit,
            decimal minTargetLtvToCac = 3.0m,
            decimal minTargetMarginPercent = 35.0m,
            CancellationToken cancellationToken = default);

        Task<GrowthMissionPlan> ActivateMissionAsync(
            string missionId,
            IEnumerable<string> agentRoles,
            CancellationToken cancellationToken = default);

        Task<GrowthMissionPlan> RecordMissionProgressAsync(
            string missionId,
            decimal addedSpend,
            decimal addedRealizedRevenue,
            CancellationToken cancellationToken = default);
    }
}
