using System.Collections.Concurrent;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Allocation;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Allocation;

/// <summary>
/// Thread-safe resource registry and epistemic availability resolver.
/// Invariant I32-U: Epistemic Capacity Sovereignty. Unmeasured/Unknown capacity cannot be treated as usable capacity.
/// Invariant I32-W: Simulation Capacity Cannot Become Real Capacity.
/// </summary>
public class ResourceRegistry : IResourceRegistry, IResourceAvailabilityResolver
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, OrganizationalResource>> _resourcesByTenant = new();

    public Task RegisterResourceAsync(string tenantId, OrganizationalResource resource)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (resource == null) throw new ArgumentNullException(nameof(resource));

        var tenantStore = _resourcesByTenant.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, OrganizationalResource>());
        tenantStore[resource.ResourceId] = resource;
        return Task.CompletedTask;
    }

    public Task<OrganizationalResource?> GetResourceAsync(string tenantId, string resourceId)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(resourceId))
            return Task.FromResult<OrganizationalResource?>(null);

        if (_resourcesByTenant.TryGetValue(tenantId, out var tenantStore) && tenantStore.TryGetValue(resourceId, out var res))
        {
            return Task.FromResult<OrganizationalResource?>(res);
        }
        return Task.FromResult<OrganizationalResource?>(null);
    }

    public Task<IReadOnlyList<OrganizationalResource>> ListResourcesAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || !_resourcesByTenant.TryGetValue(tenantId, out var tenantStore))
        {
            return Task.FromResult<IReadOnlyList<OrganizationalResource>>(Array.Empty<OrganizationalResource>());
        }
        return Task.FromResult<IReadOnlyList<OrganizationalResource>>(tenantStore.Values.ToList());
    }

    public Task<bool> UpdateResourceCapacityAsync(string tenantId, string resourceId, double deltaAvailable, double deltaReserved)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(resourceId)) return Task.FromResult(false);

        if (_resourcesByTenant.TryGetValue(tenantId, out var tenantStore) && tenantStore.TryGetValue(resourceId, out var res))
        {
            lock (res)
            {
                res.AvailableCapacity = Math.Max(0.0, res.AvailableCapacity + deltaAvailable);
                res.ReservedCapacity = Math.Max(0.0, res.ReservedCapacity + deltaReserved);
                res.LastEvaluatedUtc = DateTime.UtcNow;
            }
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<(double UsableCapacity, ResourceCapacityReality Reality)> ResolveUsableCapacityAsync(string tenantId, OrganizationalResourceType type)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || !_resourcesByTenant.TryGetValue(tenantId, out var tenantStore))
        {
            return Task.FromResult((0.0, ResourceCapacityReality.Unknown));
        }

        var match = tenantStore.Values.FirstOrDefault(r => r.ResourceType == type);
        if (match == null)
        {
            return Task.FromResult((0.0, ResourceCapacityReality.NotConnected));
        }

        // Enforce I32-U and I32-W: Only Verified and Inferred realities yield usable allocation capacity
        if (match.EpistemicReality is ResourceCapacityReality.Unknown or ResourceCapacityReality.NotConnected or ResourceCapacityReality.Stale)
        {
            return Task.FromResult((0.0, match.EpistemicReality));
        }

        if (match.EpistemicReality == ResourceCapacityReality.Simulated)
        {
            // Invariant I32-W: Simulated capacity cannot become real usable capacity
            return Task.FromResult((0.0, ResourceCapacityReality.Simulated));
        }

        double usable = Math.Max(0.0, match.AvailableCapacity);
        return Task.FromResult((usable, match.EpistemicReality));
    }
}
