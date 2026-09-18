using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce;

namespace BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Workforce
{
    public interface IContinuousBusinessOperationsCoordinator
    {
        Task<ContinuousBusinessCycle> TriggerCycleAsync(string tenantId, BusinessCycleTrigger trigger, decimal budget = 100m);
        Task<ContinuousBusinessCycle?> GetCycleAsync(string tenantId, string cycleId);
        Task<IReadOnlyList<ContinuousBusinessCycle>> ListCyclesAsync(string tenantId);
        Task<bool> AdvanceCheckpointAsync(string tenantId, string cycleId, string nextState, decimal incrementalCost = 0m);
        Task<bool> CompleteCycleAsync(string tenantId, string cycleId, string outcomeSummary);
        Task<ContinuousBusinessCycle?> RecoverLastCheckpointAsync(string tenantId);
    }
}
