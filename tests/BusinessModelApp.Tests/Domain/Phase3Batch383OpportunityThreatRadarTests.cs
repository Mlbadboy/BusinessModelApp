using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Forecasting;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Kernel;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Radar;
using BusinessModelApp.Infrastructure.Runtime.Intelligence.Radar;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public class Phase3Batch383OpportunityThreatRadarTests
    {
        private readonly SignificanceScorer _scorer;
        private readonly OpportunityDetector _oppDetector;
        private readonly ThreatDetector _threatDetector;
        private readonly RadarConstraintEvaluator _constraintEvaluator;
        private readonly InMemoryRadarSignalStore _store;
        private readonly OpportunityThreatRadarOrchestrator _orchestrator;

        public Phase3Batch383OpportunityThreatRadarTests()
        {
            _scorer = new SignificanceScorer();
            _oppDetector = new OpportunityDetector(_scorer);
            _threatDetector = new ThreatDetector(_scorer);
            _constraintEvaluator = new RadarConstraintEvaluator();
            _store = new InMemoryRadarSignalStore();
            _orchestrator = new OpportunityThreatRadarOrchestrator(
                _oppDetector,
                _threatDetector,
                _constraintEvaluator,
                _store,
                _scorer);
        }

        private static KpiObservation CreateObservation(string id, string tenant, string metric, decimal val, int daysAgo = 0)
        {
            return new KpiObservation(
                ObservationId: id,
                TenantId: tenant,
                MetricId: metric,
                Value: val,
                ObservedAtUtc: DateTime.UtcNow.AddDays(-daysAgo),
                SourceRecordId: $"SRC-{id}",
                AuditLedgerTx: $"TX-{id}",
                IntegrityHash: $"hash-{id}");
        }

        // =========================================================================
        // OTE-01: OPPORTUNITY DETECTION
        // =========================================================================

        [Fact]
        public async Task OTE01_01_PositiveInflection_DetectsRevenueExpansionOpportunity()
        {
            var observations = new List<KpiObservation>
            {
                CreateObservation("OBS-01", "tenant-alpha", "conversion_rate", 2.0m, 2),
                CreateObservation("OBS-02", "tenant-alpha", "conversion_rate", 2.6m, 0) // +30%
            };

            var signals = await _oppDetector.DetectOpportunitiesAsync("tenant-alpha", observations, new List<ForecastOutput>());

            Assert.Single(signals);
            var opp = signals[0];
            Assert.Equal(RadarSignalType.Opportunity, opp.Type);
            Assert.Equal(OpportunityCategory.RevenueExpansion, opp.OpportunityCategory);
            Assert.Equal(RadarEpistemicTier.FactBacked, opp.EpistemicTier);
            Assert.True(opp.Breakdown.SignificanceScore > 0);
        }

        [Fact]
        public async Task OTE01_02_ValidUpwardForecast_DetectsForwardOpportunity()
        {
            var intervals = new List<PredictionInterval>
            {
                new(1, DateTime.UtcNow.AddDays(1), 40000m, 50000m, 60000m, 0.90m, IntervalCalibrationMethod.ParametricStandardErrorScaling),
                new(2, DateTime.UtcNow.AddDays(2), 60000m, 75000m, 90000m, 0.90m, IntervalCalibrationMethod.ParametricStandardErrorScaling)
            };

            var forecast = new ForecastOutput(
                ForecastId: "FC-01",
                TenantId: "tenant-alpha",
                MetricId: "mrr_growth",
                ModelId: "linear-v1",
                ModelVersion: "1.0.0",
                HorizonSteps: 2,
                Intervals: intervals,
                UncertaintyRadius: 1000m,
                ApplicabilityStatus: ForecastApplicabilityStatus.Valid,
                StatedAssumptions: new List<string>(),
                GeneratedAtUtc: DateTime.UtcNow,
                ProvenanceHash: "prov-fc-01");

            var signals = await _oppDetector.DetectOpportunitiesAsync("tenant-alpha", new List<KpiObservation>(), new[] { forecast });

            Assert.Single(signals);
            var opp = signals[0];
            Assert.Equal(RadarSignalType.Opportunity, opp.Type);
            Assert.Equal(RadarEpistemicTier.ForecastDerived, opp.EpistemicTier);
            Assert.Equal("FC-01", opp.Linkage.ForecastOutputId);
        }

        // =========================================================================
        // OTE-02: THREAT DETECTION
        // =========================================================================

        [Fact]
        public async Task OTE02_01_CommercialDrop_DetectsThreatWithAppropriateUrgency()
        {
            var observations = new List<KpiObservation>
            {
                CreateObservation("OBS-10", "tenant-alpha", "gross_margin", 65.0m, 2),
                CreateObservation("OBS-11", "tenant-alpha", "gross_margin", 45.0m, 0) // -30%
            };

            var threats = await _threatDetector.DetectThreatsAsync("tenant-alpha", observations, new List<ForecastOutput>());

            Assert.Single(threats);
            var threat = threats[0];
            Assert.Equal(RadarSignalType.Threat, threat.Type);
            Assert.Equal(ThreatCategory.CommercialDeterioration, threat.ThreatCategory);
            Assert.Equal(SignalUrgency.Immediate, threat.Urgency);
        }

        [Fact]
        public async Task OTE02_02_StructuralRegimeShift_DetectsDemandShockThreat()
        {
            var regimeAssessments = new List<RegimeAssessment>
            {
                new("REG-01", "tenant-alpha", "user_churn", RegimeShiftStatus.Detected, 2.45m, true, true, "Residual break", DateTime.UtcNow)
            };

            var threats = await _threatDetector.DetectThreatsAsync(
                "tenant-alpha",
                new List<KpiObservation>(),
                new List<ForecastOutput>(),
                regimeAssessments);

            Assert.Single(threats);
            Assert.Equal(ThreatCategory.DemandShock, threats[0].ThreatCategory);
        }

        // =========================================================================
        // OTE-03: DETERMINISTIC MULTI-FACTOR SCORING
        // =========================================================================

        [Fact]
        public void OTE03_01_SignificanceScorer_ProducesDeterministicNormalizedScores()
        {
            var policy = _scorer.GetActivePolicy("tenant-alpha");

            var score1 = _scorer.ScoreSignal(RadarSignalType.Opportunity, 5000000m, 80.0, SignalUrgency.High, 4, 70.0, 90.0, policy);
            var score2 = _scorer.ScoreSignal(RadarSignalType.Opportunity, 5000000m, 80.0, SignalUrgency.High, 4, 70.0, 90.0, policy);

            Assert.Equal(score1.SignificanceScore, score2.SignificanceScore);
            Assert.Equal(score1.Priority, score2.Priority);
            Assert.InRange(score1.SignificanceScore, 0.0, 100.0);
            Assert.InRange(score1.ImpactScore, 0.0, 100.0);
        }

        // =========================================================================
        // OTE-04: EVIDENCE MANDATORY INVARIANT (I21-C)
        // =========================================================================

        [Fact]
        public async Task OTE04_01_EmptyObservationsAndForecasts_ProducesZeroSignals()
        {
            var scan = await _orchestrator.ExecuteRadarScanAsync("tenant-alpha", new List<KpiObservation>(), new List<ForecastOutput>());

            Assert.Equal(0, scan.OpportunitiesDetected);
            Assert.Equal(0, scan.ThreatsDetected);
            Assert.Empty(scan.Signals);
        }

        // =========================================================================
        // OTE-05: CAUSAL & FORECAST LINKAGE (I21-D)
        // =========================================================================

        [Fact]
        public async Task OTE05_01_SignalPreservesForecastAndObservationLinkage()
        {
            var intervals = new List<PredictionInterval>
            {
                new(1, DateTime.UtcNow.AddDays(1), 180000m, 200000m, 220000m, 0.95m, IntervalCalibrationMethod.ParametricStandardErrorScaling)
            };

            var forecast = new ForecastOutput(
                ForecastId: "FC-LINK-01",
                TenantId: "tenant-alpha",
                MetricId: "acv",
                ModelId: "linear-v1",
                ModelVersion: "1.0.0",
                HorizonSteps: 1,
                Intervals: intervals,
                UncertaintyRadius: 1000m,
                ApplicabilityStatus: ForecastApplicabilityStatus.Valid,
                StatedAssumptions: new List<string>(),
                GeneratedAtUtc: DateTime.UtcNow,
                ProvenanceHash: "prov-link");

            var scan = await _orchestrator.ExecuteRadarScanAsync("tenant-alpha", new List<KpiObservation>(), new[] { forecast });

            Assert.NotEmpty(scan.Signals);
            var signal = scan.Signals[0];
            Assert.Equal("FC-LINK-01", signal.Linkage.ForecastOutputId);
            Assert.NotNull(signal.Linkage.ForecastConfidence);
        }

        // =========================================================================
        // OTE-06: UNCERTAINTY & STALE DATA HANDLING (I21-E)
        // =========================================================================

        [Fact]
        public void OTE06_01_LowConfidence_ClampsPriorityToInformational()
        {
            var policy = _scorer.GetActivePolicy("tenant-alpha");

            // Even with enormous impact and immediate urgency, low confidence (< policy threshold 40.0) fails-closed
            var breakdown = _scorer.ScoreSignal(RadarSignalType.Threat, 50000000m, 95.0, SignalUrgency.Immediate, 5, 100.0, 25.0, policy);

            Assert.Equal(RadarPriority.Informational, breakdown.Priority);
        }

        // =========================================================================
        // OTE-07: CONSTRAINT GATING & SAFE ALTERNATIVES (I21-G)
        // =========================================================================

        [Fact]
        public async Task OTE07_01_CapitalConstraintExceeded_FlagsConstrainedWithAlternatives()
        {
            var highCapOpp = new RadarSignal
            {
                TenantId = "tenant-alpha",
                Type = RadarSignalType.Opportunity,
                EstimatedMonetaryImpactINR = 15000000m // ₹1.5 Cr > ₹20L ceiling
            };

            var result = await _constraintEvaluator.EvaluateConstraintsAsync("tenant-alpha", highCapOpp);

            Assert.Equal(ConstraintFeasibilityStatus.Constrained, result.Status);
            Assert.NotNull(result.ViolationReason);
            Assert.NotEmpty(result.SafeAlternatives);
        }

        // =========================================================================
        // OTE-08: 8-STATE LIFECYCLE & TRANSITIONS
        // =========================================================================

        [Fact]
        public async Task OTE08_01_LifecycleTransitions_ProgressThroughValidStates()
        {
            var signal = new RadarSignal
            {
                Id = "SIG-LIFECYCLE-01",
                TenantId = "tenant-alpha",
                LifecycleState = RadarSignalLifecycleState.Detected
            };
            await _store.SaveSignalAsync(signal);

            await _store.UpdateLifecycleStateAsync("tenant-alpha", "SIG-LIFECYCLE-01", RadarSignalLifecycleState.Active, "Corroborated");
            var retrieved = await _store.GetSignalAsync("tenant-alpha", "SIG-LIFECYCLE-01");
            Assert.Equal(RadarSignalLifecycleState.Active, retrieved!.LifecycleState);

            await _store.UpdateLifecycleStateAsync("tenant-alpha", "SIG-LIFECYCLE-01", RadarSignalLifecycleState.ActionRecommended, "Ready for executive review");
            retrieved = await _store.GetSignalAsync("tenant-alpha", "SIG-LIFECYCLE-01");
            Assert.Equal(RadarSignalLifecycleState.ActionRecommended, retrieved!.LifecycleState);

            await _store.UpdateLifecycleStateAsync("tenant-alpha", "SIG-LIFECYCLE-01", RadarSignalLifecycleState.Mitigated, "Resolved");
            retrieved = await _store.GetSignalAsync("tenant-alpha", "SIG-LIFECYCLE-01");
            Assert.Equal(RadarSignalLifecycleState.Mitigated, retrieved!.LifecycleState);
        }

        // =========================================================================
        // OTE-09: ZERO EXECUTION AUTHORITY (I21-H)
        // =========================================================================

        [Fact]
        public void OTE09_01_RadarSignal_PossessesZeroExecutionPermitAuthority()
        {
            var signal = new RadarSignal
            {
                LifecycleState = RadarSignalLifecycleState.ActionRecommended,
                Priority = RadarPriority.Critical
            };

            Assert.False(signal.IsActionAuthorized);
            Assert.False(signal.IsEmergencyBypassAllowed);
        }

        // =========================================================================
        // OTE-10: CRYPTOGRAPHIC PROVENANCE (I21-I)
        // =========================================================================

        [Fact]
        public async Task OTE10_01_OrchestrationGeneratesValidSha256Provenance()
        {
            var observations = new List<KpiObservation>
            {
                CreateObservation("OBS-PROV-1", "tenant-alpha", "win_rate", 20.0m, 1),
                CreateObservation("OBS-PROV-2", "tenant-alpha", "win_rate", 28.0m, 0)
            };

            var scan = await _orchestrator.ExecuteRadarScanAsync("tenant-alpha", observations, new List<ForecastOutput>());

            Assert.NotEmpty(scan.Signals);
            var sig = scan.Signals[0];
            Assert.False(string.IsNullOrEmpty(sig.ProvenanceHash));
            Assert.Equal(64, sig.ProvenanceHash.Length);

            var record = await _store.GetAnalysisRecordAsync("tenant-alpha", sig.Id);
            Assert.NotNull(record);
            Assert.Equal(sig.ProvenanceHash, record!.IntegrityHash);
        }

        // =========================================================================
        // OTE-11: FULL RADAR ORCHESTRATION PIPELINE
        // =========================================================================

        [Fact]
        public async Task OTE11_01_FullScanPipeline_CoordinatesBothOpportunitiesAndThreats()
        {
            var observations = new List<KpiObservation>
            {
                // Opportunity
                CreateObservation("OBS-OPP-1", "tenant-alpha", "pipeline_velocity", 100m, 1),
                CreateObservation("OBS-OPP-2", "tenant-alpha", "pipeline_velocity", 150m, 0),
                // Threat
                CreateObservation("OBS-THR-1", "tenant-alpha", "retention_rate", 95m, 1),
                CreateObservation("OBS-THR-2", "tenant-alpha", "retention_rate", 70m, 0)
            };

            var result = await _orchestrator.ExecuteRadarScanAsync("tenant-alpha", observations, new List<ForecastOutput>());

            Assert.True(result.OpportunitiesDetected >= 1);
            Assert.True(result.ThreatsDetected >= 1);
            Assert.NotEmpty(result.Signals);
        }

        // =========================================================================
        // OTE-12: MULTI-TENANT DATA ISOLATION (I21-J)
        // =========================================================================

        [Fact]
        public async Task OTE12_01_SignalsAreStrictlyIsolatedByTenant()
        {
            var signalA = new RadarSignal { Id = "SIG-A", TenantId = "tenant-A", Title = "Confidential A" };
            var signalB = new RadarSignal { Id = "SIG-B", TenantId = "tenant-B", Title = "Confidential B" };

            await _store.SaveSignalAsync(signalA);
            await _store.SaveSignalAsync(signalB);

            var retrievedA = await _store.GetSignalsAsync("tenant-A");
            var retrievedB = await _store.GetSignalsAsync("tenant-B");

            Assert.Contains(retrievedA, s => s.Id == "SIG-A");
            Assert.DoesNotContain(retrievedA, s => s.Id == "SIG-B");

            Assert.Contains(retrievedB, s => s.Id == "SIG-B");
            Assert.DoesNotContain(retrievedB, s => s.Id == "SIG-A");
        }

        // =========================================================================
        // OTE-13: AI PROMPT INJECTION RESISTANCE
        // =========================================================================

        [Fact]
        public async Task OTE13_01_InjectionPayloadInMetric_DoesNotBypassGovernance()
        {
            var maliciousObservation = new List<KpiObservation>
            {
                CreateObservation("OBS-MAL-1", "tenant-alpha", "OVERRIDE_ALL; EXECUTE_NOW = true; DROP TABLE", 10m, 1),
                CreateObservation("OBS-MAL-2", "tenant-alpha", "OVERRIDE_ALL; EXECUTE_NOW = true; DROP TABLE", 25m, 0)
            };

            var scan = await _orchestrator.ExecuteRadarScanAsync("tenant-alpha", maliciousObservation, new List<ForecastOutput>());

            foreach (var sig in scan.Signals)
            {
                Assert.False(sig.IsActionAuthorized);
                Assert.False(sig.IsEmergencyBypassAllowed);
            }
        }

        // =========================================================================
        // OTE-14: SIGNAL SCORE DETERMINISM
        // =========================================================================

        [Fact]
        public void OTE14_01_IdenticalInputs_YieldIdenticalScoresAcrossRuns()
        {
            var policy = _scorer.GetActivePolicy("tenant-alpha");

            var run1 = _scorer.ScoreSignal(RadarSignalType.Threat, 2500000m, 75.0, SignalUrgency.High, 3, 50.0, 85.0, policy);
            var run2 = _scorer.ScoreSignal(RadarSignalType.Threat, 2500000m, 75.0, SignalUrgency.High, 3, 50.0, 85.0, policy);

            Assert.Equal(run1.SignificanceScore, run2.SignificanceScore);
            Assert.Equal(run1.ImpactScore, run2.ImpactScore);
            Assert.Equal(run1.Priority, run2.Priority);
        }

        // =========================================================================
        // OTE-15: FORECAST CONFIDENCE CLAMP
        // =========================================================================

        [Fact]
        public async Task OTE15_01_LowConfidenceForecast_ClampsRadarConfidenceAndEpistemicTier()
        {
            var intervals = new List<PredictionInterval>
            {
                new(1, DateTime.UtcNow.AddDays(1), 40000m, 50000m, 60000m, 0.20m, IntervalCalibrationMethod.ParametricStandardErrorScaling) // 20% coverage (< 40% threshold)
            };

            var lowCoverageForecast = new ForecastOutput(
                ForecastId: "FC-LOW-CONF",
                TenantId: "tenant-alpha",
                MetricId: "speculative_sales",
                ModelId: "linear-v1",
                ModelVersion: "1.0.0",
                HorizonSteps: 1,
                Intervals: intervals,
                UncertaintyRadius: 500m,
                ApplicabilityStatus: ForecastApplicabilityStatus.Valid,
                StatedAssumptions: new List<string>(),
                GeneratedAtUtc: DateTime.UtcNow,
                ProvenanceHash: "prov-low");

            var signals = await _oppDetector.DetectOpportunitiesAsync("tenant-alpha", new List<KpiObservation>(), new[] { lowCoverageForecast });

            Assert.Single(signals);
            var sig = signals[0];
            Assert.Equal(RadarEpistemicTier.Unknown, sig.EpistemicTier);
            Assert.Equal(RadarPriority.Informational, sig.Priority);
        }

        // =========================================================================
        // OTE-16: CAUSAL CONFIDENCE CLAMP
        // =========================================================================

        [Fact]
        public void OTE16_01_CausalConfidence_ClampsOverallSignalConfidence()
        {
            var policy = _scorer.GetActivePolicy("tenant-alpha");

            // Sub-threshold confidence forces informational priority
            var breakdown = _scorer.ScoreSignal(RadarSignalType.Opportunity, 1000000m, 80.0, SignalUrgency.Medium, 1, 50.0, 35.0, policy);

            Assert.Equal(RadarPriority.Informational, breakdown.Priority);
        }

        // =========================================================================
        // OTE-17: PROVENANCE TAMPER DETECTION
        // =========================================================================

        [Fact]
        public void OTE17_01_TamperingWithInputs_AltersIntegrityHash()
        {
            var record = new RadarAnalysisRecord
            {
                TenantId = "tenant-alpha",
                SignalId = "SIG-001",
                InputObservationHashes = "OBS-1,OBS-2",
                ScoringPolicyId = "POL-1",
                ScoringPolicyVersion = "1.0.0"
            };
            var initialHash = record.ComputeIntegrityHash();

            record.InputObservationHashes = "OBS-1,OBS-TAMPERED";
            var tamperedHash = record.ComputeIntegrityHash();

            Assert.NotEqual(initialHash, tamperedHash);
        }

        // =========================================================================
        // OTE-18: CROSS-TENANT CONTAMINATION DEFENSE
        // =========================================================================

        [Fact]
        public async Task OTE18_01_CrossTenantContamination_ObservationsDoNotLeakAcrossTenants()
        {
            var obsA = new List<KpiObservation>
            {
                CreateObservation("OBS-A-1", "tenant-A", "arr", 100m, 1),
                CreateObservation("OBS-A-2", "tenant-A", "arr", 200m, 0)
            };

            await _orchestrator.ExecuteRadarScanAsync("tenant-A", obsA, new List<ForecastOutput>());

            var tenantBSignals = await _store.GetSignalsAsync("tenant-B");
            Assert.Empty(tenantBSignals);
        }

        // =========================================================================
        // OTE-19: CONCURRENT DETECTION IDEMPOTENCY
        // =========================================================================

        [Fact]
        public async Task OTE19_01_ConcurrentScans_ExecuteSafelyWithoutCorruptingStore()
        {
            var obs = new List<KpiObservation>
            {
                CreateObservation("OBS-CONC-1", "tenant-concurrent", "cac", 500m, 1),
                CreateObservation("OBS-CONC-2", "tenant-concurrent", "cac", 250m, 0)
            };

            var tasks = Enumerable.Range(0, 5)
                .Select(_ => _orchestrator.ExecuteRadarScanAsync("tenant-concurrent", obs, new List<ForecastOutput>()))
                .ToArray();

            var results = await Task.WhenAll(tasks);

            foreach (var r in results)
            {
                Assert.NotNull(r);
                Assert.True(r.OpportunitiesDetected >= 1);
            }
        }

        // =========================================================================
        // OTE-20: CONSTRAINT MUTATION RACE HANDLING
        // =========================================================================

        [Fact]
        public async Task OTE20_01_ConcurrentConstraintEvaluations_ResolveDeterministically()
        {
            var sig = new RadarSignal
            {
                TenantId = "tenant-alpha",
                Type = RadarSignalType.Opportunity,
                EstimatedMonetaryImpactINR = 1000000m
            };

            var evalTasks = Enumerable.Range(0, 10)
                .Select(_ => _constraintEvaluator.EvaluateConstraintsAsync("tenant-alpha", sig))
                .ToArray();

            var results = await Task.WhenAll(evalTasks);

            foreach (var res in results)
            {
                Assert.Equal(ConstraintFeasibilityStatus.Satisfied, res.Status);
            }
        }

        // =========================================================================
        // OTE-21: DECAY & REINFORCEMENT METROLOGY (I21-K)
        // =========================================================================

        [Fact]
        public async Task OTE21_01_SignalDecaysOverTime_AndIsReinforcedByNewEvidence()
        {
            var signal = new RadarSignal
            {
                Id = "SIG-DECAY-01",
                TenantId = "tenant-alpha",
                InitialStrength = 80.0,
                CurrentStrength = 80.0,
                ObservationReinforcementCount = 1,
                LifecycleState = RadarSignalLifecycleState.Active
            };
            await _store.SaveSignalAsync(signal);

            // 1. Simulate 30 days elapsed -> decay
            await _store.ApplyTemporalDecayAsync("tenant-alpha", TimeSpan.FromDays(30));
            var decayed = await _store.GetSignalAsync("tenant-alpha", "SIG-DECAY-01");

            Assert.True(decayed!.CurrentStrength < 80.0);
            var decayedStrength = decayed.CurrentStrength;

            // 2. Corroborating evidence reinforces signal
            await _store.ReinforceSignalAsync("tenant-alpha", "SIG-DECAY-01", 15.0);
            var reinforced = await _store.GetSignalAsync("tenant-alpha", "SIG-DECAY-01");

            Assert.True(reinforced!.CurrentStrength > decayedStrength);
            Assert.Equal(2, reinforced.ObservationReinforcementCount);
        }

        // =========================================================================
        // OTE-22: UNKNOWN UPSTREAM PROPAGATION (I21-E, I21-L)
        // =========================================================================

        [Fact]
        public async Task OTE22_01_InsufficientObservations_DoesNotFabricateConfidentSignal()
        {
            // Only 1 observation -> cannot determine trend or rate of change
            var singleObservation = new List<KpiObservation>
            {
                CreateObservation("OBS-SOLITARY", "tenant-alpha", "nps", 50m, 0)
            };

            var signals = await _oppDetector.DetectOpportunitiesAsync("tenant-alpha", singleObservation, new List<ForecastOutput>());

            Assert.Empty(signals); // Zero fabrication when evidence is insufficient
        }
    }
}
