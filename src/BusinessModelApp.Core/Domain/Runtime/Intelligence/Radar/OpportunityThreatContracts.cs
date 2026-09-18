using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace BusinessModelApp.Core.Domain.Runtime.Intelligence.Radar
{
    // =========================================================================
    // SIGNIFICANCE SOVEREIGNTY ENUMERATIONS (INVARIANT I21)
    // =========================================================================

    public enum RadarSignalType
    {
        Opportunity,
        Threat
    }

    public enum OpportunityCategory
    {
        RevenueExpansion,
        UpsellCrossSell,
        PricingOptimization,
        CustomerRetention,
        ProductGap,
        MarketExpansion,
        OperationalEfficiency
    }

    public enum ThreatCategory
    {
        CommercialDeterioration,
        ConversionDecline,
        MarginCompression,
        LiquidityRisk,
        CompetitivePriceWar,
        DemandShock,
        OperationalDisruption
    }

    public enum SignalUrgency
    {
        Low,
        Medium,
        High,
        Immediate
    }

    public enum RadarPriority
    {
        Informational,
        Low,
        Medium,
        High,
        Critical
    }

    public enum RadarEpistemicTier
    {
        FactBacked,
        Inference,
        Hypothesis,
        ForecastDerived,
        Unknown
    }

    public enum RadarSignalLifecycleState
    {
        Detected,
        Validating,
        Active,
        UnderReview,
        ActionRecommended,
        Mitigated,
        Dismissed,
        Expired
    }

    public enum ConstraintFeasibilityStatus
    {
        Satisfied,
        Constrained,
        Violated,
        Unknown
    }

    // =========================================================================
    // VERSIONED RADAR SIGNIFICANCE POLICY (I21-F)
    // =========================================================================

    public class RadarSignificancePolicy
    {
        public string PolicyId { get; set; } = "POLICY-RADAR-DEFAULT-V1";
        public string Version { get; set; } = "1.0.0";
        public string TenantId { get; set; } = string.Empty;

        // Normalized weights (sum = 1.0)
        public double ImpactWeight { get; set; } = 0.35;
        public double ProbabilityWeight { get; set; } = 0.25;
        public double UrgencyWeight { get; set; } = 0.15;
        public double PersistenceWeight { get; set; } = 0.15;
        public double ExposureWeight { get; set; } = 0.10;

        // Confidence Gate Threshold
        public double MinimumConfidenceThreshold { get; set; } = 40.0;

        // Temporal Decay Parameters
        public double DecayRateLambda { get; set; } = 0.05; // 5% decay per epoch without evidence
        public int FreshnessRequirementDays { get; set; } = 14;
        public double EvidenceReinforcementBoost { get; set; } = 15.0;

        public DateTime EffectiveDateUtc { get; set; } = DateTime.UtcNow;
        public string IntegrityHash { get; set; } = string.Empty;

        public string ComputeIntegrityHash()
        {
            var raw = $"{PolicyId}|{Version}|{TenantId}|{ImpactWeight:F4}|{ProbabilityWeight:F4}|{UrgencyWeight:F4}|{PersistenceWeight:F4}|{ExposureWeight:F4}|{MinimumConfidenceThreshold:F2}|{DecayRateLambda:F4}";
            using var sha = SHA256.Create();
            IntegrityHash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw))).Replace("-", "").ToLowerInvariant();
            return IntegrityHash;
        }
    }

    // =========================================================================
    // DETERMINISTIC SIGNIFICANCE BREAKDOWN (I21-F)
    // =========================================================================

    public class RadarSignificanceBreakdown
    {
        public double ImpactScore { get; set; }        // [0, 100]
        public double ProbabilityScore { get; set; }   // [0, 100]
        public double UrgencyScore { get; set; }       // [0, 100]
        public double PersistenceScore { get; set; }   // [0, 100]
        public double ExposureScore { get; set; }      // [0, 100]
        public double ConfidenceScore { get; set; }    // [0, 100]

        public double SignificanceScore { get; set; }  // [0, 100]
        public RadarPriority Priority { get; set; } = RadarPriority.Informational;
    }

    // =========================================================================
    // CAUSAL & FORECAST LINKAGE (I21-D, I21-C)
    // =========================================================================

    public class RadarSignalLinkage
    {
        public List<string> MetricObservationIds { get; set; } = new();
        public List<string> AnalysisRecordIds { get; set; } = new();
        public string? CausalHypothesisId { get; set; }
        public double? CausalHypothesisConfidence { get; set; }
        public string? ForecastOutputId { get; set; }
        public double? ForecastConfidence { get; set; }
        public string? ConstraintSnapshotId { get; set; }
        public string? RealityEnvelopeId { get; set; }
    }

    // =========================================================================
    // UNIFIED RADAR SIGNAL (I21, I21-A, I21-B, I21-H, I21-L)
    // =========================================================================

    public class RadarSignal
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public RadarSignalType Type { get; set; }
        public OpportunityCategory? OpportunityCategory { get; set; }
        public ThreatCategory? ThreatCategory { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal EstimatedMonetaryImpactINR { get; set; }

        public RadarEpistemicTier EpistemicTier { get; set; } = RadarEpistemicTier.Unknown;
        public RadarSignalLifecycleState LifecycleState { get; set; } = RadarSignalLifecycleState.Detected;
        public SignalUrgency Urgency { get; set; } = SignalUrgency.Low;
        public RadarPriority Priority { get; set; } = RadarPriority.Informational;

        public RadarSignificanceBreakdown Breakdown { get; set; } = new();
        public RadarSignalLinkage Linkage { get; set; } = new();

        // Constraint Integration (I21-G)
        public ConstraintFeasibilityStatus ConstraintStatus { get; set; } = ConstraintFeasibilityStatus.Unknown;
        public List<string> SafeAlternatives { get; set; } = new();
        public string? ConstraintViolationReason { get; set; }

        // Temporal Decay & Reinforcement (I21-K)
        public double InitialStrength { get; set; } = 100.0;
        public double CurrentStrength { get; set; } = 100.0;
        public int ObservationReinforcementCount { get; set; } = 1;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime LastEvaluatedAtUtc { get; set; } = DateTime.UtcNow;

        // Cryptographic Provenance (I21-I)
        public string ProvenanceHash { get; set; } = string.Empty;

        // Invariant I21-H: Signals possess ZERO execution authority
        public bool IsActionAuthorized => false;
        public bool IsEmergencyBypassAllowed => false;
    }

    // =========================================================================
    // CRYPTOGRAPHIC RADAR PROVENANCE RECORD (I21-I)
    // =========================================================================

    public class RadarAnalysisRecord
    {
        public string AnalysisId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string SignalId { get; set; } = string.Empty;

        public string InputObservationHashes { get; set; } = string.Empty;
        public string CausalEvidenceHashes { get; set; } = string.Empty;
        public string ForecastHashes { get; set; } = string.Empty;
        public string ConstraintSnapshotHash { get; set; } = string.Empty;

        public string ScoringPolicyId { get; set; } = string.Empty;
        public string ScoringPolicyVersion { get; set; } = string.Empty;
        public string AlgorithmVersion { get; set; } = "3.8.3-RADAR-V1";

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public string IntegrityHash { get; set; } = string.Empty;

        public string ComputeIntegrityHash()
        {
            var raw = $"{AnalysisId}|{TenantId}|{SignalId}|{InputObservationHashes}|{CausalEvidenceHashes}|{ForecastHashes}|{ConstraintSnapshotHash}|{ScoringPolicyId}|{ScoringPolicyVersion}|{AlgorithmVersion}";
            using var sha = SHA256.Create();
            IntegrityHash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw))).Replace("-", "").ToLowerInvariant();
            return IntegrityHash;
        }
    }
}
