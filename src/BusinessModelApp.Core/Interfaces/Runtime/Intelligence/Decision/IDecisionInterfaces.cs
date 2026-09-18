using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Decision;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Radar;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Scenario;

namespace BusinessModelApp.Core.Interfaces.Runtime.Intelligence.Decision
{
    public interface IDecisionScorer
    {
        DecisionScoreBreakdown ScoreCandidate(
            DecisionCandidate candidate,
            DecisionPolicy policy,
            EvidenceAssessment evidence);

        EvidenceAssessment AssessEvidence(
            string tenantId,
            double evidenceQuality,
            double evidenceCoverage,
            double causalConf,
            double forecastConf,
            double scenarioConf);
    }

    public interface IDecisionReversibilityEvaluator
    {
        ReversibilityTier ClassifyReversibility(DecisionCandidate candidate);

        decimal CalculateMinimaxRegret(
            DecisionCandidate candidate,
            IReadOnlyList<DecisionCandidate> allCandidates);
    }

    public interface IDecisionCandidateSynthesizer
    {
        Task<IReadOnlyList<DecisionCandidate>> SynthesizeCandidatesAsync(
            string tenantId,
            RadarSignal? signal,
            ScenarioComparisonResult? scenarioFrontier,
            CancellationToken ct = default);
    }

    public interface IDecisionStore
    {
        Task SaveCandidateAsync(DecisionCandidate candidate, CancellationToken ct = default);
        Task<DecisionCandidate?> GetCandidateAsync(string tenantId, string candidateId, CancellationToken ct = default);
        Task<IReadOnlyList<DecisionCandidate>> ListCandidatesAsync(string tenantId, DecisionLifecycleState? state = null, CancellationToken ct = default);
        Task<bool> UpdateLifecycleStateAsync(
            string tenantId,
            string candidateId,
            DecisionLifecycleState newState,
            string reason,
            bool isHumanActor = false,
            CancellationToken ct = default);

        Task SaveRankingResultAsync(DecisionRankingResult ranking, CancellationToken ct = default);
        Task<DecisionRankingResult?> GetLatestRankingAsync(string tenantId, CancellationToken ct = default);

        Task SaveProvenanceAsync(DecisionProvenance provenance, CancellationToken ct = default);
        Task<DecisionProvenance?> GetProvenanceAsync(string tenantId, string decisionId, CancellationToken ct = default);

        Task CheckAndExpireStaleDecisionsAsync(string tenantId, TimeSpan maxAge, CancellationToken ct = default);
    }

    public interface IDecisionOrchestrator
    {
        Task<DecisionRankingResult> RankDecisionsAsync(
            string tenantId,
            IReadOnlyList<DecisionCandidate> candidates,
            DecisionPolicy? policy = null,
            CancellationToken ct = default);

        Task<DecisionRankingResult> SynthesizeAndRankFromRadarAndScenarioAsync(
            string tenantId,
            string radarSignalId,
            CancellationToken ct = default);
    }
}
