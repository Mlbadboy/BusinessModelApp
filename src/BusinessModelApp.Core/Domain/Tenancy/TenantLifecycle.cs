using System;

namespace BusinessModelApp.Core.Domain.Tenancy
{
    public enum TenantLifecycleState
    {
        Active = 1,
        ReadOnly = 2,
        Suspended = 3,
        Archived = 4
    }

    public enum TenantTier
    {
        Starter = 1,
        Professional = 2,
        Enterprise = 3
    }

    public class TenantPolicy
    {
        public Guid OrganizationId { get; set; }
        public string OrganizationName { get; set; } = string.Empty;
        public TenantLifecycleState State { get; set; } = TenantLifecycleState.Active;
        public TenantTier Tier { get; set; } = TenantTier.Professional;
        public int MaxOpportunities { get; set; } = 250;
        public int MaxLeads { get; set; } = 1000;
        public int MaxDailyAICalls { get; set; } = 500;
        public decimal MonthlyBudgetCapINR { get; set; } = 25000m;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SuspendedAt { get; set; }
        public string? SuspensionReason { get; set; }
    }

    public class TenantAuthorizationResult
    {
        public bool IsAuthorized { get; set; }
        public bool CanLogin { get; set; }
        public bool CanRead { get; set; }
        public bool CanWrite { get; set; }
        public bool CanUseAI { get; set; }
        public string? RejectionReason { get; set; }
    }
}
