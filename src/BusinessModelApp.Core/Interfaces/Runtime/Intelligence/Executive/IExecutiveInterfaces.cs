using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Decision;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Executive;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Radar;

namespace BusinessModelApp.Core.Interfaces.Runtime.Intelligence.Executive
{
    public interface IExecutiveMaterialityEngine
    {
        ExecutiveMaterialityScore EvaluateMateriality(
            string tenantId,
            double financialImpact,
            double operationalImpact,
            double strategicImpact,
            double riskExposure,
            double timeSensitivity,
            ExecutiveMaterialityPolicy? policy = null);
    }

    public interface IExecutivePriorityEngine
    {
        IReadOnlyList<ExecutiveInsight> PrioritizeForAudience(
            ExecutiveAudience audience,
            IReadOnlyList<ExecutiveInsight> candidateInsights,
            ExecutiveMaterialityPolicy policy);
    }

    /// <summary>
    /// Validates the full claim lineage:
    /// Claim -> Evidence -> Reality Envelope -> BI Analysis -> Causal/Forecast/Radar/Scenario/Decision -> Epistemic classification -> Freshness -> Policy -> Claim Validity.
    /// Prevents AI overstatement or promotion of ungrounded hypotheses.
    /// </summary>
    public interface IExecutiveClaimValidator
    {
        ExecutiveClaim ValidateClaim(
            ExecutiveClaim candidateClaim,
            ExecutiveInputSnapshot snapshot,
            ExecutiveMaterialityPolicy policy);

        IReadOnlyList<ExecutiveClaim> ValidateClaimGraph(
            IReadOnlyList<ExecutiveClaim> candidateClaims,
            ExecutiveInputSnapshot snapshot,
            ExecutiveMaterialityPolicy policy);
    }

    public interface IExecutiveEvidenceValidator
    {
        bool ValidateFreshness(DateTime evidenceTimestamp, TimeSpan maxAge);
        double ComputeClampedConfidence(params double[] ancestorConfidences);
    }

    public interface IExecutiveContradictionEngine
    {
        IReadOnlyList<ExecutiveContradictionRecord> DetectContradictions(
            IReadOnlyList<ExecutiveClaim> claims,
            IReadOnlyList<RadarSignal> radarSignals,
            IReadOnlyList<DecisionCandidate> decisionCandidates);
    }

    public interface IExecutiveGovernanceAnalyzer
    {
        IReadOnlyList<ExecutiveGovernanceItem> BuildGovernanceQueue(
            string tenantId,
            IReadOnlyList<DecisionCandidate> decisions,
            IReadOnlyList<RadarSignal> radarSignals);

        IReadOnlyList<ExecutiveAttentionItem> BuildAttentionQueue(
            string tenantId,
            IReadOnlyList<ExecutiveInsight> insights,
            ExecutiveMaterialityPolicy policy);
    }

    public interface IExecutiveBriefStore
    {
        Task SaveBriefAsync(ExecutiveBrief brief, CancellationToken cancellationToken = default);
        Task<ExecutiveBrief?> GetBriefByIdAsync(string tenantId, string briefId, CancellationToken cancellationToken = default);
        Task<ExecutiveBrief?> GetLatestBriefAsync(string tenantId, ExecutiveAudience audience, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ExecutiveBrief>> ListBriefsAsync(string tenantId, CancellationToken cancellationToken = default);
        Task<bool> AcknowledgeBriefAsync(string tenantId, string briefId, string acknowledgedBy, CancellationToken cancellationToken = default);
        Task<int> CheckAndExpireStaleBriefsAsync(string tenantId, DateTime currentUtc, CancellationToken cancellationToken = default);
    }

    public interface IExecutiveBriefSnapshotStore
    {
        Task SaveSnapshotAsync(ExecutiveInputSnapshot snapshot, CancellationToken cancellationToken = default);
        Task<ExecutiveInputSnapshot?> GetSnapshotByIdAsync(string snapshotId, CancellationToken cancellationToken = default);
    }

    public interface IExecutiveBriefComposer
    {
        ExecutiveBrief ComposeBrief(
            string tenantId,
            ExecutiveAudience audience,
            ExecutiveInputSnapshot snapshot,
            ExecutiveMaterialityPolicy policy,
            IReadOnlyList<ExecutiveMetricSummary> metrics,
            IReadOnlyList<ExecutiveInsight> insights,
            IReadOnlyList<ExecutiveClaim> claims,
            IReadOnlyList<DecisionCandidate> decisionCandidates,
            IReadOnlyList<RadarSignal> radarSignals);
    }

    public interface IExecutiveBriefOrchestrator
    {
        Task<ExecutiveBrief> GenerateExecutiveBriefAsync(
            string tenantId,
            ExecutiveAudience audience,
            ExecutiveMaterialityPolicy? customPolicy = null,
            CancellationToken cancellationToken = default);

        Task<bool> ValidateAndAcknowledgeBriefAsync(
            string tenantId,
            string briefId,
            string executiveActor,
            CancellationToken cancellationToken = default);
    }
}
