using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Strategy;

namespace BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Strategy
{
    public interface IAutonomousBusinessStrategyStore
    {
        Task SaveObjectiveAsync(StrategicObjective objective, CancellationToken cancellationToken = default);
        Task<StrategicObjective?> GetObjectiveAsync(string objectiveId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<StrategicObjective>> ListObjectivesForTenantAsync(string tenantId, CancellationToken cancellationToken = default);

        Task SaveInitiativeAsync(StrategicInitiative initiative, CancellationToken cancellationToken = default);
        Task<StrategicInitiative?> GetInitiativeAsync(string initiativeId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<StrategicInitiative>> ListInitiativesForObjectiveAsync(string objectiveId, CancellationToken cancellationToken = default);
    }

    public interface IAutonomousBusinessStrategyService
    {
        Task<StrategicObjective> CreateStrategicObjectiveAsync(
            string tenantId,
            string title,
            StrategicTimeHorizon horizon,
            decimal targetRevenueINR,
            decimal allocatedCapitalINR,
            decimal maxRiskToleranceScore = 0.30m,
            CancellationToken cancellationToken = default);

        Task<StrategicInitiative> ProposeInitiativeAsync(
            string tenantId,
            string objectiveId,
            string title,
            string description,
            decimal strategicValueScore,
            decimal requiredCapitalINR,
            CancellationToken cancellationToken = default);

        Task<StrategicInitiative> ApproveInitiativeWithPRG1Async(
            string initiativeId,
            string prg1SignoffSha256,
            CancellationToken cancellationToken = default);

        Task<StrategicInitiative> LaunchInitiativeExecutionAsync(
            string initiativeId,
            CancellationToken cancellationToken = default);
    }
}
