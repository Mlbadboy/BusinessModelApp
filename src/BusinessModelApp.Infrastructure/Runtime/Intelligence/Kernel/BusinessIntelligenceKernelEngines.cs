using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Kernel;
using BusinessModelApp.Core.Interfaces.Runtime.Intelligence.Kernel;

namespace BusinessModelApp.Infrastructure.Runtime.Intelligence.Kernel
{
    // =========================================================================
    // 1. IN-MEMORY KPI REGISTRY
    // =========================================================================

    public sealed class InMemoryKpiRegistry : IKpiRegistry
    {
        private readonly ConcurrentDictionary<(string TenantId, string MetricId), KpiDefinition> _kpis = new();

        public Task RegisterKpiAsync(KpiDefinition definition, CancellationToken ct = default)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            _kpis[(definition.TenantId, definition.MetricId)] = definition;
            return Task.CompletedTask;
        }

        public Task<KpiDefinition?> GetKpiAsync(string tenantId, string metricId, CancellationToken ct = default)
        {
            _kpis.TryGetValue((tenantId, metricId), out var def);
            return Task.FromResult(def);
        }

        public Task<IReadOnlyList<KpiDefinition>> GetAllKpisAsync(string tenantId, CancellationToken ct = default)
        {
            var list = _kpis.Values.Where(k => k.TenantId == tenantId).ToList();
            return Task.FromResult<IReadOnlyList<KpiDefinition>>(list);
        }
    }

    // =========================================================================
    // 2. IN-MEMORY KPI OBSERVATION STORE (PROVENANCE & FRESHNESS)
    // =========================================================================

    public sealed class InMemoryKpiObservationStore : IKpiObservationStore
    {
        private readonly ConcurrentDictionary<(string TenantId, string MetricId), List<KpiObservation>> _store = new();
        private readonly IKpiRegistry _registry;

        public InMemoryKpiObservationStore(IKpiRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public async Task RecordObservationAsync(KpiObservation observation, CancellationToken ct = default)
        {
            if (observation == null) throw new ArgumentNullException(nameof(observation));

            // Verify integrity hash
            var expectedHash = KpiObservation.ComputeIntegrityHash(
                observation.TenantId,
                observation.MetricId,
                observation.Value,
                observation.ObservedAtUtc,
                observation.SourceRecordId,
                observation.AuditLedgerTx);

            if (!string.Equals(expectedHash, observation.IntegrityHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Observation integrity verification failed for {observation.MetricId}. Hash mismatch.");
            }

            var kpi = await _registry.GetKpiAsync(observation.TenantId, observation.MetricId, ct);
            var now = DateTime.UtcNow;
            var age = now - observation.ObservedAtUtc;

            var temporalState = MetricTemporalState.Fresh;
            if (kpi != null)
            {
                if (age > kpi.FreshnessSla * 3) temporalState = MetricTemporalState.Expired;
                else if (age > kpi.FreshnessSla * 1.5) temporalState = MetricTemporalState.Stale;
                else if (age > kpi.FreshnessSla) temporalState = MetricTemporalState.Decaying;
            }

            var stamped = observation with { TemporalState = temporalState };

            _store.AddOrUpdate(
                (observation.TenantId, observation.MetricId),
                _ => new List<KpiObservation> { stamped },
                (_, list) =>
                {
                    lock (list)
                    {
                        list.Add(stamped);
                        list.Sort((a, b) => a.ObservedAtUtc.CompareTo(b.ObservedAtUtc));
                    }
                    return list;
                });
        }

        public Task<IReadOnlyList<KpiObservation>> GetObservationsAsync(string tenantId, string metricId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
        {
            if (_store.TryGetValue((tenantId, metricId), out var list))
            {
                lock (list)
                {
                    var result = list.Where(o => o.ObservedAtUtc >= fromUtc && o.ObservedAtUtc <= toUtc).ToList();
                    return Task.FromResult<IReadOnlyList<KpiObservation>>(result);
                }
            }
            return Task.FromResult<IReadOnlyList<KpiObservation>>(Array.Empty<KpiObservation>());
        }

        public Task<KpiObservation?> GetLatestObservationAsync(string tenantId, string metricId, CancellationToken ct = default)
        {
            if (_store.TryGetValue((tenantId, metricId), out var list))
            {
                lock (list)
                {
                    var latest = list.LastOrDefault();
                    return Task.FromResult(latest);
                }
            }
            return Task.FromResult<KpiObservation?>(null);
        }
    }

    // =========================================================================
    // 3. IN-MEMORY ANALYSIS RECORD STORE (DETERMINISTIC REPRODUCIBILITY)
    // =========================================================================

    public sealed class InMemoryAnalysisRecordStore : IAnalysisRecordStore
    {
        private readonly ConcurrentDictionary<string, AnalysisRecord> _analyses = new();

        public Task RecordAnalysisAsync(AnalysisRecord record, CancellationToken ct = default)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            _analyses[record.AnalysisId] = record;
            return Task.CompletedTask;
        }

        public Task<AnalysisRecord?> GetAnalysisAsync(string analysisId, CancellationToken ct = default)
        {
            _analyses.TryGetValue(analysisId, out var record);
            return Task.FromResult(record);
        }

        public Task<IReadOnlyList<AnalysisRecord>> GetAnalysesForMetricAsync(string tenantId, string metricId, CancellationToken ct = default)
        {
            var list = _analyses.Values
                .Where(a => a.TenantId == tenantId && a.MetricId == metricId)
                .OrderByDescending(a => a.CreatedAtUtc)
                .ToList();
            return Task.FromResult<IReadOnlyList<AnalysisRecord>>(list);
        }
    }

    // =========================================================================
    // 4. BASELINE QUALITY EVALUATOR (PRE-FLIGHT ANOMALY GATE)
    // =========================================================================

    public sealed class BaselineQualityEvaluator : IBaselineQualityEvaluator
    {
        public BaselineQualityAssessment AssessBaseline(IReadOnlyList<KpiObservation> observations, AnalysisMethodPolicy policy, DateTime evaluationTimeUtc)
        {
            if (observations == null || observations.Count == 0)
            {
                return new BaselineQualityAssessment(
                    MetricId: "UNKNOWN",
                    ObservationCount: 0,
                    Variance: 0,
                    BaselineWindow: policy.BaselineWindow,
                    IsFresh: false,
                    HasSufficientData: false,
                    HasSufficientVariance: false,
                    IsContaminated: false,
                    QualityPassed: false,
                    FailureReason: "No observations present in baseline window.");
            }

            var metricId = observations[0].MetricId;
            var windowStart = evaluationTimeUtc - policy.BaselineWindow;
            var windowObservations = observations.Where(o => o.ObservedAtUtc >= windowStart && o.ObservedAtUtc <= evaluationTimeUtc).ToList();

            if (windowObservations.Count < policy.MinimumSampleSize)
            {
                return new BaselineQualityAssessment(
                    MetricId: metricId,
                    ObservationCount: windowObservations.Count,
                    Variance: 0,
                    BaselineWindow: policy.BaselineWindow,
                    IsFresh: false,
                    HasSufficientData: false,
                    HasSufficientVariance: false,
                    IsContaminated: false,
                    QualityPassed: false,
                    FailureReason: $"Insufficient observations in baseline window ({windowObservations.Count} < {policy.MinimumSampleSize}).");
            }

            // Freshness check
            var latest = windowObservations.Max(o => o.ObservedAtUtc);
            var isFresh = (evaluationTimeUtc - latest) <= policy.FreshnessRequirement;
            if (!isFresh)
            {
                return new BaselineQualityAssessment(
                    MetricId: metricId,
                    ObservationCount: windowObservations.Count,
                    Variance: 0,
                    BaselineWindow: policy.BaselineWindow,
                    IsFresh: false,
                    HasSufficientData: true,
                    HasSufficientVariance: false,
                    IsContaminated: false,
                    QualityPassed: false,
                    FailureReason: $"Baseline data is stale. Latest observation age {(evaluationTimeUtc - latest).TotalHours:F1}h exceeds freshness requirement {policy.FreshnessRequirement.TotalHours:F1}h.");
            }

            // Variance calculation
            var values = windowObservations.Select(o => (double)o.Value).ToList();
            var mean = values.Average();
            var variance = (decimal)(values.Sum(v => Math.Pow(v - mean, 2)) / Math.Max(1, values.Count - 1));

            var hasSufficientVariance = variance >= policy.MinimumVariance;
            if (!hasSufficientVariance)
            {
                return new BaselineQualityAssessment(
                    MetricId: metricId,
                    ObservationCount: windowObservations.Count,
                    Variance: variance,
                    BaselineWindow: policy.BaselineWindow,
                    IsFresh: true,
                    HasSufficientData: true,
                    HasSufficientVariance: false,
                    IsContaminated: false,
                    QualityPassed: false,
                    FailureReason: $"Baseline variance ({variance:F6}) is below required minimum threshold ({policy.MinimumVariance}).");
            }

            return new BaselineQualityAssessment(
                MetricId: metricId,
                ObservationCount: windowObservations.Count,
                Variance: variance,
                BaselineWindow: policy.BaselineWindow,
                IsFresh: true,
                HasSufficientData: true,
                HasSufficientVariance: true,
                IsContaminated: false,
                QualityPassed: true,
                FailureReason: string.Empty);
        }
    }

    // =========================================================================
    // 5. STATISTICAL ANOMALY DETECTOR
    // =========================================================================

    public sealed class StatisticalAnomalyDetector : IAnomalyDetector
    {
        private readonly IKpiRegistry _registry;
        private readonly IKpiObservationStore _observations;
        private readonly IBaselineQualityEvaluator _baselineEvaluator;
        private readonly IAnalysisRecordStore _analysisStore;
        private readonly ConcurrentDictionary<string, MetricAnomaly> _activeAnomalies = new();

        public StatisticalAnomalyDetector(
            IKpiRegistry registry,
            IKpiObservationStore observations,
            IBaselineQualityEvaluator baselineEvaluator,
            IAnalysisRecordStore analysisStore)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _observations = observations ?? throw new ArgumentNullException(nameof(observations));
            _baselineEvaluator = baselineEvaluator ?? throw new ArgumentNullException(nameof(baselineEvaluator));
            _analysisStore = analysisStore ?? throw new ArgumentNullException(nameof(analysisStore));
        }

        public async Task<MetricAnomaly?> DetectAnomalyAsync(string tenantId, string metricId, CancellationToken ct = default)
        {
            var kpi = await _registry.GetKpiAsync(tenantId, metricId, ct);
            if (kpi == null) return null;

            var policy = kpi.EffectivePolicy;
            var now = DateTime.UtcNow;
            var fromUtc = now - policy.BaselineWindow;

            var allObs = await _observations.GetObservationsAsync(tenantId, metricId, fromUtc, now, ct);
            if (allObs.Count == 0) return null;

            var baselineAssessment = _baselineEvaluator.AssessBaseline(allObs, policy, now);
            if (!baselineAssessment.QualityPassed)
            {
                // Invariant I18-C: If baseline quality fails, state is UNKNOWN. Do not evaluate anomaly.
                return null;
            }

            var latestObs = allObs.Last();
            var baselineObs = allObs.Take(allObs.Count - 1).ToList();
            if (baselineObs.Count < policy.MinimumSampleSize) return null;

            var values = baselineObs.Select(o => (double)o.Value).ToList();
            var mean = (decimal)values.Average();
            var stdDev = (decimal)Math.Sqrt(values.Sum(v => Math.Pow(v - (double)mean, 2)) / Math.Max(1, values.Count - 1));

            if (stdDev <= 0) return null;

            var observed = latestObs.Value;
            var zScore = (observed - mean) / stdDev;
            var absZ = Math.Abs(zScore);

            // Anomaly threshold check (e.g. |Z| >= 2.0)
            if (absZ < 2.0m)
            {
                // Normal
                return null;
            }

            var direction = zScore > 0 ? AnomalyDirection.Surge : AnomalyDirection.Plunge;
            var severity = absZ switch
            {
                >= 4.0m => AnomalySeverity.Critical,
                >= 3.0m => AnomalySeverity.High,
                >= 2.5m => AnomalySeverity.Medium,
                _ => AnomalySeverity.Low
            };

            var margin = 2.0m * stdDev;
            var lowerBound = mean - margin;
            var upperBound = mean + margin;
            var confidence = Math.Min(0.999m, 0.90m + (absZ - 2.0m) * 0.04m);

            var analysisId = $"analysis-anom-{Guid.NewGuid():N}";
            var inputHashes = allObs.Select(o => o.IntegrityHash).ToList();
            var paramSnapshot = new Dictionary<string, string>
            {
                { "Mean", mean.ToString("F4") },
                { "StdDev", stdDev.ToString("F4") },
                { "ZScore", zScore.ToString("F4") },
                { "Observed", observed.ToString("F4") }
            };

            var recordHash = AnalysisRecord.ComputeHash(
                tenantId,
                metricId,
                inputHashes,
                policy.Method,
                policy.Version,
                $"{direction}:{severity}:{zScore:F2}");

            var analysisRecord = new AnalysisRecord(
                AnalysisId: analysisId,
                TenantId: tenantId,
                MetricId: metricId,
                InputObservationHashes: inputHashes,
                Method: policy.Method,
                MethodVersion: policy.Version,
                ParameterSnapshot: paramSnapshot,
                OutputSummary: $"Detected {direction} anomaly with Z-Score={zScore:F2}, Severity={severity}",
                AlgorithmVersion: "BIK-1.0.0",
                CreatedAtUtc: now,
                IntegrityHash: recordHash);

            await _analysisStore.RecordAnalysisAsync(analysisRecord, ct);

            var anomaly = new MetricAnomaly(
                AnomalyId: $"anom-{Guid.NewGuid():N}",
                TenantId: tenantId,
                MetricId: metricId,
                Direction: direction,
                Severity: severity,
                ObservedValue: observed,
                ExpectedValue: mean,
                ExpectedLowerBound: lowerBound,
                ExpectedUpperBound: upperBound,
                ZScore: zScore,
                ConfidenceScore: confidence,
                BaselineAssessment: baselineAssessment,
                AnalysisRecordId: analysisId,
                DetectedAtUtc: now,
                EvidenceRecordIds: new[] { latestObs.ObservationId, latestObs.SourceRecordId });

            _activeAnomalies[anomaly.AnomalyId] = anomaly;
            return anomaly;
        }

        public Task<IReadOnlyList<MetricAnomaly>> GetAllActiveAnomaliesAsync(string tenantId, CancellationToken ct = default)
        {
            var list = _activeAnomalies.Values.Where(a => a.TenantId == tenantId).ToList();
            return Task.FromResult<IReadOnlyList<MetricAnomaly>>(list);
        }
    }

    // =========================================================================
    // 6. TREND & ACCELERATION ANALYZER
    // =========================================================================

    public sealed class TrendAnalyzer : ITrendAnalyzer
    {
        private readonly IKpiRegistry _registry;
        private readonly IKpiObservationStore _observations;
        private readonly IAnalysisRecordStore _analysisStore;

        public TrendAnalyzer(IKpiRegistry registry, IKpiObservationStore observations, IAnalysisRecordStore analysisStore)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _observations = observations ?? throw new ArgumentNullException(nameof(observations));
            _analysisStore = analysisStore ?? throw new ArgumentNullException(nameof(analysisStore));
        }

        public async Task<MetricTrend?> AnalyzeTrendAsync(string tenantId, string metricId, CancellationToken ct = default)
        {
            var kpi = await _registry.GetKpiAsync(tenantId, metricId, ct);
            if (kpi == null) return null;

            var policy = AnalysisMethodPolicy.DefaultTrendPolicy;
            var now = DateTime.UtcNow;
            var fromUtc = now - policy.BaselineWindow;

            var obs = await _observations.GetObservationsAsync(tenantId, metricId, fromUtc, now, ct);

            // Invariant I18-C: If sample count < minimum, return Unknown trend direction
            if (obs.Count < policy.MinimumSampleSize)
            {
                return new MetricTrend(
                    TrendId: $"trend-{Guid.NewGuid():N}",
                    TenantId: tenantId,
                    MetricId: metricId,
                    Direction: TrendDirection.Unknown,
                    Velocity: 0,
                    Acceleration: 0,
                    RSquared: 0,
                    SampleCount: obs.Count,
                    AnalysisRecordId: string.Empty,
                    EvaluatedAtUtc: now,
                    EvidenceRecordIds: Array.Empty<string>());
            }

            var points = obs.Select((o, idx) => (X: (double)idx, Y: (double)o.Value)).ToList();
            var n = points.Count;

            var sumX = points.Sum(p => p.X);
            var sumY = points.Sum(p => p.Y);
            var sumX2 = points.Sum(p => p.X * p.X);
            var sumY2 = points.Sum(p => p.Y * p.Y);
            var sumXY = points.Sum(p => p.X * p.Y);

            var slope = (n * sumXY - sumX * sumY) / Math.Max(0.00001, (n * sumX2 - sumX * sumX));
            var meanY = sumY / n;

            // R-squared
            var ssTot = points.Sum(p => Math.Pow(p.Y - meanY, 2));
            var intercept = (sumY - slope * sumX) / n;
            var ssRes = points.Sum(p => Math.Pow(p.Y - (slope * p.X + intercept), 2));
            var rSquared = ssTot > 0 ? Math.Max(0, 1.0 - (ssRes / ssTot)) : 1.0;

            // Velocity (slope) & Acceleration (difference between first-half and second-half slopes)
            var mid = n / 2;
            var firstHalf = points.Take(mid).ToList();
            var secondHalf = points.Skip(mid).ToList();

            double slope1 = 0, slope2 = 0;
            if (firstHalf.Count >= 2 && secondHalf.Count >= 2)
            {
                slope1 = (firstHalf.Last().Y - firstHalf.First().Y) / Math.Max(1, firstHalf.Count - 1);
                slope2 = (secondHalf.Last().Y - secondHalf.First().Y) / Math.Max(1, secondHalf.Count - 1);
            }
            var acceleration = slope2 - slope1;

            var dir = TrendDirection.Flat;
            if (Math.Abs(slope) < 0.01)
            {
                dir = TrendDirection.Flat;
            }
            else if (slope > 0)
            {
                dir = acceleration > 0.02 ? TrendDirection.AcceleratingUp : TrendDirection.SteadyUp;
            }
            else
            {
                dir = acceleration < -0.02 ? TrendDirection.AcceleratingDown : TrendDirection.SteadyDown;
            }

            var analysisId = $"analysis-trend-{Guid.NewGuid():N}";
            var inputHashes = obs.Select(o => o.IntegrityHash).ToList();

            var recordHash = AnalysisRecord.ComputeHash(
                tenantId,
                metricId,
                inputHashes,
                policy.Method,
                policy.Version,
                $"{dir}:{slope:F4}:{rSquared:F4}");

            var analysisRecord = new AnalysisRecord(
                AnalysisId: analysisId,
                TenantId: tenantId,
                MetricId: metricId,
                InputObservationHashes: inputHashes,
                Method: policy.Method,
                MethodVersion: policy.Version,
                ParameterSnapshot: new Dictionary<string, string>
                {
                    { "Slope", slope.ToString("F4") },
                    { "Acceleration", acceleration.ToString("F4") },
                    { "RSquared", rSquared.ToString("F4") }
                },
                OutputSummary: $"Trend: {dir}, Velocity={slope:F4}, RSquared={rSquared:F4}",
                AlgorithmVersion: "BIK-1.0.0",
                CreatedAtUtc: now,
                IntegrityHash: recordHash);

            await _analysisStore.RecordAnalysisAsync(analysisRecord, ct);

            return new MetricTrend(
                TrendId: $"trend-{Guid.NewGuid():N}",
                TenantId: tenantId,
                MetricId: metricId,
                Direction: dir,
                Velocity: (decimal)slope,
                Acceleration: (decimal)acceleration,
                RSquared: (decimal)rSquared,
                SampleCount: n,
                AnalysisRecordId: analysisId,
                EvaluatedAtUtc: now,
                EvidenceRecordIds: obs.Select(o => o.ObservationId).ToList());
        }
    }

    // =========================================================================
    // 7. METRIC RELATIONSHIP & CORRELATION ANALYZER (INVARIANT I18-A)
    // =========================================================================

    public sealed class MetricRelationshipAnalyzer : IMetricRelationshipAnalyzer
    {
        private readonly IKpiObservationStore _observations;
        private readonly IAnalysisRecordStore _analysisStore;

        public MetricRelationshipAnalyzer(IKpiObservationStore observations, IAnalysisRecordStore analysisStore)
        {
            _observations = observations ?? throw new ArgumentNullException(nameof(observations));
            _analysisStore = analysisStore ?? throw new ArgumentNullException(nameof(analysisStore));
        }

        public async Task<MetricRelationship?> AnalyzeRelationshipAsync(string tenantId, string metricAId, string metricBId, CancellationToken ct = default)
        {
            var policy = AnalysisMethodPolicy.DefaultCorrelationPolicy;
            var now = DateTime.UtcNow;
            var fromUtc = now - policy.BaselineWindow;

            var obsA = await _observations.GetObservationsAsync(tenantId, metricAId, fromUtc, now, ct);
            var obsB = await _observations.GetObservationsAsync(tenantId, metricBId, fromUtc, now, ct);

            var count = Math.Min(obsA.Count, obsB.Count);
            if (count < policy.MinimumSampleSize)
            {
                // Insufficient observations for valid statistical correlation
                return null;
            }

            var pairedA = obsA.Take(count).Select(o => (double)o.Value).ToList();
            var pairedB = obsB.Take(count).Select(o => (double)o.Value).ToList();

            var meanA = pairedA.Average();
            var meanB = pairedB.Average();

            var numerator = 0.0;
            var denomA = 0.0;
            var denomB = 0.0;

            for (var i = 0; i < count; i++)
            {
                var diffA = pairedA[i] - meanA;
                var diffB = pairedB[i] - meanB;
                numerator += diffA * diffB;
                denomA += diffA * diffA;
                denomB += diffB * diffB;
            }

            var denom = Math.Sqrt(denomA * denomB);
            var r = denom > 0 ? (decimal)(numerator / denom) : 0m;
            var pValue = count > 2 ? Math.Max(0.001m, (decimal)(1.0 - Math.Abs((double)r))) : 0.5m;
            var stability = Math.Min(1.0m, (decimal)count / (policy.MinimumSampleSize * 2));

            var analysisId = $"analysis-rel-{Guid.NewGuid():N}";
            var inputHashes = obsA.Take(count).Select(o => o.IntegrityHash)
                .Concat(obsB.Take(count).Select(o => o.IntegrityHash))
                .ToList();

            var recordHash = AnalysisRecord.ComputeHash(
                tenantId,
                $"{metricAId}->{metricBId}",
                inputHashes,
                policy.Method,
                policy.Version,
                $"r={r:F4}:p={pValue:F4}");

            var analysisRecord = new AnalysisRecord(
                AnalysisId: analysisId,
                TenantId: tenantId,
                MetricId: $"{metricAId}<->{metricBId}",
                InputObservationHashes: inputHashes,
                Method: policy.Method,
                MethodVersion: policy.Version,
                ParameterSnapshot: new Dictionary<string, string>
                {
                    { "MetricA", metricAId },
                    { "MetricB", metricBId },
                    { "SampleSize", count.ToString() }
                },
                OutputSummary: $"Correlation r={r:F4}, p={pValue:F4}, Stability={stability:F2}. Correlation != Causation enforced under I18-A.",
                AlgorithmVersion: "BIK-1.0.0",
                CreatedAtUtc: now,
                IntegrityHash: recordHash);

            await _analysisStore.RecordAnalysisAsync(analysisRecord, ct);

            // Invariant I18-A: IsCausal is strictly FALSE.
            return new MetricRelationship(
                RelationshipId: $"rel-{Guid.NewGuid():N}",
                TenantId: tenantId,
                MetricAId: metricAId,
                MetricBId: metricBId,
                PearsonCoefficient: r,
                LagOffsetSeconds: 0,
                SampleSize: count,
                PValue: pValue,
                StabilityIndex: stability,
                IsCausal: false, // Invariant I18-A: Correlation != Causation
                AnalysisRecordId: analysisId,
                DiscoveredAtUtc: now);
        }
    }

    // =========================================================================
    // 8. BUSINESS STATE INTERPRETER (OBSERVED VS INTERPRETED)
    // =========================================================================

    public sealed class BusinessStateInterpreter : IBusinessStateInterpreter
    {
        private readonly IAnomalyDetector _anomalies;
        private readonly IAnalysisRecordStore _analysisStore;

        public BusinessStateInterpreter(IAnomalyDetector anomalies, IAnalysisRecordStore analysisStore)
        {
            _anomalies = anomalies ?? throw new ArgumentNullException(nameof(anomalies));
            _analysisStore = analysisStore ?? throw new ArgumentNullException(nameof(analysisStore));
        }

        public async Task<InterpretedBusinessState> InterpretStateAsync(string tenantId, ObservedBusinessState observedState, CancellationToken ct = default)
        {
            if (observedState == null) throw new ArgumentNullException(nameof(observedState));

            var activeAnomalies = await _anomalies.GetAllActiveAnomaliesAsync(tenantId, ct);
            var criticalCount = activeAnomalies.Count(a => a.Severity == AnomalySeverity.Critical);
            var highCount = activeAnomalies.Count(a => a.Severity == AnomalySeverity.High);

            // Evaluate regime stability
            var regime = BusinessStabilityRegime.Stable;
            if (criticalCount > 0 || observedState.LiquidReserveCash < 500000m)
            {
                regime = BusinessStabilityRegime.Critical;
            }
            else if (highCount > 1 || observedState.VerifiedGrossMarginPercentage < 20m)
            {
                regime = BusinessStabilityRegime.Volatile;
            }
            else if (activeAnomalies.Count > 0 || observedState.CacObserved > 2500m)
            {
                regime = BusinessStabilityRegime.Drifting;
            }

            // Health Index computation
            var health = 100m;
            health -= criticalCount * 25m;
            health -= highCount * 12m;
            health -= activeAnomalies.Count * 4m;
            if (observedState.LiquidReserveCash < 1000000m) health -= 10m;
            if (observedState.VerifiedGrossMarginPercentage < 25m) health -= 8m;
            health = Math.Max(10m, Math.Min(100m, health));

            var regimeAlignment = regime switch
            {
                BusinessStabilityRegime.Stable => 95m,
                BusinessStabilityRegime.Drifting => 80m,
                BusinessStabilityRegime.Volatile => 65m,
                BusinessStabilityRegime.Critical => 40m,
                _ => 50m
            };

            var summaries = new List<string>
            {
                $"Stability: {regime} (Health Index: {health:F1})",
                $"Liquid Reserve: ₹{observedState.LiquidReserveCash:N0}, Gross Margin: {observedState.VerifiedGrossMarginPercentage:F1}%",
                $"Active Anomalies: {activeAnomalies.Count} (Critical: {criticalCount}, High: {highCount})"
            };

            var now = DateTime.UtcNow;
            var analysisId = $"analysis-state-{Guid.NewGuid():N}";
            var inputHashes = observedState.TelemetryRecordIds;

            var recordHash = AnalysisRecord.ComputeHash(
                tenantId,
                "ENTERPRISE_STATE",
                inputHashes,
                "MultivariateBusinessStateEvaluation",
                "1.0.0",
                $"{regime}:{health:F1}:{regimeAlignment:F1}");

            var analysisRecord = new AnalysisRecord(
                AnalysisId: analysisId,
                TenantId: tenantId,
                MetricId: "ENTERPRISE_STATE",
                InputObservationHashes: inputHashes,
                Method: "MultivariateBusinessStateEvaluation",
                MethodVersion: "1.0.0",
                ParameterSnapshot: new Dictionary<string, string>
                {
                    { "Regime", regime.ToString() },
                    { "HealthIndex", health.ToString("F1") },
                    { "AnomaliesCount", activeAnomalies.Count.ToString() }
                },
                OutputSummary: $"Evaluated Business State: {regime}, Health={health:F1}, Alignment={regimeAlignment:F1}",
                AlgorithmVersion: "BIK-1.0.0",
                CreatedAtUtc: now,
                IntegrityHash: recordHash);

            await _analysisStore.RecordAnalysisAsync(analysisRecord, ct);

            return new InterpretedBusinessState(
                InterpretationId: $"interp-{Guid.NewGuid():N}",
                TenantId: tenantId,
                StabilityRegime: regime,
                HealthIndex: health,
                RegimeAlignmentScore: regimeAlignment,
                ActiveAnomaliesCount: activeAnomalies.Count,
                KeyTrendSummaries: summaries,
                AnalysisRecordId: analysisId,
                EvaluatedAtUtc: now);
        }
    }

    // =========================================================================
    // 9. EVIDENCE-LINKED EXPLAINER (INVARIANTS I18-B & I18-H)
    // =========================================================================

    public sealed class EvidenceLinkedExplainer : IEvidenceLinkedExplainer
    {
        private readonly IKpiObservationStore _observations;
        private readonly IAnalysisRecordStore _analysisStore;
        private readonly ConcurrentDictionary<string, EvidenceLinkedInsight> _insights = new();

        public EvidenceLinkedExplainer(IKpiObservationStore observations, IAnalysisRecordStore analysisStore)
        {
            _observations = observations ?? throw new ArgumentNullException(nameof(observations));
            _analysisStore = analysisStore ?? throw new ArgumentNullException(nameof(analysisStore));
        }

        public async Task<EvidenceLinkedInsight> GenerateInsightAsync(
            string tenantId,
            string metricId,
            EpistemicKind kind,
            string title,
            string narrative,
            string? anomalyId = null,
            string? trendId = null,
            CancellationToken ct = default)
        {
            // Invariant I18-B: Every insight MUST have underlying verified evidence. No evidence -> rejection.
            var latest = await _observations.GetLatestObservationAsync(tenantId, metricId, ct);
            if (latest == null)
            {
                throw new InvalidOperationException($"Invariant I18-B violation: Cannot generate insight for {metricId} without verified telemetry evidence.");
            }

            var evidenceHashes = new List<string> { latest.IntegrityHash };
            var now = DateTime.UtcNow;

            var analysisId = $"analysis-insight-{Guid.NewGuid():N}";
            var recordHash = AnalysisRecord.ComputeHash(
                tenantId,
                metricId,
                evidenceHashes,
                "EvidenceLinkedExplanationGenerator",
                "1.0.0",
                $"{kind}:{title}");

            var analysisRecord = new AnalysisRecord(
                AnalysisId: analysisId,
                TenantId: tenantId,
                MetricId: metricId,
                InputObservationHashes: evidenceHashes,
                Method: "EvidenceLinkedExplanationGenerator",
                MethodVersion: "1.0.0",
                ParameterSnapshot: new Dictionary<string, string>
                {
                    { "Kind", kind.ToString() },
                    { "Title", title },
                    { "SourceRecord", latest.SourceRecordId }
                },
                OutputSummary: $"Insight generated under Invariant I18-B with verified telemetry {latest.SourceRecordId}",
                AlgorithmVersion: "BIK-1.0.0",
                CreatedAtUtc: now,
                IntegrityHash: recordHash);

            await _analysisStore.RecordAnalysisAsync(analysisRecord, ct);

            var insight = new EvidenceLinkedInsight(
                InsightId: $"ins-{Guid.NewGuid():N}",
                TenantId: tenantId,
                Kind: kind,
                Title: title,
                Narrative: narrative,
                ObservedMetricId: metricId,
                AnomalyId: anomalyId,
                TrendId: trendId,
                AnalysisRecordId: analysisId,
                EvidenceHashes: evidenceHashes,
                Confidence: 0.95m,
                CreatedAtUtc: now);

            _insights[insight.InsightId] = insight;
            return insight;
        }

        public Task<IReadOnlyList<EvidenceLinkedInsight>> GetInsightsAsync(string tenantId, CancellationToken ct = default)
        {
            var list = _insights.Values.Where(i => i.TenantId == tenantId).ToList();
            return Task.FromResult<IReadOnlyList<EvidenceLinkedInsight>>(list);
        }
    }

    // =========================================================================
    // 10. MASTER BUSINESS INTELLIGENCE KERNEL ORCHESTRATOR
    // =========================================================================

    public sealed class BusinessIntelligenceKernel : IBusinessIntelligenceKernel
    {
        public IKpiRegistry Registry { get; }
        public IKpiObservationStore Observations { get; }
        public IAnomalyDetector Anomalies { get; }
        public ITrendAnalyzer Trends { get; }
        public IMetricRelationshipAnalyzer Relationships { get; }
        public IBusinessStateInterpreter StateInterpreter { get; }
        public IEvidenceLinkedExplainer Explainer { get; }

        public BusinessIntelligenceKernel(
            IKpiRegistry registry,
            IKpiObservationStore observations,
            IAnomalyDetector anomalies,
            ITrendAnalyzer trends,
            IMetricRelationshipAnalyzer relationships,
            IBusinessStateInterpreter stateInterpreter,
            IEvidenceLinkedExplainer explainer)
        {
            Registry = registry ?? throw new ArgumentNullException(nameof(registry));
            Observations = observations ?? throw new ArgumentNullException(nameof(observations));
            Anomalies = anomalies ?? throw new ArgumentNullException(nameof(anomalies));
            Trends = trends ?? throw new ArgumentNullException(nameof(trends));
            Relationships = relationships ?? throw new ArgumentNullException(nameof(relationships));
            StateInterpreter = stateInterpreter ?? throw new ArgumentNullException(nameof(stateInterpreter));
            Explainer = explainer ?? throw new ArgumentNullException(nameof(explainer));
        }

        public async Task IngestObservationAsync(KpiObservation observation, CancellationToken ct = default)
        {
            await Observations.RecordObservationAsync(observation, ct);

            // Pre-flight anomaly detection trigger
            await Anomalies.DetectAnomalyAsync(observation.TenantId, observation.MetricId, ct);

            // Trigger trend analysis
            await Trends.AnalyzeTrendAsync(observation.TenantId, observation.MetricId, ct);
        }

        public async Task<InterpretedBusinessState> EvaluateEnterpriseStateAsync(string tenantId, ObservedBusinessState observedState, CancellationToken ct = default)
        {
            return await StateInterpreter.InterpretStateAsync(tenantId, observedState, ct);
        }
    }
}
