using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth
{
    public enum GrowthObjectiveStatus
    {
        PLANNED = 0,
        ACTIVE = 1,
        ON_TRACK = 2,
        OFF_TRACK = 3,
        ACHIEVED = 4,
        TERMINATED = 5
    }

    public class GrowthObjective
    {
        public string ObjectiveId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string BusinessObjectiveId { get; set; } = string.Empty; // Hierarchical parent link
        public string Title { get; set; } = string.Empty;
        public string TimeHorizon { get; set; } = "ANNUAL_2026";
        public decimal TargetRevenueINR { get; set; } = 5000000m;
        public decimal TargetGrossMarginPercent { get; set; } = 50.0m;
        public decimal TargetLtvCacRatio { get; set; } = 3.5m;
        public int MaxPaybackPeriodMonths { get; set; } = 9;
        public decimal TargetNetRetentionRatePercent { get; set; } = 110.0m;

        public decimal CurrentRealizedRevenueINR { get; set; } = 0m;
        public decimal CurrentGrossMarginPercent { get; set; } = 0m;
        public decimal CurrentLtvCacRatio { get; set; } = 0m;
        public decimal CurrentNetRetentionRatePercent { get; set; } = 100.0m;

        public GrowthObjectiveStatus Status { get; set; } = GrowthObjectiveStatus.PLANNED;
        public string? HumanSignoffId { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public bool IsEconomicallySustainable =>
            TargetGrossMarginPercent >= GrowthConstitutionalInvariants.MinGrossMarginPercent &&
            TargetLtvCacRatio >= GrowthConstitutionalInvariants.MinLtvToCacRatio &&
            MaxPaybackPeriodMonths <= GrowthConstitutionalInvariants.MaxPaybackPeriodMonths;
    }

    public class GrowthStrategyPlan
    {
        public string StrategyId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string GrowthObjectiveId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string TargetIcpSegment { get; set; } = string.Empty;
        public string AcquisitionChannel { get; set; } = "CONTENT_AND_DIRECT_OUTREACH";
        public decimal AllocatedBudgetINR { get; set; } = 0m;
        public decimal SpentBudgetINR { get; set; } = 0m;
        public string HumanSignoffId { get; set; } = string.Empty;
        public bool IsAuthorized => !string.IsNullOrWhiteSpace(HumanSignoffId);
        public DateTime FormulatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public class GrowthMetricSnapshot
    {
        public string TenantId { get; set; } = string.Empty;
        public decimal TotalRealizedRevenueINR { get; set; } = 0m;
        public decimal BlendedGrossMarginPercent { get; set; } = 0m;
        public decimal BlendedLtvCacRatio { get; set; } = 0m;
        public decimal AveragePaybackMonths { get; set; } = 0m;
        public decimal NetRevenueRetentionRatePercent { get; set; } = 100.0m;
        public int ActiveCustomersCount { get; set; } = 0;
        public int ChurnedCustomersCount { get; set; } = 0;
        public decimal MonthlyBurnRateINR { get; set; } = 0m;
        public decimal TreasuryCashRunwayMonths { get; set; } = 0m;
        public DateTime ComputedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
