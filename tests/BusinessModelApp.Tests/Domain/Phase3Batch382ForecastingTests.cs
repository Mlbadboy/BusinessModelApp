using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Forecasting;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Kernel;
using BusinessModelApp.Infrastructure.Runtime.Intelligence.Forecasting;
using BusinessModelApp.Infrastructure.Runtime.Intelligence.Kernel;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public class Phase3Batch382ForecastingTests
    {
        private const string TenantA = "tenant-alpha";
        private const string TenantB = "tenant-beta";

        private static (
            ForecastModelRegistry registry,
            RegimeChangeDetector regimeDetector,
            BacktestEngine backtester,
            ForecastDriftMonitor driftMonitor,
            ForecastRecordStore recordStore,
            ForecastEngine engine,
            ForecastingMetrologyOrchestrator orchestrator,
            InMemoryKpiObservationStore observationStore,
            InMemoryKpiRegistry kpiRegistry) CreateFixture()
        {
            var kpiRegistry = new InMemoryKpiRegistry();
            var observationStore = new InMemoryKpiObservationStore(kpiRegistry);
            var registry = new ForecastModelRegistry();
            var regimeDetector = new RegimeChangeDetector(observationStore);
            var backtester = new BacktestEngine(observationStore, registry);
            var driftMonitor = new ForecastDriftMonitor(observationStore);
            var recordStore = new ForecastRecordStore();
            var engine = new ForecastEngine(observationStore, registry, regimeDetector, driftMonitor, recordStore);
            var orchestrator = new ForecastingMetrologyOrchestrator(registry, engine, regimeDetector, backtester, driftMonitor, recordStore);

            return (registry, regimeDetector, backtester, driftMonitor, recordStore, engine, orchestrator, observationStore, kpiRegistry);
        }

        private static KpiObservation CreateObservation(string tenantId, string metricId, decimal value, DateTime timestamp)
        {
            var recordId = $"rec-{Guid.NewGuid():N}";
            var txId = $"tx-{Guid.NewGuid():N}";
            var hash = KpiObservation.ComputeIntegrityHash(tenantId, metricId, value, timestamp, recordId, txId);

            return new KpiObservation(
                ObservationId: $"obs-{Guid.NewGuid():N}",
                TenantId: tenantId,
                MetricId: metricId,
                Value: value,
                ObservedAtUtc: timestamp,
                SourceRecordId: recordId,
                AuditLedgerTx: txId,
                IntegrityHash: hash,
                SampleSize: 1,
                TemporalState: MetricTemporalState.Fresh);
        }

        // =====================================================================
        // FCE-01: Multi-Horizon Forecasting & Calibrated Prediction Intervals
        // =====================================================================

        [Fact]
        public async Task FCE01_01_GenerateForecast_ProducesMultiHorizon_WithPredictionIntervals()
        {
            var (_, _, _, _, _, engine, _, observationStore, kpiRegistry) = CreateFixture();
            var now = DateTime.UtcNow;

            await kpiRegistry.RegisterKpiAsync(new KpiDefinition("rev", TenantA, "Revenue", "", "", KpiNature.Leading, TimeSpan.FromDays(2)));

            for (int i = 0; i < 10; i++)
            {
                var time = now.AddHours(-20 + i * 2);
                await observationStore.RecordObservationAsync(CreateObservation(TenantA, "rev", 1000m + i * 50m + (i % 2) * 5m, time));
            }

            var forecast = await engine.GenerateForecastAsync(TenantA, "rev", ForecastModelAlgorithm.LinearTrend, horizon: 5);

            Assert.NotNull(forecast);
            Assert.Equal(5, forecast.Intervals.Count);
            Assert.Equal(ForecastApplicabilityStatus.Valid, forecast.ApplicabilityStatus);
            Assert.NotEmpty(forecast.ProvenanceHash);
        }

        [Fact]
        public async Task FCE01_02_PredictionIntervals_AreMathematicallyConsistent()
        {
            var (_, _, _, _, _, engine, _, observationStore, kpiRegistry) = CreateFixture();
            var now = DateTime.UtcNow;

            await kpiRegistry.RegisterKpiAsync(new KpiDefinition("m1", TenantA, "Metric1", "", "", KpiNature.Leading, TimeSpan.FromDays(2)));

            for (int i = 0; i < 8; i++)
            {
                var time = now.AddHours(-16 + i * 2);
                await observationStore.RecordObservationAsync(CreateObservation(TenantA, "m1", 200m + i * 10m, time));
            }

            var forecast = await engine.GenerateForecastAsync(TenantA, "m1", ForecastModelAlgorithm.LinearTrend, horizon: 4);

            foreach (var interval in forecast.Intervals)
            {
                Assert.True(interval.LowerBound <= interval.MedianValue, "Lower bound must be <= median");
                Assert.True(interval.MedianValue <= interval.UpperBound, "Median must be <= upper bound");
                Assert.True(interval.IntervalWidth > 0, "Interval width must be positive");
            }
        }

        [Fact]
        public async Task FCE01_03_UncertaintyWidth_DoesNotFalselyContract()
        {
            var (_, _, _, _, _, engine, _, observationStore, kpiRegistry) = CreateFixture();
            var now = DateTime.UtcNow;

            await kpiRegistry.RegisterKpiAsync(new KpiDefinition("m1", TenantA, "Metric1", "", "", KpiNature.Leading, TimeSpan.FromDays(2)));

            for (int i = 0; i < 8; i++)
            {
                var time = now.AddHours(-16 + i * 2);
                await observationStore.RecordObservationAsync(CreateObservation(TenantA, "m1", 500m + i * 25m, time));
            }

            var forecast = await engine.GenerateForecastAsync(TenantA, "m1", ForecastModelAlgorithm.LinearTrend, horizon: 4);

            for (int i = 1; i < forecast.Intervals.Count; i++)
            {
                Assert.True(forecast.Intervals[i].IntervalWidth >= forecast.Intervals[i - 1].IntervalWidth,
                    "Interval width must not falsely contract into the future");
            }
        }

        // =====================================================================
        // FCE-02: Forecast != Fact & Prediction != Reality (Invariants I20-A, I20-B)
        // =====================================================================

        [Fact]
        public async Task FCE02_01_ForecastOutput_IsStrictlyTypedAsForecast()
        {
            var (_, _, _, _, _, engine, _, observationStore, kpiRegistry) = CreateFixture();
            var now = DateTime.UtcNow;

            await kpiRegistry.RegisterKpiAsync(new KpiDefinition("sales", TenantA, "Sales", "", "", KpiNature.Leading, TimeSpan.FromDays(2)));

            for (int i = 0; i < 6; i++)
            {
                var time = now.AddHours(-12 + i * 2);
                await observationStore.RecordObservationAsync(CreateObservation(TenantA, "sales", 100m + i * 10m, time));
            }

            var forecast = await engine.GenerateForecastAsync(TenantA, "sales", ForecastModelAlgorithm.LinearTrend, horizon: 3);

            // Invariant I20-A: Forecast is never Fact
            Assert.Equal(EpistemicKind.Forecast, forecast.EpistemicKind);
            Assert.NotEqual(EpistemicKind.Fact, forecast.EpistemicKind);
        }

        // =====================================================================
        // FCE-03: Policy-Driven Minimum Evidence & Freshness Gating (Amendment 1)
        // =====================================================================

        [Fact]
        public async Task FCE03_01_InsufficientObservations_FailsApplicability_FailsClosed()
        {
            var (_, _, _, _, _, engine, _, observationStore, kpiRegistry) = CreateFixture();
            var now = DateTime.UtcNow;

            await kpiRegistry.RegisterKpiAsync(new KpiDefinition("low_data", TenantA, "LowData", "", "", KpiNature.Leading, TimeSpan.FromDays(2)));

            // LinearTrend policy requires minimum 4 observations (Amendment 1)
            await observationStore.RecordObservationAsync(CreateObservation(TenantA, "low_data", 100m, now.AddHours(-2)));
            await observationStore.RecordObservationAsync(CreateObservation(TenantA, "low_data", 105m, now));

            var applicability = await engine.AssessApplicabilityAsync(TenantA, "low_data", ForecastModelAlgorithm.LinearTrend, horizon: 3);
            Assert.Equal(ForecastApplicabilityStatus.NotApplicable, applicability);

            var forecast = await engine.GenerateForecastAsync(TenantA, "low_data", ForecastModelAlgorithm.LinearTrend, horizon: 3);
            Assert.Equal(ForecastApplicabilityStatus.NotApplicable, forecast.ApplicabilityStatus);
            Assert.Empty(forecast.Intervals);
        }

        [Fact]
        public async Task FCE03_02_StaleDataExceedingSla_FailsApplicability()
        {
            var (_, _, _, _, _, engine, _, observationStore, kpiRegistry) = CreateFixture();
            var now = DateTime.UtcNow;

            await kpiRegistry.RegisterKpiAsync(new KpiDefinition("stale_kpi", TenantA, "Stale", "", "", KpiNature.Leading, TimeSpan.FromDays(2)));

            // Observations 5 days old (> 48h FreshnessSla)
            for (int i = 0; i < 6; i++)
            {
                var time = now.AddDays(-10 + i);
                await observationStore.RecordObservationAsync(CreateObservation(TenantA, "stale_kpi", 200m + i * 10m, time));
            }

            var applicability = await engine.AssessApplicabilityAsync(TenantA, "stale_kpi", ForecastModelAlgorithm.LinearTrend, horizon: 3);
            Assert.Equal(ForecastApplicabilityStatus.NotApplicable, applicability);
        }

        // =====================================================================
        // FCE-04: Regime Change Detection & Policy Uncertainty Multiplier (Amendment 4)
        // =====================================================================

        [Fact]
        public async Task FCE04_01_RegimeShift_DetectedOnStructuralBreak()
        {
            var (_, regimeDetector, _, _, _, _, _, observationStore, kpiRegistry) = CreateFixture();
            var now = DateTime.UtcNow;

            await kpiRegistry.RegisterKpiAsync(new KpiDefinition("macro", TenantA, "Macro", "", "", KpiNature.Leading, TimeSpan.FromDays(2)));

            // First regime: steady around 100
            for (int i = 0; i < 5; i++)
            {
                var time = now.AddHours(-20 + i * 2);
                await observationStore.RecordObservationAsync(CreateObservation(TenantA, "macro", 100m + (i % 2) * 2m, time));
            }

            // Structural break: sudden 100% surge to 200 with high variance
            for (int i = 0; i < 5; i++)
            {
                var time = now.AddHours(-10 + i * 2);
                await observationStore.RecordObservationAsync(CreateObservation(TenantA, "macro", 200m + (i % 2) * 30m, time));
            }

            var assessment = await regimeDetector.DetectRegimeShiftAsync(TenantA, "macro");

            Assert.Equal(RegimeShiftStatus.Detected, assessment.Status);
            Assert.True(assessment.MeanShiftDetected);
            Assert.True(assessment.ShiftMagnitude > 0.20m);
        }

        [Fact]
        public async Task FCE04_02_RegimeShift_WidensUncertaintyIntervals_DegradesStatus()
        {
            var (_, _, _, _, _, engine, _, observationStore, kpiRegistry) = CreateFixture();
            var now = DateTime.UtcNow;

            await kpiRegistry.RegisterKpiAsync(new KpiDefinition("vol", TenantA, "Volatility", "", "", KpiNature.Leading, TimeSpan.FromDays(2)));

            for (int i = 0; i < 5; i++)
            {
                var time = now.AddHours(-20 + i * 2);
                await observationStore.RecordObservationAsync(CreateObservation(TenantA, "vol", 100m, time));
            }
            for (int i = 0; i < 5; i++)
            {
                var time = now.AddHours(-10 + i * 2);
                await observationStore.RecordObservationAsync(CreateObservation(TenantA, "vol", 250m + (i % 2) * 50m, time));
            }

            var forecast = await engine.GenerateForecastAsync(TenantA, "vol", ForecastModelAlgorithm.LinearTrend, horizon: 3);

            // Invariant I20-G: Degrades status and widens uncertainty radius
            Assert.Equal(ForecastApplicabilityStatus.Degraded, forecast.ApplicabilityStatus);
            Assert.True(forecast.UncertaintyRadius > 10m);
            Assert.Contains(forecast.StatedAssumptions, a => a.Contains("Structural regime break detected"));
        }

        // =====================================================================
        // FCE-05: Model Consensus Is Not Ground Truth (Invariant I20-D)
        // =====================================================================

        [Fact]
        public async Task FCE05_01_EnsembleAgreement_PreservesForecastEpistemics()
        {
            var (_, _, _, _, _, engine, _, observationStore, kpiRegistry) = CreateFixture();
            var now = DateTime.UtcNow;

            await kpiRegistry.RegisterKpiAsync(new KpiDefinition("ens", TenantA, "Ensemble", "", "", KpiNature.Leading, TimeSpan.FromDays(2)));

            for (int i = 0; i < 12; i++)
            {
                var time = now.AddHours(-24 + i * 2);
                await observationStore.RecordObservationAsync(CreateObservation(TenantA, "ens", 1000m + i * 50m, time));
            }

            var forecast = await engine.GenerateForecastAsync(TenantA, "ens", ForecastModelAlgorithm.EnsembleConsensus, horizon: 4);

            // Invariant I20-D: Model consensus does NOT equal truth
            Assert.Equal(EpistemicKind.Forecast, forecast.EpistemicKind);
        }

        // =====================================================================
        // FCE-06: Extrapolation Does Not Claim Causal Effect (Invariant I20-E)
        // =====================================================================

        [Fact]
        public async Task FCE06_01_Forecast_IncludesMandatoryNonCausalDisclaimer()
        {
            var (_, _, _, _, _, engine, _, observationStore, kpiRegistry) = CreateFixture();
            var now = DateTime.UtcNow;

            await kpiRegistry.RegisterKpiAsync(new KpiDefinition("trend", TenantA, "Trend", "", "", KpiNature.Leading, TimeSpan.FromDays(2)));

            for (int i = 0; i < 8; i++)
            {
                var time = now.AddHours(-16 + i * 2);
                await observationStore.RecordObservationAsync(CreateObservation(TenantA, "trend", 500m + i * 20m, time));
            }

            var forecast = await engine.GenerateForecastAsync(TenantA, "trend", ForecastModelAlgorithm.LinearTrend, horizon: 3);

            // Invariant I20-E: Non-causal assumption disclaimer mandatory
            Assert.Contains(forecast.StatedAssumptions, a => a.Contains("Invariant I20-E"));
        }

        // =====================================================================
        // FCE-07: Separation of Accuracy and Uncertainty Calibration (Amendment 5)
        // =====================================================================

        [Fact]
        public async Task FCE07_01_BacktestEngine_ComputesAccuracyAndCalibrationSeparately()
        {
            var (_, _, backtester, _, _, _, _, observationStore, kpiRegistry) = CreateFixture();
            var now = DateTime.UtcNow;

            await kpiRegistry.RegisterKpiAsync(new KpiDefinition("bt_test", TenantA, "BacktestTest", "", "", KpiNature.Leading, TimeSpan.FromDays(2)));

            // 15 observations: 10 train, 5 test
            for (int i = 0; i < 15; i++)
            {
                var time = now.AddHours(-30 + i * 2);
                await observationStore.RecordObservationAsync(CreateObservation(TenantA, "bt_test", 100m + i * 10m + (i % 3) * 2m, time));
            }

            var scorecard = await backtester.BacktestModelAsync(TenantA, "bt_test", ForecastModelAlgorithm.LinearTrend, testSteps: 5);

            Assert.Equal(5, scorecard.TestedSteps);
            Assert.True(scorecard.PointRmse >= 0);
            Assert.True(scorecard.PointMape >= 0);
            Assert.True(scorecard.EmpiricalCoverage >= 0 && scorecard.EmpiricalCoverage <= 1.0m);
            Assert.True(scorecard.MeanWeightedIntervalScore >= 0);
            Assert.NotEqual(CalibrationTier.Uncalibrated, scorecard.CalibrationTier);
        }

        // =====================================================================
        // FCE-08: Champion / Challenger Evaluation Framework
        // =====================================================================

        [Fact]
        public async Task FCE08_01_ChampionModel_IsHonoredByOrchestrator()
        {
            var (registry, _, _, _, _, _, orchestrator, observationStore, kpiRegistry) = CreateFixture();
            var now = DateTime.UtcNow;

            await kpiRegistry.RegisterKpiAsync(new KpiDefinition("champ", TenantA, "Champ", "", "", KpiNature.Leading, TimeSpan.FromDays(2)));

            for (int i = 0; i < 12; i++)
            {
                var time = now.AddHours(-24 + i * 2);
                await observationStore.RecordObservationAsync(CreateObservation(TenantA, "champ", 300m + i * 15m, time));
            }

            await registry.SetChampionModelAsync(TenantA, "champ", ForecastModelAlgorithm.AutoRegressive.ToString());
            var champion = await registry.GetChampionModelAsync(TenantA, "champ");
            Assert.Equal("AutoRegressive", champion);

            var forecast = await orchestrator.GenerateGovernedForecastAsync(TenantA, "champ", horizon: 3);
            Assert.Equal("AutoRegressive", forecast.ModelId);
        }

        // =====================================================================
        // FCE-09: Live Drift & Calibration Decay Monitoring (Invariant I20-N)
        // =====================================================================

        [Fact]
        public async Task FCE09_01_IntervalBreaches_TriggerDriftAlert_NoAutoExecution()
        {
            var (_, _, _, driftMonitor, _, _, _, observationStore, kpiRegistry) = CreateFixture();
            var now = DateTime.UtcNow;

            await kpiRegistry.RegisterKpiAsync(new KpiDefinition("drift_metric", TenantA, "DriftMetric", "", "", KpiNature.Leading, TimeSpan.FromDays(2)));

            // Record predicted interval: [90, 110]
            var intervals = new List<PredictionInterval>
            {
                new(1, now.AddHours(-4), 90m, 100m, 110m, 0.80m, IntervalCalibrationMethod.ParametricStandardErrorScaling),
                new(2, now.AddHours(-2), 90m, 100m, 110m, 0.80m, IntervalCalibrationMethod.ParametricStandardErrorScaling),
                new(3, now, 90m, 100m, 110m, 0.80m, IntervalCalibrationMethod.ParametricStandardErrorScaling)
            };
            await driftMonitor.RecordForecastIntervalsAsync(TenantA, "drift_metric", intervals);

            // Ingest actual observations severely breaching the upper bound: 250, 300, 350
            await observationStore.RecordObservationAsync(CreateObservation(TenantA, "drift_metric", 250m, now.AddHours(-4)));
            await observationStore.RecordObservationAsync(CreateObservation(TenantA, "drift_metric", 300m, now.AddHours(-2)));
            await observationStore.RecordObservationAsync(CreateObservation(TenantA, "drift_metric", 350m, now));

            var report = await driftMonitor.EvaluateDriftAsync(TenantA, "drift_metric");

            Assert.Equal(3, report.BreachCount);
            Assert.Equal(DriftSeverity.Critical, report.Severity);
            Assert.True(report.RequiresModelReevaluation);
        }

        // =====================================================================
        // FCE-10: Provenance-Addressed Deterministic Reproducibility (Invariants I20-I, I20-O)
        // =====================================================================

        [Fact]
        public void FCE10_01_IdenticalInputs_ProduceIdenticalProvenanceHash()
        {
            var snap1 = new ForecastProvenanceSnapshot(
                TenantA, "m1", "LinearTrend", "1.0.0", "1.0.0",
                new[] { "hash1", "hash2" },
                new Dictionary<string, string> { ["P1"] = "V1" },
                42, "1.0.0");

            var snap2 = new ForecastProvenanceSnapshot(
                TenantA, "m1", "LinearTrend", "1.0.0", "1.0.0",
                new[] { "hash1", "hash2" },
                new Dictionary<string, string> { ["P1"] = "V1" },
                42, "1.0.0");

            Assert.Equal(snap1.ComputeHash(), snap2.ComputeHash());
        }

        [Fact]
        public void FCE10_02_AlteredInputHash_ChangesProvenanceHash()
        {
            var snap1 = new ForecastProvenanceSnapshot(
                TenantA, "m1", "LinearTrend", "1.0.0", "1.0.0",
                new[] { "hash1", "hash2" },
                new Dictionary<string, string> { ["P1"] = "V1" },
                42, "1.0.0");

            var snap2 = new ForecastProvenanceSnapshot(
                TenantA, "m1", "LinearTrend", "1.0.0", "1.0.0",
                new[] { "hash1", "tampered_hash" },
                new Dictionary<string, string> { ["P1"] = "V1" },
                42, "1.0.0");

            Assert.NotEqual(snap1.ComputeHash(), snap2.ComputeHash());
        }

        // =====================================================================
        // FCE-11: Zero Execution Authority Invariance (Invariant I20-H)
        // =====================================================================

        [Fact]
        public void FCE11_01_ForecastingTypes_HaveZeroExecutionPermitMethods()
        {
            var types = typeof(ForecastOutput).Assembly.GetTypes()
                .Where(t => t.Namespace != null && t.Namespace.Contains("Intelligence.Forecasting"))
                .ToList();

            foreach (var type in types)
            {
                var methods = type.GetMethods();
                Assert.DoesNotContain(methods, m => m.Name.Contains("IssuePermit"));
                Assert.DoesNotContain(methods, m => m.Name.Contains("AuthorizeExecution"));
                Assert.DoesNotContain(methods, m => m.Name.Contains("BypassFirewall"));
            }
        }

        // =====================================================================
        // FCE-12: Full End-to-End BI Kernel -> Forecasting Orchestration & Isolation
        // =====================================================================

        [Fact]
        public async Task FCE12_01_FullOrchestrator_GovernedForecastLifecycle()
        {
            var (_, _, _, _, recordStore, _, orchestrator, observationStore, kpiRegistry) = CreateFixture();
            var now = DateTime.UtcNow;

            await kpiRegistry.RegisterKpiAsync(new KpiDefinition("mrr", TenantA, "Monthly Recurring Revenue", "", "", KpiNature.Leading, TimeSpan.FromDays(2)));

            for (int i = 0; i < 10; i++)
            {
                var time = now.AddHours(-20 + i * 2);
                await observationStore.RecordObservationAsync(CreateObservation(TenantA, "mrr", 50000m + i * 2000m, time));
            }

            var output = await orchestrator.GenerateGovernedForecastAsync(TenantA, "mrr", horizon: 4);

            Assert.NotNull(output);
            Assert.Equal(4, output.Intervals.Count);
            Assert.Equal(ForecastApplicabilityStatus.Valid, output.ApplicabilityStatus);

            var stored = await recordStore.GetForecastAsync(output.ForecastId);
            Assert.NotNull(stored);
            Assert.Equal(output.ForecastId, stored.ForecastId);

            var provenance = await recordStore.GetProvenanceAsync(output.ForecastId);
            Assert.NotNull(provenance);
            Assert.Equal(10, provenance.InputObservationHashes.Count);
        }

        [Fact]
        public async Task FCE12_02_MultiTenant_StrictDataIsolation()
        {
            var (registry, _, _, _, recordStore, engine, _, observationStore, kpiRegistry) = CreateFixture();
            var now = DateTime.UtcNow;

            await kpiRegistry.RegisterKpiAsync(new KpiDefinition("kpiA", TenantA, "KpiA", "", "", KpiNature.Leading, TimeSpan.FromDays(2)));
            await kpiRegistry.RegisterKpiAsync(new KpiDefinition("kpiB", TenantB, "KpiB", "", "", KpiNature.Leading, TimeSpan.FromDays(2)));

            for (int i = 0; i < 8; i++)
            {
                var time = now.AddHours(-16 + i * 2);
                await observationStore.RecordObservationAsync(CreateObservation(TenantA, "kpiA", 100m + i * 10m, time));
                await observationStore.RecordObservationAsync(CreateObservation(TenantB, "kpiB", 500m + i * 50m, time));
            }

            var fcstA = await engine.GenerateForecastAsync(TenantA, "kpiA", ForecastModelAlgorithm.LinearTrend, horizon: 3);
            var fcstB = await engine.GenerateForecastAsync(TenantB, "kpiB", ForecastModelAlgorithm.LinearTrend, horizon: 3);

            var historyA = await recordStore.GetForecastsForMetricAsync(TenantA, "kpiA");
            var historyB = await recordStore.GetForecastsForMetricAsync(TenantB, "kpiB");

            Assert.Single(historyA);
            Assert.Single(historyB);
            Assert.NotEqual(historyA[0].ForecastId, historyB[0].ForecastId);
        }
    }
}
