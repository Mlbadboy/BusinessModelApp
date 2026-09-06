using System;
using BusinessModelApp.Core.Interfaces.Runtime;

namespace BusinessModelApp.Infrastructure.Runtime
{
    public class Phase3TenantIsolationViolationException : Exception
    {
        public Guid CallerTenantId { get; }
        public Guid ResourceTenantId { get; }

        public Phase3TenantIsolationViolationException(Guid callerTenantId, Guid resourceTenantId, string resourceName)
            : base($"Zero-Trust Tenant Isolation Violation: Caller workspace '{callerTenantId}' attempted unauthorized access to resource '{resourceName}' belonging to workspace '{resourceTenantId}'.")
        {
            CallerTenantId = callerTenantId;
            ResourceTenantId = resourceTenantId;
        }
    }

    public class Phase3TenantIsolationGuard : IPhase3TenantIsolationGuard
    {
        public void AssertTenantAccess(Guid resourceTenantId, Guid callerTenantId, string resourceName)
        {
            AssertTenantValid(resourceTenantId, $"{resourceName} (Resource)");
            AssertTenantValid(callerTenantId, $"{resourceName} (Caller)");

            if (resourceTenantId != callerTenantId)
            {
                throw new Phase3TenantIsolationViolationException(callerTenantId, resourceTenantId, resourceName);
            }
        }

        public void AssertTenantValid(Guid tenantId, string resourceName)
        {
            if (tenantId == Guid.Empty)
            {
                throw new ArgumentException($"Invalid Tenant ID: Workspace cannot be Guid.Empty for resource '{resourceName}'.", nameof(tenantId));
            }
        }

        public bool IsAccessible(Guid resourceTenantId, Guid callerTenantId)
        {
            if (resourceTenantId == Guid.Empty || callerTenantId == Guid.Empty)
                return false;

            return resourceTenantId == callerTenantId;
        }
    }
}
