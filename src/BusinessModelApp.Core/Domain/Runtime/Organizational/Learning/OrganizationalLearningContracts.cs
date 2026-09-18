using System.Security.Cryptography;
using System.Text;

namespace BusinessModelApp.Core.Domain.Runtime.Organizational.Learning;

/// <summary>
/// Constitutional Invariant I35 — Organizational Learning & Adaptation Sovereignty.
/// OUTCOME != OBSERVATION != EVIDENCE != CORRELATION != CAUSATION != LESSON != ADAPTATION != POLICY != AUTHORITY.
/// </summary>
public static class OrganizationalLearningInvariants
{
    public const string PrimaryInvariant = "OUTCOME != OBSERVATION != EVIDENCE != CORRELATION != CAUSATION != LESSON != ADAPTATION != POLICY != AUTHORITY";
    public const string EpistemicClassificationLesson = "EmpiricalLesson";

    // 26 Sub-laws I35-A through I35-Z
    public const string I35_A_OutcomeNotLesson = "I35-A: An empirical result is raw observation; turning it into a lesson requires counterfactual reconciliation and attribution.";
    public const string I35_B_LessonNotAdaptation = "I35-B: Distilling a lesson does not authorize changing operational parameters or rules.";
    public const string I35_C_AdaptationNotPolicy = "I35-C: Parameter adaptation is governed operational tuning; it cannot rewrite constitutional constraints or safety floors.";
    public const string I35_D_CorrelationNotCausalDrift = "I35-D: Spurious covariance does not alter causal directed graphs without interventional validation.";
    public const string I35_E_AdaptationCannotSelfApprove = "I35-E: AdaptationProposal requires PRG-1 approval or governed threshold clearance; cannot self-activate.";
    public const string I35_F_CalibrationErrorMeasured = "I35-F: Metrology tracks directional accuracy, Brier scores, mean absolute percentage error (MAPE), and calibration drift transparently.";
    public const string I35_G_SimulationCalibrationNotFact = "I35-G: Measuring simulation accuracy evaluates simulator reliability; it never retroactively marks simulation runs as real facts.";
    public const string I35_H_HeuristicDecay = "I35-H: Unvalidated or stale heuristics decay in confidence over time if not corroborated by subsequent empirical observations.";
    public const string I35_I_AntiHallucinationGating = "I35-I: Statistically insignificant sample sizes produce InconclusiveEvidence rather than lessons.";
    public const string I35_J_StructuralDriftDetection = "I35-J: System explicitly detects regime shifts, covariate shifts, and concept drift, transitioning to a cautious posture rather than forcing old models.";
    public const string I35_K_FirewallSovereignty = "I35-K: OLMA cannot grant execution permits, modify connector permissions, or execute real-world mutations.";
    public const string I35_L_AllocationPortfolioNonMutation = "I35-L: OLMA provides historical efficiency feedback to OARA and Portfolio; it cannot alter live allocations or active work items.";
    public const string I35_M_MultiTenantIsolation = "I35-M: Learning models, calibration histories, and distilled lessons are strictly isolated by TenantId.";
    public const string I35_N_DeterministicAuditProvenance = "I35-N: Every lesson and adaptation proposal carries full attribution back to triggering outcome events, snapshot hashes, and calibration metrics.";
    public const string I35_O_ReversibilityAndRollback = "I35-O: Any applied parameter adaptation maintains a complete inverse diff and can be rolled back immediately.";
    public const string I35_P_CounterfactualVerification = "I35-P: Lessons evaluate what would have happened under alternative choices (using 3.9.9 simulation) before proposing adaptations.";
    public const string I35_Q_MaterialityThreshold = "I35-Q: Adaptations require materiality score >= 0.15; insignificant drift ticks do not trigger parameter thrashing.";
    public const string I35_R_KnowledgePromotionGate = "I35-R: Lessons promoted to 3.9.3 Organizational Memory carry epistemic tag EmpiricalLesson with explicit validity scope and confidence interval.";
    public const string I35_S_FailClosedAmbiguity = "I35-S: Conflicting evidence or high epistemic uncertainty flags the domain for human review rather than guessing.";
    public const string I35_T_CanonicalLessonHash = "I35-T: Every lesson generates an invariant SHA-256 LessonHash from its causal attribution and empirical evidence.";
    public const string I35_U_ApprovalSovereignty = "I35-U: OLMA cannot approve an AdaptationProposal. All human approval must occur through the existing PRG-1 HumanApprovalManager.";
    public const string I35_V_EvidenceSufficiencySovereignty = "I35-V: Minimum sample size is necessary but never sufficient for lesson promotion. Requires effect size, confidence, stability, and regime consistency.";
    public const string I35_W_CausalIntelligenceSovereignty = "I35-W: OLMA cannot redefine, overwrite, or independently replace canonical causal relationships owned by Phase 3.8.1.";
    public const string I35_X_SimulationEngineSovereignty = "I35-X: OLMA may consume governed simulation results from 3.9.9 but cannot create an independent simulation authority or alter simulation results.";
    public const string I35_Y_AdaptationHysteresis = "I35-Y: An adaptation cannot be reversed or reapplied repeatedly within a governed cooldown window unless a materially stronger evidence threshold is met.";
    public const string I35_Z_DriftNotAdaptationAuthority = "I35-Z: A detected regime/model/concept drift can change confidence or trigger review, but cannot independently modify policy, models, allocations, or execution behavior.";
}

/// <summary>
/// Epistemic progression lifecycle for organizational learning.
/// </summary>
public enum LearningEpistemicState
{
    RawOutcome,
    ObservationRecorded,
    EvidenceValidated,
    InconclusiveEvidence,
    LessonDistilled,
    AdaptationProposed,
    HumanApproved,
    ApplicationAdmitted,
    Applied,
    Verified,
    Rejected,
    Decayed,
    Archived
}

public enum MetrologyMetricType
{
    ForecastAccuracy,
    AllocationEfficiency,
    PortfolioYield,
    SimulationCalibration,
    ExecutionLatency,
    ErrorRate
}

public enum AdaptationTargetType
{
    HeuristicWeight,
    ForecastConfidenceDiscount,
    AttentionBudgetWeight,
    MaterialityThreshold,
    ReadinessBufferMultiplier
}

public enum DriftType
{
    None,
    CovariateShift,
    ConceptDrift,
    RegimeTransition,
    ModelDegradation
}

/// <summary>
/// Empirical outcome event recording actual real-world result against expected metrics.
/// </summary>
public sealed class EmpiricalOutcomeEvent
{
    public string OutcomeEventId { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = string.Empty;
    public string SourceDomain { get; set; } = "Portfolio"; // Watchtower, Readiness, OARA, Portfolio, Mission, Simulation
    public string SourceEntityId { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public double ExpectedValue { get; set; }
    public double ActualValue { get; set; }
    public double DeltaValue => ActualValue - ExpectedValue;
    public double VarianceScore { get; set; }
    public string CausalHypothesisId { get; set; } = string.Empty;
    public string EnvironmentRegime { get; set; } = "StableGrowth";
    public DateTime ObservedUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Multi-dimensional evidence sufficiency evaluation (I35-V).
/// Sample size N >= 10 is necessary but not sufficient.
/// </summary>
public sealed class EvidenceSufficiencyScore
{
    public int SampleSize { get; set; }
    public double EffectSize { get; set; } // 0.0 to 1.0
    public double Confidence { get; set; } // 0.0 to 1.0
    public double Stability { get; set; } // 1.0 = low variance, 0.0 = high variance
    public double AttributionQuality { get; set; } // 0.0 to 1.0
    public double RegimeConsistency { get; set; } // 1.0 = stable regime

    public bool IsSufficient =>
        SampleSize >= 10 &&
        EffectSize >= 0.10 &&
        Confidence >= 0.80 &&
        Stability >= 0.65 &&
        AttributionQuality >= 0.70 &&
        RegimeConsistency >= 0.70;

    public double ComputeCompositeScore()
    {
        return Math.Round(
            (0.20 * Math.Clamp(SampleSize / 50.0, 0.0, 1.0)) +
            (0.20 * EffectSize) +
            (0.20 * Confidence) +
            (0.15 * Stability) +
            (0.15 * AttributionQuality) +
            (0.10 * RegimeConsistency),
            4);
    }
}

/// <summary>
/// Statistical calibration metrology record (I35-F).
/// </summary>
public sealed class MetrologyRecord
{
    public string RecordId { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = string.Empty;
    public string ModelOrDomain { get; set; } = string.Empty;
    public MetrologyMetricType MetricType { get; set; } = MetrologyMetricType.ForecastAccuracy;
    public int SampleCount { get; set; }
    public double MeanAbsolutePercentageError { get; set; }
    public double BrierScore { get; set; }
    public double DirectionalAccuracy { get; set; }
    public double CalibrationSlope { get; set; }
    public double ObservedVariance { get; set; }
    public DateTime CalculatedUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Structural or concept drift detection result (I35-J, I35-Z).
/// </summary>
public sealed class DriftDetectionResult
{
    public string DriftId { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = string.Empty;
    public string TargetModel { get; set; } = string.Empty;
    public DriftType DetectedDriftType { get; set; } = DriftType.None;
    public double DivergenceScore { get; set; } // 0.0 to 1.0
    public double PValue { get; set; }
    public bool RequiresCaution { get; set; }
    public string RecommendedPosture { get; set; } = "Nominal";
    public DateTime DetectedUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Distilled organizational lesson with cryptographic provenance (I35-T).
/// </summary>
public sealed class OrganizationalLesson
{
    public string LessonId { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ObservationSummary { get; set; } = string.Empty;
    public string CausalAttribution { get; set; } = string.Empty;
    public string CounterfactualInsight { get; set; } = string.Empty;
    public EvidenceSufficiencyScore EvidenceSufficiency { get; set; } = new();
    public double Confidence { get; set; } = 0.85;
    public LearningEpistemicState State { get; set; } = LearningEpistemicState.LessonDistilled;
    public string EpistemicTag { get; set; } = OrganizationalLearningInvariants.EpistemicClassificationLesson;
    public string LessonHash { get; set; } = string.Empty;
    public DateTime DistilledUtc { get; set; } = DateTime.UtcNow;

    public void ComputeLessonHash()
    {
        var raw = $"{TenantId}:{Title}:{ObservationSummary}:{CausalAttribution}:{CounterfactualInsight}:{Confidence:F4}";
        using var sha = SHA256.Create();
        LessonHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
    }
}

/// <summary>
/// Whitelist definition for closed-world adaptation targets.
/// </summary>
public sealed class WhitelistTargetDefinition
{
    public AdaptationTargetType TargetType { get; set; }
    public string ParameterKey { get; set; } = string.Empty;
    public double SafeMin { get; set; }
    public double SafeMax { get; set; }
    public double MaxDeltaPerAdaptation { get; set; }
}

/// <summary>
/// Closed-world adaptation target whitelist registry.
/// Explicitly rejects unapproved parameters (credentials, permits, policies).
/// </summary>
public static class AdaptationTargetWhitelist
{
    public static readonly Dictionary<string, WhitelistTargetDefinition> AllowedTargets = new(StringComparer.OrdinalIgnoreCase)
    {
        ["HeuristicWeight.GrowthFocus"] = new()
        {
            TargetType = AdaptationTargetType.HeuristicWeight,
            ParameterKey = "HeuristicWeight.GrowthFocus",
            SafeMin = 0.1,
            SafeMax = 0.9,
            MaxDeltaPerAdaptation = 0.2
        },
        ["ForecastConfidenceDiscount.Macro"] = new()
        {
            TargetType = AdaptationTargetType.ForecastConfidenceDiscount,
            ParameterKey = "ForecastConfidenceDiscount.Macro",
            SafeMin = 0.05,
            SafeMax = 0.50,
            MaxDeltaPerAdaptation = 0.15
        },
        ["AttentionBudgetWeight.Cognitive"] = new()
        {
            TargetType = AdaptationTargetType.AttentionBudgetWeight,
            ParameterKey = "AttentionBudgetWeight.Cognitive",
            SafeMin = 0.1,
            SafeMax = 0.8,
            MaxDeltaPerAdaptation = 0.15
        },
        ["MaterialityThreshold.Rebalance"] = new()
        {
            TargetType = AdaptationTargetType.MaterialityThreshold,
            ParameterKey = "MaterialityThreshold.Rebalance",
            SafeMin = 0.10,
            SafeMax = 0.35,
            MaxDeltaPerAdaptation = 0.05
        },
        ["ReadinessBufferMultiplier.Operations"] = new()
        {
            TargetType = AdaptationTargetType.ReadinessBufferMultiplier,
            ParameterKey = "ReadinessBufferMultiplier.Operations",
            SafeMin = 1.0,
            SafeMax = 2.5,
            MaxDeltaPerAdaptation = 0.30
        }
    };

    public static bool IsWhitelisted(string parameterKey, out WhitelistTargetDefinition? def)
    {
        return AllowedTargets.TryGetValue(parameterKey, out def);
    }
}

/// <summary>
/// Hysteresis state tracking cooldown windows to prevent parameter thrashing (I35-Y).
/// </summary>
public sealed class AdaptationHysteresisState
{
    public string ParameterKey { get; set; } = string.Empty;
    public double LastAdaptedValue { get; set; }
    public DateTime LastAdaptedUtc { get; set; } = DateTime.MinValue;
    public TimeSpan CooldownWindow { get; set; } = TimeSpan.FromHours(24);
    public int AdaptationCountInEpoch { get; set; }

    public bool IsInCooldown(DateTime now) => now - LastAdaptedUtc < CooldownWindow;
}

/// <summary>
/// Reversible, governed adaptation proposal (I35-E, I35-O).
/// Does NOT self-apply or self-approve.
/// </summary>
public sealed class AdaptationProposal
{
    public string ProposalId { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = string.Empty;
    public string LessonId { get; set; } = string.Empty;
    public string ParameterKey { get; set; } = string.Empty;
    public AdaptationTargetType TargetType { get; set; } = AdaptationTargetType.HeuristicWeight;

    public double CurrentValue { get; set; }
    public double ProposedValue { get; set; }
    public double MaterialityScore { get; set; }
    public double RollbackInverseValue => CurrentValue;

    public string JustificationRationale { get; set; } = string.Empty;
    public LearningEpistemicState State { get; set; } = LearningEpistemicState.AdaptationProposed;
    public string RequiredGovernanceApprovalLevel { get; set; } = "PRG-1 Human Governance Required";
    public DateTime ProposedUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Retrospective explainability trace detailing why an adaptation was proposed (I35-N).
/// </summary>
public sealed class WhyAdaptationTrace
{
    public string TraceId { get; set; } = Guid.NewGuid().ToString("N");
    public string ProposalId { get; set; } = string.Empty;
    public string TriggeringOutcomeId { get; set; } = string.Empty;
    public string DistilledLessonTitle { get; set; } = string.Empty;
    public string CalibrationEvidenceSummary { get; set; } = string.Empty;
    public string CausalAttributionSummary { get; set; } = string.Empty;
    public string CounterfactualCheckSummary { get; set; } = string.Empty;
    public string HysteresisVerification { get; set; } = string.Empty;
    public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;
}
