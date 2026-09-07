using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Forecasting;

namespace BusinessModelApp.Core.Interfaces.Runtime.Intelligence.Forecasting
{
    // =========================================================================
    // 1. FORECAST MODEL REGISTRY INTERFACE
    // =========================================================================

    public interface IForecastModelRegistry
    {
        Task RegisterPolicyAsync(ForecastMethodPolicy policy, CancellationToken ct = default);
        Task<ForecastMethodPolicy> GetPolicyAsync(string tenantId, ForecastModelAlgorithm algorithm, CancellationToken ct = default);
        Task<IReadOnlyList<ForecastMethodPolicy>> GetAllPoliciesAsync(string tenantId, CancellationToken ct = default);
        Task SetChampionModelAsync(string tenantId, string metricId, string modelId, CancellationToken ct = default);
        Task<string?> GetChampionModelAsync(string tenantId, string metricId, CancellationToken ct = default);
    }

    // =========================================================================
    // 2. REGIME CHANGE & STRUCTURAL BREAK DETECTOR INTERFACE
    // =========================================================================

    public interface IRegimeChangeDetector
    {
        Task<RegimeAssessment> DetectRegimeShiftAsync(string tenantId, string metricId, CancellationToken ct = default);
    }

    // =========================================================================
    // 3. BACKTEST ENGINE INTERFACE (ACCURACY & CALIBRATION)
    // =========================================================================

    public interface IBacktestEngine
    {
        Task<BacktestScorecard> BacktestModelAsync(
            string tenantId,
            string metricId,
            ForecastModelAlgorithm algorithm,
            int testSteps = 5,
            CancellationToken ct = default);
    }

    // =========================================================================
    // 4. DRIFT & CALIBRATION DECAY MONITOR INTERFACE
    // =========================================================================

    public interface IForecastDriftMonitor
    {
        Task RecordForecastIntervalsAsync(string tenantId, string metricId, IReadOnlyList<PredictionInterval> intervals, CancellationToken ct = default);
        Task<DriftReport> EvaluateDriftAsync(string tenantId, string metricId, CancellationToken ct = default);
    }

    // =========================================================================
    // 5. PROVENANCE-ADDRESSED FORECAST RECORD STORE INTERFACE
    // =========================================================================

    public interface IForecastRecordStore
    {
        Task StoreForecastAsync(ForecastOutput forecast, ForecastProvenanceSnapshot provenance, CancellationToken ct = default);
        Task<ForecastOutput?> GetForecastAsync(string forecastId, CancellationToken ct = default);
        Task<IReadOnlyList<ForecastOutput>> GetForecastsForMetricAsync(string tenantId, string metricId, CancellationToken ct = default);
        Task<ForecastProvenanceSnapshot?> GetProvenanceAsync(string forecastId, CancellationToken ct = default);
    }

    // =========================================================================
    // 6. FORECAST ENGINE INTERFACE
    // =========================================================================

    public interface IForecastEngine
    {
        Task<ForecastApplicabilityStatus> AssessApplicabilityAsync(
            string tenantId,
            string metricId,
            ForecastModelAlgorithm algorithm,
            int horizon,
            CancellationToken ct = default);

        Task<ForecastOutput> GenerateForecastAsync(
            string tenantId,
            string metricId,
            ForecastModelAlgorithm algorithm,
            int horizon,
            CancellationToken ct = default);
    }

    // =========================================================================
    // 7. FORECASTING METROLOGY ORCHESTRATOR INTERFACE
    // =========================================================================

    public interface IForecastingMetrologyOrchestrator
    {
        IForecastModelRegistry Registry { get; }
        IForecastEngine Engine { get; }
        IRegimeChangeDetector RegimeDetector { get; }
        IBacktestEngine Backtester { get; }
        IForecastDriftMonitor DriftMonitor { get; }
        IForecastRecordStore RecordStore { get; }

        Task<ForecastOutput> GenerateGovernedForecastAsync(
            string tenantId,
            string metricId,
            int horizon,
            CancellationToken ct = default);
    }
}
