using System;
using System.Threading;
using BusinessModelApp.Core.Security;

namespace BusinessModelApp.Infrastructure.Security
{
    public class TenantContextAccessor : ITenantContextAccessor
    {
        private static readonly AsyncLocal<string> _currentTenantId = new() { Value = "TENANT_DEFAULT" };

        public string CurrentTenantId => _currentTenantId.Value ?? "TENANT_DEFAULT";

        public void SetCurrentTenant(string tenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            _currentTenantId.Value = tenantId;
        }
    }

    public class TenantIsolationGuard : ITenantIsolationGuard
    {
        private readonly ITenantContextAccessor _tenantAccessor;

        public TenantIsolationGuard(ITenantContextAccessor tenantAccessor)
        {
            _tenantAccessor = tenantAccessor ?? throw new ArgumentNullException(nameof(tenantAccessor));
        }

        public void AssertAccess(string resourceTenantId, string resourceName)
        {
            if (string.IsNullOrWhiteSpace(resourceTenantId))
            {
                throw new ArgumentNullException(nameof(resourceTenantId), "Resource must have an authoritative TenantId.");
            }

            var current = _tenantAccessor.CurrentTenantId;

            if (!string.Equals(current, resourceTenantId, StringComparison.OrdinalIgnoreCase))
            {
                throw new TenantIsolationViolationException(current, resourceTenantId, resourceName);
            }
        }

        public bool IsAccessible(string resourceTenantId)
        {
            if (string.IsNullOrWhiteSpace(resourceTenantId)) return false;
            return string.Equals(_tenantAccessor.CurrentTenantId, resourceTenantId, StringComparison.OrdinalIgnoreCase);
        }
    }
}
