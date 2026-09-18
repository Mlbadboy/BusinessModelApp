using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Production
{
    public enum ProductionActivationStatus
    {
        Draft = 0,
        PendingApproval = 1,
        Active = 2,
        Suspended = 3,
        RolledBack = 4,
        Terminated = 5
    }

    public enum ConnectorHealthStatus
    {
        Registered = 0,
        HealthChecked = 1,
        Authenticated = 2,
        CapabilityVerified = 3,
        PolicyBound = 4,
        ProductionEnabled = 5,
        Degraded = 6,
        Disabled = 7
    }

    public sealed class ProductionConnectorRegistration
    {
        public string RegistrationId { get; init; } = Guid.NewGuid().ToString("N");
        public required string TenantId { get; init; }
        public required string ConnectorName { get; init; }
        public required string ExternalProvider { get; init; }
        public required string CapabilityScope { get; init; }
        public int RateLimitPerMinute { get; init; } = 60;
        public decimal MonthlyBudgetCeilingINR { get; init; } = 50_000m;
        public decimal CurrentMonthSpentINR { get; private set; }
        public ConnectorHealthStatus HealthStatus { get; private set; } = ConnectorHealthStatus.Registered;
        public bool RequiresHumanApprovalForConsequentialActions { get; init; } = true;
        public DateTime RegisteredAtUtc { get; init; } = DateTime.UtcNow;
        public DateTime? LastHealthCheckUtc { get; private set; }

        public void UpdateHealth(ConnectorHealthStatus status)
        {
            HealthStatus = status;
            LastHealthCheckUtc = DateTime.UtcNow;
        }

        public void RecordUsage(decimal spendINR)
        {
            CurrentMonthSpentINR += spendINR;
            if (CurrentMonthSpentINR > MonthlyBudgetCeilingINR)
            {
                HealthStatus = ConnectorHealthStatus.Degraded;
            }
        }
    }

    public sealed class ProductionConfigVersion
    {
        public string VersionId { get; init; } = Guid.NewGuid().ToString("N");
        public required string TenantId { get; init; }
        public int VersionNumber { get; init; } = 1;
        public required string ConfigPayloadHashSha256 { get; init; }
        public required string ApprovedBySignoffId { get; init; }
        public DateTime ActivatedAtUtc { get; init; } = DateTime.UtcNow;
        public bool IsActive { get; private set; } = true;

        public void Deactivate() => IsActive = false;
    }

    public sealed class ProductionTenantActivation
    {
        public string ActivationId { get; init; } = Guid.NewGuid().ToString("N");
        public required string TenantId { get; init; }
        public required string BusinessObjectiveId { get; init; }
        public required string LegalBusinessName { get; init; }
        public ProductionActivationStatus Status { get; private set; } = ProductionActivationStatus.PendingApproval;
        public string? HumanSignoffId { get; private set; }
        public int ActiveConfigVersionNumber { get; private set; } = 1;
        public List<string> EnabledConnectorIds { get; } = new();
        public DateTime ActivatedAtUtc { get; private set; } = DateTime.UtcNow;
        public DateTime? SuspendedAtUtc { get; private set; }

        public void ApproveAndActivate(string humanSignoffId)
        {
            if (string.IsNullOrWhiteSpace(humanSignoffId))
                throw new InvalidOperationException("Production tenant activation requires PRG-1 executive human authorization (Law I43-L).");

            Status = ProductionActivationStatus.Active;
            HumanSignoffId = humanSignoffId.Trim();
            ActivatedAtUtc = DateTime.UtcNow;
        }

        public void Suspend(string reason)
        {
            Status = ProductionActivationStatus.Suspended;
            SuspendedAtUtc = DateTime.UtcNow;
        }

        public void Rollback(int targetVersionNumber)
        {
            ActiveConfigVersionNumber = targetVersionNumber;
            Status = ProductionActivationStatus.RolledBack;
        }
    }
}
