using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Constraints
{
    public record DigitalTwinBusinessState
    {
        public Guid WorkspaceId { get; init; }
        public double CurrentCashBalanceINR { get; init; } = 2_000_000.0;
        public double DailyBurnRateINR { get; init; } = 40_000.0;
        public double RunwayDays => DailyBurnRateINR > 0 ? CurrentCashBalanceINR / DailyBurnRateINR : 999.0;
        public double WorkingCapitalINR { get; init; } = 1_500_000.0;
        public double InventoryValueINR { get; init; } = 800_000.0;
        public double GrossMarginPercent { get; init; } = 32.0;
        public double CustomerAcquisitionCostINR { get; init; } = 3_500.0;
        public int PendingHumanApprovals { get; init; } = 5;
        public int ActiveConcurrentMissions { get; init; } = 3;
        public double AvailableCreditLimitINR { get; init; } = 500_000.0;
        public DateTimeOffset TelemetryCapturedAt { get; init; } = DateTimeOffset.UtcNow;
        public bool IsUnknown { get; init; } = false;
        public bool IsConflicted { get; init; } = false;
        public string SourceProvenance { get; init; } = "VerifiedDigitalTwinLedger";
    }

    public record PreFlightEffectProposal
    {
        public double CashOutflowINR { get; init; }
        public double ExpectedRevenueINR { get; init; }
        public double ProjectedBurnRateChangeINR { get; init; }
        public double ProjectedGrossMarginPercent { get; init; }
        public double ProjectedCacINR { get; init; }
        public int AdditionalApprovalLoad { get; init; }
        public string ActionCategory { get; init; } = "Marketing";
    }

    public record PreFlightSimulationResult
    {
        public Guid SimulationId { get; init; } = Guid.NewGuid();
        public bool IsPermissible { get; init; }
        public DigitalTwinBusinessState CurrentState { get; init; } = null!;
        public DigitalTwinBusinessState ProjectedState { get; init; } = null!;
        public IReadOnlyList<ConstraintEvaluationResult> ConstraintEvaluations { get; init; } = Array.Empty<ConstraintEvaluationResult>();
        public IReadOnlyList<string> HardViolations { get; init; } = Array.Empty<string>();
        public double ProjectedRunwayChangeDays { get; init; }
        public string SummaryRationale { get; init; } = string.Empty;
        public DateTimeOffset SimulatedAt { get; init; } = DateTimeOffset.UtcNow;
    }

    public record SafeAlternativeProposal
    {
        public Guid AlternativeId { get; init; } = Guid.NewGuid();
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public PreFlightEffectProposal AlternativeEffect { get; init; } = null!;
        public double CostReductionPercentage { get; init; }
        public double FeasibilityScore { get; init; }
        public bool PassesAllHardConstraints { get; init; }
        public string Rationale { get; init; } = string.Empty;
    }

    public record BusinessFeasibilityResult
    {
        public Guid FeasibilityId { get; init; } = Guid.NewGuid();
        public Guid WorkspaceId { get; init; }
        public bool IsFeasible { get; init; }
        public ConstraintEvaluationState OverallState { get; init; } = ConstraintEvaluationState.Satisfied;
        public IReadOnlyList<ConstraintEvaluationResult> EvaluatedConstraints { get; init; } = Array.Empty<ConstraintEvaluationResult>();
        public IReadOnlyList<string> HardViolations { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> UnknownRealities { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> StaleRealities { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> ConflictedRealities { get; init; } = Array.Empty<string>();
        public PreFlightSimulationResult? Simulation { get; init; }
        public IReadOnlyList<SafeAlternativeProposal> RecommendedSafeAlternatives { get; init; } = Array.Empty<SafeAlternativeProposal>();
        public string SummaryRationale { get; init; } = string.Empty;
        public string ConstraintSnapshotHash { get; init; } = string.Empty;
        public string AuditHash { get; init; } = string.Empty;
    }
}
