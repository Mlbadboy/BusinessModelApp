using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Constraints
{
    public enum StrategicRegimeType
    {
        CashPreservation_Distressed,
        BalancedProfitability_Conservative,
        AggressiveGrowth_Expansion,
        MarketDefense_PriceWar
    }

    public enum StrategicObjectiveType
    {
        PreserveLiquidity,
        PreserveSolvency,
        ProtectMandatoryOperations,
        PreservePositiveUnitEconomics,
        MaximizeContributionMargin,
        ProtectStrategicRevenue,
        GrowRevenue,
        ExpandMarketShare,
        AcquireCustomers,
        OptimizeCost,
        MinimizeLatency,
        MaximizeCustomerRetention
    }

    public record StrategicObjectivePriority
    {
        public StrategicObjectiveType Objective { get; init; }
        public int LexicographicRank { get; init; } // 1 is highest priority
        public double Weight { get; init; } // for multi-objective trade-off within same rank
        public string Description { get; init; } = string.Empty;
    }

    public record StrategicRegimePolicy
    {
        public Guid WorkspaceId { get; init; }
        public StrategicRegimeType Regime { get; init; } = StrategicRegimeType.BalancedProfitability_Conservative;
        public int PolicyVersion { get; set; } = 1;
        public string Authority { get; init; } = "BoardOfDirectors";
        public IReadOnlyList<StrategicObjectivePriority> ObjectivePriorities { get; init; } = Array.Empty<StrategicObjectivePriority>();
        public double MinCashReserveThresholdINR { get; init; } = 1_000_000.0; // ₹10L default
        public double MaxDailyBurnRateINR { get; init; } = 100_000.0; // ₹1L default
        public double MinRunwayDays { get; init; } = 90.0; // 90 days default
        public double MinGrossMarginPercent { get; init; } = 25.0; // 25% default
        public double MaxCustomerAcquisitionCostINR { get; init; } = 5_000.0;
        public int MaxPendingHumanApprovals { get; init; } = 20;
        public int MaxConcurrentActiveMissions { get; init; } = 10;
        public DateTimeOffset EffectiveFrom { get; init; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? ExpiresAt { get; init; }
        public string PolicyHash { get; set; } = string.Empty;

        public static StrategicRegimePolicy CreateDefault(Guid workspaceId, StrategicRegimeType regime)
        {
            return regime switch
            {
                StrategicRegimeType.CashPreservation_Distressed => new StrategicRegimePolicy
                {
                    WorkspaceId = workspaceId,
                    Regime = regime,
                    MinCashReserveThresholdINR = 1_500_000.0,
                    MaxDailyBurnRateINR = 30_000.0,
                    MinRunwayDays = 120.0,
                    MinGrossMarginPercent = 35.0,
                    MaxCustomerAcquisitionCostINR = 2_000.0,
                    MaxPendingHumanApprovals = 10,
                    MaxConcurrentActiveMissions = 5,
                    ObjectivePriorities = new List<StrategicObjectivePriority>
                    {
                        new() { Objective = StrategicObjectiveType.PreserveLiquidity, LexicographicRank = 1, Weight = 0.40 },
                        new() { Objective = StrategicObjectiveType.PreserveSolvency, LexicographicRank = 2, Weight = 0.25 },
                        new() { Objective = StrategicObjectiveType.ProtectMandatoryOperations, LexicographicRank = 3, Weight = 0.15 },
                        new() { Objective = StrategicObjectiveType.PreservePositiveUnitEconomics, LexicographicRank = 4, Weight = 0.10 },
                        new() { Objective = StrategicObjectiveType.ProtectStrategicRevenue, LexicographicRank = 5, Weight = 0.05 },
                        new() { Objective = StrategicObjectiveType.GrowRevenue, LexicographicRank = 6, Weight = 0.05 }
                    }
                },
                StrategicRegimeType.AggressiveGrowth_Expansion => new StrategicRegimePolicy
                {
                    WorkspaceId = workspaceId,
                    Regime = regime,
                    MinCashReserveThresholdINR = 500_000.0,
                    MaxDailyBurnRateINR = 250_000.0,
                    MinRunwayDays = 60.0,
                    MinGrossMarginPercent = 15.0,
                    MaxCustomerAcquisitionCostINR = 10_000.0,
                    MaxPendingHumanApprovals = 30,
                    MaxConcurrentActiveMissions = 20,
                    ObjectivePriorities = new List<StrategicObjectivePriority>
                    {
                        new() { Objective = StrategicObjectiveType.PreserveSolvency, LexicographicRank = 1, Weight = 0.20 },
                        new() { Objective = StrategicObjectiveType.ProtectMandatoryOperations, LexicographicRank = 2, Weight = 0.15 },
                        new() { Objective = StrategicObjectiveType.GrowRevenue, LexicographicRank = 3, Weight = 0.25 },
                        new() { Objective = StrategicObjectiveType.ExpandMarketShare, LexicographicRank = 4, Weight = 0.20 },
                        new() { Objective = StrategicObjectiveType.AcquireCustomers, LexicographicRank = 5, Weight = 0.10 },
                        new() { Objective = StrategicObjectiveType.MaximizeContributionMargin, LexicographicRank = 6, Weight = 0.10 }
                    }
                },
                StrategicRegimeType.MarketDefense_PriceWar => new StrategicRegimePolicy
                {
                    WorkspaceId = workspaceId,
                    Regime = regime,
                    MinCashReserveThresholdINR = 800_000.0,
                    MaxDailyBurnRateINR = 150_000.0,
                    MinRunwayDays = 75.0,
                    MinGrossMarginPercent = 10.0, // compressed margin permitted to defend
                    MaxCustomerAcquisitionCostINR = 7_500.0,
                    MaxPendingHumanApprovals = 15,
                    MaxConcurrentActiveMissions = 12,
                    ObjectivePriorities = new List<StrategicObjectivePriority>
                    {
                        new() { Objective = StrategicObjectiveType.PreserveSolvency, LexicographicRank = 1, Weight = 0.25 },
                        new() { Objective = StrategicObjectiveType.ProtectStrategicRevenue, LexicographicRank = 2, Weight = 0.25 },
                        new() { Objective = StrategicObjectiveType.MaximizeCustomerRetention, LexicographicRank = 3, Weight = 0.20 },
                        new() { Objective = StrategicObjectiveType.ExpandMarketShare, LexicographicRank = 4, Weight = 0.15 },
                        new() { Objective = StrategicObjectiveType.PreservePositiveUnitEconomics, LexicographicRank = 5, Weight = 0.10 },
                        new() { Objective = StrategicObjectiveType.OptimizeCost, LexicographicRank = 6, Weight = 0.05 }
                    }
                },
                _ => new StrategicRegimePolicy
                {
                    WorkspaceId = workspaceId,
                    Regime = StrategicRegimeType.BalancedProfitability_Conservative,
                    MinCashReserveThresholdINR = 1_000_000.0,
                    MaxDailyBurnRateINR = 100_000.0,
                    MinRunwayDays = 90.0,
                    MinGrossMarginPercent = 25.0,
                    MaxCustomerAcquisitionCostINR = 5_000.0,
                    MaxPendingHumanApprovals = 20,
                    MaxConcurrentActiveMissions = 10,
                    ObjectivePriorities = new List<StrategicObjectivePriority>
                    {
                        new() { Objective = StrategicObjectiveType.PreserveSolvency, LexicographicRank = 1, Weight = 0.30 },
                        new() { Objective = StrategicObjectiveType.MaximizeContributionMargin, LexicographicRank = 2, Weight = 0.25 },
                        new() { Objective = StrategicObjectiveType.PreservePositiveUnitEconomics, LexicographicRank = 3, Weight = 0.20 },
                        new() { Objective = StrategicObjectiveType.GrowRevenue, LexicographicRank = 4, Weight = 0.15 },
                        new() { Objective = StrategicObjectiveType.OptimizeCost, LexicographicRank = 5, Weight = 0.10 }
                    }
                }
            };
        }
    }
}
