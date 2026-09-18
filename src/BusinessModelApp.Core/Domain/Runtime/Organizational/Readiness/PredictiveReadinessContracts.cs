namespace BusinessModelApp.Core.Domain.Runtime.Organizational.Readiness;

/// <summary>
/// Invariant I31: Constitutional Invariant of Predictive Organizational Readiness (POR).
/// OBSERVATION != SIGNAL != FORECAST != SCENARIO != RISK != READINESS != WORK != AUTHORITY != EXECUTION
///
/// Subordinate Invariants:
/// - I31-A: Forecast != Fact. Forecasts remain probabilistic hypotheses; empirical ground telemetry required.
/// - I31-B: Prediction != Inevitability. Plausible future scenarios do not guarantee occurrence.
/// - I31-C: Scenario != Forecast. A scenario is a counterfactual stress condition, not a probabilistic point forecast.
/// - I31-D: Risk != Event. High risk score indicates exposure and vulnerability, not event occurrence.
/// - I31-E: Readiness != Authorization. Preparedness does not authorize unilateral action or bypass governance gates.
/// - I31-F: Stress Test != Real-World Experiment. Simulations cannot incur real-world side effects or capital allocation.
/// - I31-G: Probability != Certainty. All forecasts must carry explicit uncertainty bounds and confidence intervals.
/// - I31-H: Multiple Models != Truth. Multi-model agreement is ensemble consensus, not empirical ground truth.
/// - I31-I: Historical Precedent != Future Guarantee. Regime shifts can invalidate historical patterns.
/// - I31-J: Early Warning != Emergency Authority. Alerts inform PRG-1; zero emergency powers or firewall bypass.
/// - I31-K: Contingency Proposal != Approved Work. Pre-staged proposals require normal 3.9.0 admission before execution.
/// - I31-L: Readiness Score != Organizational Priority. Governance sets priority; algorithms only report readiness.
/// - I31-M: Prediction Cannot Mutate Truth. Predictions cannot alter historical records, audit logs, or telemetry.
/// - I31-N: Prediction Cannot Mutate Policy. Forecasted distress cannot autonomously relax spending or compliance rules.
/// - I31-O: POR Cannot Create Execution Authority. Zero ExecutionPermit generation; cannot invoke worker connectors.
/// </summary>
public static class InvariantI31
{
    public const string InvariantName = "I31_PredictiveOrganizationalReadinessSovereignty";
    public const string CoreDoctrine = "OBSERVATION != SIGNAL != FORECAST != SCENARIO != RISK != READINESS != WORK != AUTHORITY != EXECUTION";

    public const string I31_A = "I31-A: Forecast != Fact. Forecasts remain probabilistic hypotheses.";
    public const string I31_B = "I31-B: Prediction != Inevitability. Plausible future scenarios do not guarantee occurrence.";
    public const string I31_C = "I31-C: Scenario != Forecast. Counterfactual stress conditions are not probabilistic forecasts.";
    public const string I31_D = "I31-D: Risk != Event. Risk measures exposure and vulnerability, not event occurrence.";
    public const string I31_E = "I31-E: Readiness != Authorization. Preparedness does not authorize autonomous organizational actions.";
    public const string I31_F = "I31-F: Stress Test != Real-World Experiment. Simulations carry zero real-world side effects.";
    public const string I31_G = "I31-G: Probability != Certainty. Forecasts must carry explicit uncertainty bounds.";
    public const string I31_H = "I31-H: Multiple Models != Truth. Ensemble agreement is model consensus, not empirical truth.";
    public const string I31_I = "I31-I: Historical Precedent != Future Guarantee. Regime shifts can invalidate historical baselines.";
    public const string I31_J = "I31-J: Early Warning != Emergency Authority. Early warnings inform PRG-1; no emergency powers.";
    public const string I31_K = "I31-K: Contingency Proposal != Approved Work. Contingencies require standard 3.9.0 admission.";
    public const string I31_L = "I31-L: Readiness Score != Organizational Priority. Human leadership sets organizational priorities.";
    public const string I31_M = "I31-M: Prediction Cannot Mutate Truth. Predictions cannot alter audit logs or state telemetry.";
    public const string I31_N = "I31-N: Prediction Cannot Mutate Policy. Predictive distress cannot relax business constraints.";
    public const string I31_O = "I31-O: POR Cannot Create Execution Authority. POR cannot issue ExecutionPermits or call connectors.";
}

/// <summary>
/// Status of organizational readiness for a given scenario or overall posture.
/// </summary>
public enum ReadinessStatus
{
    Green = 0,   // Score >= 0.85: Organization is well-prepared
    Amber = 1,   // 0.50 <= Score < 0.85: Vulnerabilities detected; preparation recommended
    Red = 2      // Score < 0.50: Critical gaps present; immediate action posture required
}

/// <summary>
/// Recommended organizational posture: "Prepare Now vs Wait".
/// </summary>
public enum ReadinessActionPosture
{
    Wait = 0,                  // Low probability / distant horizon / low impact: maintain status quo
    Watch = 1,                 // Medium probability / distant horizon: monitor drift via Watchtower
    Prepare = 2,               // Medium-to-high probability / near-to-medium horizon: stage contingent proposals
    ActNow = 3,                // High probability / imminent horizon / high impact: prioritize proposal admission
    InsufficientEvidence = 4   // Missing confidence, low sample count, or ungrounded scenario: collect evidence first
}

/// <summary>
/// The 8 decomposable organizational readiness dimensions.
/// </summary>
public enum ReadinessDimension
{
    Liquidity = 0,    // Weight: 0.20 - Cash reserves, runway, credit capacity
    Capacity = 1,     // Weight: 0.18 - Production/service bandwidth, throughput buffer
    Demand = 2,       // Weight: 0.15 - Pipeline elasticity, order handling capacity
    Operational = 3,  // Weight: 0.15 - Process resilience, SLAs, logistics buffer
    Workforce = 4,    // Weight: 0.12 - Staffing levels, skill coverage, key-person risk
    Supplier = 5,     // Weight: 0.10 - Vendor redundancy, supply chain continuity
    Technology = 6,   // Weight: 0.05 - System headroom, failover, infrastructure scale
    Governance = 7    // Weight: 0.05 - Policy adherence, regulatory readiness, audit state
}

/// <summary>
/// Forecast time horizon window.
/// </summary>
public enum HorizonWindow
{
    Immediate = 0,    // <= 2 weeks
    NearTerm = 1,     // 2 to 8 weeks
    MediumTerm = 2,   // 2 to 6 months
    LongTerm = 3      // > 6 months
}

/// <summary>
/// Dimension of persistent readiness debt.
/// </summary>
public enum ReadinessDebtType
{
    CapacityDebt = 0,
    CashDebt = 1,
    PeopleDebt = 2,
    TechnologyDebt = 3,
    SupplierDebt = 4,
    ComplianceDebt = 5
}

/// <summary>
/// Represents a future scenario (from 3.8 Intelligence or counterfactual stress specification).
/// Invariant I31-C: Scenario != Forecast.
/// </summary>
public class ReadinessScenario
{
    public string ScenarioId { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public HorizonWindow Horizon { get; set; } = HorizonWindow.NearTerm;
    public double EstimatedProbability { get; set; } = 0.5; // 0.0 to 1.0 (I31-G)
    public double ConfidenceInterval { get; set; } = 0.8;   // Epistemic confidence of model
    public double ProjectedDemandDelta { get; set; } = 0.0; // Percentage change (+0.30 = +30%)
    public double ProjectedCostDelta { get; set; } = 0.0;   // Percentage change
    public double ProjectedCapacityStrain { get; set; } = 0.0; // Strain factor (0.0 to 1.0)
    public string SourceModel { get; set; } = "3.8-Scenario-Engine";
    public string EvidenceRef { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// An identified organizational vulnerability discovered during stress testing.
/// Invariant I31-D: Risk != Event.
/// </summary>
public class ReadinessVulnerability
{
    public string VulnerabilityId { get; set; } = Guid.NewGuid().ToString("N");
    public ReadinessDimension Dimension { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Severity { get; set; } = 0.5; // 0.0 to 1.0
    public double EstimatedLeadTimeToRemediateWeeks { get; set; } = 4.0;
    public double TimeToBufferExhaustionWeeks { get; set; } = 6.0;
    public double GapMagnitude { get; set; } = 0.0; // Capacity/cash gap in units/currency
    public string RemediationRecommendation { get; set; } = string.Empty;
}

/// <summary>
/// Result of capacity & buffer stress simulation.
/// Invariant I31-F: Stress Test != Real-World Experiment.
/// </summary>
public class CapacityStressTestResult
{
    public string TestId { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = string.Empty;
    public string ScenarioId { get; set; } = string.Empty;
    public double CurrentBufferCapacity { get; set; }
    public double SimulatedStressDemand { get; set; }
    public double BufferExhaustionHorizonWeeks { get; set; }
    public bool IsBufferExhaustedWithinHorizon { get; set; }
    public double PeakStrainFactor { get; set; } // 0.0 to 2.0+
    public List<string> BottleneckResourceIds { get; set; } = new();
    public DateTime SimulatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Transparent deterministic explanation of why an assessment received Green, Amber, or Red.
/// </summary>
public class WhyReadinessTrace
{
    public string AssessmentId { get; set; } = string.Empty;
    public ReadinessStatus Status { get; set; }
    public ReadinessActionPosture Posture { get; set; }
    public double CompositeScore { get; set; }
    public string EvidenceRef { get; set; } = string.Empty;
    public string ForecastHorizonDescription { get; set; } = string.Empty;
    public double ScenarioExposureMagnitude { get; set; }
    public double CapacityGap { get; set; }
    public double WeeksToImpact { get; set; }
    public double ModelConfidence { get; set; }
    public bool IsFreshData { get; set; } = true;
    public List<string> PrimaryContributingFactors { get; set; } = new();
    public Dictionary<ReadinessDimension, double> DimensionScores { get; set; } = new();
}

/// <summary>
/// Persistent Readiness Debt record.
/// Known Future Exposure + Insufficient Preparation = Readiness Debt.
/// </summary>
public class ReadinessDebtRecord
{
    public string DebtId { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = string.Empty;
    public ReadinessDebtType DebtType { get; set; }
    public double QuantifiedAmount { get; set; } // Financial or capacity debt magnitude
    public string ExposureDescription { get; set; } = string.Empty;
    public double CompoundingFactor { get; set; } = 1.0;
    public int AgeInDays { get; set; } = 0;
    public DateTime FirstIdentifiedUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastEvaluatedUtc { get; set; } = DateTime.UtcNow;
    public bool IsRemediated { get; set; } = false;
}

/// <summary>
/// Pre-staged contingency proposal for 3.9.0 Organizational Work Control Plane.
/// Invariant I31-K: Contingency Proposal != Approved Work.
/// Invariant I31-O: POR Cannot Create Execution Authority.
/// </summary>
public class ContingencyProposal
{
    public string ContingencyId { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = string.Empty;
    public string ScenarioId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Objective { get; set; } = string.Empty;
    public string ProposedWorkPayload { get; set; } = string.Empty; // Description of proactive work
    public string ActivationTriggerCondition { get; set; } = string.Empty; // E.g. "Demand index > 1.25 for 14 days"
    public double EstimatedPreparationCost { get; set; } = 0.0;
    public double ReversibilityScore { get; set; } = 1.0; // 1.0 = fully reversible, 0.0 = irreversible
    public bool IsStagedToWorkControlPlane { get; set; } = false;
    public string? StagedWorkProposalId { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Complete decomposable 8-dimensional readiness assessment for an organization under a scenario.
/// </summary>
public class OrganizationalReadinessAssessment
{
    public string AssessmentId { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = string.Empty;
    public string ScenarioId { get; set; } = string.Empty;
    public DateTime EvaluatedUtc { get; set; } = DateTime.UtcNow;

    // 8 Decomposable Dimensions (each 0.0 to 1.0)
    public double DemandReadiness { get; set; } = 1.0;
    public double CapacityReadiness { get; set; } = 1.0;
    public double LiquidityReadiness { get; set; } = 1.0;
    public double OperationalReadiness { get; set; } = 1.0;
    public double TechnologyReadiness { get; set; } = 1.0;
    public double WorkforceReadiness { get; set; } = 1.0;
    public double SupplierReadiness { get; set; } = 1.0;
    public double GovernanceReadiness { get; set; } = 1.0;

    public double CompositeReadinessScore { get; set; } = 1.0;
    public ReadinessStatus Status { get; set; } = ReadinessStatus.Green;
    public ReadinessActionPosture ActionPosture { get; set; } = ReadinessActionPosture.Watch;

    public List<ReadinessVulnerability> DetectedVulnerabilities { get; set; } = new();
    public WhyReadinessTrace WhyTrace { get; set; } = new();
}
