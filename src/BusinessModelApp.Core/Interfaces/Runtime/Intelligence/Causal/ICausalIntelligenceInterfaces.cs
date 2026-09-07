using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Causal;

namespace BusinessModelApp.Core.Interfaces.Runtime.Intelligence.Causal
{
    // =========================================================================
    // 1. CAUSAL GRAPH ENGINE INTERFACE
    // =========================================================================

    public interface ICausalGraphEngine
    {
        Task<CausalGraph> GetGraphAsync(string tenantId, CancellationToken ct = default);
        Task<CausalNode> AddNodeAsync(CausalNode node, CancellationToken ct = default);
        Task<CausalEdge> AddEdgeAsync(CausalEdge edge, CancellationToken ct = default);
        Task<bool> ValidateAcyclicAsync(string tenantId, CancellationToken ct = default);
        Task<IReadOnlyList<string>> GetTopologicalSortAsync(string tenantId, CancellationToken ct = default);
        Task<IReadOnlyList<CausalNode>> GetAncestorsAsync(string tenantId, string nodeId, CancellationToken ct = default);
        Task<IReadOnlyList<CausalNode>> GetDescendantsAsync(string tenantId, string nodeId, CancellationToken ct = default);
        Task<IReadOnlyList<CausalEdge>> GetIncomingEdgesAsync(string tenantId, string nodeId, CancellationToken ct = default);
        Task<IReadOnlyList<CausalEdge>> GetOutgoingEdgesAsync(string tenantId, string nodeId, CancellationToken ct = default);
    }

    // =========================================================================
    // 2. CAUSAL HYPOTHESIS ENGINE INTERFACE
    // =========================================================================

    public interface ICausalHypothesisEngine
    {
        Task<CausalHypothesis> FormulateHypothesisAsync(
            string tenantId,
            string causeMetricId,
            string effectMetricId,
            string mechanism,
            IEnumerable<string>? statedAssumptions = null,
            IEnumerable<string>? alternativeExplanationIds = null,
            CancellationToken ct = default);

        Task<CausalHypothesis> AttachEvidenceAsync(
            string hypothesisId,
            string evidenceId,
            bool isSupporting,
            EvidenceTier tier,
            CancellationToken ct = default);

        Task<CausalHypothesis?> GetHypothesisAsync(string hypothesisId, CancellationToken ct = default);
        Task<IReadOnlyList<CausalHypothesis>> GetHypothesesForMetricAsync(string tenantId, string metricId, CancellationToken ct = default);
        Task<IReadOnlyList<CausalHypothesis>> GetAllHypothesesAsync(string tenantId, CancellationToken ct = default);
        Task<CausalHypothesis> UpdateHypothesisStatusAsync(string hypothesisId, CausalHypothesisStatus status, CancellationToken ct = default);
    }

    // =========================================================================
    // 3. CONFOUNDER DETECTION INTERFACE
    // =========================================================================

    public interface IConfounderDetector
    {
        Task<ConfounderAssessment> AssessConfoundersAsync(
            string tenantId,
            string causeMetricId,
            string effectMetricId,
            CancellationToken ct = default);
    }

    // =========================================================================
    // 4. TEMPORAL CAUSAL INVESTIGATION INTERFACE
    // =========================================================================

    public interface ITemporalCausalInvestigator
    {
        Task<TemporalPrecedenceResult> InvestigateTemporalPrecedenceAsync(
            string tenantId,
            string metricA,
            string metricB,
            TimeSpan? maxLag = null,
            CancellationToken ct = default);
    }

    // =========================================================================
    // 5. INTERVENTION / COUNTERFACTUAL SIMULATOR INTERFACE (DO-CALCULUS)
    // =========================================================================

    public interface IInterventionSimulator
    {
        Task<CausalIdentifiabilityStatus> CheckIdentifiabilityAsync(
            string tenantId,
            string targetMetricId,
            IEnumerable<string>? conditioningNodes = null,
            CancellationToken ct = default);

        Task<InterventionSimulationResult> SimulateInterventionAsync(
            InterventionSpec spec,
            CancellationToken ct = default);
    }

    // =========================================================================
    // 6. CAUSAL EVIDENCE EVALUATOR & SCORECARD INTERFACE
    // =========================================================================

    public interface ICausalEvidenceEvaluator
    {
        Task<CausalEvidenceScorecard> EvaluateEvidenceAsync(
            string tenantId,
            string hypothesisId,
            CancellationToken ct = default);
    }

    // =========================================================================
    // 7. UNIFIED CAUSAL INTELLIGENCE ENGINE (ORCHESTRATOR)
    // =========================================================================

    public interface ICausalIntelligenceEngine
    {
        ICausalGraphEngine Graph { get; }
        ICausalHypothesisEngine Hypotheses { get; }
        IConfounderDetector Confounders { get; }
        ITemporalCausalInvestigator Temporal { get; }
        IInterventionSimulator Interventions { get; }
        ICausalEvidenceEvaluator Evaluator { get; }

        Task<CausalHypothesis> EvaluateFullCausalExplanationAsync(
            string tenantId,
            string causeMetricId,
            string effectMetricId,
            string initialMechanism,
            CancellationToken ct = default);
    }
}
