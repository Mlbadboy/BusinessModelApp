using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace BusinessModelApp.Core.Domain.Runtime.Intelligence.Decision
{
    // =========================================================================
    // THE GOLDEN DECISION SOVEREIGNTY RULE (INVARIANT I23)
    // =========================================================================
    // THE RADAR DETECTS WHAT DESERVES ATTENTION.
    // THE SCENARIO ENGINE SIMULATES THE PARETO FRONTIER.
    // THE DECISION ENGINE EVALUATES, RANKS, AND RECOMMENDS CANDIDATES.
    // THE GOVERNANCE LAYER (HUMAN / PRG-1) REVIEWS AND APPROVES.
    // THE EXECUTION FIREWALL AUTHORIZES WITH EXECUTION PERMITS.
    // THE WORKER FABRIC EXECUTES.
    //
    // RECOMMENDED SCENARIO != RECOMMENDED DECISION
    // RECOMMENDED DECISION != APPROVAL
    // APPROVAL != EXECUTION AUTHORITY
    // EXECUTION AUTHORITY != CONDITIONLESS DISPATCH
    // CHARLIE MAY RECOMMEND. CHARLIE CANNOT APPROVE. CHARLIE CANNOT SELF-AUTHORIZE.
    // =========================================================================

    public enum DecisionCategory
    {
        GrowthAcceleration,
        CostOptimization,
        RiskMitigation,
        PricingAdjustment,
        ProductExpansion,
        OperationalPivoting
    }

    public enum ReversibilityTier
    {
        Type1_EasilyReversible,        // Two-way door: easy to undo with minimal sunk cost
        Type2_PartiallyReversible,     // Moderate unwind cost / delayed reversal
        Type3_IrreversibleOneWayDoor   // One-way door: irreversible capital or brand commitment
    }

    public enum DecisionFeasibilityStatus
    {
        Feasible,
        ConstrainedWithWaiver,
        InfeasibleViolation
    }

    public enum DecisionValidity
    {
        Valid,
        ConditionallyValid,
        InsufficientEvidence,
        StaleEvidence,
        ConflictedEvidence,
        Unknown
    }

    public enum DecisionLifecycleState
    {
        Synthesized,
        Evaluated,
        Ranked,
        Recommended,
        PendingReview,
        ApprovedByHuman,
        RejectedByHuman,
        Superseded,
        Expired
    }

    public enum DecisionEpistemicTier
    {
        FactBacked,
        ModelDerived,
        Hypothetical,
        Unknown
    }

    public enum StrategicRegime
    {
        AggressiveGrowth,
        CashConservation,
        MarginExpansion,
        DefensiveHold
    }

    // =========================================================================
    // EVIDENCE ASSESSMENT & CONFIDENCE CLAMPING (I23-G, I23-O)
    // =========================================================================

    public sealed record EvidenceAssessment
    {
        public double EvidenceQuality { get; init; } = 80.0;
        public double EvidenceCoverage { get; init; } = 80.0;
        public double CausalConfidence { get; init; } = 80.0;
        public double ForecastConfidence { get; init; } = 80.0;
        public double ScenarioConfidence { get; init; } = 80.0;
        public double CompositeDecisionConfidence { get; init; }

        public static EvidenceAssessment CreateClamped(
            double evidenceQuality,
            double evidenceCoverage,
            double causalConf,
            double forecastConf,
            double scenarioConf)
        {
            var clamped = Math.Min(
                Math.Clamp(evidenceQuality, 0.0, 100.0),
                Math.Min(
                    Math.Clamp(evidenceCoverage, 0.0, 100.0),
                    Math.Min(
                        Math.Clamp(causalConf, 0.0, 100.0),
                        Math.Min(
                            Math.Clamp(forecastConf, 0.0, 100.0),
                            Math.Clamp(scenarioConf, 0.0, 100.0)))));

            return new EvidenceAssessment
            {
                EvidenceQuality = Math.Round(evidenceQuality, 2),
                EvidenceCoverage = Math.Round(evidenceCoverage, 2),
                CausalConfidence = Math.Round(causalConf, 2),
                ForecastConfidence = Math.Round(forecastConf, 2),
                ScenarioConfidence = Math.Round(scenarioConf, 2),
                CompositeDecisionConfidence = Math.Round(clamped, 2)
            };
        }
    }

    // =========================================================================
    // IMMUTABLE VERSIONED DECISION POLICY (I23-C, I23-N)
    // =========================================================================

    public sealed record DecisionPolicy
    {
        public string PolicyId { get; init; } = "POL-DECISION-V1";
        public string Version { get; init; } = "1.0.0";
        public string TenantId { get; init; } = string.Empty;
        public double ExpectedValueWeight { get; init; } = 0.35;
        public double DownsideRiskWeight { get; init; } = 0.25;
        public double ReversibilityWeight { get; init; } = 0.15;
        public double TimeToImpactWeight { get; init; } = 0.10;
        public double ComplexityWeight { get; init; } = 0.05;
        public double StrategicFitWeight { get; init; } = 0.10;
        public double MinimumConfidenceThreshold { get; init; } = 40.0;
        public decimal RiskToleranceINR { get; init; } = 500000m;
        public TimeSpan FreshnessRequirement { get; init; } = TimeSpan.FromDays(14);
        public StrategicRegime ActiveStrategicRegime { get; init; } = StrategicRegime.MarginExpansion;
        public string AlgorithmVersion { get; init; } = "3.8.5-RELEASE";
        public DateTime EffectiveFromUtc { get; init; } = DateTime.UtcNow;
        public string IntegrityHash { get; set; } = string.Empty;

        public string ComputeIntegrityHash()
        {
            var raw = $"{PolicyId}:{Version}:{TenantId}:{ExpectedValueWeight:F2}:{DownsideRiskWeight:F2}:{ReversibilityWeight:F2}:{StrategicFitWeight:F2}:{ActiveStrategicRegime}";
            using var sha = SHA256.Create();
            IntegrityHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
            return IntegrityHash;
        }
    }

    // =========================================================================
    // DECISION CANDIDATE & SCORE BREAKDOWN (I23, I23-C, I23-E, I23-H)
    // =========================================================================

    public sealed record DecisionScoreBreakdown
    {
        public double ExpectedValueScore { get; init; }
        public double DownsideRiskScore { get; init; }
        public double ReversibilityScore { get; init; }
        public double TimeToImpactScore { get; init; }
        public double ComplexityScore { get; init; }
        public double StrategicFitScore { get; init; }
        public double CompositeScore { get; init; }
    }

    public sealed record DecisionCandidate
    {
        public string CandidateId { get; init; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; init; } = string.Empty;
        public string? RadarSignalId { get; init; }
        public string? LinkedScenarioId { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public DecisionCategory Category { get; init; } = DecisionCategory.GrowthAcceleration;
        public ReversibilityTier Reversibility { get; init; } = ReversibilityTier.Type1_EasilyReversible;
        public bool IsDoNothingBaseline { get; init; } = false;

        // Purely descriptive intended actions — ZERO execution authority (I23-B)
        public IReadOnlyList<string> DescriptiveActionSummary { get; init; } = Array.Empty<string>();

        public decimal ExpectedReturnINR { get; init; }
        public decimal DownsideRiskP10INR { get; init; }
        public decimal UpsidePotentialP90INR { get; init; }
        public TimeSpan EstimatedTimeToImpact { get; init; } = TimeSpan.FromDays(30);
        public double ImplementationComplexityScore { get; init; } = 30.0; // 0 (trivial) to 100 (extreme)

        public IReadOnlyList<string> ExpectedOutcomes { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> UnintendedConsequences { get; init; } = Array.Empty<string>();

        public DecisionFeasibilityStatus FeasibilityStatus { get; init; } = DecisionFeasibilityStatus.Feasible;
        public DecisionValidity Validity { get; set; } = DecisionValidity.Valid;
        public DecisionEpistemicTier EpistemicTier { get; init; } = DecisionEpistemicTier.ModelDerived;
        public DecisionLifecycleState LifecycleState { get; set; } = DecisionLifecycleState.Synthesized;

        public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
        public DateTime? ExpiresAtUtc { get; init; }
    }

    public sealed record DecisionEvaluationRecord
    {
        public string CandidateId { get; init; } = string.Empty;
        public DecisionScoreBreakdown Breakdown { get; init; } = new();
        public EvidenceAssessment Evidence { get; init; } = new();
        public decimal MinimaxRegretINR { get; init; }
        public DecisionFeasibilityStatus FeasibilityStatus { get; init; }
        public DecisionValidity Validity { get; init; }
        public DateTime EvaluatedAtUtc { get; init; } = DateTime.UtcNow;
    }

    /// <summary>
    /// INVARIANT: RecommendedDecision != Approval != Execution.
    /// Charlie synthesizes, evaluates, ranks, and recommends.
    /// Charlie NEVER approves. Approval strictly requires Human / PRG-1 Authority.
    /// </summary>
    public sealed record DecisionRankingResult
    {
        public string RankingId { get; init; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; init; } = string.Empty;
        public IReadOnlyList<DecisionCandidate> RankedCandidates { get; init; } = Array.Empty<DecisionCandidate>();
        public IReadOnlyDictionary<string, DecisionEvaluationRecord> Evaluations { get; init; } = new Dictionary<string, DecisionEvaluationRecord>();
        public string? RecommendedCandidateId { get; init; }
        public string? DoNothingCandidateId { get; init; }
        public string TradeoffRationale { get; init; } = string.Empty;
        public DecisionPolicy PolicySnapshot { get; init; } = new();
        public DateTime RankedAtUtc { get; init; } = DateTime.UtcNow;
        public string IntegrityHash { get; set; } = string.Empty;

        public string ComputeIntegrityHash()
        {
            var raw = $"{RankingId}:{TenantId}:{RecommendedCandidateId}:{DoNothingCandidateId}:{PolicySnapshot.IntegrityHash}:{string.Join(",", RankedCandidates.Select(c => c.CandidateId))}";
            using var sha = SHA256.Create();
            IntegrityHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
            return IntegrityHash;
        }
    }

    // =========================================================================
    // DECISION PROVENANCE RECORD (I23-I)
    // =========================================================================

    public sealed record DecisionProvenance
    {
        public string ProvenanceId { get; init; } = Guid.NewGuid().ToString("N");
        public string DecisionId { get; init; } = string.Empty;
        public string TenantId { get; init; } = string.Empty;
        public string? RadarSignalId { get; init; }
        public string? LinkedScenarioId { get; init; }
        public string? CausalHypothesisId { get; init; }
        public string? ForecastId { get; init; }
        public string PolicyHash { get; init; } = string.Empty;
        public string RankingHash { get; init; } = string.Empty;
        public DateTime SealedAtUtc { get; init; } = DateTime.UtcNow;

        public bool VerifyLineage()
        {
            return !string.IsNullOrWhiteSpace(DecisionId) &&
                   !string.IsNullOrWhiteSpace(TenantId) &&
                   !string.IsNullOrWhiteSpace(PolicyHash) &&
                   !string.IsNullOrWhiteSpace(RankingHash);
        }
    }
}
