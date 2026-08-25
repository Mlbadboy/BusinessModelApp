using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Commercial
{
    public class BusinessHealthResult
    {
        public double OverallHealthScore { get; set; }
        public double ConfidenceScore { get; set; }
        public string ConfidenceLevel { get; set; } = "Medium";
        public string ConfidenceReason { get; set; } = string.Empty;
        public string CalculationVersion { get; set; } = "HealthEngine:v1.0";
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        // Metric Values
        public decimal TotalPipelineValue { get; set; }
        public decimal WeightedForecastValue { get; set; }
        public decimal ClosedWonRevenue { get; set; }
        public decimal QuarterlyTarget { get; set; }
        public double PipelineCoverageRatio { get; set; }
        public double WinRate { get; set; }
        public double LeadQualificationRate { get; set; }
        public double AvgVelocityDays { get; set; }
        public double StalledRiskIndex { get; set; }

        // Breakdown & Components
        public HealthSubScores SubScores { get; set; } = new HealthSubScores();
        public List<ScoreComponentDetail> ComponentBreakdown { get; set; } = new List<ScoreComponentDetail>();
        public List<EvidenceRecord> EvidenceRecords { get; set; } = new List<EvidenceRecord>();
    }

    public class HealthSubScores
    {
        public double PipelineScore { get; set; }      // Weight: 40%
        public double ConversionScore { get; set; }    // Weight: 25%
        public double VelocityScore { get; set; }      // Weight: 20%
        public double RiskScore { get; set; }          // Weight: 15%
    }

    public class ScoreComponentDetail
    {
        public string ComponentName { get; set; } = string.Empty;
        public double WeightPercent { get; set; }
        public double RawScore { get; set; }
        public double WeightedContribution { get; set; }
        public string Explanation { get; set; } = string.Empty;
    }

    public class EvidenceRecord
    {
        public string EvidenceId { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        public string EvidenceType { get; set; } = "Pipeline";
        public string MetricKey { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string FormattedValue { get; set; } = string.Empty;
        public double NumericValue { get; set; }
        public string CalculationVersion { get; set; } = "HealthEngine:v1.0";
        public string Formula { get; set; } = string.Empty;
        public string Period { get; set; } = "Current Quarter";
        public double ConfidenceScore { get; set; } = 1.0;
        public string ImpactLevel { get; set; } = "Medium";
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public List<EvidenceContributor> Contributors { get; set; } = new List<EvidenceContributor>();
    }

    public class EvidenceContributor
    {
        public string EntityType { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal ContributionValue { get; set; }
        public string ContributionDetails { get; set; } = string.Empty;
    }

    public class ExecutiveBriefContext
    {
        public BusinessHealthResult Health { get; set; } = new BusinessHealthResult();
        public List<string> CriticalRiskAlerts { get; set; } = new List<string>();
        public List<string> PositiveMomentumSignals { get; set; } = new List<string>();
        public bool IsActionRequired { get; set; }
        public string Summary { get; set; } = string.Empty;
        public List<ExecutiveRecommendation> Recommendations { get; set; } = new List<ExecutiveRecommendation>();
    }

    public class ExecutiveRecommendation
    {
        public string Title { get; set; } = string.Empty;
        public string Rationale { get; set; } = string.Empty;
        public string CitedEvidenceId { get; set; } = string.Empty;
        public string ActionType { get; set; } = "Review";
        public string TargetEntity { get; set; } = string.Empty;
        public Guid? TargetEntityId { get; set; }
    }
}
