using System;
using System.Collections.Generic;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Finance;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Lifecycle;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Recovery;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.ControlTower
{
    public sealed class ControlTowerExecutiveDashboard
    {
        public required string TenantId { get; init; }
        public GrowthHealthGrade OverallHealthGrade { get; init; }
        public decimal TotalRealizedRevenueINR { get; init; }
        public decimal TotalBankCashBalanceINR { get; init; }
        public decimal GrossContributionMarginINR { get; init; }
        public decimal MonthlyNetBurnINR { get; init; }
        public decimal RunwayMonths { get; init; }
        public decimal NetRevenueRetentionPct { get; init; }
        public int TotalActiveCustomers { get; init; }
        public int AtRiskCustomersCount { get; init; }
        public int ActiveMissionsCount { get; init; }
        public int OpenIncidentsCount { get; init; }
        public bool KillSwitchEngaged { get; init; }
        public bool ProductionCertified { get; init; }
        public DateTime ProjectedAtUtc { get; init; } = DateTime.UtcNow;
    }
}
