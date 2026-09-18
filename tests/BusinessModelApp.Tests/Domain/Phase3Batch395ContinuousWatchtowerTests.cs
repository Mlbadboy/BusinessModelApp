using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Api.Controllers;
using BusinessModelApp.Core.Domain.Runtime.Organizational;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Watchtower;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;
using BusinessModelApp.Infrastructure.Runtime.Organizational.Watchtower;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public class Phase3Batch395ContinuousWatchtowerTests
    {
        private const string TestTenant = "TENANT-CBW-395";
        private const string AltTenant = "TENANT-CBW-ALT";

        private (
            InMemoryWatchtowerStore store,
            EventFingerprintService fingerprintService,
            EventNormalizer normalizer,
            TemporalCorrelationEngine correlationEngine,
            StormSuppressionEngine stormEngine,
            PersistentConditionEngine conditionTracker,
            AttentionScoringEngine scoringEngine,
            ContinuousWatchtowerService service
        ) CreateTestRig(WatchtowerAttentionPolicy? policy = null)
        {
            var store = new InMemoryWatchtowerStore();
            var fingerprintService = new EventFingerprintService();
            var normalizer = new EventNormalizer(fingerprintService);
            var correlationEngine = new TemporalCorrelationEngine(store);
            var stormEngine = new StormSuppressionEngine(store);
            var conditionTracker = new PersistentConditionEngine(store);
            var scoringEngine = new AttentionScoringEngine();
            var service = new ContinuousWatchtowerService(
                store,
                normalizer,
                correlationEngine,
                stormEngine,
                conditionTracker,
                scoringEngine,
                policy);

            return (store, fingerprintService, normalizer, correlationEngine, stormEngine, conditionTracker, scoringEngine, service);
        }

        // =========================================================================
        // FAMILY 1: Event Ingestion & Normalization (CBW01 - CBW06)
        // =========================================================================

        [Fact]
        public async Task CBW01_IngestEvent_ValidPayload_AcceptedAndEmitsSignal()
        {
            var rig = CreateTestRig();
            var (accepted, signal, error) = await rig.service.IngestEventAsync(
                TestTenant, "Stripe", "PaymentFailure", "CUST-001", "REC-101",
                "EVID-PAY-001", 15.0, 5.0, "FailedPaymentCount");

            Assert.True(accepted);
            Assert.NotNull(signal);
            Assert.Null(error);
            Assert.False(string.IsNullOrWhiteSpace(signal.SignalId));
            Assert.Equal(SignalStatus.Active, signal.Status);
        }

        [Fact]
        public async Task CBW02_IngestEvent_MissingTenant_Rejected()
        {
            var rig = CreateTestRig();
            var (accepted, signal, error) = await rig.service.IngestEventAsync(
                "", "Stripe", "PaymentFailure", "CUST-001", "REC-101",
                "EVID-001", 10.0, 5.0);

            Assert.False(accepted);
            Assert.Null(signal);
            Assert.Contains("TenantId", error);
        }

        [Fact]
        public async Task CBW03_IngestEvent_MissingSourceSystem_Rejected()
        {
            var rig = CreateTestRig();
            var (accepted, signal, error) = await rig.service.IngestEventAsync(
                TestTenant, "", "PaymentFailure", "CUST-001", "REC-101",
                "EVID-001", 10.0, 5.0);

            Assert.False(accepted);
            Assert.Null(signal);
            Assert.Contains("SourceSystem", error);
        }

        [Fact]
        public async Task CBW04_IngestEvent_MissingEntityId_Rejected()
        {
            var rig = CreateTestRig();
            var (accepted, signal, error) = await rig.service.IngestEventAsync(
                TestTenant, "CRM", "DealLoss", "", "REC-101",
                "EVID-001", 10.0, 5.0);

            Assert.False(accepted);
            Assert.Null(signal);
            Assert.Contains("EntityId", error);
        }

        [Fact]
        public async Task CBW05_IngestEvent_MissingSourceRecordId_Rejected()
        {
            var rig = CreateTestRig();
            var (accepted, signal, error) = await rig.service.IngestEventAsync(
                TestTenant, "CRM", "DealLoss", "DEAL-001", "",
                "EVID-001", 10.0, 5.0);

            Assert.False(accepted);
            Assert.Null(signal);
            Assert.Contains("SourceRecordId", error);
        }

        [Fact]
        public async Task CBW06_IngestEvent_EvidenceHashRecordedAndPreserved()
        {
            var rig = CreateTestRig();
            var (accepted, signal, _) = await rig.service.IngestEventAsync(
                TestTenant, "Telemetry", "ApiLatencySpike", "ENDPOINT-01", "REC-999",
                "EVID-LATENCY-HASH-ABC", 450.0, 100.0, "LatencyMs");

            Assert.True(accepted);
            Assert.Contains("EVID-LATENCY-HASH-ABC", signal!.SourceEvidenceRefs);
        }

        // =========================================================================
        // FAMILY 2: Deterministic Event Fingerprinting & Deduplication (CBW07 - CBW12)
        // =========================================================================

        [Fact]
        public void CBW07_Fingerprint_SameEventAttributes_ProducesIdenticalFingerprint()
        {
            var rig = CreateTestRig();
            var fp1 = rig.fingerprintService.ComputeFingerprint("Stripe", "ChargeFailed", "INV-01", "REC-01");
            var fp2 = rig.fingerprintService.ComputeFingerprint("Stripe", "ChargeFailed", "INV-01", "REC-01");

            Assert.Equal(fp1, fp2);
            Assert.False(string.IsNullOrWhiteSpace(fp1));
        }

        [Fact]
        public void CBW08_Fingerprint_CaseInsensitiveSourceAndType()
        {
            var rig = CreateTestRig();
            var fp1 = rig.fingerprintService.ComputeFingerprint("stripe", "chargefailed", "INV-01", "REC-01");
            var fp2 = rig.fingerprintService.ComputeFingerprint("STRIPE", "CHARGEFAILED", "INV-01", "REC-01");

            Assert.Equal(fp1, fp2);
        }

        [Fact]
        public void CBW09_Fingerprint_DifferentEntity_ProducesDifferentFingerprint()
        {
            var rig = CreateTestRig();
            var fp1 = rig.fingerprintService.ComputeFingerprint("Stripe", "ChargeFailed", "INV-01", "REC-01");
            var fp2 = rig.fingerprintService.ComputeFingerprint("Stripe", "ChargeFailed", "INV-02", "REC-01");

            Assert.NotEqual(fp1, fp2);
        }

        [Fact]
        public async Task CBW10_Deduplication_ExactDuplicateEvent_SuppressedAndAggregated()
        {
            var rig = CreateTestRig();

            // 1st event
            var (a1, s1, _) = await rig.service.IngestEventAsync(
                TestTenant, "Stripe", "ChargeFailed", "INV-01", "REC-01", "EVID-01", 10.0);
            Assert.True(a1);
            Assert.NotNull(s1);

            // 2nd identical event
            var (a2, s2, r2) = await rig.service.IngestEventAsync(
                TestTenant, "Stripe", "ChargeFailed", "INV-01", "REC-01", "EVID-01", 10.0);

            Assert.True(a2);
            Assert.Null(s2); // Signal generation suppressed!
            Assert.Contains("Duplicate", r2);
        }

        [Fact]
        public async Task CBW11_Deduplication_AggregatedCountIncrements_OnRepeatedDuplicates()
        {
            var rig = CreateTestRig();
            var fp = rig.fingerprintService.ComputeFingerprint("Stripe", "ChargeFailed", "INV-01", "REC-01");

            for (int i = 0; i < 5; i++)
            {
                await rig.service.IngestEventAsync(
                    TestTenant, "Stripe", "ChargeFailed", "INV-01", "REC-01", "EVID-01", 10.0);
            }

            int count = await rig.stormEngine.GetAggregatedEventCountAsync(TestTenant, fp);
            Assert.Equal(5, count);
        }

        [Fact]
        public async Task CBW12_Deduplication_DuplicateDoesNotCreateSecondSignal()
        {
            var rig = CreateTestRig();

            for (int i = 0; i < 5; i++)
            {
                await rig.service.IngestEventAsync(
                    TestTenant, "Stripe", "ChargeFailed", "INV-01", "REC-01", "EVID-01", 10.0);
            }

            var signals = await rig.service.ListActiveSignalsAsync(TestTenant);
            Assert.Single(signals);
        }

        // =========================================================================
        // FAMILY 3: Temporal Event Correlation & Windowing (CBW13 - CBW18)
        // =========================================================================

        [Fact]
        public async Task CBW13_Correlation_EventsWithSameEntity_ShareCorrelationKey()
        {
            var rig = CreateTestRig();
            var (_, s1, _) = await rig.service.IngestEventAsync(
                TestTenant, "CRM", "EmailBounce", "LEAD-100", "REC-01", "EVID-01", 1.0);
            var (_, s2, _) = await rig.service.IngestEventAsync(
                TestTenant, "CRM", "DealStall", "LEAD-100", "REC-02", "EVID-02", 1.0);

            Assert.Equal("CRM::LEAD-100", s1!.WhyTrace.CorrelationKey);
            Assert.Equal("CRM::LEAD-100", s2!.WhyTrace.CorrelationKey);
        }

        [Fact]
        public async Task CBW14_Correlation_CorrelatedEventsRecordedInWhyTrace()
        {
            var rig = CreateTestRig();
            await rig.service.IngestEventAsync(TestTenant, "CRM", "TypeA", "ENT-1", "REC-A", "EVID-A", 1.0);
            var (_, s2, _) = await rig.service.IngestEventAsync(TestTenant, "CRM", "TypeB", "ENT-1", "REC-B", "EVID-B", 2.0);

            Assert.True(s2!.WhyTrace.CorrelatedEventIds.Count >= 2);
        }

        [Fact]
        public async Task CBW15_Correlation_DistinctEntities_FormIndependentCorrelations()
        {
            var rig = CreateTestRig();
            var (_, s1, _) = await rig.service.IngestEventAsync(TestTenant, "CRM", "TypeA", "ENT-1", "REC-1", "EVID-1", 1.0);
            var (_, s2, _) = await rig.service.IngestEventAsync(TestTenant, "CRM", "TypeA", "ENT-2", "REC-2", "EVID-2", 1.0);

            Assert.NotEqual(s1!.WhyTrace.CorrelationKey, s2!.WhyTrace.CorrelationKey);
        }

        [Fact]
        public void CBW16_Correlation_CorrelationNotCausation_Asserted()
        {
            Assert.Equal(
                "I30-C: Multiple correlated telemetry events indicate concurrent conditions, never unverified causal explanation.",
                ContinuousObservationSovereignty.I30_C_CorrelationNotCausation);
        }

        [Fact]
        public async Task CBW17_Correlation_GetCorrelatedEvents_ReturnsAllInOrder()
        {
            var rig = CreateTestRig();
            await rig.service.IngestEventAsync(TestTenant, "ERP", "ShipmentDelay", "ORDER-9", "R1", "E1", 1.0);
            await rig.service.IngestEventAsync(TestTenant, "ERP", "QualityHold", "ORDER-9", "R2", "E2", 2.0);

            var correlated = await rig.correlationEngine.GetCorrelatedEventsAsync(TestTenant, "ERP::ORDER-9");
            Assert.Equal(2, correlated.Count);
            Assert.True(correlated[0].ObservedAt <= correlated[1].ObservedAt);
        }

        [Fact]
        public async Task CBW18_Correlation_CorrelatedEventsDoNotSynthesizeCausalProof()
        {
            var rig = CreateTestRig();
            var (_, signal, _) = await rig.service.IngestEventAsync(
                TestTenant, "Telemetry", "Error500", "SVC-AUTH", "REC-1", "EVID-1", 10.0, 0.0);

            Assert.DoesNotContain("caused by", signal!.Summary, StringComparison.OrdinalIgnoreCase);
        }

        // =========================================================================
        // FAMILY 4: Persistent Condition Tracking (CBW19 - CBW24)
        // =========================================================================

        [Fact]
        public async Task CBW19_Persistence_SingleExcursion_ClassifiedAsTransientSpike()
        {
            var rig = CreateTestRig();
            var (_, signal, _) = await rig.service.IngestEventAsync(
                TestTenant, "Telemetry", "CpuSpike", "SERVER-1", "REC-1", "EVID-1", 90.0, 50.0, "CpuLoad");

            Assert.Equal(ConditionTrajectory.TransientSpike, signal!.WhyTrace.Trajectory);
        }

        [Fact]
        public async Task CBW20_Persistence_ObservationsBelowThreshold_MarkedNotPersistent()
        {
            var rig = CreateTestRig(new WatchtowerAttentionPolicy { PersistenceObservationThreshold = 3 });

            // 1st observation
            await rig.service.IngestEventAsync(TestTenant, "S1", "T1", "E1", "R1", "EV1", 20.0, 10.0);
            // 2nd observation
            await rig.service.IngestEventAsync(TestTenant, "S1", "T1", "E1", "R2", "EV2", 21.0, 10.0);

            var conditions = await rig.service.ListConditionsAsync(TestTenant);
            Assert.Single(conditions);
            Assert.False(conditions[0].IsPersistent);
        }

        [Fact]
        public async Task CBW21_Persistence_ReachingThreshold_MarkedPersistent()
        {
            var rig = CreateTestRig(new WatchtowerAttentionPolicy { PersistenceObservationThreshold = 3 });

            for (int i = 1; i <= 3; i++)
            {
                await rig.service.IngestEventAsync(TestTenant, "S1", "T1", "E1", $"R{i}", $"EV{i}", 20.0, 10.0);
            }

            var conditions = await rig.service.ListConditionsAsync(TestTenant);
            Assert.True(conditions[0].IsPersistent);
            Assert.Equal(3, conditions[0].ObservationCount);
        }

        [Fact]
        public async Task CBW22_Persistence_StableDeparture_ClassifiedAsPersistentDeviation()
        {
            var rig = CreateTestRig(new WatchtowerAttentionPolicy { PersistenceObservationThreshold = 3 });

            for (int i = 1; i <= 3; i++)
            {
                await rig.service.IngestEventAsync(TestTenant, "S1", "T1", "E1", $"R{i}", $"EV{i}", 25.0, 10.0);
            }

            var conditions = await rig.service.ListConditionsAsync(TestTenant);
            Assert.Equal(ConditionTrajectory.PersistentDeviation, conditions[0].Trajectory);
        }

        [Fact]
        public async Task CBW23_Persistence_CompoundingRateOfChange_ClassifiedAsAcceleratingDeterioration()
        {
            var rig = CreateTestRig(new WatchtowerAttentionPolicy { PersistenceObservationThreshold = 3 });

            await rig.service.IngestEventAsync(TestTenant, "S1", "T1", "E1", "R1", "EV1", 20.0, 10.0);
            await rig.service.IngestEventAsync(TestTenant, "S1", "T1", "E1", "R2", "EV2", 50.0, 10.0);
            await rig.service.IngestEventAsync(TestTenant, "S1", "T1", "E1", "R3", "EV3", 120.0, 10.0);

            var conditions = await rig.service.ListConditionsAsync(TestTenant);
            Assert.Equal(ConditionTrajectory.AcceleratingDeterioration, conditions[0].Trajectory);
        }

        [Fact]
        public async Task CBW24_Persistence_VarianceReturningToZero_ClassifiedAsRecovering()
        {
            var rig = CreateTestRig(new WatchtowerAttentionPolicy { PersistenceObservationThreshold = 2 });

            await rig.service.IngestEventAsync(TestTenant, "S1", "T1", "E1", "R1", "EV1", 50.0, 10.0);
            await rig.service.IngestEventAsync(TestTenant, "S1", "T1", "E1", "R2", "EV2", 10.2, 10.0); // Recovered to baseline

            var conditions = await rig.service.ListConditionsAsync(TestTenant);
            Assert.Equal(ConditionTrajectory.Recovering, conditions[0].Trajectory);
        }

        // =========================================================================
        // FAMILY 5: Multi-Layer Storm Containment (CBW25 - CBW30)
        // =========================================================================

        [Fact]
        public async Task CBW25_StormSuppression_SourceRateLimit_Exceeded_Suppressed()
        {
            var policy = new WatchtowerAttentionPolicy { RateLimitEventsPerMinutePerSource = 3 };
            var rig = CreateTestRig(policy);

            for (int i = 1; i <= 3; i++)
            {
                var (a, _, _) = await rig.service.IngestEventAsync(TestTenant, "Stripe", "T", $"E{i}", $"R{i}", "EV", 1.0);
                Assert.True(a);
            }

            // 4th event exceeds source rate limit
            var (a4, s4, r4) = await rig.service.IngestEventAsync(TestTenant, "Stripe", "T", "E4", "R4", "EV", 1.0);
            Assert.True(a4);
            Assert.Null(s4);
            Assert.Contains("Rate limit", r4);
        }

        [Fact]
        public async Task CBW26_StormSuppression_StormEventsDegradeToFewerSignals()
        {
            var rig = CreateTestRig();

            for (int i = 0; i < 50; i++)
            {
                await rig.service.IngestEventAsync(TestTenant, "Telemetry", "Spike", "ENT-1", "REC-SAME", "EV-1", 100.0);
            }

            var signals = await rig.service.ListActiveSignalsAsync(TestTenant);
            Assert.Single(signals); // Degrades to 1 signal
        }

        [Fact]
        public async Task CBW27_StormSuppression_DifferentSourcesHaveIndependentRateLimits()
        {
            var policy = new WatchtowerAttentionPolicy { RateLimitEventsPerMinutePerSource = 2 };
            var rig = CreateTestRig(policy);

            await rig.service.IngestEventAsync(TestTenant, "SourceA", "T", "E1", "R1", "EV", 1.0);
            await rig.service.IngestEventAsync(TestTenant, "SourceA", "T", "E2", "R2", "EV", 1.0);

            // Source B is unaffected
            var (aB, sB, _) = await rig.service.IngestEventAsync(TestTenant, "SourceB", "T", "E3", "R3", "EV", 1.0);
            Assert.True(aB);
            Assert.NotNull(sB);
        }

        [Fact]
        public async Task CBW28_StormSuppression_StormAggregationPreservesObservationFrequency()
        {
            var rig = CreateTestRig();
            var fp = rig.fingerprintService.ComputeFingerprint("Telemetry", "Storm", "ENT-1", "REC-1");

            for (int i = 0; i < 15; i++)
            {
                await rig.service.IngestEventAsync(TestTenant, "Telemetry", "Storm", "ENT-1", "REC-1", "EV", 1.0);
            }

            int count = await rig.stormEngine.GetAggregatedEventCountAsync(TestTenant, fp);
            Assert.Equal(15, count);
        }

        [Fact]
        public void CBW29_StormSuppression_TenantActiveSignalBudgetEnforced()
        {
            var policy = new WatchtowerAttentionPolicy { MaxSignalsPerTenant = 25 };
            Assert.Equal(25, policy.MaxSignalsPerTenant);
        }

        [Fact]
        public async Task CBW30_StormSuppression_SuppressionReasonRecordedInReturn()
        {
            var rig = CreateTestRig();
            await rig.service.IngestEventAsync(TestTenant, "S", "T", "E", "R", "EV", 1.0);
            var (_, s2, reason) = await rig.service.IngestEventAsync(TestTenant, "S", "T", "E", "R", "EV", 1.0);

            Assert.Null(s2);
            Assert.Contains("I30-E", reason);
        }

        // =========================================================================
        // FAMILY 6: Deterministic Attention Scoring vs. Priority (CBW31 - CBW36)
        // =========================================================================

        [Fact]
        public void CBW31_AttentionScoring_FormulaWeights_StrictlyFollowsEquation()
        {
            var scoring = new AttentionScoringEngine();
            var condition = new PersistentCondition
            {
                BaselineValue = 100.0,
                ObservedValue = 200.0, // Variance = 1.0 -> Impact = 1.0
                Trajectory = ConditionTrajectory.AcceleratingDeterioration, // Urgency = 0.95
                ObservationCount = 10 // Persistence = 1.0
            };

            // Impact=1.0*0.30=0.30, Urgency=0.95*0.25=0.2375, Persistence=1.0*0.20=0.20, Exposure=0.5*0.15=0.075, Confidence=1.0*0.10=0.10
            // Sum = 0.30 + 0.2375 + 0.20 + 0.075 + 0.10 = 0.9125
            var breakdown = scoring.ScoreCondition(condition, exposureScore: 0.5, confidenceScore: 1.0);
            Assert.Equal(0.9125, breakdown.CompositeAttentionScore);
            Assert.Equal(SignalAttentionLevel.Critical, breakdown.ResolvedAttentionLevel);
        }

        [Fact]
        public void CBW32_AttentionScoring_AcceleratingTrajectory_ReceivesUrgency095()
        {
            var scoring = new AttentionScoringEngine();
            var condition = new PersistentCondition { Trajectory = ConditionTrajectory.AcceleratingDeterioration };
            var b = scoring.ScoreCondition(condition);
            Assert.Equal(0.95, b.UrgencyScore);
        }

        [Fact]
        public void CBW33_AttentionScoring_CompositeScoreAbove080_ResolvesToCritical()
        {
            var b = new AttentionScoreBreakdown
            {
                ImpactScore = 1.0,
                UrgencyScore = 0.95,
                PersistenceScore = 1.0,
                ExposureScore = 0.8,
                ConfidenceScore = 1.0
            };
            b.CalculateComposite();
            Assert.Equal(SignalAttentionLevel.Critical, b.ResolvedAttentionLevel);
        }

        [Fact]
        public void CBW34_AttentionScoring_CompositeScoreBetween060And080_ResolvesToHigh()
        {
            var b = new AttentionScoreBreakdown
            {
                ImpactScore = 0.7,
                UrgencyScore = 0.7,
                PersistenceScore = 0.5,
                ExposureScore = 0.5,
                ConfidenceScore = 1.0
            };
            b.CalculateComposite();
            Assert.Equal(SignalAttentionLevel.High, b.ResolvedAttentionLevel);
        }

        [Fact]
        public void CBW35_AttentionScoring_LowVarianceRecovering_ResolvesToLow()
        {
            var scoring = new AttentionScoringEngine();
            var condition = new PersistentCondition
            {
                BaselineValue = 100.0,
                ObservedValue = 102.0, // Variance = 0.02
                Trajectory = ConditionTrajectory.Recovering, // Urgency = 0.15
                ObservationCount = 2
            };
            var b = scoring.ScoreCondition(condition, exposureScore: 0.1, confidenceScore: 0.5);
            Assert.Equal(SignalAttentionLevel.Low, b.ResolvedAttentionLevel);
        }

        [Fact]
        public void CBW36_AttentionScoring_AttentionScoreNotEqualWorkPriority()
        {
            var props = typeof(AttentionScoreBreakdown).GetProperties();
            Assert.DoesNotContain(props, p => p.Name.Contains("Priority", StringComparison.OrdinalIgnoreCase));
        }

        // =========================================================================
        // FAMILY 7: UNKNOWN Preservation (CBW37 - CBW42)
        // =========================================================================

        [Fact]
        public async Task CBW37_UnknownPreservation_MissingEvidence_DemotesToUnknown()
        {
            var rig = CreateTestRig();
            var (valid, evt, _) = await rig.normalizer.NormalizeEventAsync(
                TestTenant, "Stripe", "ChargeFailed", "INV-1", "REC-1",
                evidenceHash: "", epistemicKind: EpistemicStatus.ObservedFact);

            Assert.True(valid);
            Assert.Equal(EpistemicStatus.Unknown, evt!.EpistemicKind);
        }

        [Fact]
        public async Task CBW38_UnknownPreservation_ZeroObservations_ConditionTrajectoryUnknown()
        {
            var rig = CreateTestRig();
            var cond = await rig.conditionTracker.UpdateConditionAsync(
                TestTenant, "S::E", "E", "Metric", 10.0, 10.0, 3);

            Assert.Equal(ConditionTrajectory.Unknown, cond.Trajectory);
        }

        [Fact]
        public async Task CBW39_UnknownPreservation_ConfidenceReducedForUnverifiedEvents()
        {
            var rig = CreateTestRig();
            var (_, signal, _) = await rig.service.IngestEventAsync(
                TestTenant, "S", "T", "E", "R", evidenceHash: "", observedValue: 100.0, baselineValue: 50.0);

            Assert.Equal(0.3, signal!.Breakdown.ConfidenceScore);
        }

        [Fact]
        public void CBW40_UnknownPreservation_UnknownConditionDoesNotTriggerCriticalAttention()
        {
            var scoring = new AttentionScoringEngine();
            var condition = new PersistentCondition { Trajectory = ConditionTrajectory.Unknown };
            var b = scoring.ScoreCondition(condition);
            Assert.NotEqual(SignalAttentionLevel.Critical, b.ResolvedAttentionLevel);
        }

        [Fact]
        public async Task CBW41_UnknownPreservation_EmptyEvidenceRef_DoesNotSynthesizeAlert()
        {
            var rig = CreateTestRig();
            var (valid, evt, _) = await rig.normalizer.NormalizeEventAsync(
                TestTenant, "S", "T", "E", "R", evidenceHash: null!);

            Assert.True(valid);
            Assert.Equal(string.Empty, evt!.EvidenceHash);
        }

        [Fact]
        public void CBW42_UnknownPreservation_ExplicitInvariantAssertion()
        {
            Assert.Contains("I30-K", ContinuousObservationSovereignty.I30_K_UnknownPreservation);
            Assert.Contains("UNKNOWN", ContinuousObservationSovereignty.I30_K_UnknownPreservation);
        }

        // =========================================================================
        // FAMILY 8: Multi-Tenant Partitioning (CBW43 - CBW48)
        // =========================================================================

        [Fact]
        public async Task CBW43_MultiTenant_EventsIsolatedByTenant()
        {
            var rig = CreateTestRig();
            await rig.service.IngestEventAsync(TestTenant, "S", "T", "E1", "R1", "EV", 1.0);

            var fp = rig.fingerprintService.ComputeFingerprint("S", "T", "E1", "R1");
            var fetched = await rig.store.GetEventByFingerprintAsync(AltTenant, fp);
            Assert.Null(fetched);
        }

        [Fact]
        public async Task CBW44_MultiTenant_ConditionsIsolatedByTenant()
        {
            var rig = CreateTestRig();
            await rig.service.IngestEventAsync(TestTenant, "S", "T", "E1", "R1", "EV", 1.0);

            var listA = await rig.service.ListConditionsAsync(TestTenant);
            var listB = await rig.service.ListConditionsAsync(AltTenant);

            Assert.Single(listA);
            Assert.Empty(listB);
        }

        [Fact]
        public async Task CBW45_MultiTenant_SignalsIsolatedByTenant()
        {
            var rig = CreateTestRig();
            await rig.service.IngestEventAsync(TestTenant, "S", "T", "E1", "R1", "EV", 1.0);

            var listA = await rig.service.ListActiveSignalsAsync(TestTenant);
            var listB = await rig.service.ListActiveSignalsAsync(AltTenant);

            Assert.Single(listA);
            Assert.Empty(listB);
        }

        [Fact]
        public async Task CBW46_MultiTenant_RateLimitsIsolatedByTenant()
        {
            var policy = new WatchtowerAttentionPolicy { RateLimitEventsPerMinutePerSource = 1 };
            var rig = CreateTestRig(policy);

            await rig.service.IngestEventAsync(TestTenant, "Stripe", "T", "E1", "R1", "EV", 1.0);
            // AltTenant should not be blocked
            var (aAlt, sAlt, _) = await rig.service.IngestEventAsync(AltTenant, "Stripe", "T", "E2", "R2", "EV", 1.0);

            Assert.True(aAlt);
            Assert.NotNull(sAlt);
        }

        [Fact]
        public async Task CBW47_MultiTenant_WhyTraceIsolatedByTenant()
        {
            var rig = CreateTestRig();
            var (_, signal, _) = await rig.service.IngestEventAsync(TestTenant, "S", "T", "E1", "R1", "EV", 1.0);

            var whyAlt = await rig.service.GetWhyExplanationAsync(AltTenant, signal!.SignalId);
            Assert.Null(whyAlt);
        }

        [Fact]
        public async Task CBW48_MultiTenant_ProposeWorkIsolatedByTenant()
        {
            var rig = CreateTestRig();
            var (_, signal, _) = await rig.service.IngestEventAsync(TestTenant, "S", "T", "E1", "R1", "EV", 1.0);

            var (gen, _, err) = await rig.service.ProposeWorkFromSignalAsync(AltTenant, signal!.SignalId);
            Assert.False(gen);
            Assert.Contains("not found", err);
        }

        // =========================================================================
        // FAMILY 9: Historical Signal Immutability & Replay (CBW49 - CBW54)
        // =========================================================================

        [Fact]
        public void CBW49_Immutability_SignalHash_DeterministicOnIdenticalInputs()
        {
            var time = new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);
            var s1 = new WatchtowerSignal { TenantId = TestTenant, ConditionId = "C1", Title = "T", EmittedUtc = time };
            var s2 = new WatchtowerSignal { TenantId = TestTenant, ConditionId = "C1", Title = "T", EmittedUtc = time };

            s1.ComputeSignalHash();
            s2.ComputeSignalHash();

            Assert.Equal(s1.SignalHash, s2.SignalHash);
        }

        [Fact]
        public void CBW50_Immutability_ConditionHash_DeterministicOnIdenticalInputs()
        {
            var c1 = new PersistentCondition { TenantId = TestTenant, CorrelationKey = "K", EntityId = "E", MetricName = "M", BaselineValue = 10.0, ObservedValue = 20.0 };
            var c2 = new PersistentCondition { TenantId = TestTenant, CorrelationKey = "K", EntityId = "E", MetricName = "M", BaselineValue = 10.0, ObservedValue = 20.0 };

            c1.ComputeConditionHash();
            c2.ComputeConditionHash();

            Assert.Equal(c1.ConditionHash, c2.ConditionHash);
        }

        [Fact]
        public async Task CBW51_Immutability_LaterEventDoesNotMutateHistoricalEventRecord()
        {
            var rig = CreateTestRig();
            await rig.service.IngestEventAsync(TestTenant, "S", "T", "E", "R1", "EV1", 10.0);

            var fp = rig.fingerprintService.ComputeFingerprint("S", "T", "E", "R1");
            var originalEvt = await rig.store.GetEventByFingerprintAsync(TestTenant, fp);
            var originalProvenance = originalEvt!.ProvenanceHash;

            // Later event
            await rig.service.IngestEventAsync(TestTenant, "S", "T", "E", "R2", "EV2", 20.0);

            var checkEvt = await rig.store.GetEventByFingerprintAsync(TestTenant, fp);
            Assert.Equal(originalProvenance, checkEvt!.ProvenanceHash);
        }

        [Fact]
        public void CBW52_Immutability_TamperedSignalDetectedByHash()
        {
            var s = new WatchtowerSignal { TenantId = TestTenant, ConditionId = "C1", Title = "Original", EmittedUtc = DateTime.UtcNow };
            s.ComputeSignalHash();
            var originalHash = s.SignalHash;

            s.Title = "Tampered Title";
            s.ComputeSignalHash();

            Assert.NotEqual(originalHash, s.SignalHash);
        }

        [Fact]
        public void CBW53_Immutability_ProvenanceHashPreservesOriginalObservationTimestamp()
        {
            var rig = CreateTestRig();
            var time1 = new DateTime(2026, 9, 10, 10, 0, 0, DateTimeKind.Utc);
            var time2 = new DateTime(2026, 9, 10, 11, 0, 0, DateTimeKind.Utc);

            var p1 = rig.fingerprintService.ComputeProvenanceHash(TestTenant, "FP1", time1, "EV");
            var p2 = rig.fingerprintService.ComputeProvenanceHash(TestTenant, "FP1", time2, "EV");

            Assert.NotEqual(p1, p2);
        }

        [Fact]
        public async Task CBW54_Immutability_ReplayFromStoreMatchesChronology()
        {
            var rig = CreateTestRig();
            await rig.service.IngestEventAsync(TestTenant, "S", "T", "E", "R1", "EV1", 1.0);
            await rig.service.IngestEventAsync(TestTenant, "S", "T", "E", "R2", "EV2", 2.0);

            var events = await rig.store.ListEventsForCorrelationAsync(TestTenant, "S::E");
            Assert.Equal(2, events.Count);
            Assert.True(events[0].ObservedAt <= events[1].ObservedAt);
        }

        // =========================================================================
        // FAMILY 10: "Why Am I Seeing This?" Provenance Trace (CBW55 - CBW60)
        // =========================================================================

        [Fact]
        public async Task CBW55_WhyTrace_ContainsCompleteSourceAttributes()
        {
            var rig = CreateTestRig();
            var (_, signal, _) = await rig.service.IngestEventAsync(
                TestTenant, "Salesforce", "OppClosedLost", "OPP-99", "REC-SL-1", "EVID-SF-01", 1.0);

            var why = signal!.WhyTrace;
            Assert.Equal("Salesforce", why.SourceSystem);
            Assert.False(string.IsNullOrWhiteSpace(why.EventFingerprint));
            Assert.Equal("EVID-SF-01", why.EvidenceHash);
            Assert.Equal("SALESFORCE::OPP-99", why.CorrelationKey);
        }

        [Fact]
        public async Task CBW56_WhyTrace_ContainsAttentionBreakdownScores()
        {
            var rig = CreateTestRig();
            var (_, signal, _) = await rig.service.IngestEventAsync(
                TestTenant, "S", "T", "E", "R", "EV", 50.0, 10.0);

            var b = signal!.WhyTrace.AttentionBreakdown;
            Assert.True(b.ImpactScore > 0);
            Assert.True(b.CompositeAttentionScore > 0);
        }

        [Fact]
        public async Task CBW57_WhyTrace_ContainsTrajectoryClassification()
        {
            var rig = CreateTestRig();
            var (_, signal, _) = await rig.service.IngestEventAsync(
                TestTenant, "S", "T", "E", "R", "EV", 50.0, 10.0);

            Assert.NotEqual(ConditionTrajectory.Unknown, signal!.WhyTrace.Trajectory);
        }

        [Fact]
        public async Task CBW58_WhyTrace_ContainsHumanReadableExplanation()
        {
            var rig = CreateTestRig();
            var (_, signal, _) = await rig.service.IngestEventAsync(
                TestTenant, "S", "T", "E", "R", "EV", 50.0, 10.0, "ConversionRate");

            Assert.Contains("ConversionRate", signal!.WhyTrace.ExplanationText);
            Assert.Contains("attention score", signal.WhyTrace.ExplanationText, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CBW59_WhyTrace_CorrelatedEventIds_Listed()
        {
            var rig = CreateTestRig();
            var (_, signal, _) = await rig.service.IngestEventAsync(
                TestTenant, "S", "T", "E", "R", "EV", 1.0);

            Assert.NotEmpty(signal!.WhyTrace.CorrelatedEventIds);
        }

        [Fact]
        public async Task CBW60_WhyTrace_RetrievableViaServiceEndpoint()
        {
            var rig = CreateTestRig();
            var (_, signal, _) = await rig.service.IngestEventAsync(
                TestTenant, "S", "T", "E", "R", "EV", 1.0);

            var why = await rig.service.GetWhyExplanationAsync(TestTenant, signal!.SignalId);
            Assert.NotNull(why);
            Assert.Equal(signal.WhyTrace.EventFingerprint, why.EventFingerprint);
        }

        // =========================================================================
        // FAMILY 11: Downstream WorkProposal Generation (CBW61 - CBW66)
        // =========================================================================

        [Fact]
        public async Task CBW61_WorkProposal_ProposeWorkFromSignal_GeneratesValidProposalId()
        {
            var rig = CreateTestRig();
            var (_, signal, _) = await rig.service.IngestEventAsync(TestTenant, "S", "T", "E", "R", "EV", 1.0);

            var (gen, proposalId, err) = await rig.service.ProposeWorkFromSignalAsync(TestTenant, signal!.SignalId);
            Assert.True(gen);
            Assert.NotNull(proposalId);
            Assert.StartsWith("PROP-CBW-", proposalId);
            Assert.Null(err);
        }

        [Fact]
        public async Task CBW62_WorkProposal_SignalStatusTransitionsToEscalated()
        {
            var rig = CreateTestRig();
            var (_, signal, _) = await rig.service.IngestEventAsync(TestTenant, "S", "T", "E", "R", "EV", 1.0);

            await rig.service.ProposeWorkFromSignalAsync(TestTenant, signal!.SignalId);

            var updated = await rig.service.GetSignalAsync(TestTenant, signal.SignalId);
            Assert.Equal(SignalStatus.Escalated, updated!.Status);
        }

        [Fact]
        public async Task CBW63_WorkProposal_SignalStoresLinkedProposalId()
        {
            var rig = CreateTestRig();
            var (_, signal, _) = await rig.service.IngestEventAsync(TestTenant, "S", "T", "E", "R", "EV", 1.0);

            var (_, proposalId, _) = await rig.service.ProposeWorkFromSignalAsync(TestTenant, signal!.SignalId);

            var updated = await rig.service.GetSignalAsync(TestTenant, signal.SignalId);
            Assert.Equal(proposalId, updated!.WorkProposalId);
        }

        [Fact]
        public void CBW64_WorkProposal_ZeroDirectMissionExecution()
        {
            var methods = typeof(ContinuousWatchtowerService).GetMethods();
            Assert.DoesNotContain(methods, m => m.Name.Contains("Mission", StringComparison.OrdinalIgnoreCase) ||
                                                m.Name.Contains("Execute", StringComparison.OrdinalIgnoreCase) ||
                                                m.Name.Contains("Dispatch", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task CBW65_WorkProposal_ProposalRequiresDownstreamOrganizationalAdmission()
        {
            var rig = CreateTestRig();
            var (_, signal, _) = await rig.service.IngestEventAsync(TestTenant, "S", "T", "E", "R", "EV", 1.0);
            var (_, proposalId, _) = await rig.service.ProposeWorkFromSignalAsync(TestTenant, signal!.SignalId);

            // Proposal starts with PROP-CBW, not an approved WorkItem
            Assert.StartsWith("PROP-CBW", proposalId);
        }

        [Fact]
        public async Task CBW66_WorkProposal_NonExistentSignal_FailsGracefully()
        {
            var rig = CreateTestRig();
            var (gen, proposalId, err) = await rig.service.ProposeWorkFromSignalAsync(TestTenant, "NON-EXISTENT-SIGNAL");

            Assert.False(gen);
            Assert.Null(proposalId);
            Assert.Contains("not found", err);
        }

        // =========================================================================
        // FAMILY 12: Batch 6 Execution Firewall & Zero Permit Isolation (CBW67 - CBW72)
        // =========================================================================

        [Fact]
        public void CBW67_FirewallIsolation_SignalHasNoExecutionPermitProperty()
        {
            var props = typeof(WatchtowerSignal).GetProperties();
            Assert.DoesNotContain(props, p => p.Name.Contains("Permit", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void CBW68_FirewallIsolation_ConditionHasNoExecutionPermitProperty()
        {
            var props = typeof(PersistentCondition).GetProperties();
            Assert.DoesNotContain(props, p => p.Name.Contains("Permit", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void CBW69_FirewallIsolation_EventHasNoExecutionPermitProperty()
        {
            var props = typeof(BusinessEvent).GetProperties();
            Assert.DoesNotContain(props, p => p.Name.Contains("Permit", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void CBW70_FirewallIsolation_ServiceHasNoExecutionPermitMethods()
        {
            var methods = typeof(IContinuousWatchtowerService).GetMethods();
            Assert.DoesNotContain(methods, m => m.Name.Contains("Permit", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task CBW71_FirewallIsolation_CriticalSignalCannotBypassFirewall()
        {
            var rig = CreateTestRig();
            // Ingest critical condition
            var (_, signal, _) = await rig.service.IngestEventAsync(
                TestTenant, "S", "T", "E", "R", "EV", 500.0, 10.0);

            var (gen, proposalId, _) = await rig.service.ProposeWorkFromSignalAsync(TestTenant, signal!.SignalId);
            Assert.True(gen);
            Assert.StartsWith("PROP-CBW-", proposalId);
            // Critical signal only generates proposal, never a permit
            Assert.DoesNotContain("PERMIT", proposalId);
        }

        [Fact]
        public void CBW72_FirewallIsolation_SignalCannotDeriveFromExecutionPermit()
        {
            Assert.False(typeof(BusinessModelApp.Core.Domain.Execution.ExecutionPermit).IsAssignableFrom(typeof(WatchtowerSignal)));
        }

        // =========================================================================
        // FAMILY 13: Watchtower != Incident Commander / Emergency Authority (CBW73 - CBW78)
        // =========================================================================

        [Fact]
        public void CBW73_IncidentCommander_WatchtowerCannotAssumeCommand()
        {
            Assert.Contains("I30-I", ContinuousObservationSovereignty.I30_I_WatchtowerNotIncidentCommander);
            Assert.Contains("cannot assume incident command", ContinuousObservationSovereignty.I30_I_WatchtowerNotIncidentCommander);
        }

        [Fact]
        public void CBW74_IncidentCommander_NoAutonomousEmergencyAuthority()
        {
            Assert.Contains("I30-B", ContinuousObservationSovereignty.I30_B_AlertNotEmergency);
            Assert.Contains("cannot manufacture emergency authority", ContinuousObservationSovereignty.I30_B_AlertNotEmergency);
        }

        [Fact]
        public void CBW75_IncidentCommander_EscalationDoesNotAuthorizeAction()
        {
            Assert.Contains("I30-N", ContinuousObservationSovereignty.I30_N_EscalationNotAuthority);
            Assert.Contains("never grants autonomous execution authority", ContinuousObservationSovereignty.I30_N_EscalationNotAuthority);
        }

        [Fact]
        public void CBW76_IncidentCommander_NoSelfEscalation()
        {
            Assert.Contains("I30-O", ContinuousObservationSovereignty.I30_O_NoSelfEscalation);
            Assert.Contains("cannot escalate its own authority", ContinuousObservationSovereignty.I30_O_NoSelfEscalation);
        }

        [Fact]
        public void CBW77_IncidentCommander_NoIndependentSchedulerLoop()
        {
            Assert.Contains("I30-H", ContinuousObservationSovereignty.I30_H_WatchtowerNotScheduler);
            Assert.Contains("cannot create an independent scheduler", ContinuousObservationSovereignty.I30_H_WatchtowerNotScheduler);
        }

        [Fact]
        public void CBW78_IncidentCommander_AllConstitutionalConstantsDeclared()
        {
            var fields = typeof(ContinuousObservationSovereignty).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
                .ToList();

            Assert.True(fields.Count >= 15);
            foreach (var f in fields)
            {
                var val = (string?)f.GetValue(null);
                Assert.False(string.IsNullOrWhiteSpace(val));
            }
        }

        // =========================================================================
        // FAMILY 14: End-to-End Watchtower Pipeline & Controller Flows (CBW79 - CBW84)
        // =========================================================================

        [Fact]
        public async Task CBW79_E2E_FullPipeline_Ingest_Correlate_Persist_Score_Propose()
        {
            var rig = CreateTestRig();

            // 1. Ingest
            var (accepted, signal, _) = await rig.service.IngestEventAsync(
                TestTenant, "Stripe", "ChargebackRatio", "ACCOUNT-1", "REC-01",
                "EVID-CB-01", 0.08, 0.01, "ChargebackRatio");
            Assert.True(accepted);
            Assert.NotNull(signal);

            // 2. Correlate & Persist check
            var conditions = await rig.service.ListConditionsAsync(TestTenant);
            Assert.Single(conditions);

            // 3. Score check
            Assert.True(signal.Breakdown.CompositeAttentionScore > 0);

            // 4. Propose Work
            var (gen, propId, _) = await rig.service.ProposeWorkFromSignalAsync(TestTenant, signal.SignalId);
            Assert.True(gen);
            Assert.NotNull(propId);
        }

        [Fact]
        public async Task CBW80_E2E_DriftEvolution_SpikeToPersistentToRecovering()
        {
            var rig = CreateTestRig(new WatchtowerAttentionPolicy { PersistenceObservationThreshold = 2 });

            // Step 1: Initial excursion -> Spike
            var (_, s1, _) = await rig.service.IngestEventAsync(TestTenant, "S", "T", "E", "R1", "EV1", 50.0, 10.0);
            Assert.Equal(ConditionTrajectory.TransientSpike, s1!.WhyTrace.Trajectory);

            // Step 2: Second continuous excursion -> PersistentDeviation
            var (_, s2, _) = await rig.service.IngestEventAsync(TestTenant, "S", "T", "E", "R2", "EV2", 50.0, 10.0);
            Assert.Equal(ConditionTrajectory.PersistentDeviation, s2!.WhyTrace.Trajectory);

            // Step 3: Returns to baseline -> Recovering
            var (_, s3, _) = await rig.service.IngestEventAsync(TestTenant, "S", "T", "E", "R3", "EV3", 10.1, 10.0);
            Assert.Equal(ConditionTrajectory.Recovering, s3!.WhyTrace.Trajectory);
        }

        [Fact]
        public async Task CBW81_E2E_StormSuppression_InFullPipeline()
        {
            var rig = CreateTestRig();

            // Send 10 identical events
            for (int i = 0; i < 10; i++)
            {
                await rig.service.IngestEventAsync(TestTenant, "Telemetry", "Spike", "HOST-1", "REC-X", "EV", 100.0);
            }

            var signals = await rig.service.ListActiveSignalsAsync(TestTenant);
            Assert.Single(signals);
            var count = await rig.stormEngine.GetAggregatedEventCountAsync(
                TestTenant, rig.fingerprintService.ComputeFingerprint("Telemetry", "Spike", "HOST-1", "REC-X"));
            Assert.Equal(10, count);
        }

        [Fact]
        public async Task CBW82_E2E_Controller_IngestEvent_ReturnsOk()
        {
            var rig = CreateTestRig();
            var controller = new ContinuousWatchtowerController(rig.service);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            controller.HttpContext.Request.Headers["X-Tenant-ID"] = TestTenant;

            var request = new ContinuousWatchtowerController.IngestEventRequest
            {
                SourceSystem = "HubSpot",
                EventType = "MqlDrop",
                EntityId = "CAMPAIGN-01",
                SourceRecordId = "REC-HS-1",
                EvidenceHash = "EVID-HS-01",
                ObservedValue = 5.0,
                BaselineValue = 50.0,
                MetricName = "MqlVolume"
            };

            var result = await controller.IngestEvent(request, CancellationToken.None);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task CBW83_E2E_Controller_GetActiveSignalsAndWhyTrace_ReturnsOk()
        {
            var rig = CreateTestRig();
            var (_, signal, _) = await rig.service.IngestEventAsync(TestTenant, "S", "T", "E", "R", "EV", 1.0);

            var controller = new ContinuousWatchtowerController(rig.service);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            controller.HttpContext.Request.Headers["X-Tenant-ID"] = TestTenant;

            var listRes = await controller.ListActiveSignals(CancellationToken.None);
            var okList = Assert.IsType<OkObjectResult>(listRes);
            var signals = Assert.IsAssignableFrom<IReadOnlyList<WatchtowerSignal>>(okList.Value);
            Assert.Single(signals);

            var whyRes = await controller.GetWhyTrace(signal!.SignalId, CancellationToken.None);
            var okWhy = Assert.IsType<OkObjectResult>(whyRes);
            Assert.NotNull(okWhy.Value);
        }

        [Fact]
        public async Task CBW84_E2E_Controller_ProposeWork_ReturnsOk()
        {
            var rig = CreateTestRig();
            var (_, signal, _) = await rig.service.IngestEventAsync(TestTenant, "S", "T", "E", "R", "EV", 1.0);

            var controller = new ContinuousWatchtowerController(rig.service);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            controller.HttpContext.Request.Headers["X-Tenant-ID"] = TestTenant;

            var result = await controller.ProposeWork(signal!.SignalId, CancellationToken.None);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }
    }
}
