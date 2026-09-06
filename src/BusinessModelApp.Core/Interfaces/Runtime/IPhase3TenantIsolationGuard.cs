using System;

namespace BusinessModelApp.Core.Interfaces.Runtime
{
    public interface IPhase3TenantIsolationGuard
    {
        void AssertTenantAccess(Guid resourceTenantId, Guid callerTenantId, string resourceName);
        void AssertTenantValid(Guid tenantId, string resourceName);
        bool IsAccessible(Guid resourceTenantId, Guid callerTenantId);
    }
}
