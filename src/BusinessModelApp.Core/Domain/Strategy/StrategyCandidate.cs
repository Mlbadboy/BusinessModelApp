using System;
using System.Collections.Generic;
using BusinessModelApp.Core.Domain.WorldModel;

namespace BusinessModelApp.Core.Domain.Strategy
{
    public class StrategyCandidate
    {
        public Guid StrategyId { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string ObjectiveTitle { get; set; } = string.Empty;
        public Guid ObjectiveId { get; set; }

        public decimal ExpectedRevenueINR { get; set; }
        public int RequiredDeliveryCapacitySlots { get; set; } = 1;
        public decimal RequiredBudgetINR { get; set; } = 25000m;
        public int TimelineDays { get; set; } = 60;

        public ReverseFunnelResult RequiredFunnel { get; set; } = new();

        public List<AssumptionProvenance> Assumptions { get; set; } = new();
        public List<string> EvidenceReferences { get; set; } = new();

        public StrategyFeasibilityClassification Feasibility { get; set; } = StrategyFeasibilityClassification.HypotheticalUnverified;
        public FeasibilityReport FeasibilityReport { get; set; } = new();

        public bool ConstitutionCompliant { get; set; } = true;
        public List<string> ConstitutionViolations { get; set; } = new();

        public double RiskScore { get; set; } = 0.2; // 0.0 (low) to 1.0 (critical)
        public double ConfidenceScore { get; set; } = 0.8;
        public bool IsRecommended { get; set; } = false;
        public bool IsDeterministic { get; set; } = true;

        public bool HasUnverifiedAssumptions =>
            Assumptions.Exists(a => a.Origin == Objectives.MetricProvenanceSource.AiEstimate || a.Origin == Objectives.MetricProvenanceSource.Unknown);
    }
}
