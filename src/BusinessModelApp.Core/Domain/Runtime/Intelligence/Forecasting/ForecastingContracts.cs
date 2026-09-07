using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Kernel;

namespace BusinessModelApp.Core.Domain.Runtime.Intelligence.Forecasting
{
    // =========================================================================
    // PREDICTIVE SOVEREIGNTY ENUMERATIONS (INVARIANT I20)
    // =========================================================================

    public enum ForecastModelAlgorithm
    {
        LinearTrend,
        HoltWinters,
        AutoRegressive,
        EnsembleConsensus
    }

    public enum ForecastApplicabilityStatus
    {
        Valid,
        Degraded,
        NotApplicable,
        Unknown
    }

    public enum RegimeShiftStatus
    {
        None,
        Suspected,
        Detected,
        Unknown
    }

    public enum IntervalCalibrationMethod
    {
        ParametricStandardErrorScaling,
        ResidualBootstrapQuantiles,
        EmpiricalConformal
    }

    public enum CalibrationTier
    {
        Exemplary,
        WellCalibrated,
        Underconfident,
        Overconfident,
        Uncalibrated
    }

    public enum DriftSeverity
    {
        None,
        Low,
        Medium,
        High,
        Critical
    }

    // =========================================================================
    // METHOD POLICY CONTRACTS (AMENDMENT 1)
    // =========================================================================

    public sealed record ForecastMethodPolicy(
        string PolicyId,
        string TenantId,
        ForecastModelAlgorithm Algorithm,
        int MinimumSampleSize,
        decimal MinimumVariance,
        TimeSpan FreshnessSla,
        int MaxHorizon,
        IntervalCalibrationMethod DefaultIntervalMethod,
        decimal RegimeShiftUncertaintyMultiplier,
        decimal TargetCoverageProbability,
        string Version)
    {
        public static ForecastMethodPolicy DefaultLinearPolicy => new(
            PolicyId: "pol-linear-default",
            TenantId: "global",
            Algorithm: ForecastModelAlgorithm.LinearTrend,
            MinimumSampleSize: 4,
            MinimumVariance: 0.0001m,
            FreshnessSla: TimeSpan.FromHours(48),
            MaxHorizon: 14,
            DefaultIntervalMethod: IntervalCalibrationMethod.ParametricStandardErrorScaling,
            RegimeShiftUncertaintyMultiplier: 2.5m,
            TargetCoverageProbability: 0.80m,
            Version: "1.0.0");

        public static ForecastMethodPolicy DefaultHoltWintersPolicy => new(
            PolicyId: "pol-hw-default",
            TenantId: "global",
            Algorithm: ForecastModelAlgorithm.HoltWinters,
            MinimumSampleSize: 14,
            MinimumVariance: 0.0001m,
            FreshnessSla: TimeSpan.FromHours(48),
            MaxHorizon: 30,
            DefaultIntervalMethod: IntervalCalibrationMethod.ResidualBootstrapQuantiles,
            RegimeShiftUncertaintyMultiplier: 2.5m,
            TargetCoverageProbability: 0.80m,
            Version: "1.0.0");

        public static ForecastMethodPolicy DefaultAutoRegressivePolicy => new(
            PolicyId: "pol-ar-default",
            TenantId: "global",
            Algorithm: ForecastModelAlgorithm.AutoRegressive,
            MinimumSampleSize: 10,
            MinimumVariance: 0.0001m,
            FreshnessSla: TimeSpan.FromHours(48),
            MaxHorizon: 21,
            DefaultIntervalMethod: IntervalCalibrationMethod.ParametricStandardErrorScaling,
            RegimeShiftUncertaintyMultiplier: 2.5m,
            TargetCoverageProbability: 0.80m,
            Version: "1.0.0");

        public static ForecastMethodPolicy DefaultEnsemblePolicy => new(
            PolicyId: "pol-ensemble-default",
            TenantId: "global",
            Algorithm: ForecastModelAlgorithm.EnsembleConsensus,
            MinimumSampleSize: 10,
            MinimumVariance: 0.0001m,
            FreshnessSla: TimeSpan.FromHours(48),
            MaxHorizon: 30,
            DefaultIntervalMethod: IntervalCalibrationMethod.EmpiricalConformal,
            RegimeShiftUncertaintyMultiplier: 2.5m,
            TargetCoverageProbability: 0.80m,
            Version: "1.0.0");
    }

    // =========================================================================
    // PREDICTION INTERVAL & FORECAST CONTRACTS (AMENDMENT 2, INVARIANT I20-C)
    // =========================================================================

    public sealed record PredictionInterval(
        int StepAhead,
        DateTime TargetTimestampUtc,
        decimal LowerBound,
        decimal MedianValue,
        decimal UpperBound,
        decimal ConfidenceLevel,
        IntervalCalibrationMethod CalibrationMethod,
        decimal WinklerScore = 0.0m)
    {
        public decimal IntervalWidth => UpperBound - LowerBound;
    }

    public sealed record ForecastOutput(
        string ForecastId,
        string TenantId,
        string MetricId,
        string ModelId,
        string ModelVersion,
        int HorizonSteps,
        IReadOnlyList<PredictionInterval> Intervals,
        decimal UncertaintyRadius,
        ForecastApplicabilityStatus ApplicabilityStatus,
        IReadOnlyList<string> StatedAssumptions,
        DateTime GeneratedAtUtc,
        string ProvenanceHash,
        EpistemicKind EpistemicKind = EpistemicKind.Forecast);

    // =========================================================================
    // REGIME SHIFT ASSESSMENT (AMENDMENT 4, INVARIANT I20-G)
    // =========================================================================

    public sealed record RegimeAssessment(
        string AssessmentId,
        string TenantId,
        string MetricId,
        RegimeShiftStatus Status,
        decimal ShiftMagnitude,
        bool MeanShiftDetected,
        bool VarianceShiftDetected,
        string EvidenceNotes,
        DateTime EvaluatedAtUtc);

    // =========================================================================
    // BACKTEST SCORECARD & PROBABILISTIC CALIBRATION (AMENDMENT 5, INVARIANT I20-L)
    // =========================================================================

    public sealed record BacktestScorecard(
        string ScorecardId,
        string TenantId,
        string MetricId,
        string ModelId,
        decimal PointRmse,
        decimal PointMape,
        decimal PointMae,
        decimal EmpiricalCoverage,
        decimal MeanWeightedIntervalScore,
        CalibrationTier CalibrationTier,
        int TestedSteps,
        DateTime EvaluatedAtUtc);

    // =========================================================================
    // DRIFT MONITORING (INVARIANT I20-N)
    // =========================================================================

    public sealed record DriftReport(
        string ReportId,
        string TenantId,
        string MetricId,
        decimal DriftScore,
        DriftSeverity Severity,
        int BreachCount,
        int EvaluatedObservationsCount,
        bool RequiresModelReevaluation,
        DateTime EvaluatedAtUtc);

    // =========================================================================
    // PROVENANCE SNAPSHOT (AMENDMENT 6 & DIRECTIVE, INVARIANTS I20-I, I20-O)
    // =========================================================================

    public sealed record ForecastProvenanceSnapshot(
        string TenantId,
        string MetricId,
        string ModelId,
        string ModelVersion,
        string AlgorithmVersion,
        IReadOnlyList<string> InputObservationHashes,
        IReadOnlyDictionary<string, string> ExecutionPolicySnapshot,
        int? RandomSeed,
        string TransformationVersion,
        string? NondeterministicComponent = null)
    {
        public string ComputeHash()
        {
            var sb = new StringBuilder();
            sb.Append(TenantId).Append(':')
              .Append(MetricId).Append(':')
              .Append(ModelId).Append(':')
              .Append(ModelVersion).Append(':')
              .Append(AlgorithmVersion).Append(':')
              .Append(string.Join(',', InputObservationHashes ?? Array.Empty<string>())).Append(':');

            if (ExecutionPolicySnapshot != null)
            {
                foreach (var kvp in ExecutionPolicySnapshot.OrderBy(k => k.Key))
                {
                    sb.Append(kvp.Key).Append('=').Append(kvp.Value).Append(';');
                }
            }

            sb.Append(':')
              .Append(RandomSeed?.ToString() ?? "none").Append(':')
              .Append(TransformationVersion).Append(':')
              .Append(NondeterministicComponent ?? "none");

            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()))).ToLowerInvariant();
        }
    }
}
