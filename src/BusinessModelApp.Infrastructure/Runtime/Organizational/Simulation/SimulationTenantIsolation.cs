using BusinessModelApp.Core.Domain.Runtime.Organizational.Simulation;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational.Simulation;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Simulation;

public sealed class SimulationTenantIsolation : ISimulationTenantIsolation
{
    public void AssertTenantAccess(string requestingTenantId, string resourceTenantId)
    {
        if (string.IsNullOrWhiteSpace(requestingTenantId))
            throw new ArgumentNullException(nameof(requestingTenantId), "Requesting tenant ID cannot be empty.");

        if (string.IsNullOrWhiteSpace(resourceTenantId))
            throw new ArgumentNullException(nameof(resourceTenantId), "Resource tenant ID cannot be empty.");

        if (!string.Equals(requestingTenantId, resourceTenantId, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException(
                $"Cross-tenant access violation: Tenant '{requestingTenantId}' cannot access resources belonging to '{resourceTenantId}'. (I34-R)");
        }
    }
}
