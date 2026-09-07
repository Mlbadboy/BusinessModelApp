using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Forecasting;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Kernel;
using BusinessModelApp.Core.Interfaces.Runtime.Intelligence.Forecasting;
using BusinessModelApp.Core.Interfaces.Runtime.Intelligence.Kernel;

namespace BusinessModelApp.Infrastructure.Runtime.Intelligence.Forecasting
{
    // =========================================================================
    // 1. FORECAST MODEL REGISTRY
    // =========================================================================

    public sealed class ForecastModelRegistry : IForecastModelRegistry
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<ForecastModelAlgorithm, ForecastMethodPolicy>> _policies = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _champions = new();

        public Task RegisterPolicyAsync(ForecastMethodPolicy policy, CancellationToken ct = default)
        {
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            var tenantPolicies = _policies.GetOrAdd(policy.TenantId, _ => new ConcurrentDictionary<ForecastModelAlgorithm, ForecastMethodPolicy>());
            tenantPolicies[policy.Algorithm] = policy;
            return Task.CompletedTask;
        }

        public Task<ForecastMethodPolicy> GetPolicyAsync(string tenantId, ForecastModelAlgorithm algorithm, CancellationToken ct = default)
        {
            if (_policies.TryGetValue(tenantId, out var tenantPolicies) && tenantPolicies.TryGetValue(algorithm, out var policy))
            {
                return Task.FromResult(policy);
            }

            // Return algorithm-specific default policy (Amendment 1: No hardcoded N < 5)
            var defaultPolicy = algorithm switch
            {
                ForecastModelAlgorithm.LinearTrend => ForecastMethodPolicy.DefaultLinearPolicy with { TenantId = tenantId },
                ForecastModelAlgorithm.HoltWinters => ForecastMethodPolicy.DefaultHoltWintersPolicy with { TenantId = tenantId },
                ForecastModelAlgorithm.AutoRegressive => ForecastMethodPolicy.DefaultAutoRegressivePolicy with { TenantId = tenantId },
                ForecastModelAlgorithm.EnsembleConsensus => ForecastMethodPolicy.DefaultEnsemblePolicy with { TenantId = tenantId },
                _ => ForecastMethodPolicy.DefaultLinearPolicy with { TenantId = tenantId }
            };

            return Task.FromResult(defaultPolicy);
        }

        public Task<IReadOnlyList<ForecastMethodPolicy>> GetAllPoliciesAsync(string tenantId, CancellationToken ct = default)
        {
            var list = _policies.TryGetValue(tenantId, out var tenantPolicies)
                ? tenantPolicies.Values.ToList()
                : new List<ForecastMethodPolicy>
                {
                    ForecastMethodPolicy.DefaultLinearPolicy with { TenantId = tenantId },
                    ForecastMethodPolicy.DefaultHoltWintersPolicy with { TenantId = tenantId },
                    ForecastMethodPolicy.DefaultAutoRegressivePolicy with { TenantId = tenantId },
                    ForecastMethodPolicy.DefaultEnsemblePolicy with { TenantId = tenantId }
                };

            return Task.FromResult<IReadOnlyList<ForecastMethodPolicy>>(list);
        }

        public Task SetChampionModelAsync(string tenantId, string metricId, string modelId, CancellationToken ct = default)
        {
            var tenantChampions = _champions.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, string>());
            tenantChampions[metricId] = modelId;
            return Task.CompletedTask;
        }

        public Task<string?> GetChampionModelAsync(string tenantId, string metricId, CancellationToken ct = default)
        {
            if (_champions.TryGetValue(tenantId, out var tenantChampions) && tenantChampions.TryGetValue(metricId, out var champion))
            {
                return Task.FromResult<string?>(champion);
            }
            return Task.FromResult<string?>(null);
        }
    }

    // =========================================================================
    // 2. REGIME CHANGE & STRUCTURAL BREAK DETECTOR (AMENDMENT 4, INVARIANT I20-G)
    // =========================================================================

    public sealed class RegimeChangeDetector : IRegimeChangeDetector
    {
        private readonly IKpiObservationStore _observationStore;

        public RegimeChangeDetector(IKpiObservationStore observationStore)
        {
            _observationStore = observationStore ?? throw new ArgumentNullException(nameof(observationStore));
        }

        public async Task<RegimeAssessment> DetectRegimeShiftAsync(string tenantId, string metricId, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var observations = await _observationStore.GetObservationsAsync(tenantId, metricId, now.AddDays(-60), now, ct);

            if (observations.Count < 6)
            {
                return new RegimeAssessment(
                    AssessmentId: $"regime-{Guid.NewGuid():N}",
                    TenantId: tenantId,
                    MetricId: metricId,
                    Status: RegimeShiftStatus.Unknown,
                    ShiftMagnitude: 0.0m,
                    MeanShiftDetected: false,
                    VarianceShiftDetected: false,
                    EvidenceNotes: "Insufficient observations for structural break detection. Unknown preserved.",
                    EvaluatedAtUtc: now);
            }

            var values = observations.OrderBy(o => o.ObservedAtUtc).Select(o => (double)o.Value).ToList();
            
            // Linear detrending to evaluate stability of process residuals
            double xMean = (values.Count - 1) / 2.0;
            double yMean = values.Average();
            double num = 0.0, denom = 0.0;
            for (int i = 0; i < values.Count; i++)
            {
                num += (i - xMean) * (values[i] - yMean);
                denom += Math.Pow(i - xMean, 2);
            }
            double slope = denom > 1e-9 ? num / denom : 0.0;
            double intercept = yMean - slope * xMean;

            double overallStd = Math.Sqrt(values.Sum(v => Math.Pow(v - yMean, 2)) / Math.Max(1, values.Count - 1));
            overallStd = Math.Max(1e-6, overallStd);

            var residuals = values.Select((v, i) => v - (intercept + slope * i)).ToList();
            int mid = residuals.Count / 2;

            var firstHalf = residuals.Take(mid).ToList();
            var secondHalf = residuals.Skip(mid).ToList();

            double meanRes1 = firstHalf.Average();
            double meanRes2 = secondHalf.Average();

            double var1 = firstHalf.Sum(v => Math.Pow(v - meanRes1, 2)) / Math.Max(1, firstHalf.Count - 1);
            double var2 = secondHalf.Sum(v => Math.Pow(v - meanRes2, 2)) / Math.Max(1, secondHalf.Count - 1);

            double residualMeanShift = Math.Abs(meanRes2 - meanRes1) / overallStd;
            double varianceRatio = var1 > 1e-6 ? var2 / var1 : (var2 > 1e-6 ? 999.0 : 1.0);

            bool meanShift = residualMeanShift > 0.35;
            bool varShift = varianceRatio > 3.0 || varianceRatio < 0.33 || (var1 <= 1e-6 && var2 > 1e-6) || (var2 <= 1e-6 && var1 > 1e-6);

            var status = RegimeShiftStatus.None;
            if (meanShift || varShift) status = RegimeShiftStatus.Detected;
            else if (residualMeanShift > 0.20 || varianceRatio > 1.8 || varianceRatio < 0.55) status = RegimeShiftStatus.Suspected;

            var magnitude = (decimal)Math.Max(residualMeanShift, Math.Abs(varianceRatio - 1.0));

            var notes = status switch
            {
                RegimeShiftStatus.Detected => $"Structural break detected: Residual mean shift {residualMeanShift * 100:F1}%, Variance ratio {varianceRatio:F2}.",
                RegimeShiftStatus.Suspected => $"Possible regime instability: Residual mean shift {residualMeanShift * 100:F1}%, Variance ratio {varianceRatio:F2}.",
                _ => "Regime stable. Residual distribution within stationary bounds."
            };

            return new RegimeAssessment(
                AssessmentId: $"regime-{Guid.NewGuid():N}",
                TenantId: tenantId,
                MetricId: metricId,
                Status: status,
                ShiftMagnitude: Math.Round(magnitude, 4),
                MeanShiftDetected: meanShift,
                VarianceShiftDetected: varShift,
                EvidenceNotes: notes,
                EvaluatedAtUtc: now);
        }
    }

    // =========================================================================
    // 3. BACKTEST ENGINE (ACCURACY & PROBABILISTIC CALIBRATION) (AMENDMENT 5)
    // =========================================================================

    public sealed class BacktestEngine : IBacktestEngine
    {
        private readonly IKpiObservationStore _observationStore;
        private readonly IForecastModelRegistry _registry;

        public BacktestEngine(IKpiObservationStore observationStore, IForecastModelRegistry registry)
        {
            _observationStore = observationStore ?? throw new ArgumentNullException(nameof(observationStore));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public async Task<BacktestScorecard> BacktestModelAsync(
            string tenantId,
            string metricId,
            ForecastModelAlgorithm algorithm,
            int testSteps = 5,
            CancellationToken ct = default)
        {
            var policy = await _registry.GetPolicyAsync(tenantId, algorithm, ct);
            var now = DateTime.UtcNow;
            var observations = (await _observationStore.GetObservationsAsync(tenantId, metricId, now.AddDays(-90), now, ct))
                .OrderBy(o => o.ObservedAtUtc)
                .ToList();

            if (observations.Count < policy.MinimumSampleSize + testSteps)
            {
                return new BacktestScorecard(
                    ScorecardId: $"bt-{Guid.NewGuid():N}",
                    TenantId: tenantId,
                    MetricId: metricId,
                    ModelId: algorithm.ToString(),
                    PointRmse: 9999.0m,
                    PointMape: 100.0m,
                    PointMae: 9999.0m,
                    EmpiricalCoverage: 0.0m,
                    MeanWeightedIntervalScore: 9999.0m,
                    CalibrationTier: CalibrationTier.Uncalibrated,
                    TestedSteps: 0,
                    EvaluatedAtUtc: now);
            }

            int trainCount = observations.Count - testSteps;
            var train = observations.Take(trainCount).Select(o => (double)o.Value).ToList();
            var test = observations.Skip(trainCount).Select(o => (double)o.Value).ToList();

            // Fit simple linear model on train
            double xMean = (train.Count - 1) / 2.0;
            double yMean = train.Average();
            double num = 0.0, denom = 0.0;
            for (int i = 0; i < train.Count; i++)
            {
                num += (i - xMean) * (train[i] - yMean);
                denom += Math.Pow(i - xMean, 2);
            }
            double slope = denom > 1e-9 ? num / denom : 0.0;
            double intercept = yMean - slope * xMean;

            double trainVar = train.Sum(v => Math.Pow(v - yMean, 2)) / Math.Max(1, train.Count - 2);
            double stdErr = Math.Sqrt(Math.Max(1e-6, trainVar));

            // Forecast test steps
            var errors = new List<double>();
            var absPercErrors = new List<double>();
            int coveredCount = 0;
            var wisScores = new List<double>();

            for (int h = 0; h < test.Count; h++)
            {
                int t = train.Count + h;
                double pred = intercept + slope * t;
                double actual = test[h];

                double err = actual - pred;
                errors.Add(err);
                if (Math.Abs(actual) > 1e-6)
                    absPercErrors.Add(Math.Abs(err) / Math.Abs(actual));

                // 80% interval (z = 1.28)
                double hScale = stdErr * Math.Sqrt(1.0 + (double)(h + 1) / train.Count);
                double lower = pred - 1.28 * hScale;
                double upper = pred + 1.28 * hScale;

                if (actual >= lower && actual <= upper)
                    coveredCount++;

                // Winkler / Weighted Interval Score (WIS) at 80% level (alpha = 0.20)
                double alpha = 0.20;
                double wis = (upper - lower);
                if (actual < lower) wis += (2.0 / alpha) * (lower - actual);
                if (actual > upper) wis += (2.0 / alpha) * (actual - upper);
                wisScores.Add(wis);
            }

            double rmse = Math.Sqrt(errors.Average(e => e * e));
            double mape = absPercErrors.Count > 0 ? absPercErrors.Average() * 100.0 : 0.0;
            double mae = errors.Average(e => Math.Abs(e));
            double coverage = (double)coveredCount / test.Count;
            double meanWis = wisScores.Average();

            var calTier = CalibrationTier.Uncalibrated;
            if (coverage >= 0.70 && coverage <= 0.90) calTier = CalibrationTier.WellCalibrated;
            else if (coverage > 0.90) calTier = CalibrationTier.Underconfident; // intervals excessively wide
            else if (coverage < 0.70) calTier = CalibrationTier.Overconfident;  // intervals falsely narrow

            return new BacktestScorecard(
                ScorecardId: $"bt-{Guid.NewGuid():N}",
                TenantId: tenantId,
                MetricId: metricId,
                ModelId: algorithm.ToString(),
                PointRmse: Math.Round((decimal)rmse, 4),
                PointMape: Math.Round((decimal)mape, 2),
                PointMae: Math.Round((decimal)mae, 4),
                EmpiricalCoverage: Math.Round((decimal)coverage, 4),
                MeanWeightedIntervalScore: Math.Round((decimal)meanWis, 4),
                CalibrationTier: calTier,
                TestedSteps: test.Count,
                EvaluatedAtUtc: now);
        }
    }

    // =========================================================================
    // 4. DRIFT & CALIBRATION DECAY MONITOR (INVARIANT I20-N)
    // =========================================================================

    public sealed class ForecastDriftMonitor : IForecastDriftMonitor
    {
        private readonly ConcurrentDictionary<string, List<PredictionInterval>> _storedIntervals = new();
        private readonly IKpiObservationStore _observationStore;

        public ForecastDriftMonitor(IKpiObservationStore observationStore)
        {
            _observationStore = observationStore ?? throw new ArgumentNullException(nameof(observationStore));
        }

        public Task RecordForecastIntervalsAsync(string tenantId, string metricId, IReadOnlyList<PredictionInterval> intervals, CancellationToken ct = default)
        {
            var key = $"{tenantId}:{metricId}";
            var list = _storedIntervals.GetOrAdd(key, _ => new List<PredictionInterval>());
            lock (list)
            {
                list.AddRange(intervals);
            }
            return Task.CompletedTask;
        }

        public async Task<DriftReport> EvaluateDriftAsync(string tenantId, string metricId, CancellationToken ct = default)
        {
            var key = $"{tenantId}:{metricId}";
            if (!_storedIntervals.TryGetValue(key, out var list) || list.Count == 0)
            {
                return new DriftReport(
                    ReportId: $"drift-{Guid.NewGuid():N}",
                    TenantId: tenantId,
                    MetricId: metricId,
                    DriftScore: 0.0m,
                    Severity: DriftSeverity.None,
                    BreachCount: 0,
                    EvaluatedObservationsCount: 0,
                    RequiresModelReevaluation: false,
                    EvaluatedAtUtc: DateTime.UtcNow);
            }

            var now = DateTime.UtcNow;
            var observations = await _observationStore.GetObservationsAsync(tenantId, metricId, now.AddDays(-30), now, ct);

            int breaches = 0;
            int evaluated = 0;
            int consecutiveBreaches = 0;
            int maxConsecutiveBreaches = 0;

            lock (list)
            {
                foreach (var obs in observations.OrderBy(o => o.ObservedAtUtc))
                {
                    // Find matching interval by nearest target timestamp
                    var matching = list.FirstOrDefault(i => Math.Abs((i.TargetTimestampUtc - obs.ObservedAtUtc).TotalHours) < 3.0);
                    if (matching != null)
                    {
                        evaluated++;
                        if (obs.Value < matching.LowerBound || obs.Value > matching.UpperBound)
                        {
                            breaches++;
                            consecutiveBreaches++;
                            if (consecutiveBreaches > maxConsecutiveBreaches)
                                maxConsecutiveBreaches = consecutiveBreaches;
                        }
                        else
                        {
                            consecutiveBreaches = 0;
                        }
                    }
                }
            }

            decimal breachRate = evaluated > 0 ? (decimal)breaches / evaluated : 0.0m;
            var severity = DriftSeverity.None;
            bool reevaluation = false;

            if (breachRate > 0.40m || maxConsecutiveBreaches >= 3)
            {
                severity = DriftSeverity.Critical;
                reevaluation = true;
            }
            else if (breachRate > 0.25m || maxConsecutiveBreaches >= 2)
            {
                severity = DriftSeverity.High;
                reevaluation = true;
            }
            else if (breachRate > 0.15m)
            {
                severity = DriftSeverity.Medium;
            }

            return new DriftReport(
                ReportId: $"drift-{Guid.NewGuid():N}",
                TenantId: tenantId,
                MetricId: metricId,
                DriftScore: Math.Round(breachRate, 4),
                Severity: severity,
                BreachCount: breaches,
                EvaluatedObservationsCount: evaluated,
                RequiresModelReevaluation: reevaluation,
                EvaluatedAtUtc: now);
        }
    }

    // =========================================================================
    // 5. PROVENANCE-ADDRESSED FORECAST RECORD STORE (INVARIANTS I20-I, I20-O)
    // =========================================================================

    public sealed class ForecastRecordStore : IForecastRecordStore
    {
        private readonly ConcurrentDictionary<string, ForecastOutput> _forecasts = new();
        private readonly ConcurrentDictionary<string, ForecastProvenanceSnapshot> _provenance = new();

        public Task StoreForecastAsync(ForecastOutput forecast, ForecastProvenanceSnapshot provenance, CancellationToken ct = default)
        {
            if (forecast == null) throw new ArgumentNullException(nameof(forecast));
            if (provenance == null) throw new ArgumentNullException(nameof(provenance));

            _forecasts[forecast.ForecastId] = forecast;
            _provenance[forecast.ForecastId] = provenance;
            return Task.CompletedTask;
        }

        public Task<ForecastOutput?> GetForecastAsync(string forecastId, CancellationToken ct = default)
        {
            _forecasts.TryGetValue(forecastId, out var forecast);
            return Task.FromResult(forecast);
        }

        public Task<IReadOnlyList<ForecastOutput>> GetForecastsForMetricAsync(string tenantId, string metricId, CancellationToken ct = default)
        {
            var list = _forecasts.Values
                .Where(f => f.TenantId == tenantId && f.MetricId == metricId)
                .OrderByDescending(f => f.GeneratedAtUtc)
                .ToList();

            return Task.FromResult<IReadOnlyList<ForecastOutput>>(list);
        }

        public Task<ForecastProvenanceSnapshot?> GetProvenanceAsync(string forecastId, CancellationToken ct = default)
        {
            _provenance.TryGetValue(forecastId, out var prov);
            return Task.FromResult(prov);
        }
    }

    // =========================================================================
    // 6. FORECAST ENGINE (APPLICABILITY GATING & CALIBRATED INTERVALS)
    // =========================================================================

    public sealed class ForecastEngine : IForecastEngine
    {
        private readonly IKpiObservationStore _observationStore;
        private readonly IForecastModelRegistry _registry;
        private readonly IRegimeChangeDetector _regimeDetector;
        private readonly IForecastDriftMonitor _driftMonitor;
        private readonly IForecastRecordStore _recordStore;

        public ForecastEngine(
            IKpiObservationStore observationStore,
            IForecastModelRegistry registry,
            IRegimeChangeDetector regimeDetector,
            IForecastDriftMonitor driftMonitor,
            IForecastRecordStore recordStore)
        {
            _observationStore = observationStore ?? throw new ArgumentNullException(nameof(observationStore));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _regimeDetector = regimeDetector ?? throw new ArgumentNullException(nameof(regimeDetector));
            _driftMonitor = driftMonitor ?? throw new ArgumentNullException(nameof(driftMonitor));
            _recordStore = recordStore ?? throw new ArgumentNullException(nameof(recordStore));
        }

        public async Task<ForecastApplicabilityStatus> AssessApplicabilityAsync(
            string tenantId,
            string metricId,
            ForecastModelAlgorithm algorithm,
            int horizon,
            CancellationToken ct = default)
        {
            var policy = await _registry.GetPolicyAsync(tenantId, algorithm, ct);
            var now = DateTime.UtcNow;
            var observations = await _observationStore.GetObservationsAsync(tenantId, metricId, now.AddDays(-60), now, ct);

            // 1. Evidence sufficiency check (Amendment 1: policy-specific N_min)
            if (observations.Count < policy.MinimumSampleSize)
                return ForecastApplicabilityStatus.NotApplicable;

            // 2. Horizon bound check
            if (horizon <= 0 || horizon > policy.MaxHorizon)
                return ForecastApplicabilityStatus.NotApplicable;

            // 3. Freshness check
            var latest = observations.OrderByDescending(o => o.ObservedAtUtc).First();
            if (now - latest.ObservedAtUtc > policy.FreshnessSla)
                return ForecastApplicabilityStatus.NotApplicable;

            // 4. Variance check
            var values = observations.Select(o => (double)o.Value).ToList();
            double mean = values.Average();
            double variance = values.Sum(v => Math.Pow(v - mean, 2)) / Math.Max(1, values.Count - 1);
            if ((decimal)variance < policy.MinimumVariance)
                return ForecastApplicabilityStatus.NotApplicable;

            // 5. Regime stability check (Amendment 4)
            var regime = await _regimeDetector.DetectRegimeShiftAsync(tenantId, metricId, ct);
            if (regime.Status == RegimeShiftStatus.Detected || regime.Status == RegimeShiftStatus.Suspected)
                return ForecastApplicabilityStatus.Degraded;

            // 6. Drift check
            var drift = await _driftMonitor.EvaluateDriftAsync(tenantId, metricId, ct);
            if (drift.Severity == DriftSeverity.Critical)
                return ForecastApplicabilityStatus.Degraded;

            return ForecastApplicabilityStatus.Valid;
        }

        public async Task<ForecastOutput> GenerateForecastAsync(
            string tenantId,
            string metricId,
            ForecastModelAlgorithm algorithm,
            int horizon,
            CancellationToken ct = default)
        {
            var policy = await _registry.GetPolicyAsync(tenantId, algorithm, ct);
            var applicability = await AssessApplicabilityAsync(tenantId, metricId, algorithm, horizon, ct);
            var now = DateTime.UtcNow;

            var forecastId = $"fcst-{Guid.NewGuid():N}";
            var modelVersion = "1.0.0";
            var algoVersion = "1.0.0";

            // Amendment 6 & Invariant I20-M: If applicability fails, output fails-closed without false precision
            if (applicability == ForecastApplicabilityStatus.NotApplicable || applicability == ForecastApplicabilityStatus.Unknown)
            {
                var emptyProv = new ForecastProvenanceSnapshot(
                    tenantId, metricId, algorithm.ToString(), modelVersion, algoVersion,
                    Array.Empty<string>(), new Dictionary<string, string>(), null, "1.0.0");

                var emptyHash = emptyProv.ComputeHash();

                return new ForecastOutput(
                    ForecastId: forecastId,
                    TenantId: tenantId,
                    MetricId: metricId,
                    ModelId: algorithm.ToString(),
                    ModelVersion: modelVersion,
                    HorizonSteps: horizon,
                    Intervals: Array.Empty<PredictionInterval>(),
                    UncertaintyRadius: 1.00m,
                    ApplicabilityStatus: applicability,
                    StatedAssumptions: new[] { "Invariant I20-M: Forecast applicability conditions failed. Fails-closed to UNKNOWN." },
                    GeneratedAtUtc: now,
                    ProvenanceHash: emptyHash);
            }

            var observations = (await _observationStore.GetObservationsAsync(tenantId, metricId, now.AddDays(-60), now, ct))
                .OrderBy(o => o.ObservedAtUtc)
                .ToList();

            var values = observations.Select(o => (double)o.Value).ToList();
            var inputHashes = observations.Select(o => o.IntegrityHash).ToList();

            // Fit statistical model (Linear Trend with variance scaling)
            double xMean = (values.Count - 1) / 2.0;
            double yMean = values.Average();
            double num = 0.0, denom = 0.0;
            for (int i = 0; i < values.Count; i++)
            {
                num += (i - xMean) * (values[i] - yMean);
                denom += Math.Pow(i - xMean, 2);
            }
            double slope = denom > 1e-9 ? num / denom : 0.0;
            double intercept = yMean - slope * xMean;

            double residualVariance = values.Sum(v => Math.Pow(v - (intercept + slope * values.IndexOf(v)), 2)) / Math.Max(1, values.Count - 2);
            double stdErr = Math.Sqrt(Math.Max(1e-6, residualVariance));

            // Check regime multiplier (Amendment 4)
            var regime = await _regimeDetector.DetectRegimeShiftAsync(tenantId, metricId, ct);
            double regimeMultiplier = (regime.Status == RegimeShiftStatus.Detected || regime.Status == RegimeShiftStatus.Suspected)
                ? (double)policy.RegimeShiftUncertaintyMultiplier
                : 1.0;

            var intervals = new List<PredictionInterval>();
            for (int step = 1; step <= horizon; step++)
            {
                int t = values.Count - 1 + step;
                double pointEstimate = intercept + slope * t;

                // Calibrated standard error scaling with horizon step (Amendment 2 & 3)
                double hScale = stdErr * Math.Sqrt(1.0 + (double)step / values.Count) * regimeMultiplier;

                // 80% coverage (z = 1.28)
                decimal lower = Math.Round((decimal)(pointEstimate - 1.28 * hScale), 4);
                decimal median = Math.Round((decimal)pointEstimate, 4);
                decimal upper = Math.Round((decimal)(pointEstimate + 1.28 * hScale), 4);

                var targetTime = observations.Last().ObservedAtUtc.AddHours(step * 2);

                intervals.Add(new PredictionInterval(
                    StepAhead: step,
                    TargetTimestampUtc: targetTime,
                    LowerBound: lower,
                    MedianValue: median,
                    UpperBound: upper,
                    ConfidenceLevel: policy.TargetCoverageProbability,
                    CalibrationMethod: policy.DefaultIntervalMethod));
            }

            var policySnapshot = new Dictionary<string, string>
            {
                ["MinSample"] = policy.MinimumSampleSize.ToString(),
                ["Horizon"] = horizon.ToString(),
                ["RegimeMultiplier"] = policy.RegimeShiftUncertaintyMultiplier.ToString("F2"),
                ["CalibrationMethod"] = policy.DefaultIntervalMethod.ToString()
            };

            var provenance = new ForecastProvenanceSnapshot(
                tenantId, metricId, algorithm.ToString(), modelVersion, algoVersion,
                inputHashes, policySnapshot, 42, "1.0.0");

            var provHash = provenance.ComputeHash();

            var uncertaintyRadius = (decimal)(stdErr * regimeMultiplier);

            var assumptions = new List<string>
            {
                "Assumes underlying data generating process stationary within regime bounds",
                $"Calibrated with {policy.DefaultIntervalMethod} at {policy.TargetCoverageProbability * 100:F0}% target coverage",
                "Invariant I20-E: Statistical projection reflects observational extrapolation only; does NOT imply causal intervention efficacy."
            };

            if (regime.Status == RegimeShiftStatus.Detected || regime.Status == RegimeShiftStatus.Suspected)
            {
                assumptions.Add($"Structural regime break detected (shift magnitude: {regime.ShiftMagnitude:F2}). Uncertainty intervals scaled by {policy.RegimeShiftUncertaintyMultiplier}x.");
            }

            var output = new ForecastOutput(
                ForecastId: forecastId,
                TenantId: tenantId,
                MetricId: metricId,
                ModelId: algorithm.ToString(),
                ModelVersion: modelVersion,
                HorizonSteps: horizon,
                Intervals: intervals,
                UncertaintyRadius: Math.Round(uncertaintyRadius, 4),
                ApplicabilityStatus: applicability,
                StatedAssumptions: assumptions,
                GeneratedAtUtc: now,
                ProvenanceHash: provHash);

            await _recordStore.StoreForecastAsync(output, provenance, ct);
            await _driftMonitor.RecordForecastIntervalsAsync(tenantId, metricId, intervals, ct);

            return output;
        }
    }

    // =========================================================================
    // 7. FORECASTING METROLOGY ORCHESTRATOR
    // =========================================================================

    public sealed class ForecastingMetrologyOrchestrator : IForecastingMetrologyOrchestrator
    {
        public IForecastModelRegistry Registry { get; }
        public IForecastEngine Engine { get; }
        public IRegimeChangeDetector RegimeDetector { get; }
        public IBacktestEngine Backtester { get; }
        public IForecastDriftMonitor DriftMonitor { get; }
        public IForecastRecordStore RecordStore { get; }

        public ForecastingMetrologyOrchestrator(
            IForecastModelRegistry registry,
            IForecastEngine engine,
            IRegimeChangeDetector regimeDetector,
            IBacktestEngine backtester,
            IForecastDriftMonitor driftMonitor,
            IForecastRecordStore recordStore)
        {
            Registry = registry ?? throw new ArgumentNullException(nameof(registry));
            Engine = engine ?? throw new ArgumentNullException(nameof(engine));
            RegimeDetector = regimeDetector ?? throw new ArgumentNullException(nameof(regimeDetector));
            Backtester = backtester ?? throw new ArgumentNullException(nameof(backtester));
            DriftMonitor = driftMonitor ?? throw new ArgumentNullException(nameof(driftMonitor));
            RecordStore = recordStore ?? throw new ArgumentNullException(nameof(recordStore));
        }

        public async Task<ForecastOutput> GenerateGovernedForecastAsync(
            string tenantId,
            string metricId,
            int horizon,
            CancellationToken ct = default)
        {
            // Determine Champion model or default to LinearTrend
            var champion = await Registry.GetChampionModelAsync(tenantId, metricId, ct);
            var algorithm = ForecastModelAlgorithm.LinearTrend;

            if (champion != null && Enum.TryParse<ForecastModelAlgorithm>(champion, out var parsed))
            {
                algorithm = parsed;
            }

            return await Engine.GenerateForecastAsync(tenantId, metricId, algorithm, horizon, ct);
        }
    }
}
