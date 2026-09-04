using System;
using System.Collections.Generic;
using BusinessModelApp.Core.Domain.Common;
using BusinessModelApp.Core.Domain.DigitalTwin;

namespace BusinessModelApp.Core.Domain.ExternalReality
{
    public enum ExternalSignalType
    {
        PriceChange = 1,
        ProductLaunch = 2,
        ProductDiscontinuation = 3,
        CompetitorMove = 4,
        MarketGrowth = 5,
        MarketDecline = 6,
        CustomerComplaint = 7,
        CustomerPreference = 8,
        RegulatoryChange = 9,
        HiringSurge = 10,
        HiringDecline = 11,
        FundingEvent = 12,
        Partnership = 13,
        DistributionChange = 14,
        TechnologyShift = 15,
        DemandSpike = 16,
        DemandDrop = 17,
        SupplyDisruption = 18,
        SentimentShift = 19
    }

    public enum ExternalSignalPriority
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    public enum ExternalSignalStatus
    {
        Detected = 1,
        Watching = 2,
        Actionable = 3,
        Dismissed = 4,
        Stale = 5
    }

    /// <summary>
    /// Governed external business signal detected by the Market Radar.
    /// Invariant: A signal is an advisory observation. It is NOT business truth.
    /// </summary>
    public class ExternalSignal : Entity
    {
        public Guid WorkspaceId { get; set; }
        public ExternalSignalType SignalType { get; set; } = ExternalSignalType.CompetitorMove;
        public string EntityName { get; set; } = string.Empty; // e.g. Competitor name, Market segment
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Temporal Metrology
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
        public DateTime FirstObservedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastObservedAt { get; set; } = DateTime.UtcNow;

        // Metrology Vectors
        public double Magnitude { get; set; } = 0.50; // 0.0 to 1.0 impact scale
        public string Direction { get; set; } = "Neutral"; // Positive, Negative, Neutral, Volatile
        public int Frequency { get; set; } = 1;
        public double NoveltyScore { get; set; } = 0.80;
        public double PersistenceScore { get; set; } = 0.50;
        public double ReliabilityScore { get; set; } = 0.75;
        public double FreshnessScore { get; set; } = 1.0;
        public double ContradictionScore { get; set; } = 0.0;
        public double ManipulationRisk { get; set; } = 0.10;
        public double Confidence { get; set; } = 0.65;

        // Deterministic Prioritization
        public ExternalSignalPriority Priority { get; set; } = ExternalSignalPriority.Medium;
        public double PriorityScore { get; set; } = 0.50;

        // Provenance Lineage
        public List<Guid> EvidenceIds { get; set; } = new();
        public List<Guid> SourceIds { get; set; } = new();

        public TruthClassification Classification { get; set; } = TruthClassification.Observation;
        public ExternalSignalStatus Status { get; set; } = ExternalSignalStatus.Detected;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Competitor profile tracking external movements across pricing, products, and strategy.
    /// </summary>
    public class CompetitorProfile : Entity
    {
        public Guid WorkspaceId { get; set; }
        public string CompetitorName { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string ProductsJson { get; set; } = "[]";
        public string PricingSummary { get; set; } = string.Empty;
        public string ObservedPositioning { get; set; } = string.Empty;
        public string ObservedStrategy { get; set; } = string.Empty;
        public string HiringActivity { get; set; } = "Stable";
        public string StrengthsJson { get; set; } = "[]";
        public string WeaknessesJson { get; set; } = "[]";
        public string MarketPresence { get; set; } = "Established";
        public double UncertaintyScore { get; set; } = 0.30;
        public DateTime? LastSignalObservedAt { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum OpportunityStatus
    {
        Detected = 1,
        Qualifying = 2,
        EvidenceCollection = 3,
        Validating = 4,
        Simulating = 5,
        Recommended = 6,
        ApprovedForExperiment = 7, // Eligible for governed bounded experiment. NOT autonomous execution!
        ExperimentRunning = 8,
        Measured = 9,
        Validated = 10,
        Rejected = 11,
        Stale = 12,
        Superseded = 13
    }

    /// <summary>
    /// Commercial Opportunity derived from verified external market signals.
    /// </summary>
    public class MarketOpportunity : Entity
    {
        public Guid WorkspaceId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public List<Guid> OriginSignalIds { get; set; } = new();
        public List<Guid> EvidenceIds { get; set; } = new();

        public string CustomerProblem { get; set; } = string.Empty;
        public string TargetSegment { get; set; } = string.Empty;
        public string MarketContext { get; set; } = string.Empty;

        // Commercial Dimensions
        public double StrategicFitScore { get; set; } = 0.80;
        public decimal RevenuePotentialINR { get; set; } = 0m;
        public decimal MarginPotentialPercent { get; set; } = 0m;
        public int TimeToValueDays { get; set; } = 30;
        public double ImplementationComplexityScore { get; set; } = 0.40;
        public double CompetitiveIntensity { get; set; } = 0.50;
        public double RiskScore { get; set; } = 0.30;

        // Epistemic Metrology
        public double Confidence { get; set; } = 0.60;
        public double ContaminationRisk { get; set; } = 0.10;
        public double CausalConfidence { get; set; } = 0.55;
        public double FreshnessScore { get; set; } = 1.0;

        public OpportunityStatus Status { get; set; } = OpportunityStatus.Detected;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum ThreatCategory
    {
        Competitive = 1,
        Pricing = 2,
        Customer = 3,
        Regulatory = 4,
        Supply = 5,
        Technology = 6,
        Reputational = 7,
        Financial = 8,
        Distribution = 9,
        Market = 10
    }

    /// <summary>
    /// Commercial Threat detected from external competitor or regulatory movements.
    /// </summary>
    public class MarketThreat : Entity
    {
        public Guid WorkspaceId { get; set; }
        public ThreatCategory Category { get; set; } = ThreatCategory.Competitive;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string TargetArea { get; set; } = string.Empty;
        public double SeverityScore { get; set; } = 0.70;
        public double UrgencyScore { get; set; } = 0.60;
        public double Confidence { get; set; } = 0.70;
        public List<Guid> EvidenceIds { get; set; } = new();
        public List<Guid> OriginSignalIds { get; set; } = new();
        public string MitigationOptionsJson { get; set; } = "[]";
        public string Status { get; set; } = "Active";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Deterministic commercial scoring object across 13 distinct dimensions.
    /// </summary>
    public class CommercialOpportunityScore
    {
        public Guid OpportunityId { get; set; }
        public double Score { get; set; } = 0.70; // 0.0 to 1.0
        public string ScoreVersion { get; set; } = "1.0-Deterministic13Dim";

        public Dictionary<string, double> DimensionScores { get; set; } = new();
        public double ConfidenceIntervalLower { get; set; } = 0.62;
        public double ConfidenceIntervalUpper { get; set; } = 0.78;
        public List<string> BlockingFactors { get; set; } = new();
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Counterfactual scenario result for an opportunity or strategic recommendation.
    /// Invariant: Classification is strictly HYPOTHESIS / SIMULATION. NEVER FACT.
    /// </summary>
    public class ScenarioOutcome
    {
        public string ScenarioName { get; set; } = "Base"; // Base, Upside, Downside, CompetitorResponse, MacroShock
        public string AssumptionsJson { get; set; } = "[]";
        public decimal ExpectedRevenueINR { get; set; } = 0m;
        public decimal ExpectedMarginPercent { get; set; } = 0m;
        public double Confidence { get; set; } = 0.50;
        public double SensitivityRanking { get; set; } = 1.0;
        public bool IsSimulation { get; set; } = true;
        public TruthClassification Classification { get; set; } = TruthClassification.Hypothesis;
    }

    /// <summary>
    /// Governed Strategic Recommendation resulting from external intelligence.
    /// INVARIANT: Terminates at recommendation. DOES NOT execute real-world side effects.
    /// </summary>
    public class StrategicRecommendation : Entity
    {
        public Guid WorkspaceId { get; set; }
        public Guid? OpportunityId { get; set; }
        public Guid? ThreatId { get; set; }
        public string Summary { get; set; } = string.Empty;
        public string RecommendedStrategicAction { get; set; } = string.Empty;

        // Evidence and Provenance Trace
        public string EvidenceChainJson { get; set; } = "[]";
        public string PrimaryHypothesis { get; set; } = string.Empty;
        public string AlternativeHypothesesJson { get; set; } = "[]";
        public string WhyNotAnalysis { get; set; } = string.Empty;

        // Commercial Evaluation
        public double CommercialScore { get; set; } = 0.70;
        public string ScenarioMatrixJson { get; set; } = "[]";
        public string SensitivityAnalysisJson { get; set; } = "[]";

        // Epistemic Bounds
        public double ContaminationRisk { get; set; } = 0.10;
        public string UncertaintyBudgetImpactSummary { get; set; } = string.Empty;
        public decimal ExpectedValueINR { get; set; } = 0m;
        public decimal DownsideRiskINR { get; set; } = 0m;
        public double Confidence { get; set; } = 0.65;

        // Sovereign Governance Gates
        public string RequiredGovernanceApprovalsJson { get; set; } = "[\"CEOApproval\"]";
        public string Status { get; set; } = "AdvisoryPrepared"; // AdvisoryPrepared, Reviewed, Rejected, ScheduledForExperiment
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
