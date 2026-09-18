using System.Collections.Concurrent;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Responsibility;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Responsibility;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Responsibility;

public sealed class DurableResponsibilityService : IDurableResponsibilityService
{
    private readonly IResponsibilityCycleCoordinator _coordinator;
    private readonly ICycleCheckpointRepository _repository;
    private readonly ConcurrentDictionary<string, bool> _pauseStatesByTenant = new();

    public DurableResponsibilityService(
        IResponsibilityCycleCoordinator coordinator,
        ICycleCheckpointRepository repository)
    {
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<DurableResponsibilityCycle> ExecuteSingleCycleAsync(
        string tenantId,
        CycleTriggerReason reason = CycleTriggerReason.ScheduledHeartbeat)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));

        if (IsPaused(tenantId))
        {
            var pausedCycle = new DurableResponsibilityCycle
            {
                CycleId = Guid.NewGuid().ToString("N"),
                TenantId = tenantId,
                TriggerReason = reason,
                State = ResponsibilityCycleState.PausedByHuman,
                StartedUtc = DateTime.UtcNow,
                CompletedUtc = DateTime.UtcNow
            };
            pausedCycle.ComputeCycleHash();
            await _repository.SaveCycleAsync(pausedCycle);
            return pausedCycle;
        }

        return await _coordinator.ExecuteCycleStepAsync(tenantId, reason);
    }

    public Task<DurableResponsibilityCycle?> GetLatestCycleAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        return _repository.GetLatestCycleAsync(tenantId);
    }

    public Task<DurableResponsibilityCycle?> GetCycleByIdAsync(string tenantId, string cycleId)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (string.IsNullOrWhiteSpace(cycleId)) throw new ArgumentNullException(nameof(cycleId));
        return _repository.GetCycleByIdAsync(tenantId, cycleId);
    }

    public Task<IReadOnlyList<DurableResponsibilityCycle>> ListCyclesAsync(string tenantId, int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        return _repository.ListCyclesAsync(tenantId, limit);
    }

    public Task<IReadOnlyList<ResponsibilityCycleCheckpoint>> ListCheckpointsAsync(string tenantId, int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        return _repository.ListCheckpointsAsync(tenantId, limit);
    }

    public Task<bool> PauseCyclesAsync(string tenantId, string humanSupervisorId, string reason)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        _pauseStatesByTenant[tenantId] = true;
        return Task.FromResult(true);
    }

    public Task<bool> ResumeCyclesAsync(string tenantId, string humanSupervisorId)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        _pauseStatesByTenant[tenantId] = false;
        return Task.FromResult(true);
    }

    public bool IsPaused(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) return false;
        return _pauseStatesByTenant.TryGetValue(tenantId, out var paused) && paused;
    }
}
