using System;
using BusinessModelApp.Core.Domain.Common;
using BusinessModelApp.Core.Domain.Objectives;

namespace BusinessModelApp.Core.Domain.WorldModel
{
    /// <summary>
    /// First-class domain object representing Charlie's Four-State Revenue Baseline.
    /// Invariant: Autonomous Revenue Gap is a planning metric, NOT guaranteed or expected revenue!
    /// </summary>
    public class RevenueBaseline
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid WorkspaceId { get; set; }
        public Guid? ObjectiveId { get; set; }
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

        public RevenueBaselineState State { get; set; } = RevenueBaselineState.Unavailable;

        // Target Objective
        public TruthMetric<decimal> TargetRevenueINR { get; set; } = new();

        // State 1: Contracted / Guaranteed Revenue (Signed contracts, active retainers, confirmed POs)
        public TruthMetric<decimal> ContractedGuaranteedRevenueINR { get; set; } = TruthMetric<decimal>.Unavailable("No verified contracts or retainers found.");

        // State 2: Weighted Pipeline (Sum of Validated Deals * Stage Probability)
        public TruthMetric<decimal> WeightedPipelineRevenueINR { get; set; } = TruthMetric<decimal>.Unavailable("No active pipeline found.");

        // State 3: Historical Run Rate (Empirical recurring billing baseline)
        public TruthMetric<decimal> HistoricalRunRateRevenueINR { get; set; } = TruthMetric<decimal>.Unavailable("No historical billing baseline available.");

        // State 4: Autonomous Revenue Gap = Target - (Contracted + WeightedPipeline + RunRate)
        public TruthMetric<decimal> AutonomousRevenueGapINR { get; set; } = new();

        public decimal TotalBaselineGroundedINR =>
            (ContractedGuaranteedRevenueINR.IsGroundedFact ? ContractedGuaranteedRevenueINR.Value : 0m) +
            (WeightedPipelineRevenueINR.IsGroundedFact ? WeightedPipelineRevenueINR.Value : 0m) +
            (HistoricalRunRateRevenueINR.IsGroundedFact ? HistoricalRunRateRevenueINR.Value : 0m);

        public void ComputeAutonomousGap(decimal targetRevenue)
        {
            TargetRevenueINR = TruthMetric<decimal>.CeoInput(targetRevenue, "Direct CEO revenue mandate.");
            
            decimal knownFloor = 
                (ContractedGuaranteedRevenueINR.Value) +
                (WeightedPipelineRevenueINR.Value) +
                (HistoricalRunRateRevenueINR.Value);

            decimal gap = Math.Max(0m, targetRevenue - knownFloor);

            AutonomousRevenueGapINR = new TruthMetric<decimal>
            {
                Value = gap,
                Source = MetricProvenanceSource.ExplicitCeoInput,
                VerificationStatus = Reality.VerificationStatus.VerifiedFact,
                Confidence = 1.0,
                ObservedAt = DateTime.UtcNow,
                Note = "PLANNING METRIC ONLY: The commercial revenue gap Charlie must formulate strategies to capture. NOT guaranteed revenue."
            };
        }
    }
}
