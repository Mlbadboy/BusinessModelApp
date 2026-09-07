using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Kernel;
using BusinessModelApp.Infrastructure.Runtime.Intelligence.Kernel;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public class Phase3Batch380BusinessIntelligenceKernelTests
    {
        private const string TenantA = "tenant-enterprise-alpha";
        private const string TenantB = "tenant-enterprise-beta";

        private InMemoryKpiRegistry CreateRegistry() => new();

        private InMemoryKpiObservationStore CreateObservationStore(InMemoryKpiRegistry registry) => new(registry);

        private BusinessIntelligenceKernel CreateKernel()
        {
            var registry = new InMemoryKpiRegistry();
            var obsStore = new InMemoryKpiObservationStore(registry);
            var analysisStore = new InMemoryAnalysisRecordStore();
            var baselineEval = new BaselineQualityEvaluator();
            var anomalyDetector = new StatisticalAnomalyDetector(registry, obsStore, baselineEval, analysisStore);
            var trendAnalyzer = new TrendAnalyzer(registry, obsStore, analysisStore);
            var relAnalyzer = new MetricRelationshipAnalyzer(obsStore, analysisStore);
            var stateInterp = new BusinessStateInterpreter(anomalyDetector, analysisStore);
            var explainer = new EvidenceLinkedExplainer(obsStore, analysisStore);

            return new BusinessIntelligenceKernel(
                registry,
                obsStore,
                anomalyDetector,
                trendAnalyzer,
                relAnalyzer,
                stateInterp,
                explainer);
        }

        private KpiObservation CreateObservation(
            string tenantId,
            string metricId,
            decimal value,
            DateTime observedAtUtc,
            string sourceRecordId = "REC-001",
            string auditTx = "TX-9901")
        {
            var hash = KpiObservation.ComputeIntegrityHash(tenantId, metricId, value, observedAtUtc, sourceRecordId, auditTx);
            return new KpiObservation(
                ObservationId: $"obs-{Guid.NewGuid():N}",
                TenantId: tenantId,
                MetricId: metricId,
                Value: value,
                ObservedAtUtc: observedAtUtc,
                SourceRecordId: sourceRecordId,
                AuditLedgerTx: auditTx,
                IntegrityHash: hash);
        }

        // =====================================================================
        // BIK-01: KPI Registration & Multi-Tenant Partitioning
        // =====================================================================

        [Fact]
        public async Task BIK01_01_RegisterValidKpi_PersistsInRegistry()
        {
            var registry = CreateRegistry();
            var kpi = new KpiDefinition("rev_velocity", TenantA, "Revenue Velocity", "Gross revenue velocity per hour", "INR/hr", KpiNature.Leading, TimeSpan.FromHours(1));

            await registry.RegisterKpiAsync(kpi);
            var retrieved = await registry.GetKpiAsync(TenantA, "rev_velocity");

            Assert.NotNull(retrieved);
            Assert.Equal("Revenue Velocity", retrieved.Name);
            Assert.Equal(KpiNature.Leading, retrieved.Nature);
        }

        [Fact]
        public async Task BIK01_02_MultiTenantPartitioning_IsolatesTenantKpis()
        {
            var registry = CreateRegistry();
            await registry.RegisterKpiAsync(new KpiDefinition("m1", TenantA, "Metric A", "Desc", "unit", KpiNature.Coincident, TimeSpan.FromHours(1)));
            await registry.RegisterKpiAsync(new KpiDefinition("m1", TenantB, "Metric B", "Desc", "unit", KpiNature.Coincident, TimeSpan.FromHours(1)));

            var kpiA = await registry.GetKpiAsync(TenantA, "m1");
            var kpiB = await registry.GetKpiAsync(TenantB, "m1");

            Assert.NotNull(kpiA);
            Assert.NotNull(kpiB);
            Assert.Equal("Metric A", kpiA.Name);
            Assert.Equal("Metric B", kpiB.Name);
        }

        [Fact]
        public async Task BIK01_03_KpiNatureClassifications_ArePreserved()
        {
            var registry = CreateRegistry();
            await registry.RegisterKpiAsync(new KpiDefinition("lead", TenantA, "Leading", "Desc", "unit", KpiNature.Leading, TimeSpan.FromHours(1)));
            await registry.RegisterKpiAsync(new KpiDefinition("lag", TenantA, "Lagging", "Desc", "unit", KpiNature.Lagging, TimeSpan.FromHours(1)));
            await registry.RegisterKpiAsync(new KpiDefinition("coin", TenantA, "Coincident", "Desc", "unit", KpiNature.Coincident, TimeSpan.FromHours(1)));
            await registry.RegisterKpiAsync(new KpiDefinition("count", TenantA, "Counterfactual", "Desc", "unit", KpiNature.Counterfactual, TimeSpan.FromHours(1)));

            Assert.Equal(KpiNature.Leading, (await registry.GetKpiAsync(TenantA, "lead"))!.Nature);
            Assert.Equal(KpiNature.Lagging, (await registry.GetKpiAsync(TenantA, "lag"))!.Nature);
            Assert.Equal(KpiNature.Coincident, (await registry.GetKpiAsync(TenantA, "coin"))!.Nature);
            Assert.Equal(KpiNature.Counterfactual, (await registry.GetKpiAsync(TenantA, "count"))!.Nature);
        }

        [Fact]
        public async Task BIK01_04_CustomPolicy_IsPreservedOrDefaults()
        {
            var registry = CreateRegistry();
            var customPolicy = new AnalysisMethodPolicy("CustomZ", 15, 0.05m, 0.99m, TimeSpan.FromDays(45), TimeSpan.FromHours(12), OutlierExclusionPolicy.Winsorize);
            var kpiCustom = new KpiDefinition("k_cust", TenantA, "Custom", "Desc", "unit", KpiNature.Leading, TimeSpan.FromHours(1), CustomPolicy: customPolicy);
            var kpiDefault = new KpiDefinition("k_def", TenantA, "Default", "Desc", "unit", KpiNature.Leading, TimeSpan.FromHours(1));

            await registry.RegisterKpiAsync(kpiCustom);
            await registry.RegisterKpiAsync(kpiDefault);

            Assert.Equal(15, (await registry.GetKpiAsync(TenantA, "k_cust"))!.EffectivePolicy.MinimumSampleSize);
            Assert.Equal(7, (await registry.GetKpiAsync(TenantA, "k_def"))!.EffectivePolicy.MinimumSampleSize);
        }

        [Fact]
        public async Task BIK01_05_DuplicateRegistration_UpdatesSafely()
        {
            var registry = CreateRegistry();
            await registry.RegisterKpiAsync(new KpiDefinition("m1", TenantA, "V1", "Desc", "unit", KpiNature.Leading, TimeSpan.FromHours(1)));
            await registry.RegisterKpiAsync(new KpiDefinition("m1", TenantA, "V2 Updated", "Desc", "unit", KpiNature.Leading, TimeSpan.FromHours(2)));

            var updated = await registry.GetKpiAsync(TenantA, "m1");
            Assert.Equal("V2 Updated", updated!.Name);
            Assert.Equal(TimeSpan.FromHours(2), updated.FreshnessSla);
        }

        [Fact]
        public async Task BIK01_06_NullDefinition_ThrowsArgumentNullException()
        {
            var registry = CreateRegistry();
            await Assert.ThrowsAsync<ArgumentNullException>(() => registry.RegisterKpiAsync(null!));
        }

        // =====================================================================
        // BIK-02: Observation Ingestion & Provenance Integrity
        // =====================================================================

        [Fact]
        public async Task BIK02_01_RecordObservation_ValidHash_Succeeds()
        {
            var registry = CreateRegistry();
            var store = CreateObservationStore(registry);
            var obs = CreateObservation(TenantA, "m1", 42.5m, DateTime.UtcNow);

            await store.RecordObservationAsync(obs);
            var latest = await store.GetLatestObservationAsync(TenantA, "m1");

            Assert.NotNull(latest);
            Assert.Equal(42.5m, latest.Value);
        }

        [Fact]
        public async Task BIK02_02_RecordObservation_TamperedHash_ThrowsException()
        {
            var registry = CreateRegistry();
            var store = CreateObservationStore(registry);
            var obs = new KpiObservation("o-1", TenantA, "m1", 100m, DateTime.UtcNow, "REC", "TX", "corrupted-hash");

            await Assert.ThrowsAsync<InvalidOperationException>(() => store.RecordObservationAsync(obs));
        }

        [Fact]
        public async Task BIK02_03_MultipleObservations_ChronologicallySorted()
        {
            var registry = CreateRegistry();
            var store = CreateObservationStore(registry);
            var now = DateTime.UtcNow;

            await store.RecordObservationAsync(CreateObservation(TenantA, "m1", 30m, now.AddMinutes(-10)));
            await store.RecordObservationAsync(CreateObservation(TenantA, "m1", 10m, now.AddMinutes(-30)));
            await store.RecordObservationAsync(CreateObservation(TenantA, "m1", 20m, now.AddMinutes(-20)));

            var all = await store.GetObservationsAsync(TenantA, "m1", now.AddHours(-1), now);
            Assert.Equal(3, all.Count);
            Assert.Equal(10m, all[0].Value);
            Assert.Equal(20m, all[1].Value);
            Assert.Equal(30m, all[2].Value);
        }

        [Fact]
        public async Task BIK02_04_Observations_MultiTenantPartitioning()
        {
            var registry = CreateRegistry();
            var store = CreateObservationStore(registry);
            var now = DateTime.UtcNow;

            await store.RecordObservationAsync(CreateObservation(TenantA, "m1", 100m, now));
            await store.RecordObservationAsync(CreateObservation(TenantB, "m1", 200m, now));

            var obsA = await store.GetLatestObservationAsync(TenantA, "m1");
            var obsB = await store.GetLatestObservationAsync(TenantB, "m1");

            Assert.Equal(100m, obsA!.Value);
            Assert.Equal(200m, obsB!.Value);
        }

        [Fact]
        public async Task BIK02_05_Observation_PreservesExactAuditLedgerAndSourceRecord()
        {
            var registry = CreateRegistry();
            var store = CreateObservationStore(registry);
            var obs = CreateObservation(TenantA, "m1", 99.9m, DateTime.UtcNow, sourceRecordId: "SR-9821", auditTx: "TX-BANK-882");

            await store.RecordObservationAsync(obs);
            var retrieved = await store.GetLatestObservationAsync(TenantA, "m1");

            Assert.Equal("SR-9821", retrieved!.SourceRecordId);
            Assert.Equal("TX-BANK-882", retrieved.AuditLedgerTx);
        }

        [Fact]
        public async Task BIK02_06_GetLatestObservation_NoRecords_ReturnsNull()
        {
            var registry = CreateRegistry();
            var store = CreateObservationStore(registry);
            var latest = await store.GetLatestObservationAsync(TenantA, "nonexistent");

            Assert.Null(latest);
        }

        // =====================================================================
        // BIK-03: Freshness & Decay Metrology
        // =====================================================================

        [Fact]
        public async Task BIK03_01_ObservationWithinSla_IsFresh()
        {
            var registry = CreateRegistry();
            await registry.RegisterKpiAsync(new KpiDefinition("fresh_kpi", TenantA, "Fresh KPI", "Desc", "unit", KpiNature.Coincident, TimeSpan.FromHours(1)));
            var store = CreateObservationStore(registry);

            await store.RecordObservationAsync(CreateObservation(TenantA, "fresh_kpi", 10m, DateTime.UtcNow.AddMinutes(-10)));
            var latest = await store.GetLatestObservationAsync(TenantA, "fresh_kpi");

            Assert.Equal(MetricTemporalState.Fresh, latest!.TemporalState);
        }

        [Fact]
        public async Task BIK03_02_ObservationBetween1xAnd1_5xSla_IsDecaying()
        {
            var registry = CreateRegistry();
            await registry.RegisterKpiAsync(new KpiDefinition("decay_kpi", TenantA, "Decay KPI", "Desc", "unit", KpiNature.Coincident, TimeSpan.FromHours(1)));
            var store = CreateObservationStore(registry);

            // Age 75 minutes (1.25x SLA of 60 mins)
            await store.RecordObservationAsync(CreateObservation(TenantA, "decay_kpi", 10m, DateTime.UtcNow.AddMinutes(-75)));
            var latest = await store.GetLatestObservationAsync(TenantA, "decay_kpi");

            Assert.Equal(MetricTemporalState.Decaying, latest!.TemporalState);
        }

        [Fact]
        public async Task BIK03_03_ObservationBetween1_5xAnd3xSla_IsStale()
        {
            var registry = CreateRegistry();
            await registry.RegisterKpiAsync(new KpiDefinition("stale_kpi", TenantA, "Stale KPI", "Desc", "unit", KpiNature.Coincident, TimeSpan.FromHours(1)));
            var store = CreateObservationStore(registry);

            // Age 120 minutes (2x SLA of 60 mins)
            await store.RecordObservationAsync(CreateObservation(TenantA, "stale_kpi", 10m, DateTime.UtcNow.AddMinutes(-120)));
            var latest = await store.GetLatestObservationAsync(TenantA, "stale_kpi");

            Assert.Equal(MetricTemporalState.Stale, latest!.TemporalState);
        }

        [Fact]
        public async Task BIK03_04_ObservationBeyond3xSla_IsExpired()
        {
            var registry = CreateRegistry();
            await registry.RegisterKpiAsync(new KpiDefinition("exp_kpi", TenantA, "Expired KPI", "Desc", "unit", KpiNature.Coincident, TimeSpan.FromHours(1)));
            var store = CreateObservationStore(registry);

            // Age 200 minutes (3.33x SLA of 60 mins)
            await store.RecordObservationAsync(CreateObservation(TenantA, "exp_kpi", 10m, DateTime.UtcNow.AddMinutes(-200)));
            var latest = await store.GetLatestObservationAsync(TenantA, "exp_kpi");

            Assert.Equal(MetricTemporalState.Expired, latest!.TemporalState);
        }

        [Fact]
        public async Task BIK03_05_MissingKpiDefinition_DefaultsToFresh()
        {
            var registry = CreateRegistry();
            var store = CreateObservationStore(registry);

            await store.RecordObservationAsync(CreateObservation(TenantA, "unregistered", 10m, DateTime.UtcNow.AddHours(-5)));
            var latest = await store.GetLatestObservationAsync(TenantA, "unregistered");

            Assert.Equal(MetricTemporalState.Fresh, latest!.TemporalState);
        }

        [Fact]
        public async Task BIK03_06_MultipleSlas_EvaluatedIndependently()
        {
            var registry = CreateRegistry();
            await registry.RegisterKpiAsync(new KpiDefinition("fast_kpi", TenantA, "Fast", "Desc", "unit", KpiNature.Coincident, TimeSpan.FromMinutes(10)));
            await registry.RegisterKpiAsync(new KpiDefinition("slow_kpi", TenantA, "Slow", "Desc", "unit", KpiNature.Coincident, TimeSpan.FromHours(24)));
            var store = CreateObservationStore(registry);

            var now = DateTime.UtcNow;
            await store.RecordObservationAsync(CreateObservation(TenantA, "fast_kpi", 1m, now.AddMinutes(-35))); // > 3x of 10m -> Expired
            await store.RecordObservationAsync(CreateObservation(TenantA, "slow_kpi", 1m, now.AddMinutes(-35))); // << 24h -> Fresh

            Assert.Equal(MetricTemporalState.Expired, (await store.GetLatestObservationAsync(TenantA, "fast_kpi"))!.TemporalState);
            Assert.Equal(MetricTemporalState.Fresh, (await store.GetLatestObservationAsync(TenantA, "slow_kpi"))!.TemporalState);
        }

        // =====================================================================
        // BIK-04: Baseline Quality Assessment & Gating (User Amendments #1 & #3)
        // =====================================================================

        [Fact]
        public void BIK04_01_InsufficientObservations_FailsBaselineQuality()
        {
            var evaluator = new BaselineQualityEvaluator();
            var policy = AnalysisMethodPolicy.DefaultZScorePolicy; // Min 7
            var now = DateTime.UtcNow;

            var obs = Enumerable.Range(1, 4)
                .Select(i => CreateObservation(TenantA, "m1", 10m * i, now.AddDays(-i)))
                .ToList();

            var assessment = evaluator.AssessBaseline(obs, policy, now);

            Assert.False(assessment.QualityPassed);
            Assert.False(assessment.HasSufficientData);
            Assert.Contains("Insufficient observations", assessment.FailureReason);
        }

        [Fact]
        public void BIK04_02_StaleBaseline_FailsBaselineQuality()
        {
            var evaluator = new BaselineQualityEvaluator();
            var policy = AnalysisMethodPolicy.DefaultZScorePolicy; // Freshness 24h
            var now = DateTime.UtcNow;

            // 10 observations, but latest is 3 days old
            var obs = Enumerable.Range(3, 10)
                .Select(i => CreateObservation(TenantA, "m1", 10m * i, now.AddDays(-i)))
                .ToList();

            var assessment = evaluator.AssessBaseline(obs, policy, now);

            Assert.False(assessment.QualityPassed);
            Assert.False(assessment.IsFresh);
            Assert.Contains("Baseline data is stale", assessment.FailureReason);
        }

        [Fact]
        public void BIK04_03_FlatlineZeroVariance_FailsBaselineQuality()
        {
            var evaluator = new BaselineQualityEvaluator();
            var policy = AnalysisMethodPolicy.DefaultZScorePolicy;
            var now = DateTime.UtcNow;

            // 10 identical values -> variance = 0
            var obs = Enumerable.Range(0, 10)
                .Select(i => CreateObservation(TenantA, "m1", 50.0m, now.AddHours(-i)))
                .ToList();

            var assessment = evaluator.AssessBaseline(obs, policy, now);

            Assert.False(assessment.QualityPassed);
            Assert.False(assessment.HasSufficientVariance);
            Assert.Contains("below required minimum threshold", assessment.FailureReason);
        }

        [Fact]
        public void BIK04_04_SufficientAndVariantData_PassesBaselineQuality()
        {
            var evaluator = new BaselineQualityEvaluator();
            var policy = AnalysisMethodPolicy.DefaultZScorePolicy;
            var now = DateTime.UtcNow;

            var obs = Enumerable.Range(0, 12)
                .Select(i => CreateObservation(TenantA, "m1", 50.0m + i * 2.5m, now.AddHours(-i)))
                .ToList();

            var assessment = evaluator.AssessBaseline(obs, policy, now);

            Assert.True(assessment.QualityPassed);
            Assert.True(assessment.HasSufficientData);
            Assert.True(assessment.HasSufficientVariance);
            Assert.True(assessment.IsFresh);
            Assert.Empty(assessment.FailureReason);
        }

        [Fact]
        public void BIK04_05_CustomPolicyMinimumSampleSize_IsRespected()
        {
            var evaluator = new BaselineQualityEvaluator();
            var customPolicy = new AnalysisMethodPolicy("StrictN", 12, 0.001m, 0.95m, TimeSpan.FromDays(30), TimeSpan.FromHours(48), OutlierExclusionPolicy.None);
            var now = DateTime.UtcNow;

            // 10 observations: passes default (7) but fails custom (12)
            var obs = Enumerable.Range(0, 10)
                .Select(i => CreateObservation(TenantA, "m1", 10m + i, now.AddHours(-i)))
                .ToList();

            var assessment = evaluator.AssessBaseline(obs, customPolicy, now);

            Assert.False(assessment.QualityPassed);
            Assert.Equal(10, assessment.ObservationCount);
        }

        [Fact]
        public void BIK04_06_EmptyObservations_ReturnsQualityFailedImmediately()
        {
            var evaluator = new BaselineQualityEvaluator();
            var assessment = evaluator.AssessBaseline(Array.Empty<KpiObservation>(), AnalysisMethodPolicy.DefaultZScorePolicy, DateTime.UtcNow);

            Assert.False(assessment.QualityPassed);
            Assert.Equal("UNKNOWN", assessment.MetricId);
        }

        // =====================================================================
        // BIK-05: Statistical Anomaly Detection & Severity Scoring
        // =====================================================================

        [Fact]
        public async Task BIK05_01_NormalObservation_ReturnsNoAnomaly()
        {
            var kernel = CreateKernel();
            await kernel.Registry.RegisterKpiAsync(new KpiDefinition("rev", TenantA, "Revenue", "Desc", "INR", KpiNature.Coincident, TimeSpan.FromHours(24)));
            var now = DateTime.UtcNow;

            // Baseline: 10 observations with mean 100, variance around 100 (values 90..110)
            for (var i = 10; i >= 1; i--)
            {
                await kernel.Observations.RecordObservationAsync(CreateObservation(TenantA, "rev", 100m + (i % 5) * 2m, now.AddHours(-i)));
            }
            // Normal observation: 102m
            await kernel.Observations.RecordObservationAsync(CreateObservation(TenantA, "rev", 102m, now));

            var anomaly = await kernel.Anomalies.DetectAnomalyAsync(TenantA, "rev");
            Assert.Null(anomaly);
        }

        [Fact]
        public async Task BIK05_02_SurgeAnomaly_DetectedWhenZScoreExceedsThreshold()
        {
            var kernel = CreateKernel();
            await kernel.Registry.RegisterKpiAsync(new KpiDefinition("traffic", TenantA, "Traffic", "Desc", "req/s", KpiNature.Leading, TimeSpan.FromHours(24)));
            var now = DateTime.UtcNow;

            // Baseline: mean ~100, stdDev ~2.5
            var baseValues = new decimal[] { 98, 101, 100, 99, 102, 100, 99, 101 };
            for (var i = 0; i < baseValues.Length; i++)
            {
                await kernel.Observations.RecordObservationAsync(CreateObservation(TenantA, "traffic", baseValues[i], now.AddHours(-(baseValues.Length - i))));
            }

            // Massive surge: 160 (Z-score > 15)
            await kernel.Observations.RecordObservationAsync(CreateObservation(TenantA, "traffic", 160m, now));

            var anomaly = await kernel.Anomalies.DetectAnomalyAsync(TenantA, "traffic");

            Assert.NotNull(anomaly);
            Assert.Equal(AnomalyDirection.Surge, anomaly.Direction);
            Assert.Equal(AnomalySeverity.Critical, anomaly.Severity);
            Assert.True(anomaly.ZScore > 4.0m);
        }

        [Fact]
        public async Task BIK05_03_PlungeAnomaly_DetectedWhenZScoreNegativeExceedsThreshold()
        {
            var kernel = CreateKernel();
            await kernel.Registry.RegisterKpiAsync(new KpiDefinition("margin", TenantA, "Margin", "Desc", "%", KpiNature.Lagging, TimeSpan.FromHours(24)));
            var now = DateTime.UtcNow;

            // Baseline: 28, 29, 28.5, 29.2, 28.8, 29.0, 28.7, 28.9 (mean ~28.9, stdDev ~0.35)
            var baseValues = new decimal[] { 28.5m, 29.0m, 28.8m, 29.2m, 28.7m, 28.9m, 29.1m, 28.6m };
            for (var i = 0; i < baseValues.Length; i++)
            {
                await kernel.Observations.RecordObservationAsync(CreateObservation(TenantA, "margin", baseValues[i], now.AddHours(-(baseValues.Length - i))));
            }

            // Severe drop: 25.0% (Z-score < -10)
            await kernel.Observations.RecordObservationAsync(CreateObservation(TenantA, "margin", 25.0m, now));

            var anomaly = await kernel.Anomalies.DetectAnomalyAsync(TenantA, "margin");

            Assert.NotNull(anomaly);
            Assert.Equal(AnomalyDirection.Plunge, anomaly.Direction);
            Assert.True(anomaly.ZScore < -2.0m);
        }

        [Fact]
        public async Task BIK05_04_FailedBaseline_PreventsAnomalyEvaluation_PreservesUnknown()
        {
            var kernel = CreateKernel();
            await kernel.Registry.RegisterKpiAsync(new KpiDefinition("unstable", TenantA, "Unstable", "Desc", "pts", KpiNature.Leading, TimeSpan.FromHours(24)));
            var now = DateTime.UtcNow;

            // Only 3 observations (< minimum 7)
            await kernel.Observations.RecordObservationAsync(CreateObservation(TenantA, "unstable", 10m, now.AddHours(-3)));
            await kernel.Observations.RecordObservationAsync(CreateObservation(TenantA, "unstable", 20m, now.AddHours(-2)));
            await kernel.Observations.RecordObservationAsync(CreateObservation(TenantA, "unstable", 999m, now));

            var anomaly = await kernel.Anomalies.DetectAnomalyAsync(TenantA, "unstable");

            // Invariant I18-C: Must return null (UNKNOWN), not false positive
            Assert.Null(anomaly);
        }

        [Fact]
        public async Task BIK05_05_ActiveAnomalies_CanBeListedByTenant()
        {
            var kernel = CreateKernel();
            await kernel.Registry.RegisterKpiAsync(new KpiDefinition("m_anom", TenantA, "M", "Desc", "u", KpiNature.Leading, TimeSpan.FromHours(24)));
            var now = DateTime.UtcNow;

            for (var i = 0; i < 8; i++)
            {
                await kernel.Observations.RecordObservationAsync(CreateObservation(TenantA, "m_anom", 10m + (i % 2), now.AddHours(-(10 - i))));
            }
            await kernel.Observations.RecordObservationAsync(CreateObservation(TenantA, "m_anom", 50m, now)); // anomaly

            await kernel.Anomalies.DetectAnomalyAsync(TenantA, "m_anom");
            var list = await kernel.Anomalies.GetAllActiveAnomaliesAsync(TenantA);

            Assert.Single(list);
            Assert.Equal("m_anom", list[0].MetricId);
        }

        // =====================================================================
        // BIK-06: Trend & Acceleration Metrology
        // =====================================================================

        [Fact]
        public async Task BIK06_01_InsufficientSamples_ReturnsUnknownTrend()
        {
            var kernel = CreateKernel();
            await kernel.Registry.RegisterKpiAsync(new KpiDefinition("trend_kpi", TenantA, "Trend", "Desc", "u", KpiNature.Leading, TimeSpan.FromHours(24)));
            var now = DateTime.UtcNow;

            // Only 3 observations (< minimum 5)
            await kernel.Observations.RecordObservationAsync(CreateObservation(TenantA, "trend_kpi", 10m, now.AddHours(-3)));
            await kernel.Observations.RecordObservationAsync(CreateObservation(TenantA, "trend_kpi", 20m, now.AddHours(-2)));
            await kernel.Observations.RecordObservationAsync(CreateObservation(TenantA, "trend_kpi", 30m, now.AddHours(-1)));

            var trend = await kernel.Trends.AnalyzeTrendAsync(TenantA, "trend_kpi");

            Assert.NotNull(trend);
            Assert.Equal(TrendDirection.Unknown, trend.Direction);
        }

        [Fact]
        public async Task BIK06_02_SteadyUpwardTrend_DetectedWithHighRSquared()
        {
            var kernel = CreateKernel();
            await kernel.Registry.RegisterKpiAsync(new KpiDefinition("up_kpi", TenantA, "Up", "Desc", "u", KpiNature.Leading, TimeSpan.FromHours(24)));
            var now = DateTime.UtcNow;

            // Strict linear increase: 10, 20, 30, 40, 50, 60
            for (var i = 0; i < 6; i++)
            {
                await kernel.Observations.RecordObservationAsync(CreateObservation(TenantA, "up_kpi", 10m + i * 10m, now.AddHours(-(6 - i))));
            }

            var trend = await kernel.Trends.AnalyzeTrendAsync(TenantA, "up_kpi");

            Assert.NotNull(trend);
            Assert.True(trend.Direction == TrendDirection.SteadyUp || trend.Direction == TrendDirection.AcceleratingUp);
            Assert.True(trend.Velocity > 0);
            Assert.True(trend.RSquared > 0.95m);
        }

        [Fact]
        public async Task BIK06_03_SteadyDownwardTrend_DetectedWithNegativeVelocity()
        {
            var kernel = CreateKernel();
            await kernel.Registry.RegisterKpiAsync(new KpiDefinition("down_kpi", TenantA, "Down", "Desc", "u", KpiNature.Leading, TimeSpan.FromHours(24)));
            var now = DateTime.UtcNow;

            // Linear decrease: 100, 90, 80, 70, 60, 50
            for (var i = 0; i < 6; i++)
            {
                await kernel.Observations.RecordObservationAsync(CreateObservation(TenantA, "down_kpi", 100m - i * 10m, now.AddHours(-(6 - i))));
            }

            var trend = await kernel.Trends.AnalyzeTrendAsync(TenantA, "down_kpi");

            Assert.NotNull(trend);
            Assert.True(trend.Direction == TrendDirection.SteadyDown || trend.Direction == TrendDirection.AcceleratingDown);
            Assert.True(trend.Velocity < 0);
        }

        [Fact]
        public async Task BIK06_04_FlatSeries_DetectedAsFlatTrend()
        {
            var kernel = CreateKernel();
            await kernel.Registry.RegisterKpiAsync(new KpiDefinition("flat_kpi", TenantA, "Flat", "Desc", "u", KpiNature.Leading, TimeSpan.FromHours(24)));
            var now = DateTime.UtcNow;

            for (var i = 0; i < 6; i++)
            {
                await kernel.Observations.RecordObservationAsync(CreateObservation(TenantA, "flat_kpi", 50m, now.AddHours(-(6 - i))));
            }

            var trend = await kernel.Trends.AnalyzeTrendAsync(TenantA, "flat_kpi");

            Assert.NotNull(trend);
            Assert.Equal(TrendDirection.Flat, trend.Direction);
        }

        // =====================================================================
        // BIK-07: Metric Correlation vs Causation Guard (Invariant I18-A)
        // =====================================================================

        [Fact]
        public async Task BIK07_01_CorrelatedMetrics_ProduceHighPearsonCoefficient()
        {
            var kernel = CreateKernel();
            var now = DateTime.UtcNow;

            // Co-moving metrics: A and B move in lockstep
            for (var i = 0; i < 12; i++)
            {
                var t = now.AddHours(-(12 - i));
                await kernel.Observations.RecordObservationAsync(CreateObservation(TenantA, "metric_a", 10m + i * 5m, t));
                await kernel.Observations.RecordObservationAsync(CreateObservation(TenantA, "metric_b", 20m + i * 10m, t));
            }

            var rel = await kernel.Relationships.AnalyzeRelationshipAsync(TenantA, "metric_a", "metric_b");

            Assert.NotNull(rel);
            Assert.True(rel.PearsonCoefficient > 0.95m);
        }

        [Fact]
        public async Task BIK07_02_InvariantI18A_IsCausalMustAlwaysBeFalse()
        {
            var kernel = CreateKernel();
            var now = DateTime.UtcNow;

            for (var i = 0; i < 12; i++)
            {
                var t = now.AddHours(-(12 - i));
                await kernel.Observations.RecordObservationAsync(CreateObservation(TenantA, "m_x", 10m + i, t));
                await kernel.Observations.RecordObservationAsync(CreateObservation(TenantA, "m_y", 10m + i, t));
            }

            var rel = await kernel.Relationships.AnalyzeRelationshipAsync(TenantA, "m_x", "m_y");

            Assert.NotNull(rel);
            // Invariant I18-A: Correlation != Causation
            Assert.False(rel.IsCausal);
        }

        [Fact]
        public async Task BIK07_03_InsufficientSample_ReturnsNullRelationship()
        {
            var kernel = CreateKernel();
            var now = DateTime.UtcNow;

            // Only 4 pairs (< minimum 10)
            for (var i = 0; i < 4; i++)
            {
                var t = now.AddHours(-(4 - i));
                await kernel.Observations.RecordObservationAsync(CreateObservation(TenantA, "m_low1", 10m + i, t));
                await kernel.Observations.RecordObservationAsync(CreateObservation(TenantA, "m_low2", 10m + i, t));
            }

            var rel = await kernel.Relationships.AnalyzeRelationshipAsync(TenantA, "m_low1", "m_low2");

            // Invariant I18-C: Must not invent correlation on small samples
            Assert.Null(rel);
        }

        // =====================================================================
        // BIK-08: Observed vs Interpreted Business State (User Amendment #5)
        // =====================================================================

        [Fact]
        public async Task BIK08_01_HealthyObservedState_ProducesStableInterpretedState()
        {
            var kernel = CreateKernel();
            var observed = new ObservedBusinessState(
                StateId: "obs-state-1",
                TenantId: TenantA,
                LiquidReserveCash: 12000000m,
                VerifiedGrossMarginPercentage: 31.4m,
                ActiveCustomerCount: 420,
                CacObserved: 1850m,
                ActiveMissionsCount: 7,
                ActiveWorkersCount: 4,
                ObservedAtUtc: DateTime.UtcNow,
                TelemetryRecordIds: new[] { "TEL-001", "TEL-002" });

            var interpreted = await kernel.StateInterpreter.InterpretStateAsync(TenantA, observed);

            Assert.NotNull(interpreted);
            Assert.Equal(BusinessStabilityRegime.Stable, interpreted.StabilityRegime);
            Assert.True(interpreted.HealthIndex >= 85m);
        }

        [Fact]
        public async Task BIK08_02_CriticalLiquidityDeficit_ForcesCriticalRegime()
        {
            var kernel = CreateKernel();
            var observed = new ObservedBusinessState(
                StateId: "obs-state-2",
                TenantId: TenantA,
                LiquidReserveCash: 350000m, // Below 5L critical floor
                VerifiedGrossMarginPercentage: 28.0m,
                ActiveCustomerCount: 150,
                CacObserved: 2100m,
                ActiveMissionsCount: 2,
                ActiveWorkersCount: 2,
                ObservedAtUtc: DateTime.UtcNow,
                TelemetryRecordIds: new[] { "TEL-003" });

            var interpreted = await kernel.StateInterpreter.InterpretStateAsync(TenantA, observed);

            Assert.NotNull(interpreted);
            Assert.Equal(BusinessStabilityRegime.Critical, interpreted.StabilityRegime);
        }

        [Fact]
        public async Task BIK08_03_ActiveAnomalies_DegradeStabilityRegime()
        {
            var kernel = CreateKernel();
            await kernel.Registry.RegisterKpiAsync(new KpiDefinition("churn", TenantA, "Churn", "Desc", "users", KpiNature.Leading, TimeSpan.FromHours(24)));
            var now = DateTime.UtcNow;

            for (var i = 0; i < 8; i++)
            {
                await kernel.Observations.RecordObservationAsync(CreateObservation(TenantA, "churn", 2m + (i % 2), now.AddHours(-(10 - i))));
            }
            // Trigger critical anomaly
            await kernel.Observations.RecordObservationAsync(CreateObservation(TenantA, "churn", 25m, now));
            await kernel.Anomalies.DetectAnomalyAsync(TenantA, "churn");

            var observed = new ObservedBusinessState(
                StateId: "obs-state-3",
                TenantId: TenantA,
                LiquidReserveCash: 5000000m,
                VerifiedGrossMarginPercentage: 29.0m,
                ActiveCustomerCount: 300,
                CacObserved: 1900m,
                ActiveMissionsCount: 5,
                ActiveWorkersCount: 3,
                ObservedAtUtc: now,
                TelemetryRecordIds: new[] { "TEL-004" });

            var interpreted = await kernel.StateInterpreter.InterpretStateAsync(TenantA, observed);

            Assert.NotNull(interpreted);
            Assert.Equal(BusinessStabilityRegime.Critical, interpreted.StabilityRegime);
            Assert.True(interpreted.ActiveAnomaliesCount > 0);
        }

        // =====================================================================
        // BIK-09: Deterministic Reproducibility via AnalysisRecord (User Amendment #4)
        // =====================================================================

        [Fact]
        public async Task BIK09_01_AnalysisRecord_GeneratedAndHashVerified()
        {
            var kernel = CreateKernel();
            await kernel.Registry.RegisterKpiAsync(new KpiDefinition("k_rep", TenantA, "Rep", "Desc", "u", KpiNature.Leading, TimeSpan.FromHours(24)));
            var now = DateTime.UtcNow;

            for (var i = 0; i < 8; i++)
            {
                await kernel.Observations.RecordObservationAsync(CreateObservation(TenantA, "k_rep", 10m + (i % 2), now.AddHours(-(10 - i))));
            }
            await kernel.Observations.RecordObservationAsync(CreateObservation(TenantA, "k_rep", 100m, now));

            var anomaly = await kernel.Anomalies.DetectAnomalyAsync(TenantA, "k_rep");
            Assert.NotNull(anomaly);

            // Anomaly must cite a valid AnalysisRecordId
            Assert.False(string.IsNullOrWhiteSpace(anomaly.AnalysisRecordId));
        }

        [Fact]
        public void BIK09_02_TamperedAnalysisRecord_FailsIntegrityHash()
        {
            var inputHashes = new[] { "hash-1", "hash-2" };
            var originalHash = AnalysisRecord.ComputeHash(TenantA, "m1", inputHashes, "ZScore", "1.0", "result-A");
            var tamperedHash = AnalysisRecord.ComputeHash(TenantA, "m1", inputHashes, "ZScore", "1.0", "result-B-tampered");

            Assert.NotEqual(originalHash, tamperedHash);
        }

        // =====================================================================
        // BIK-10: Evidence-Linked Insight Explanations (Invariants I18-B & I18-H)
        // =====================================================================

        [Fact]
        public async Task BIK10_01_InsightWithVerifiedTelemetry_Succeeds()
        {
            var kernel = CreateKernel();
            await kernel.Registry.RegisterKpiAsync(new KpiDefinition("margin_kpi", TenantA, "Margin", "Desc", "%", KpiNature.Lagging, TimeSpan.FromHours(24)));
            var now = DateTime.UtcNow;

            await kernel.Observations.RecordObservationAsync(CreateObservation(TenantA, "margin_kpi", 28.5m, now));

            var insight = await kernel.Explainer.GenerateInsightAsync(
                tenantId: TenantA,
                metricId: "margin_kpi",
                kind: EpistemicKind.Inference,
                title: "Margin Compression Detected",
                narrative: "Gross margin declined by 1.2% due to increased cloud infrastructure allocation.");

            Assert.NotNull(insight);
            Assert.Equal(EpistemicKind.Inference, insight.Kind);
            Assert.NotEmpty(insight.EvidenceHashes);
        }

        [Fact]
        public async Task BIK10_02_InvariantI18B_NoEvidence_ThrowsInvalidOperationException()
        {
            var kernel = CreateKernel();

            // Attempting to generate an insight for a metric with ZERO telemetry
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                kernel.Explainer.GenerateInsightAsync(
                    tenantId: TenantA,
                    metricId: "ghost_metric_without_data",
                    kind: EpistemicKind.Fact,
                    title: "Fabricated Claim",
                    narrative: "This should be rejected under Invariant I18-B."));
        }

        [Fact]
        public async Task BIK10_03_InsightEpistemicKind_IsExplicitlyPreserved()
        {
            var kernel = CreateKernel();
            await kernel.Registry.RegisterKpiAsync(new KpiDefinition("m_kind", TenantA, "Kind", "Desc", "u", KpiNature.Coincident, TimeSpan.FromHours(24)));
            await kernel.Observations.RecordObservationAsync(CreateObservation(TenantA, "m_kind", 50m, DateTime.UtcNow));

            var infInsight = await kernel.Explainer.GenerateInsightAsync(TenantA, "m_kind", EpistemicKind.Inference, "Inf", "Desc");
            var hypInsight = await kernel.Explainer.GenerateInsightAsync(TenantA, "m_kind", EpistemicKind.Hypothesis, "Hyp", "Desc");

            Assert.Equal(EpistemicKind.Inference, infInsight.Kind);
            Assert.Equal(EpistemicKind.Hypothesis, hypInsight.Kind);
            Assert.NotEqual(infInsight.Kind, hypInsight.Kind);
        }

        // =====================================================================
        // BIK-11: Zero Authority & Firewall Sovereignty (Invariants I18-F, I18-I, I18-J)
        // =====================================================================

        [Fact]
        public void BIK11_01_KernelTypes_CannotIssueExecutionPermits()
        {
            var kernelType = typeof(BusinessIntelligenceKernel);
            var methods = kernelType.GetMethods().Select(m => m.Name).ToList();

            // Ensure no methods grant execution authority or issue permits
            Assert.DoesNotContain("IssuePermit", methods);
            Assert.DoesNotContain("AuthorizeExecution", methods);
            Assert.DoesNotContain("BypassFirewall", methods);
        }

        [Fact]
        public void BIK11_02_InsightsCannotBypassFirewall()
        {
            var insightType = typeof(EvidenceLinkedInsight);
            var properties = insightType.GetProperties().Select(p => p.Name).ToList();

            Assert.DoesNotContain("ExecutionPermit", properties);
            Assert.DoesNotContain("BypassToken", properties);
        }

        // =====================================================================
        // BIK-12: Full End-to-End Kernel Orchestrator Integration
        // =====================================================================

        [Fact]
        public async Task BIK12_01_FullLifecycle_IngestToAnomalyToTrendToInsight()
        {
            var kernel = CreateKernel();
            var now = DateTime.UtcNow;

            await kernel.Registry.RegisterKpiAsync(new KpiDefinition("cac", TenantA, "CAC", "Customer Acquisition Cost", "INR", KpiNature.Leading, TimeSpan.FromHours(24)));

            // Ingest 8 baseline observations
            for (var i = 0; i < 8; i++)
            {
                var obs = CreateObservation(TenantA, "cac", 1800m + (i % 3) * 20m, now.AddHours(-(10 - i)));
                await kernel.IngestObservationAsync(obs);
            }

            // Ingest severe CAC spike
            var spikeObs = CreateObservation(TenantA, "cac", 3500m, now);
            await kernel.IngestObservationAsync(spikeObs);

            // Verify anomaly detected
            var activeAnomalies = await kernel.Anomalies.GetAllActiveAnomaliesAsync(TenantA);
            Assert.NotEmpty(activeAnomalies);
            Assert.Equal("cac", activeAnomalies[0].MetricId);
            Assert.Equal(AnomalyDirection.Surge, activeAnomalies[0].Direction);

            // Verify trend computed
            var trend = await kernel.Trends.AnalyzeTrendAsync(TenantA, "cac");
            Assert.NotNull(trend);
            Assert.True(trend.Velocity > 0);

            // Generate verified insight
            var insight = await kernel.Explainer.GenerateInsightAsync(
                TenantA,
                "cac",
                EpistemicKind.Inference,
                "CAC Surge Alert",
                "Customer acquisition cost surged by 90% in the last 2 hours.",
                anomalyId: activeAnomalies[0].AnomalyId,
                trendId: trend.TrendId);

            Assert.NotNull(insight);
            Assert.Equal("cac", insight.ObservedMetricId);
            Assert.Equal(activeAnomalies[0].AnomalyId, insight.AnomalyId);

            // Verify enterprise state interpretation
            var observedState = new ObservedBusinessState(
                StateId: "obs-e2e",
                TenantId: TenantA,
                LiquidReserveCash: 8000000m,
                VerifiedGrossMarginPercentage: 27.5m,
                ActiveCustomerCount: 500,
                CacObserved: 3500m,
                ActiveMissionsCount: 4,
                ActiveWorkersCount: 3,
                ObservedAtUtc: now,
                TelemetryRecordIds: new[] { spikeObs.SourceRecordId });

            var interpretedState = await kernel.EvaluateEnterpriseStateAsync(TenantA, observedState);

            Assert.NotNull(interpretedState);
            Assert.Equal(1, interpretedState.ActiveAnomaliesCount);
            Assert.Equal(BusinessStabilityRegime.Critical, interpretedState.StabilityRegime);
        }
    }
}
