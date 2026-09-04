using System;

namespace BusinessModelApp.Core.Security
{
    public class TenantIsolationViolationException : Exception
    {
        public string AttemptedTenantId { get; }
        public string ResourceTenantId { get; }

        public TenantIsolationViolationException(string attemptedTenantId, string resourceTenantId, string resourceName)
            : base($"Zero-Trust Boundary Violation: Tenant '{attemptedTenantId}' attempted unauthorized access to resource '{resourceName}' belonging to tenant '{resourceTenantId}'.")
        {
            AttemptedTenantId = attemptedTenantId;
            ResourceTenantId = resourceTenantId;
        }
    }

    public class TenantContext
    {
        public string TenantId { get; set; } = "TENANT_DEFAULT";
        public string OrganizationName { get; set; } = "Default Enterprise";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public interface ITenantContextAccessor
    {
        string CurrentTenantId { get; }
        void SetCurrentTenant(string tenantId);
    }

    public interface ITenantIsolationGuard
    {
        void AssertAccess(string resourceTenantId, string resourceName);
        bool IsAccessible(string resourceTenantId);
    }
}
