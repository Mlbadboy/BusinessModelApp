using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Kernel;

namespace BusinessModelApp.Core.Domain.Runtime.Intelligence.Causal
{
    // =========================================================================
    // CAUSAL SOVEREIGNTY ENUMERATIONS (INVARIANT I19)
    // =========================================================================

    public enum CausalNodeType
    {
        Metric,
        Intervention,
        ExternalFactor,
        LatentFactor
    }

    public enum CausalEdgeStatus
    {
        Hypothesized,
        Inferred,
        Observed
    }

    public enum CausalHypothesisStatus
    {
        Draft,
        Hypothesized,
        Plausible,
        Weakened,
        Refuted,
        PreservedAsUnknown
    }

    public enum ConfounderRiskLevel
    {
        None,
        Low,
        Medium,
        High,
        Critical
    }

    public enum TemporalPrecedenceDirection
    {
        Leading,
        Lagging,
        Coincident,
        Indeterminate
    }

    public enum CausalIdentifiabilityStatus
    {
        Identified,
        PartiallyIdentified,
        NotIdentified,
        Unknown
    }

    public enum EvidenceTier
    {
        Anecdotal = 0,
        ObservationalCorrelation = 1,
        TemporalPrecedence = 2,
        NaturalExperiment = 3,
        ControlledExperiment = 4
    }

    // =========================================================================
    // GRAPH STRUCTURE CONTRACTS
    // =========================================================================

    public sealed record CausalNode(
        string NodeId,
        string TenantId,
        string MetricId,
        string Label,
        CausalNodeType NodeType,
        EpistemicKind EpistemicKind,
        DateTime CreatedAtUtc);

    public sealed record CausalEdge(
        string EdgeId,
        string TenantId,
        string SourceNodeId,
        string TargetNodeId,
        decimal EstimatedStrength,
        string MechanismDescription,
        CausalEdgeStatus Status,
        decimal Confidence,
        bool IsBackdoorPath = false);

    public sealed record CausalGraph(
        string GraphId,
        string TenantId,
        IReadOnlyList<CausalNode> Nodes,
        IReadOnlyList<CausalEdge> Edges,
        DateTime LastUpdatedUtc)
    {
        public bool ContainsNode(string nodeId) => Nodes.Any(n => n.NodeId == nodeId);
        public bool ContainsEdge(string sourceNodeId, string targetNodeId) =>
            Edges.Any(e => e.SourceNodeId == sourceNodeId && e.TargetNodeId == targetNodeId);
    }

    // =========================================================================
    // CAUSAL HYPOTHESIS & EVIDENCE CONTRACTS
    // =========================================================================

    public sealed record CausalHypothesis(
        string HypothesisId,
        string TenantId,
        string CauseMetricId,
        string EffectMetricId,
        string MechanismDescription,
        IReadOnlyList<string> SupportingEvidenceIds,
        IReadOnlyList<string> ContradictoryEvidenceIds,
        IReadOnlyList<string> StatedAssumptions,
        IReadOnlyList<string> MissingEvidence,
        IReadOnlyList<string> AlternativeExplanationIds,
        decimal CausalConfidenceScore,
        CausalHypothesisStatus Status,
        EvidenceTier HighestEvidenceTier,
        DateTime CreatedAtUtc,
        string IntegrityHash)
    {
        public static string ComputeIntegrityHash(
            string tenantId,
            string causeMetricId,
            string effectMetricId,
            string mechanism,
            decimal confidence,
            CausalHypothesisStatus status,
            EvidenceTier tier,
            IEnumerable<string> evidenceIds)
        {
            var raw = $"{tenantId}:{causeMetricId}:{effectMetricId}:{mechanism}:{confidence:F4}:{status}:{tier}:{string.Join(',', evidenceIds ?? Array.Empty<string>())}";
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }
    }

    // =========================================================================
    // CONFOUNDER & TEMPORAL EVALUATION CONTRACTS
    // =========================================================================

    public sealed record ConfounderAssessment(
        string AssessmentId,
        string TenantId,
        string CauseMetricId,
        string EffectMetricId,
        bool IsConfounded,
        IReadOnlyList<string> ConfounderMetricIds,
        ConfounderRiskLevel RiskLevel,
        IReadOnlyList<string> RequiredAdjustmentSet,
        string MitigationNotes,
        DateTime AssessedAtUtc);

    public sealed record TemporalPrecedenceResult(
        string ResultId,
        string TenantId,
        string MetricA,
        string MetricB,
        TimeSpan OptimalLag,
        decimal PrecedenceScore,
        decimal CrossCorrelationAtLag,
        TemporalPrecedenceDirection Direction,
        bool MeetsCandidatePrecedenceThreshold,
        string CaveatMessage,
        DateTime EvaluatedAtUtc);

    // =========================================================================
    // INTERVENTION & COUNTERFACTUAL CONTRACTS (DO-CALCULUS)
    // =========================================================================

    public sealed record InterventionSpec(
        string SpecId,
        string TenantId,
        string TargetMetricId,
        decimal TargetValueDelta,
        decimal BaselineValue,
        IReadOnlyList<string> ConditioningNodes);

    public sealed record InterventionSimulationResult(
        string ResultId,
        string TenantId,
        string TargetMetricId,
        decimal Delta,
        CausalIdentifiabilityStatus IdentifiabilityStatus,
        IReadOnlyDictionary<string, decimal> PredictedMetricDeltas,
        decimal UncertaintyRadius,
        EpistemicKind EpistemicKind,
        IReadOnlyList<string> ActiveAssumptions,
        string Explanation,
        DateTime SimulatedAtUtc);

    // =========================================================================
    // CAUSAL EVIDENCE SCORECARD (METROLOGY & GATING)
    // =========================================================================

    public sealed record CausalEvidenceScorecard(
        string ScorecardId,
        string TenantId,
        string HypothesisId,
        decimal DirectEvidenceScore,
        decimal TemporalEvidenceScore,
        decimal StatisticalEvidenceScore,
        decimal ExperimentalEvidenceScore,
        decimal ConfounderPenalty,
        decimal CompositeConfidenceScore,
        EvidenceTier HighestTier,
        bool GatingPassed,
        CausalHypothesisStatus RecommendedStatus,
        string GatingRationale,
        DateTime EvaluatedAtUtc);
}
