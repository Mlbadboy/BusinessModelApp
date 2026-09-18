using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth;

namespace BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Growth
{
    public interface IGrowthControlPlaneStore
    {
        Task SaveObjectiveAsync(GrowthObjective objective);
        Task<GrowthObjective?> GetObjectiveAsync(string tenantId, string objectiveId);
        Task<IReadOnlyList<GrowthObjective>> ListObjectivesAsync(string tenantId);

        Task SaveStrategyPlanAsync(GrowthStrategyPlan plan);
        Task<GrowthStrategyPlan?> GetStrategyPlanAsync(string tenantId, string strategyId);
        Task<IReadOnlyList<GrowthStrategyPlan>> ListStrategyPlansAsync(string tenantId);
    }

    public interface IGrowthControlPlaneService
    {
        Task<GrowthObjective> CreateGrowthObjectiveAsync(GrowthObjective objective);
        Task<bool> AuthorizeGrowthObjectiveAsync(string tenantId, string objectiveId, string humanSignoffId);
        Task<GrowthObjective?> GetGrowthObjectiveAsync(string tenantId, string objectiveId);
        Task<IReadOnlyList<GrowthObjective>> ListGrowthObjectivesAsync(string tenantId);

        Task<GrowthStrategyPlan> FormulateStrategyPlanAsync(GrowthStrategyPlan plan);
        Task<bool> AuthorizeStrategyPlanAsync(string tenantId, string strategyId, string humanSignoffId);
        Task<GrowthStrategyPlan?> GetStrategyPlanAsync(string tenantId, string strategyId);
        Task<IReadOnlyList<GrowthStrategyPlan>> ListStrategyPlansAsync(string tenantId);

        Task<GrowthMetricSnapshot> GetGrowthMetricSnapshotAsync(string tenantId);
    }
}
