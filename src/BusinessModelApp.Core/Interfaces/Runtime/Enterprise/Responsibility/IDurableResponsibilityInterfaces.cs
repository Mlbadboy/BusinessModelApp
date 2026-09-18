using BusinessModelApp.Core.Domain.Runtime.Enterprise.Brain;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Responsibility;

namespace BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Responsibility;

public interface IReasoningBudgetEnforcer
{
    bool TryConsumeIteration(BoundedReasoningBudget budget, int tokens, TimeSpan elapsed, out string rejectionReason);
}

public interface IWorkFormulationEngine
{
    Task<IReadOnlyList<FormulatedWorkItem>> FormulateWorkAsync(
        string tenantId,
        ExecutiveCognitiveState cognitiveState,
        IReadOnlyList<DurableResponsibilityMapping> mappings);
}

public interface ICycleCheckpointRepository
{
    Task SaveCheckpointAsync(ResponsibilityCycleCheckpoint checkpoint);
    Task<ResponsibilityCycleCheckpoint?> GetLatestCheckpointAsync(string tenantId);
    Task<IReadOnlyList<ResponsibilityCycleCheckpoint>> ListCheckpointsAsync(string tenantId, int limit = 50);

    Task SaveCycleAsync(DurableResponsibilityCycle cycle);
    Task<DurableResponsibilityCycle?> GetCycleByIdAsync(string tenantId, string cycleId);
    Task<DurableResponsibilityCycle?> GetLatestCycleAsync(string tenantId);
    Task<IReadOnlyList<DurableResponsibilityCycle>> ListCyclesAsync(string tenantId, int limit = 50);

    long GetNextSequenceNumber(string tenantId);
}

public interface IResponsibilityCycleCoordinator
{
    Task<DurableResponsibilityCycle> ExecuteCycleStepAsync(string tenantId, CycleTriggerReason triggerReason);
}

/// <summary>
/// Unified facade for Charlie's Continuous Responsibility & Mission Loop (Batch 4.1).
/// Provides execution, monitoring, and human pause/resume controls under Invariant I37.
/// </summary>
public interface IDurableResponsibilityService
{
    Task<DurableResponsibilityCycle> ExecuteSingleCycleAsync(string tenantId, CycleTriggerReason reason = CycleTriggerReason.ScheduledHeartbeat);
    Task<DurableResponsibilityCycle?> GetLatestCycleAsync(string tenantId);
    Task<DurableResponsibilityCycle?> GetCycleByIdAsync(string tenantId, string cycleId);
    Task<IReadOnlyList<DurableResponsibilityCycle>> ListCyclesAsync(string tenantId, int limit = 50);
    Task<IReadOnlyList<ResponsibilityCycleCheckpoint>> ListCheckpointsAsync(string tenantId, int limit = 50);

    Task<bool> PauseCyclesAsync(string tenantId, string humanSupervisorId, string reason);
    Task<bool> ResumeCyclesAsync(string tenantId, string humanSupervisorId);
    bool IsPaused(string tenantId);
}
