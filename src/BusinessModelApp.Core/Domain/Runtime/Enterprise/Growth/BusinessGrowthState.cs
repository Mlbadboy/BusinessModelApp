using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth
{
    /// <summary>
    /// Epistemic Evidence Levels enforcing the hardened constitutional invariant:
    /// Fixture (0) < Simulation (1) < SelfReported (2) < ConnectorObserved (3) < CounterpartyAttested (4) < BankVerifiedCash (5) < RealizedRevenue (6).
    /// </summary>
    public enum EpistemicEvidenceLevel
    {
        InternalFixture = 0,
        Simulation = 1,
        SelfReported = 2,
        ConnectorObserved = 3,
        CounterpartyAttested = 4,
        BankVerifiedCash = 5,
        RealizedRevenue = 6
    }

    /// <summary>
    /// Overall growth health rating grade.
    /// </summary>
    public enum GrowthHealthGrade
    {
        Critical = 0,
        Substandard = 1,
        Adequate = 2,
        Healthy = 3,
        Exemplary = 4
    }

    #region 16 Sub-States

    public sealed class ObjectiveState
    {
        public string BusinessObjectiveId { get; init; } = string.Empty;
        public string GrowthObjectiveId { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string TimeHorizon { get; init; } = "ANNUAL_2026";
        public decimal TargetRevenue { get; init; }
        public decimal TargetGrossMarginPercent { get; init; }
        public decimal TargetLtvCacRatio { get; init; }
        public int MaxPaybackPeriodMonths { get; init; }
        public GrowthObjectiveStatus Status { get; init; } = GrowthObjectiveStatus.PLANNED;
    }

    public sealed class DemandState
    {
        public int RawMarketSignalsCount { get; init; }
        public int QualifiedDemandCount { get; init; }
        public decimal InboundVelocityPerWeek { get; init; }
        public decimal TotalIdentifiedDemandValue { get; init; }
    }

    public sealed class AcquisitionState
    {
        public int TotalTargetProspectsCount { get; init; }
        public int ActiveEngagementsCount { get; init; }
        public int BuyingCommitteesEngagedCount { get; init; }
        public decimal AverageIcpScore { get; init; }
        public decimal BlendedCac { get; init; }
    }

    public sealed class PipelineState
    {
        public int DiscoveryStageCount { get; init; }
        public int MeetingStageCount { get; init; }
        public int ProposalStageCount { get; init; }
        public int NegotiationStageCount { get; init; }
        public int ContractStageCount { get; init; }
        public decimal TotalUnweightedPipelineValue { get; init; }
        public decimal TotalWeightedPipelineValue { get; init; }
        public decimal HistoricalWinRatePercent { get; init; }
    }

    public sealed class CustomerState
    {
        public int ActiveCustomersCount { get; init; }
        public int OnboardingCustomersCount { get; init; }
        public int FullyAdoptedCustomersCount { get; init; }
        public decimal AverageCustomerHealthScore { get; init; }
    }

    public sealed class RetentionState
    {
        public decimal GrossRevenueRetentionRatePercent { get; init; } = 100.0m;
        public decimal NetRevenueRetentionRatePercent { get; init; } = 100.0m;
        public int ChurnedAccountsCount { get; init; }
        public decimal AnnualChurnRatePercent { get; init; }
        public int AccountsAtChurnRiskCount { get; init; }
    }

    public sealed class ExpansionState
    {
        public int ExpansionOpportunitiesCount { get; init; }
        public decimal ExpansionPipelineValue { get; init; }
        public decimal RealizedExpansionRevenue { get; init; }
        public decimal ExpansionWinRatePercent { get; init; }
    }

    public sealed class RevenueState
    {
        public decimal ContractedArr { get; init; }
        public decimal RecognizedRevenue { get; init; }
        public decimal TotalBilledAmount { get; init; }
        public decimal RealizedCashRevenue { get; init; }
        public EpistemicEvidenceLevel RevenueEvidenceLevel { get; init; } = EpistemicEvidenceLevel.InternalFixture;
    }

    public sealed class CashState
    {
        public decimal BankLiquidCashReserve { get; init; }
        public decimal MonthlyBurnRate { get; init; }
        public decimal MonthlyCollectedCash { get; init; }
        public decimal NetMonthlyCashFlow { get; init; }
        public decimal CashRunwayMonths { get; init; }
        public EpistemicEvidenceLevel CashEvidenceLevel { get; init; } = EpistemicEvidenceLevel.InternalFixture;
    }

    public sealed class CostState
    {
        public decimal DirectDeliveryCosts { get; init; }
        public decimal AgentComputeAndTokenCosts { get; init; }
        public decimal SoftwareLicenseCosts { get; init; }
        public decimal MarketingAndAcquisitionCosts { get; init; }
        public decimal TotalFullyLoadedCosts => DirectDeliveryCosts + AgentComputeAndTokenCosts + SoftwareLicenseCosts + MarketingAndAcquisitionCosts;
    }

    public sealed class UnitEconomicsState
    {
        public decimal GrossMarginPercent { get; init; }
        public decimal NetContributionMarginPercent { get; init; }
        public decimal BlendedLtvCacRatio { get; init; }
        public int AveragePaybackPeriodMonths { get; init; }
        public bool IsEconomicallySustainable { get; init; }
    }

    public sealed class CapacityState
    {
        public int MaxConcurrentOnboardings { get; init; } = 10;
        public int ActiveOnboardingsCount { get; init; }
        public decimal WorkforceAgentUtilizationPercent { get; init; }
        public bool HasDeliveryBottleneck => ActiveOnboardingsCount >= MaxConcurrentOnboardings;
    }

    public sealed class ExperimentState
    {
        public int ActiveExperimentsCount { get; init; }
        public int ConcludedExperimentsCount { get; init; }
        public int StatisticallySignificantWinnersCount { get; init; }
        public decimal TotalAllocatedExperimentBudget { get; init; }
    }

    public sealed class RiskState
    {
        public decimal RevenueConcentrationTopCustomerPercent { get; init; }
        public bool HasUnprofitableAccounts { get; init; }
        public bool HasRunwayAlert => CashRunwayMonthsThresholdBreached;
        public bool CashRunwayMonthsThresholdBreached { get; init; }
        public int ComplianceViolationAlertsCount { get; init; }
    }

    public sealed class EvidenceState
    {
        public int TotalAttestationsCount { get; init; }
        public int BankReconciledAttestationsCount { get; init; }
        public decimal BankReconciledAmountTotal { get; init; }
        public EpistemicEvidenceLevel HighestVerifiedLevel { get; init; } = EpistemicEvidenceLevel.InternalFixture;
        public bool HasUnverifiedClaims { get; init; }
    }

    public sealed class GrowthHealthState
    {
        public decimal CompositeHealthScore { get; init; } // 0.0 to 100.0
        public GrowthHealthGrade HealthGrade { get; init; } = GrowthHealthGrade.Adequate;
        public string PrimaryBottleneck { get; init; } = "None";
        public bool IsG1EngineeringCertified { get; init; }
        public bool IsG2ProductionCertified { get; init; }
        public bool IsG3BusinessRealityCertified { get; init; }
    }

    #endregion

    /// <summary>
    /// Authoritative single-pane snapshot of enterprise commercial growth consumed by Charlie's Brain,
    /// OARA (Resource Arbitrator), Portfolio Planner, and the Autonomous Growth Loop.
    /// </summary>
    public sealed class BusinessGrowthState
    {
        public string StateSnapshotId { get; init; } = Guid.NewGuid().ToString("N");
        public required string TenantId { get; init; }
        public DateTime SnapshotTimestampUtc { get; init; } = DateTime.UtcNow;

        public required ObjectiveState ObjectiveState { get; init; }
        public required DemandState DemandState { get; init; }
        public required AcquisitionState AcquisitionState { get; init; }
        public required PipelineState PipelineState { get; init; }
        public required CustomerState CustomerState { get; init; }
        public required RetentionState RetentionState { get; init; }
        public required ExpansionState ExpansionState { get; init; }
        public required RevenueState RevenueState { get; init; }
        public required CashState CashState { get; init; }
        public required CostState CostState { get; init; }
        public required UnitEconomicsState UnitEconomicsState { get; init; }
        public required CapacityState CapacityState { get; init; }
        public required ExperimentState ExperimentState { get; init; }
        public required RiskState RiskState { get; init; }
        public required EvidenceState EvidenceState { get; init; }
        public required GrowthHealthState GrowthHealthState { get; init; }

        public bool IsAutonomouslyGrowing =>
            GrowthHealthState.CompositeHealthScore >= 70.0m &&
            UnitEconomicsState.IsEconomicallySustainable &&
            EvidenceState.HighestVerifiedLevel >= EpistemicEvidenceLevel.BankVerifiedCash &&
            GrowthHealthState.IsG1EngineeringCertified &&
            GrowthHealthState.IsG2ProductionCertified &&
            GrowthHealthState.IsG3BusinessRealityCertified;
    }
}
