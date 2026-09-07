using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace BusinessModelApp.Core.Domain.Runtime.Intelligence.Kernel
{
    // =========================================================================
    // EPISTEMIC SEPARATION ENUMERATIONS (INVARIANT I18-A)
    // =========================================================================

    public enum EpistemicKind
    {
        Fact,
        Inference,
        Hypothesis,
        Forecast,
        Simulation,
        Opportunity,
        Recommendation,
        Decision
    }

    public enum KpiNature
    {
        Leading,
        Lagging,
        Coincident,
        Counterfactual
    }

    public enum MetricTemporalState
    {
        Fresh,
        Decaying,
        Stale,
        Expired,
        Unknown
    }

    public enum AnomalySeverity
    {
        Informational,
        Low,
        Medium,
        High,
        Critical
    }

    public enum AnomalyDirection
    {
        None,
        Surge,
        Plunge,
        VarianceSpike,
        PatternBreak,
        Stagnation
    }

    public enum TrendDirection
    {
        Unknown,
        AcceleratingUp,
        SteadyUp,
        Flat,
        SteadyDown,
        AcceleratingDown,
        Volatile
    }

    public enum BusinessStabilityRegime
    {
        Unknown,
        Stable,
        Drifting,
        Volatile,
        Critical
    }

    public enum OutlierExclusionPolicy
    {
        None,
        Winsorize,
        TrimUpperLower,
        FilterCooksDistance
    }

    // =========================================================================
    // ANALYSIS METHOD POLICY & DETERMINISTIC REPRODUCIBILITY
    // =========================================================================

    public sealed record AnalysisMethodPolicy(
        string Method,
        int MinimumSampleSize,
        decimal MinimumVariance,
        decimal ConfidenceThreshold,
        TimeSpan BaselineWindow,
        TimeSpan FreshnessRequirement,
        OutlierExclusionPolicy OutlierPolicy,
        string Version = "1.0.0")
    {
        public static AnalysisMethodPolicy DefaultZScorePolicy => new(
            Method: "ZScoreAnomalyDetection",
            MinimumSampleSize: 7,
            MinimumVariance: 0.0001m,
            ConfidenceThreshold: 0.95m,
            BaselineWindow: TimeSpan.FromDays(30),
            FreshnessRequirement: TimeSpan.FromHours(24),
            OutlierPolicy: OutlierExclusionPolicy.TrimUpperLower,
            Version: "1.0.0");

        public static AnalysisMethodPolicy DefaultTrendPolicy => new(
            Method: "LeastSquaresLinearRegression",
            MinimumSampleSize: 5,
            MinimumVariance: 0.0001m,
            ConfidenceThreshold: 0.85m,
            BaselineWindow: TimeSpan.FromDays(14),
            FreshnessRequirement: TimeSpan.FromHours(48),
            OutlierPolicy: OutlierExclusionPolicy.None,
            Version: "1.0.0");

        public static AnalysisMethodPolicy DefaultCorrelationPolicy => new(
            Method: "PearsonCorrelationWithLagOptimization",
            MinimumSampleSize: 10,
            MinimumVariance: 0.0001m,
            ConfidenceThreshold: 0.90m,
            BaselineWindow: TimeSpan.FromDays(60),
            FreshnessRequirement: TimeSpan.FromHours(72),
            OutlierPolicy: OutlierExclusionPolicy.Winsorize,
            Version: "1.0.0");
    }

    public sealed record AnalysisRecord(
        string AnalysisId,
        string TenantId,
        string MetricId,
        IReadOnlyList<string> InputObservationHashes,
        string Method,
        string MethodVersion,
        IReadOnlyDictionary<string, string> ParameterSnapshot,
        string OutputSummary,
        string AlgorithmVersion,
        DateTime CreatedAtUtc,
        string IntegrityHash)
    {
        public static string ComputeHash(
            string tenantId,
            string metricId,
            IEnumerable<string> inputHashes,
            string method,
            string methodVersion,
            string outputSummary)
        {
            var sb = new StringBuilder();
            sb.Append(tenantId).Append(':')
              .Append(metricId).Append(':')
              .Append(method).Append(':')
              .Append(methodVersion).Append(':')
              .Append(outputSummary).Append(':')
              .Append(string.Join(",", inputHashes));

            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }

    // =========================================================================
    // KPI DEFINITION & OBSERVATION CONTRACTS
    // =========================================================================

    public sealed record KpiDefinition(
        string MetricId,
        string TenantId,
        string Name,
        string Description,
        string Unit,
        KpiNature Nature,
        TimeSpan FreshnessSla,
        decimal? TargetFloor = null,
        decimal? TargetCeiling = null,
        AnalysisMethodPolicy? CustomPolicy = null)
    {
        public AnalysisMethodPolicy EffectivePolicy => CustomPolicy ?? AnalysisMethodPolicy.DefaultZScorePolicy;
    }

    public sealed record KpiObservation(
        string ObservationId,
        string TenantId,
        string MetricId,
        decimal Value,
        DateTime ObservedAtUtc,
        string SourceRecordId,
        string AuditLedgerTx,
        string IntegrityHash,
        int SampleSize = 1,
        MetricTemporalState TemporalState = MetricTemporalState.Fresh)
    {
        public static string ComputeIntegrityHash(
            string tenantId,
            string metricId,
            decimal value,
            DateTime observedAtUtc,
            string sourceRecordId,
            string auditLedgerTx)
        {
            var raw = $"{tenantId}|{metricId}|{value}|{observedAtUtc:O}|{sourceRecordId}|{auditLedgerTx}";
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
        }
    }

    // =========================================================================
    // ANOMALY DETECTION & BASELINE QUALITY CONTRACTS
    // =========================================================================

    public sealed record BaselineQualityAssessment(
        string MetricId,
        int ObservationCount,
        decimal Variance,
        TimeSpan BaselineWindow,
        bool IsFresh,
        bool HasSufficientData,
        bool HasSufficientVariance,
        bool IsContaminated,
        bool QualityPassed,
        string FailureReason);

    public sealed record MetricAnomaly(
        string AnomalyId,
        string TenantId,
        string MetricId,
        AnomalyDirection Direction,
        AnomalySeverity Severity,
        decimal ObservedValue,
        decimal ExpectedValue,
        decimal ExpectedLowerBound,
        decimal ExpectedUpperBound,
        decimal ZScore,
        decimal ConfidenceScore,
        BaselineQualityAssessment BaselineAssessment,
        string AnalysisRecordId,
        DateTime DetectedAtUtc,
        IReadOnlyList<string> EvidenceRecordIds);

    // =========================================================================
    // TREND & ACCELERATION CONTRACTS
    // =========================================================================

    public sealed record MetricTrend(
        string TrendId,
        string TenantId,
        string MetricId,
        TrendDirection Direction,
        decimal Velocity,
        decimal Acceleration,
        decimal RSquared,
        int SampleCount,
        string AnalysisRecordId,
        DateTime EvaluatedAtUtc,
        IReadOnlyList<string> EvidenceRecordIds);

    // =========================================================================
    // METRIC RELATIONSHIP & CORRELATION CONTRACTS (INVARIANT I18-A)
    // =========================================================================

    public sealed record MetricRelationship(
        string RelationshipId,
        string TenantId,
        string MetricAId,
        string MetricBId,
        decimal PearsonCoefficient,
        int LagOffsetSeconds,
        int SampleSize,
        decimal PValue,
        decimal StabilityIndex,
        bool IsCausal, // Always FALSE in 3.8.0 per Invariant I18-A: Correlation != Causation
        string AnalysisRecordId,
        DateTime DiscoveredAtUtc);

    // =========================================================================
    // OBSERVED VS INTERPRETED BUSINESS STATE (INVARIANT I18-A)
    // =========================================================================

    public sealed record ObservedBusinessState(
        string StateId,
        string TenantId,
        decimal LiquidReserveCash,
        decimal VerifiedGrossMarginPercentage,
        int ActiveCustomerCount,
        decimal CacObserved,
        int ActiveMissionsCount,
        int ActiveWorkersCount,
        DateTime ObservedAtUtc,
        IReadOnlyList<string> TelemetryRecordIds);

    public sealed record InterpretedBusinessState(
        string InterpretationId,
        string TenantId,
        BusinessStabilityRegime StabilityRegime,
        decimal HealthIndex,
        decimal RegimeAlignmentScore,
        int ActiveAnomaliesCount,
        IReadOnlyList<string> KeyTrendSummaries,
        string AnalysisRecordId,
        DateTime EvaluatedAtUtc);

    // =========================================================================
    // EVIDENCE-LINKED INSIGHT (INVARIANT I18-B & I18-H)
    // =========================================================================

    public sealed record EvidenceLinkedInsight(
        string InsightId,
        string TenantId,
        EpistemicKind Kind,
        string Title,
        string Narrative,
        string ObservedMetricId,
        string? AnomalyId,
        string? TrendId,
        string AnalysisRecordId,
        IReadOnlyList<string> EvidenceHashes,
        decimal Confidence,
        DateTime CreatedAtUtc);
}
