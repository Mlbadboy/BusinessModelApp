using System;
using System.Collections.Generic;
using BusinessModelApp.Core.Domain.Objectives;

namespace BusinessModelApp.Core.Domain.Strategy
{
    public class AssumptionProvenance
    {
        public string Key { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string AssumedValue { get; set; } = string.Empty;
        public MetricProvenanceSource Origin { get; set; } = MetricProvenanceSource.AiEstimate;
        public Guid? EvidenceRecordId { get; set; }
        public string? EvidenceHash { get; set; }
        public double Confidence { get; set; } = 0.5;

        public bool IsGroundedInReality =>
            (Origin == MetricProvenanceSource.VerifiedFact || Origin == MetricProvenanceSource.HistoricalCompanyData) &&
            EvidenceRecordId.HasValue;
    }

    public enum StrategyFeasibilityClassification
    {
        FeasibleEvidenced = 1,
        HypotheticalUnverified = 2,
        UnfeasibleResourceConstrained = 3,
        UnfeasibleConstitutionalViolation = 4
    }

    public class FeasibilityReport
    {
        public Guid StrategyId { get; set; }
        public string StrategyName { get; set; } = string.Empty;
        public StrategyFeasibilityClassification Classification { get; set; }
        public List<string> Reasons { get; set; } = new();
        public List<string> MissingEvidenceItems { get; set; } = new();
        public List<string> ResourceConstraintsViolated { get; set; } = new();
        public List<string> ConstitutionalViolations { get; set; } = new();
        public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;

        public string SummaryText =>
            $"Classification: {Classification}. Reasons: {string.Join("; ", Reasons)}";
    }
}
