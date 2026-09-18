using System.Collections.Concurrent;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Simulation;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational.Simulation;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Simulation;

public sealed class SimulationRunManager : ISimulationRunManager
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, SimulationRunMetadata>> _runsByTenant = new();
    private readonly ConcurrentDictionary<string, List<SimulationOutcome>> _outcomesByRun = new();
    private readonly ISimulationTenantIsolation _tenantIsolation;

    public SimulationRunManager(ISimulationTenantIsolation tenantIsolation)
    {
        _tenantIsolation = tenantIsolation ?? throw new ArgumentNullException(nameof(tenantIsolation));
    }

    public Task SaveRunMetadataAsync(SimulationRunMetadata metadata)
    {
        if (metadata == null) throw new ArgumentNullException(nameof(metadata));
        if (string.IsNullOrWhiteSpace(metadata.TenantId)) throw new ArgumentNullException(nameof(metadata.TenantId));

        var tenantMap = _runsByTenant.GetOrAdd(metadata.TenantId, _ => new ConcurrentDictionary<string, SimulationRunMetadata>());
        tenantMap[metadata.SimulationRunId] = metadata;

        return Task.CompletedTask;
    }

    public Task<SimulationRunMetadata?> GetRunMetadataAsync(string tenantId, string runId)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (string.IsNullOrWhiteSpace(runId)) throw new ArgumentNullException(nameof(runId));

        if (_runsByTenant.TryGetValue(tenantId, out var tenantMap) &&
            tenantMap.TryGetValue(runId, out var metadata))
        {
            _tenantIsolation.AssertTenantAccess(tenantId, metadata.TenantId);
            return Task.FromResult<SimulationRunMetadata?>(metadata);
        }

        // Cross-tenant penetration defense (I34-R): if run exists under another tenant, fail immediately
        foreach (var (otherTenant, otherMap) in _runsByTenant)
        {
            if (otherMap.ContainsKey(runId))
            {
                _tenantIsolation.AssertTenantAccess(tenantId, otherTenant);
            }
        }

        return Task.FromResult<SimulationRunMetadata?>(null);
    }

    public Task<IReadOnlyList<SimulationRunMetadata>> ListRunsAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));

        if (_runsByTenant.TryGetValue(tenantId, out var tenantMap))
        {
            return Task.FromResult<IReadOnlyList<SimulationRunMetadata>>(
                tenantMap.Values.OrderByDescending(r => r.StartedAtUtc).ToList());
        }

        return Task.FromResult<IReadOnlyList<SimulationRunMetadata>>(Array.Empty<SimulationRunMetadata>());
    }

    public Task SaveOutcomesAsync(string tenantId, string runId, IEnumerable<SimulationOutcome> outcomes)
    {
        if (string.IsNullOrWhiteSpace(runId)) throw new ArgumentNullException(nameof(runId));
        _outcomesByRun[runId] = outcomes.ToList();
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SimulationOutcome>> GetOutcomesAsync(string tenantId, string runId)
    {
        if (string.IsNullOrWhiteSpace(runId)) throw new ArgumentNullException(nameof(runId));

        if (_outcomesByRun.TryGetValue(runId, out var outcomes))
        {
            return Task.FromResult<IReadOnlyList<SimulationOutcome>>(outcomes);
        }

        return Task.FromResult<IReadOnlyList<SimulationOutcome>>(Array.Empty<SimulationOutcome>());
    }
}
