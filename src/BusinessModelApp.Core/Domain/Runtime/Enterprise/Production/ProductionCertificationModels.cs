using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Production
{
    public enum ProductionGateStatus
    {
        Pending = 0,
        Certified = 1,
        Failed = 2,
        Expired = 3
    }

    public sealed class ProductionCertificationReport
    {
        public string ReportId { get; init; } = Guid.NewGuid().ToString("N");
        public required string TenantId { get; init; }
        public required string BusinessObjectiveId { get; init; }
        public ProductionGateStatus P1EnvironmentStatus { get; init; } = ProductionGateStatus.Pending;
        public ProductionGateStatus P2GovernanceStatus { get; init; } = ProductionGateStatus.Pending;
        public ProductionGateStatus P3BusinessRealityStatus { get; init; } = ProductionGateStatus.Pending;
        public ProductionGateStatus P4RepeatedAutonomyStatus { get; init; } = ProductionGateStatus.Pending;
        public int CompletedCyclesCount { get; init; }
        public decimal TotalRealizedCashINR { get; init; }
        public decimal TotalNetContributionINR { get; init; }
        public DateTime EvaluatedAtUtc { get; init; } = DateTime.UtcNow;
        public string CertifiedByAuthority { get; init; } = string.Empty;

        public bool IsFullyProductionCertified =>
            P1EnvironmentStatus == ProductionGateStatus.Certified &&
            P2GovernanceStatus == ProductionGateStatus.Certified &&
            P3BusinessRealityStatus == ProductionGateStatus.Certified &&
            P4RepeatedAutonomyStatus == ProductionGateStatus.Certified &&
            CompletedCyclesCount >= 3 &&
            TotalNetContributionINR > 0;
    }
}
