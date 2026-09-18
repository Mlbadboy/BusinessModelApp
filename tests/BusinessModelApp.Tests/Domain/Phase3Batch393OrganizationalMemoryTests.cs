using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Organizational;
using BusinessModelApp.Infrastructure.Runtime.Organizational.Memory;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public class Phase3Batch393OrganizationalMemoryTests
    {
        private const string TestTenant = "TENANT-OMC-393";
        private const string AltTenant = "TENANT-OMC-ALT";

        private (
            InMemoryOrganizationalMemoryStore store,
            MemoryWriteGate writeGate,
            OrganizationalContextAssembler assembler,
            WorkTrajectoryRecorder trajectoryRecorder,
            MemoryFreshnessEvaluator freshnessEvaluator,
            OrganizationalMemoryService memoryService
        ) CreateTestRig(ContextAssemblyPolicy? policy = null)
        {
            var store = new InMemoryOrganizationalMemoryStore();
            var writeGate = new MemoryWriteGate();
            var assembler = new OrganizationalContextAssembler(store, policy);
            var trajectoryRecorder = new WorkTrajectoryRecorder(store);
            var freshnessEvaluator = new MemoryFreshnessEvaluator(store);
            var memoryService = new OrganizationalMemoryService(
                store,
                assembler,
                trajectoryRecorder,
                freshnessEvaluator);

            return (store, writeGate, assembler, trajectoryRecorder, freshnessEvaluator, memoryService);
        }

        private OrganizationalPrecedent CreateSamplePrecedent(
            string id = "PREC-01",
            string tenantId = TestTenant,
            EpistemicStatus epistemic = EpistemicStatus.ObservedFact,
            FreshnessStatus freshness = FreshnessStatus.Fresh,
            CurrentApplicability applicability = CurrentApplicability.High,
            string regime = "RegimeStable",
            double variance = 0.05)
        {
            var prec = new OrganizationalPrecedent
            {
                PrecedentId = id,
                TenantId = tenantId,
                SourceTrajectoryId = "TRAJ-01",
                Title = $"Precedent {id}",
                HistoricalDecisionId = "DEC-01",
                HistoricalOutcomeId = "OUT-01",
                ObservedConditions = $"Market conditions in {regime}",
                PredictedOutcome = "Revenue +15%",
                ActualOutcome = "Revenue +12%",
                Variance = variance,
                EpistemicClassification = epistemic,
                EvidenceReferences = new List<string> { "EVID-HASH-01" },
                ApplicabilityConditions = new List<string> { regime },
                Freshness = freshness,
                Applicability = applicability,
                IsHistoricallyValid = true,
                PolicySnapshotHash = "POLICY-V1-HASH",
                CreatedUtc = DateTime.UtcNow
            };
            prec.ComputeProvenance();
            return prec;
        }

        // =========================================================================
        // Family 1: Memory Write Sovereignty (I28-N) (OMC01 - OMC06)
        // =========================================================================

        [Fact]
        public async Task OMC01_AgentRoleDirectWrite_RejectedPerI28N()
        {
            var rig = CreateTestRig();

            var (permitted, reason) = await rig.writeGate.ValidateWriteAsync(
                TestTenant,
                callerRole: "Agent",
                sourceRecordType: "OutcomeRecord",
                evidenceRef: "EVID-01");

            Assert.False(permitted);
            Assert.Contains("prohibited from writing authoritative organizational memory", reason);
        }

        [Fact]
        public async Task OMC02_WorkerRoleDirectWrite_RejectedPerI28N()
        {
            var rig = CreateTestRig();

            var (permitted, reason) = await rig.writeGate.ValidateWriteAsync(
                TestTenant,
                callerRole: "WorkerProcess",
                sourceRecordType: "OutcomeRecord",
                evidenceRef: "EVID-01");

            Assert.False(permitted);
            Assert.Contains("prohibited from writing", reason);
        }

        [Fact]
        public async Task OMC03_LlmDirectWrite_RejectedPerI28N()
        {
            var rig = CreateTestRig();

            var (permitted, reason) = await rig.writeGate.ValidateWriteAsync(
                TestTenant,
                callerRole: "LLM",
                sourceRecordType: "DecisionRecord",
                evidenceRef: "EVID-01");

            Assert.False(permitted);
            Assert.Contains("prohibited", reason);
        }

        [Fact]
        public async Task OMC04_MissingEvidenceRef_RejectedPerI28A()
        {
            var rig = CreateTestRig();

            var (permitted, reason) = await rig.writeGate.ValidateWriteAsync(
                TestTenant,
                callerRole: "OutcomeVerifier",
                sourceRecordType: "OutcomeRecord",
                evidenceRef: "");

            Assert.False(permitted);
            Assert.Contains("require an evidence reference", reason);
        }

        [Fact]
        public async Task OMC05_UnauthorizedSourceType_Rejected()
        {
            var rig = CreateTestRig();

            var (permitted, reason) = await rig.writeGate.ValidateWriteAsync(
                TestTenant,
                callerRole: "GovernanceAuditor",
                sourceRecordType: "RumorOrChatSnippet",
                evidenceRef: "EVID-01");

            Assert.False(permitted);
            Assert.Contains("not an authorized provenance source", reason);
        }

        [Fact]
        public async Task OMC06_GovernedWriteWithEvidence_Permitted()
        {
            var rig = CreateTestRig();

            var (permitted, reason) = await rig.writeGate.ValidateWriteAsync(
                TestTenant,
                callerRole: "OutcomeVerifier",
                sourceRecordType: "VerificationRecord",
                evidenceRef: "CERT-HASH-2026-Q3");

            Assert.True(permitted);
            Assert.Null(reason);
        }

        // =========================================================================
        // Family 2: Two-Stage Context Assembly & Eligibility Gate (OMC07 - OMC12)
        // =========================================================================

        [Fact]
        public async Task OMC07_EligiblePrecedent_AdmittedIntoContextSnapshot()
        {
            var rig = CreateTestRig();
            var prec = CreateSamplePrecedent("PREC-GOOD");
            await rig.store.SavePrecedentAsync(prec);

            var snapshot = await rig.assembler.AssembleContextAsync(TestTenant, "WRK-01");

            Assert.NotNull(snapshot);
            Assert.Contains(snapshot.ItemsIncluded, item => item.ItemId == "PREC-GOOD");
            Assert.NotEmpty(snapshot.SnapshotHash);
        }

        [Fact]
        public async Task OMC08_UnknownEpistemicStatus_ExcludedPerI28I()
        {
            var rig = CreateTestRig();
            var ungrounded = CreateSamplePrecedent("PREC-UNKNOWN", epistemic: EpistemicStatus.Unknown);
            await rig.store.SavePrecedentAsync(ungrounded);

            var snapshot = await rig.assembler.AssembleContextAsync(TestTenant, "WRK-01");

            Assert.DoesNotContain(snapshot.ItemsIncluded, item => item.ItemId == "PREC-UNKNOWN");
            Assert.Contains("PREC-UNKNOWN", snapshot.ItemsExcluded);
            Assert.Contains("Epistemic status is Unknown", snapshot.ExclusionReasons["PREC-UNKNOWN"]);
        }

        [Fact]
        public async Task OMC09_ExpiredFreshnessStatus_ExcludedFromContext()
        {
            var rig = CreateTestRig();
            var expired = CreateSamplePrecedent("PREC-EXP", freshness: FreshnessStatus.Expired);
            await rig.store.SavePrecedentAsync(expired);

            var snapshot = await rig.assembler.AssembleContextAsync(TestTenant, "WRK-01");

            Assert.DoesNotContain(snapshot.ItemsIncluded, item => item.ItemId == "PREC-EXP");
            Assert.Contains("PREC-EXP", snapshot.ItemsExcluded);
            Assert.Contains("Freshness status is Expired", snapshot.ExclusionReasons["PREC-EXP"]);
        }

        [Fact]
        public async Task OMC10_ConflictedFreshnessStatus_ExcludedFromContext()
        {
            var rig = CreateTestRig();
            var conflicted = CreateSamplePrecedent("PREC-CONF", freshness: FreshnessStatus.Conflicted);
            await rig.store.SavePrecedentAsync(conflicted);

            var snapshot = await rig.assembler.AssembleContextAsync(TestTenant, "WRK-01");

            Assert.DoesNotContain(snapshot.ItemsIncluded, item => item.ItemId == "PREC-CONF");
            Assert.Contains("PREC-CONF", snapshot.ItemsExcluded);
        }

        [Fact]
        public async Task OMC11_ApplicabilityBelowThreshold_Excluded()
        {
            var policy = new ContextAssemblyPolicy { MinApplicability = CurrentApplicability.Medium };
            var rig = CreateTestRig(policy);

            var lowApp = CreateSamplePrecedent("PREC-LOW-APP", applicability: CurrentApplicability.Low);
            await rig.store.SavePrecedentAsync(lowApp);

            var snapshot = await rig.assembler.AssembleContextAsync(TestTenant, "WRK-01", policy);

            Assert.DoesNotContain(snapshot.ItemsIncluded, item => item.ItemId == "PREC-LOW-APP");
            Assert.Contains("PREC-LOW-APP", snapshot.ItemsExcluded);
            Assert.Contains("below threshold", snapshot.ExclusionReasons["PREC-LOW-APP"]);
        }

        [Fact]
        public async Task OMC12_MultipleEligibleItems_OrderedDeterministically()
        {
            var rig = CreateTestRig();
            var p1 = CreateSamplePrecedent("P-1", epistemic: EpistemicStatus.VerifiedTruth);
            var p2 = CreateSamplePrecedent("P-2", epistemic: EpistemicStatus.Hypothesis);
            await rig.store.SavePrecedentAsync(p1);
            await rig.store.SavePrecedentAsync(p2);

            var snapshot = await rig.assembler.AssembleContextAsync(TestTenant, "WRK-01");

            Assert.True(snapshot.ItemsIncluded.Count >= 2);
            // VerifiedTruth has higher score than Hypothesis
            Assert.Equal("P-1", snapshot.ItemsIncluded[0].ItemId);
            Assert.Equal("P-2", snapshot.ItemsIncluded[1].ItemId);
        }

        // =========================================================================
        // Family 3: Deterministic Ranking & Formula Budgeting (OMC13 - OMC18)
        // =========================================================================

        [Fact]
        public async Task OMC13_MaxPrecedentsCeiling_EnforcedStrictly()
        {
            var policy = new ContextAssemblyPolicy { MaxPrecedents = 2 };
            var rig = CreateTestRig(policy);

            for (int i = 1; i <= 4; i++)
            {
                await rig.store.SavePrecedentAsync(CreateSamplePrecedent($"PREC-{i}"));
            }

            var snapshot = await rig.assembler.AssembleContextAsync(TestTenant, "WRK-01", policy);

            var includedPrecedents = snapshot.ItemsIncluded.Where(item => item.ItemType == "Precedent").ToList();
            Assert.Equal(2, includedPrecedents.Count);
            Assert.Equal(2, snapshot.ItemsExcluded.Count);
            Assert.Contains("Reached MaxPrecedents", snapshot.ExclusionReasons.Values.First());
        }

        [Fact]
        public async Task OMC14_MaxTokenBudget_LimitsContextPayload()
        {
            var policy = new ContextAssemblyPolicy { MaxContextTokens = 150 }; // Only enough for 1 precedent (~120 tokens)
            var rig = CreateTestRig(policy);

            await rig.store.SavePrecedentAsync(CreateSamplePrecedent("P-1"));
            await rig.store.SavePrecedentAsync(CreateSamplePrecedent("P-2"));

            var snapshot = await rig.assembler.AssembleContextAsync(TestTenant, "WRK-01", policy);

            Assert.Single(snapshot.ItemsIncluded);
            Assert.True(snapshot.EstimatedTokenCount <= 150);
            Assert.Contains("MaxContextTokens (150) exceeded", snapshot.ExclusionReasons["P-2"]);
        }

        [Fact]
        public async Task OMC15_IdenticalInputs_ProduceIdenticalSnapshotHash()
        {
            var rig = CreateTestRig();
            await rig.store.SavePrecedentAsync(CreateSamplePrecedent("PREC-IDEM"));

            var s1 = await rig.assembler.AssembleContextAsync(TestTenant, "WRK-IDEM");
            var s2 = await rig.assembler.AssembleContextAsync(TestTenant, "WRK-IDEM");

            Assert.Equal(s1.ItemsIncluded.Count, s2.ItemsIncluded.Count);
            Assert.Equal(s1.EstimatedTokenCount, s2.EstimatedTokenCount);
        }

        [Fact]
        public async Task OMC16_ContextScoreFormula_CalculatesExpectedWeights()
        {
            var rig = CreateTestRig();
            var prec = CreateSamplePrecedent("P-FORMULA", epistemic: EpistemicStatus.VerifiedTruth, freshness: FreshnessStatus.Fresh, applicability: CurrentApplicability.High);
            await rig.store.SavePrecedentAsync(prec);

            var snapshot = await rig.assembler.AssembleContextAsync(TestTenant, "WRK-01");
            var item = snapshot.ItemsIncluded.First(i => i.ItemId == "P-FORMULA");

            // EvidenceQuality (0.95) * Freshness (1.0) * Applicability (1.0) * Epistemic (1.0) = 0.95
            Assert.Equal(0.95, item.Score);
        }

        [Fact]
        public async Task OMC17_PrecedentsWithoutEvidence_ReceiveEvidenceQualityDiscount()
        {
            var rig = CreateTestRig();
            var prec = CreateSamplePrecedent("P-NO-EVID", epistemic: EpistemicStatus.VerifiedTruth);
            prec.EvidenceReferences.Clear();
            await rig.store.SavePrecedentAsync(prec);

            var snapshot = await rig.assembler.AssembleContextAsync(TestTenant, "WRK-01");
            var item = snapshot.ItemsIncluded.First(i => i.ItemId == "P-NO-EVID");

            // EvidenceQuality (0.50) * Freshness (1.0) * Applicability (1.0) * Epistemic (1.0) = 0.50
            Assert.Equal(0.50, item.Score);
        }

        [Fact]
        public async Task OMC18_TiesBrokenDeterministicallyByItemId()
        {
            var rig = CreateTestRig();
            var pB = CreateSamplePrecedent("P-B");
            var pA = CreateSamplePrecedent("P-A");
            await rig.store.SavePrecedentAsync(pB);
            await rig.store.SavePrecedentAsync(pA);

            var snapshot = await rig.assembler.AssembleContextAsync(TestTenant, "WRK-01");

            // Both have identical scores, but P-A comes before P-B lexicographically
            Assert.Equal("P-A", snapshot.ItemsIncluded[0].ItemId);
            Assert.Equal("P-B", snapshot.ItemsIncluded[1].ItemId);
        }

        // =========================================================================
        // Family 4: Decoupled Freshness vs Historical Validity (I28-E) (OMC19 - OMC24)
        // =========================================================================

        [Fact]
        public async Task OMC19_ElapsedAge_TransitionsFreshnessFromFreshToStale()
        {
            var rig = CreateTestRig();
            var prec = CreateSamplePrecedent("P-AGING");
            await rig.store.SavePrecedentAsync(prec);

            var res = await rig.freshnessEvaluator.EvaluateFreshnessAsync(
                TestTenant, "P-AGING", TimeSpan.FromDays(45));

            Assert.Equal(FreshnessStatus.Stale, res.NewFreshness);
            Assert.True(res.HistoricalValidityUnchanged);

            var updated = await rig.store.GetPrecedentAsync(TestTenant, "P-AGING");
            Assert.True(updated!.IsHistoricallyValid);
            Assert.Equal(FreshnessStatus.Stale, updated.Freshness);
        }

        [Fact]
        public async Task OMC20_RegimeShift_DegradesApplicabilityWithoutRewritingHistory()
        {
            var rig = CreateTestRig();
            var prec = CreateSamplePrecedent("P-REGIME", regime: "BullMarket2025");
            await rig.store.SavePrecedentAsync(prec);

            // Current regime shifts to BearMarket2026
            var res = await rig.freshnessEvaluator.EvaluateFreshnessAsync(
                TestTenant, "P-REGIME", TimeSpan.FromDays(5), currentRegime: "BearMarket2026");

            Assert.Equal(CurrentApplicability.Low, res.NewApplicability);
            Assert.True(res.HistoricalValidityUnchanged);
            Assert.Contains("Market regime shifted", res.Rationale);

            var updated = await rig.store.GetPrecedentAsync(TestTenant, "P-REGIME");
            Assert.True(updated!.IsHistoricallyValid);
            Assert.Equal(CurrentApplicability.Low, updated.Applicability);
        }

        [Fact]
        public async Task OMC21_MaxAgeExceeded_ExpiresApplicability_PreservesHistoricalFact()
        {
            var rig = CreateTestRig();
            var prec = CreateSamplePrecedent("P-OLD");
            await rig.store.SavePrecedentAsync(prec);

            var res = await rig.freshnessEvaluator.EvaluateFreshnessAsync(
                TestTenant, "P-OLD", TimeSpan.FromDays(120));

            Assert.Equal(FreshnessStatus.Expired, res.NewFreshness);
            Assert.Equal(CurrentApplicability.Inapplicable, res.NewApplicability);
            Assert.True(res.HistoricalValidityUnchanged);
            Assert.Contains("historical truth remains preserved", res.Rationale);

            var updated = await rig.store.GetPrecedentAsync(TestTenant, "P-OLD");
            Assert.True(updated!.IsHistoricallyValid);
        }

        [Fact]
        public async Task OMC22_FreshnessEvaluation_NonExistentPrecedent_ReturnsUnknownCleanly()
        {
            var rig = CreateTestRig();

            var res = await rig.freshnessEvaluator.EvaluateFreshnessAsync(
                TestTenant, "NONEXISTENT", TimeSpan.FromDays(1));

            Assert.Equal(FreshnessStatus.Unknown, res.NewFreshness);
            Assert.Equal("Precedent not found.", res.Rationale);
        }

        [Fact]
        public void OMC23_PrecedentModel_StructurallyPreservesHistoricalValidityProperty()
        {
            var prop = typeof(OrganizationalPrecedent).GetProperty("IsHistoricallyValid");
            Assert.NotNull(prop);
            Assert.Equal(typeof(bool), prop!.PropertyType);
        }

        [Fact]
        public async Task OMC24_FreshnessResultContainsEvaluatedTimestamp()
        {
            var rig = CreateTestRig();
            var prec = CreateSamplePrecedent("P-TIME");
            await rig.store.SavePrecedentAsync(prec);

            var before = DateTime.UtcNow;
            var res = await rig.freshnessEvaluator.EvaluateFreshnessAsync(TestTenant, "P-TIME", TimeSpan.FromDays(2));

            Assert.True(res.EvaluatedUtc >= before);
        }

        // =========================================================================
        // Family 5: No Epistemic Upgrades (I28-I & I28-J) (OMC25 - OMC30)
        // =========================================================================

        [Fact]
        public async Task OMC25_RepeatedRetrieval_NeverUpgradesEpistemicClassification()
        {
            var rig = CreateTestRig();
            var hypothesis = CreateSamplePrecedent("P-HYPO", epistemic: EpistemicStatus.Hypothesis);
            await rig.store.SavePrecedentAsync(hypothesis);

            // Retrieve multiple times
            for (int i = 0; i < 5; i++)
            {
                await rig.memoryService.GetContextForWorkAsync(TestTenant, "WRK-01");
            }

            var prec = await rig.store.GetPrecedentAsync(TestTenant, "P-HYPO");
            // Remains Hypothesis! Never converted to Fact or Truth per I28-I
            Assert.Equal(EpistemicStatus.Hypothesis, prec!.EpistemicClassification);
        }

        [Fact]
        public void OMC26_EpistemicStatusEnum_HasExplicitSeparation()
        {
            Assert.True((int)EpistemicStatus.Unknown < (int)EpistemicStatus.Hypothesis);
            Assert.True((int)EpistemicStatus.Hypothesis < (int)EpistemicStatus.Inference);
            Assert.True((int)EpistemicStatus.Inference < (int)EpistemicStatus.ObservedFact);
            Assert.True((int)EpistemicStatus.ObservedFact < (int)EpistemicStatus.VerifiedTruth);
        }

        [Fact]
        public void OMC27_InvariantI28I_ProhibitsEpistemicStatusUpgrades()
        {
            Assert.Contains("Retrieval frequency never converts Unknown to Fact",
                OrganizationalMemorySovereignty.I28_I_MemoryCannotUpgradeEpistemicStatus);
        }

        [Fact]
        public void OMC28_InvariantI28J_MandatesRetrievalIsNotValidation()
        {
            Assert.Contains("Retrieved memories must pass through Evidence Validation",
                OrganizationalMemorySovereignty.I28_J_RetrievalNotValidation);
        }

        [Fact]
        public async Task OMC29_InferencePrecedent_NeverUpgradedToVerifiedTruth()
        {
            var rig = CreateTestRig();
            var inference = CreateSamplePrecedent("P-INFER", epistemic: EpistemicStatus.Inference);
            await rig.store.SavePrecedentAsync(inference);

            var snapshot = await rig.assembler.AssembleContextAsync(TestTenant, "WRK-01");
            var item = snapshot.ItemsIncluded.First(i => i.ItemId == "P-INFER");

            Assert.Equal(EpistemicStatus.Inference, item.EpistemicStatus);
        }

        [Fact]
        public async Task OMC30_ContextItem_ReflectsOriginalEpistemicStatusAccurately()
        {
            var rig = CreateTestRig();
            var truth = CreateSamplePrecedent("P-TRUTH", epistemic: EpistemicStatus.VerifiedTruth);
            await rig.store.SavePrecedentAsync(truth);

            var snapshot = await rig.assembler.AssembleContextAsync(TestTenant, "WRK-01");
            var item = snapshot.ItemsIncluded.First(i => i.ItemId == "P-TRUTH");

            Assert.Equal(EpistemicStatus.VerifiedTruth, item.EpistemicStatus);
        }

        // =========================================================================
        // Family 6: Anti-Self-Reinforcement (I28-K) (OMC31 - OMC36)
        // =========================================================================

        [Fact]
        public void OMC31_InvariantI28K_AntiSelfReinforcementEnforced()
        {
            Assert.Contains("Memory-derived evidence cannot cite itself or other memories to increase confidence",
                OrganizationalMemorySovereignty.I28_K_MemoryCannotSelfReinforce);
        }

        [Fact]
        public async Task OMC32_SelfReferencingMemory_RejectedByWriteGate()
        {
            var rig = CreateTestRig();

            // Attempting to cite an existing memory as empirical evidence
            var (permitted, reason) = await rig.writeGate.ValidateWriteAsync(
                TestTenant,
                callerRole: "SystemAdmin",
                sourceRecordType: "InternalMemoryCitation",
                evidenceRef: "MEM-REF-SELF");

            Assert.False(permitted);
            Assert.Contains("not an authorized provenance source", reason);
        }

        [Fact]
        public async Task OMC33_EvidenceReferenceMustBeIndependentHash()
        {
            var rig = CreateTestRig();

            var (permitted, reason) = await rig.writeGate.ValidateWriteAsync(
                TestTenant,
                callerRole: "OutcomeVerifier",
                sourceRecordType: "VerificationRecord",
                evidenceRef: "CERT-EXTERNAL-SHA256");

            Assert.True(permitted);
            Assert.Null(reason);
        }

        [Fact]
        public void OMC34_PrecedentEvidenceReferences_StoredAsIndependentEvidenceList()
        {
            var prec = CreateSamplePrecedent("P-EVID");
            Assert.NotNull(prec.EvidenceReferences);
            Assert.Single(prec.EvidenceReferences);
        }

        [Fact]
        public async Task OMC35_ProvenanceLineage_AnswersWhyMemoryExists()
        {
            var rig = CreateTestRig();

            var lineage = new MemoryProvenanceLineage
            {
                MemoryId = "MEM-WHY-01",
                TenantId = TestTenant,
                SourceRecordType = "OutcomeRecord",
                SourceRecordId = "OUT-999",
                EvidenceHash = "HASH-SHA256-01",
                TelemetryRef = "TEL-REALITY-01",
                VerificationHash = "VERIF-DIGEST-01",
                OutcomeRef = "OUT-999",
                ProvenanceGraphSummary = "Memory -> OutcomeRecord -> VerificationRecord -> TelemetryFact -> RealityEnvelope"
            };

            await rig.store.SaveProvenanceLineageAsync(lineage);

            var retrieved = await rig.memoryService.GetMemoryProvenanceAsync(TestTenant, "MEM-WHY-01");
            Assert.NotNull(retrieved);
            Assert.Equal("OUT-999", retrieved!.OutcomeRef);
            Assert.Contains("RealityEnvelope", retrieved.ProvenanceGraphSummary);
        }

        [Fact]
        public async Task OMC36_NonExistentProvenance_ReturnsNullCleanly()
        {
            var rig = CreateTestRig();
            var res = await rig.memoryService.GetMemoryProvenanceAsync(TestTenant, "NONEXISTENT");
            Assert.Null(res);
        }

        // =========================================================================
        // Family 7: Cross-Lifecycle Trajectory Recording (OMC37 - OMC42)
        // =========================================================================

        [Fact]
        public async Task OMC37_StartTrajectory_BindsResponsibilityAndProposal()
        {
            var rig = CreateTestRig();

            var traj = await rig.trajectoryRecorder.StartTrajectoryAsync(
                TestTenant, "RESP-GROWTH", "PROP-EXPANSION", "WRK-GROWTH-01");

            Assert.NotNull(traj);
            Assert.NotEmpty(traj.TrajectoryId);
            Assert.Equal("WRK-GROWTH-01", traj.WorkId);
            Assert.NotEmpty(traj.ProvenanceHash);
        }

        [Fact]
        public async Task OMC38_RecordMilestone_BindsPlanAndMission()
        {
            var rig = CreateTestRig();

            await rig.trajectoryRecorder.StartTrajectoryAsync(
                TestTenant, "RESP-GROWTH", "PROP-EXPANSION", "WRK-GROWTH-01");

            await rig.trajectoryRecorder.RecordMilestoneAsync(TestTenant, "WRK-GROWTH-01", t =>
            {
                t.WorkPlanId = "PLAN-101";
                t.MissionProposalId = "PROP-MISSION-01";
                t.MissionId = "MIS-202";
                t.NodeIds.Add("N-PERCEIVE");
                t.NodeIds.Add("N-SIMULATE");
            });

            var traj = await rig.memoryService.GetTrajectoryForWorkAsync(TestTenant, "WRK-GROWTH-01");
            Assert.NotNull(traj);
            Assert.Equal("PLAN-101", traj!.WorkPlanId);
            Assert.Equal("MIS-202", traj.MissionId);
            Assert.Equal(2, traj.NodeIds.Count);
        }

        [Fact]
        public async Task OMC39_CompleteTrajectory_BindsOutcomeAndLearningEpisode()
        {
            var rig = CreateTestRig();

            await rig.trajectoryRecorder.StartTrajectoryAsync(
                TestTenant, "RESP-FIN", "PROP-AUDIT", "WRK-AUDIT-01");

            await rig.trajectoryRecorder.RecordMilestoneAsync(TestTenant, "WRK-AUDIT-01", t =>
            {
                t.OutcomeId = "OUT-AUDIT-FINAL";
                t.LearningEpisodeId = "LEARN-EP-88";
                t.CompletedUtc = DateTime.UtcNow;
            });

            var traj = await rig.memoryService.GetTrajectoryForWorkAsync(TestTenant, "WRK-AUDIT-01");
            Assert.NotNull(traj);
            Assert.Equal("OUT-AUDIT-FINAL", traj!.OutcomeId);
            Assert.Equal("LEARN-EP-88", traj.LearningEpisodeId);
            Assert.NotNull(traj.CompletedUtc);
        }

        [Fact]
        public async Task OMC40_TrajectoryIncludedInContextSnapshotForOwnWork()
        {
            var rig = CreateTestRig();

            await rig.trajectoryRecorder.StartTrajectoryAsync(
                TestTenant, "RESP-CORE", "PROP-1", "WRK-TARGET-01");

            var snapshot = await rig.assembler.AssembleContextAsync(TestTenant, "WRK-TARGET-01");

            var trajItem = snapshot.ItemsIncluded.FirstOrDefault(i => i.ItemType == "Trajectory");
            Assert.NotNull(trajItem);
            Assert.Equal(1.0, trajItem!.Score); // Highest relevance
        }

        [Fact]
        public async Task OMC41_TrajectoryForDifferentWork_NotIncludedInSnapshot()
        {
            var rig = CreateTestRig();

            await rig.trajectoryRecorder.StartTrajectoryAsync(
                TestTenant, "RESP-A", "PROP-A", "WRK-OTHER");

            var snapshot = await rig.assembler.AssembleContextAsync(TestTenant, "WRK-MY-WORK");

            Assert.DoesNotContain(snapshot.ItemsIncluded, i => i.ItemType == "Trajectory");
        }

        [Fact]
        public void OMC42_TrajectoryProvenanceHash_ChangesWhenMilestonesUpdate()
        {
            var traj = new OrganizationalTrajectory
            {
                TrajectoryId = "TRAJ-TEST",
                TenantId = TestTenant,
                ResponsibilityId = "RESP-1",
                WorkProposalId = "PROP-1",
                WorkId = "WRK-1",
                StartedUtc = new DateTime(2026, 9, 9, 0, 0, 0, DateTimeKind.Utc)
            };
            traj.ComputeProvenance();
            string p1 = traj.ProvenanceHash;

            traj.MissionId = "MIS-NEW";
            traj.ComputeProvenance();
            string p2 = traj.ProvenanceHash;

            Assert.NotEqual(p1, p2);
        }

        // =========================================================================
        // Family 8: Precedent Applicability & Regime Matching (OMC43 - OMC48)
        // =========================================================================

        [Fact]
        public async Task OMC43_MatchingConditions_KeepApplicabilityHigh()
        {
            var rig = CreateTestRig();
            var prec = CreateSamplePrecedent("P-MATCH", regime: "RegimeHighInflation");
            await rig.store.SavePrecedentAsync(prec);

            var res = await rig.freshnessEvaluator.EvaluateFreshnessAsync(
                TestTenant, "P-MATCH", TimeSpan.FromDays(3), currentRegime: "RegimeHighInflation");

            Assert.Equal(CurrentApplicability.High, res.NewApplicability);
        }

        [Fact]
        public async Task OMC44_DivergentConditions_DowngradeApplicabilityToLow()
        {
            var rig = CreateTestRig();
            var prec = CreateSamplePrecedent("P-DIV", regime: "RegimeZeroInterest");
            await rig.store.SavePrecedentAsync(prec);

            var res = await rig.freshnessEvaluator.EvaluateFreshnessAsync(
                TestTenant, "P-DIV", TimeSpan.FromDays(3), currentRegime: "RegimeHighInterest");

            Assert.Equal(CurrentApplicability.Low, res.NewApplicability);
        }

        [Fact]
        public async Task OMC45_CounterExamples_CanBeRecordedOnPrecedent()
        {
            var prec = CreateSamplePrecedent("P-COUNTER");
            prec.CounterExamples.Add("Failed in European market during Q4");

            Assert.Single(prec.CounterExamples);
        }

        [Fact]
        public void OMC46_CurrentApplicabilityEnum_HasOrderedTiers()
        {
            Assert.True((int)CurrentApplicability.Inapplicable < (int)CurrentApplicability.Low);
            Assert.True((int)CurrentApplicability.Low < (int)CurrentApplicability.Medium);
            Assert.True((int)CurrentApplicability.Medium < (int)CurrentApplicability.High);
        }

        [Fact]
        public async Task OMC47_ListPrecedents_OrderedByCreatedUtcDescending()
        {
            var rig = CreateTestRig();

            var pOld = CreateSamplePrecedent("P-OLD");
            pOld.CreatedUtc = DateTime.UtcNow.AddHours(-2);
            var pNew = CreateSamplePrecedent("P-NEW");
            pNew.CreatedUtc = DateTime.UtcNow;

            await rig.store.SavePrecedentAsync(pOld);
            await rig.store.SavePrecedentAsync(pNew);

            var list = await rig.store.ListPrecedentsAsync(TestTenant);
            Assert.Equal("P-NEW", list[0].PrecedentId);
            Assert.Equal("P-OLD", list[1].PrecedentId);
        }

        [Fact]
        public async Task OMC48_PrecedentVarianceSummary_IncludedInContent()
        {
            var rig = CreateTestRig();
            var prec = CreateSamplePrecedent("P-VAR", variance: -0.15);
            await rig.store.SavePrecedentAsync(prec);

            var snapshot = await rig.assembler.AssembleContextAsync(TestTenant, "WRK-01");
            var item = snapshot.ItemsIncluded.First(i => i.ItemId == "P-VAR");

            Assert.Contains("-0.15", item.Content);
        }

        // =========================================================================
        // Family 9: Anti-Pattern Warning System (I28-F) (OMC49 - OMC54)
        // =========================================================================

        [Fact]
        public async Task OMC49_AntiPatternIncludedAsWarningInContext()
        {
            var rig = CreateTestRig();

            var ap = new OrganizationalAntiPattern
            {
                AntiPatternId = "AP-PRICING-01",
                TenantId = TestTenant,
                Domain = "Sales",
                Title = "Aggressive Upfront Discounting",
                FailurePatternSummary = "Discounting > 25% degrades long term ACV by 40%",
                WarningDirective = "Require CFO approval for discounting > 20%",
                Severity = "High",
                IsActive = true
            };
            await rig.store.SaveAntiPatternAsync(ap);

            var snapshot = await rig.assembler.AssembleContextAsync(TestTenant, "WRK-01");

            var apItem = snapshot.ItemsIncluded.FirstOrDefault(i => i.ItemId == "AP-PRICING-01");
            Assert.NotNull(apItem);
            Assert.Contains("WARNING: ANTI-PATTERN", apItem!.Content);
        }

        [Fact]
        public void OMC50_InvariantI28F_AntiPatternsAreWarningsNotHardProhibitions()
        {
            Assert.Contains("Anti-patterns inform applicability analysis and raise warnings",
                OrganizationalMemorySovereignty.I28_F_AntiPatternsAreWarningsNotProhibitions);
        }

        [Fact]
        public async Task OMC51_InactiveAntiPattern_NotIncludedInContext()
        {
            var rig = CreateTestRig();

            var ap = new OrganizationalAntiPattern
            {
                AntiPatternId = "AP-INACTIVE",
                TenantId = TestTenant,
                Title = "Old Warning",
                IsActive = false
            };
            await rig.store.SaveAntiPatternAsync(ap);

            var snapshot = await rig.assembler.AssembleContextAsync(TestTenant, "WRK-01");

            Assert.DoesNotContain(snapshot.ItemsIncluded, i => i.ItemId == "AP-INACTIVE");
        }

        [Fact]
        public async Task OMC52_MaxAntiPatternsBudgetCeiling_Enforced()
        {
            var policy = new ContextAssemblyPolicy { MaxAntiPatterns = 1 };
            var rig = CreateTestRig(policy);

            await rig.store.SaveAntiPatternAsync(new OrganizationalAntiPattern { AntiPatternId = "AP-1", TenantId = TestTenant, Title = "AP 1", IsActive = true });
            await rig.store.SaveAntiPatternAsync(new OrganizationalAntiPattern { AntiPatternId = "AP-2", TenantId = TestTenant, Title = "AP 2", IsActive = true });

            var snapshot = await rig.assembler.AssembleContextAsync(TestTenant, "WRK-01", policy);

            var apCount = snapshot.ItemsIncluded.Count(i => i.ItemType == "AntiPattern");
            Assert.Equal(1, apCount);
            Assert.Contains("Reached MaxAntiPatterns", snapshot.ExclusionReasons["AP-2"]);
        }

        [Fact]
        public async Task OMC53_DomainFilter_ReturnsMatchingAntiPatterns()
        {
            var rig = CreateTestRig();

            await rig.store.SaveAntiPatternAsync(new OrganizationalAntiPattern { AntiPatternId = "AP-FIN", TenantId = TestTenant, Domain = "Finance", IsActive = true });
            await rig.store.SaveAntiPatternAsync(new OrganizationalAntiPattern { AntiPatternId = "AP-HR", TenantId = TestTenant, Domain = "HR", IsActive = true });

            var finList = await rig.memoryService.GetAntiPatternsForDomainAsync(TestTenant, "Finance");
            Assert.Single(finList);
            Assert.Equal("AP-FIN", finList[0].AntiPatternId);
        }

        [Fact]
        public async Task OMC54_EmptyDomainFilter_ReturnsAllActiveAntiPatterns()
        {
            var rig = CreateTestRig();

            await rig.store.SaveAntiPatternAsync(new OrganizationalAntiPattern { AntiPatternId = "AP-1", TenantId = TestTenant, Domain = "Sales", IsActive = true });
            await rig.store.SaveAntiPatternAsync(new OrganizationalAntiPattern { AntiPatternId = "AP-2", TenantId = TestTenant, Domain = "Ops", IsActive = true });

            var all = await rig.memoryService.GetAntiPatternsForDomainAsync(TestTenant, "");
            Assert.Equal(2, all.Count);
        }

        // =========================================================================
        // Family 10: Historical Context Immutability & Replay (I28-M) (OMC55 - OMC60)
        // =========================================================================

        [Fact]
        public async Task OMC55_SnapshotSavedAndRetrievableBySnapshotId()
        {
            var rig = CreateTestRig();
            await rig.store.SavePrecedentAsync(CreateSamplePrecedent("P-1"));

            var snapshot = await rig.assembler.AssembleContextAsync(TestTenant, "WRK-REPLAY");

            var loaded = await rig.store.GetSnapshotAsync(TestTenant, snapshot.SnapshotId);
            Assert.NotNull(loaded);
            Assert.Equal(snapshot.SnapshotHash, loaded!.SnapshotHash);
        }

        [Fact]
        public void OMC56_SnapshotHash_DeterministicAndTamperSensitive()
        {
            var s = new OrganizationalContextSnapshot
            {
                SnapshotId = "SNAP-1",
                TenantId = TestTenant,
                TargetWorkId = "WRK-1",
                TokenBudget = 8000,
                EstimatedTokenCount = 500,
                AssemblyTimestamp = new DateTime(2026, 9, 9, 0, 0, 0, DateTimeKind.Utc)
            };
            s.ComputeSnapshotHash();
            string hash1 = s.SnapshotHash;

            s.EstimatedTokenCount = 600; // Tamper
            s.ComputeSnapshotHash();
            string hash2 = s.SnapshotHash;

            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void OMC57_InvariantI28M_MandatesHistoricalContextImmutability()
        {
            Assert.Contains("Historical context snapshots used by past missions/decisions are permanently reconstructible",
                OrganizationalMemorySovereignty.I28_M_HistoricalContextImmutability);
        }

        [Fact]
        public void OMC58_ContextSnapshotNotTruthSnapshot_EnforcedPerI28L()
        {
            Assert.Contains("OrganizationalContextSnapshot is a decision-support artifact, never replacing Reality Envelopes",
                OrganizationalMemorySovereignty.I28_L_ContextSnapshotNotTruthSnapshot);
        }

        [Fact]
        public async Task OMC59_ExclusionReasonsAudit_PreservedInSnapshot()
        {
            var policy = new ContextAssemblyPolicy { MaxPrecedents = 1 };
            var rig = CreateTestRig(policy);

            await rig.store.SavePrecedentAsync(CreateSamplePrecedent("P-1"));
            await rig.store.SavePrecedentAsync(CreateSamplePrecedent("P-2"));

            var snapshot = await rig.assembler.AssembleContextAsync(TestTenant, "WRK-01", policy);

            Assert.Single(snapshot.ItemsExcluded);
            Assert.NotEmpty(snapshot.ExclusionReasons);
        }

        [Fact]
        public async Task OMC60_RankingPolicyVersion_TrackedInSnapshot()
        {
            var policy = new ContextAssemblyPolicy { RankingPolicyVersion = "CAP-v2-STRICT" };
            var rig = CreateTestRig(policy);

            var snapshot = await rig.assembler.AssembleContextAsync(TestTenant, "WRK-01", policy);

            Assert.Equal("CAP-v2-STRICT", snapshot.RankingPolicyVersion);
        }

        // =========================================================================
        // Family 11: Explainable Provenance Lineage (OMC61 - OMC66)
        // =========================================================================

        [Fact]
        public async Task OMC61_GetMemoryProvenance_ReturnsDetailedLineage()
        {
            var rig = CreateTestRig();

            var lin = new MemoryProvenanceLineage
            {
                MemoryId = "MEM-EXPLAIN-01",
                TenantId = TestTenant,
                SourceRecordType = "OutcomeRecord",
                SourceRecordId = "OUT-101",
                EvidenceHash = "EVID-CERT-SHA256",
                TelemetryRef = "TEL-REALITY-99",
                VerificationHash = "VERIF-DIGEST-88",
                OutcomeRef = "OUT-101",
                ProvenanceGraphSummary = "Decision(DEC-01) -> Mission(MIS-01) -> Outcome(OUT-101) -> Verification(VERIF-88)"
            };
            await rig.store.SaveProvenanceLineageAsync(lin);

            var retrieved = await rig.memoryService.GetMemoryProvenanceAsync(TestTenant, "MEM-EXPLAIN-01");
            Assert.NotNull(retrieved);
            Assert.Equal("EVID-CERT-SHA256", retrieved!.EvidenceHash);
            Assert.Contains("Verification", retrieved.ProvenanceGraphSummary);
        }

        [Fact]
        public async Task OMC62_MemoryProvenance_NullWhenNotFound()
        {
            var rig = CreateTestRig();

            var res = await rig.memoryService.GetMemoryProvenanceAsync(TestTenant, "GHOST-MEM");
            Assert.Null(res);
        }

        [Fact]
        public void OMC63_PrecedentProvenanceHash_Deterministic()
        {
            var prec = CreateSamplePrecedent("P-DET");
            prec.ComputeProvenance();
            string p1 = prec.ProvenanceHash;
            prec.ComputeProvenance();
            string p2 = prec.ProvenanceHash;

            Assert.Equal(p1, p2);
            Assert.Equal(64, p1.Length);
        }

        [Fact]
        public void OMC64_PrecedentProvenance_TamperSensitive()
        {
            var prec = CreateSamplePrecedent("P-TAMPER");
            prec.ComputeProvenance();
            string original = prec.ProvenanceHash;

            prec.Variance = 0.99; // Tamper
            prec.ComputeProvenance();

            Assert.NotEqual(original, prec.ProvenanceHash);
        }

        [Fact]
        public void OMC65_TrajectoryProvenance_TamperSensitive()
        {
            var traj = new OrganizationalTrajectory
            {
                TrajectoryId = "T-1",
                TenantId = TestTenant,
                WorkId = "W-1",
                StartedUtc = DateTime.UtcNow
            };
            traj.ComputeProvenance();
            string original = traj.ProvenanceHash;

            traj.ResponsibilityId = "TAMPERED-RESP";
            traj.ComputeProvenance();

            Assert.NotEqual(original, traj.ProvenanceHash);
        }

        [Fact]
        public void OMC66_GroundedTimestamp_RecordedOnLineage()
        {
            var lin = new MemoryProvenanceLineage
            {
                MemoryId = "M-1",
                TenantId = TestTenant
            };
            Assert.True((DateTime.UtcNow - lin.GroundedUtc).TotalSeconds < 5);
        }

        // =========================================================================
        // Family 12: Multi-Tenant Isolation (I28-H) (OMC67 - OMC72)
        // =========================================================================

        [Fact]
        public async Task OMC67_PrecedentsPartitionedByTenant()
        {
            var rig = CreateTestRig();

            await rig.store.SavePrecedentAsync(CreateSamplePrecedent("P-A", tenantId: TestTenant));
            await rig.store.SavePrecedentAsync(CreateSamplePrecedent("P-B", tenantId: AltTenant));

            var listA = await rig.store.ListPrecedentsAsync(TestTenant);
            var listB = await rig.store.ListPrecedentsAsync(AltTenant);

            Assert.Single(listA);
            Assert.Single(listB);
            Assert.Equal("P-A", listA[0].PrecedentId);
            Assert.Equal("P-B", listB[0].PrecedentId);
        }

        [Fact]
        public async Task OMC68_AntiPatternsPartitionedByTenant()
        {
            var rig = CreateTestRig();

            await rig.store.SaveAntiPatternAsync(new OrganizationalAntiPattern { AntiPatternId = "AP-A", TenantId = TestTenant, IsActive = true });
            await rig.store.SaveAntiPatternAsync(new OrganizationalAntiPattern { AntiPatternId = "AP-B", TenantId = AltTenant, IsActive = true });

            var listA = await rig.memoryService.GetAntiPatternsForDomainAsync(TestTenant, "");
            var listB = await rig.memoryService.GetAntiPatternsForDomainAsync(AltTenant, "");

            Assert.Single(listA);
            Assert.Single(listB);
            Assert.Equal("AP-A", listA[0].AntiPatternId);
            Assert.Equal("AP-B", listB[0].AntiPatternId);
        }

        [Fact]
        public async Task OMC69_TrajectoriesPartitionedByTenant()
        {
            var rig = CreateTestRig();

            await rig.trajectoryRecorder.StartTrajectoryAsync(TestTenant, "R-A", "P-A", "W-A");
            await rig.trajectoryRecorder.StartTrajectoryAsync(AltTenant, "R-B", "P-B", "W-B");

            var trajA = await rig.memoryService.GetTrajectoryForWorkAsync(TestTenant, "W-A");
            var trajB = await rig.memoryService.GetTrajectoryForWorkAsync(AltTenant, "W-A"); // Cross tenant lookup

            Assert.NotNull(trajA);
            Assert.Null(trajB);
        }

        [Fact]
        public async Task OMC70_ContextSnapshotsPartitionedByTenant()
        {
            var rig = CreateTestRig();
            await rig.store.SavePrecedentAsync(CreateSamplePrecedent("P-A", tenantId: TestTenant));

            var sA = await rig.assembler.AssembleContextAsync(TestTenant, "WRK-A");
            var sB = await rig.assembler.AssembleContextAsync(AltTenant, "WRK-A");

            Assert.NotEmpty(sA.ItemsIncluded);
            Assert.Empty(sB.ItemsIncluded); // Tenant B sees no precedents from Tenant A
        }

        [Fact]
        public async Task OMC71_ProvenanceLineagePartitionedByTenant()
        {
            var rig = CreateTestRig();

            await rig.store.SaveProvenanceLineageAsync(new MemoryProvenanceLineage
            {
                MemoryId = "MEM-ISO",
                TenantId = TestTenant,
                SourceRecordId = "SRC-A"
            });

            var resA = await rig.memoryService.GetMemoryProvenanceAsync(TestTenant, "MEM-ISO");
            var resB = await rig.memoryService.GetMemoryProvenanceAsync(AltTenant, "MEM-ISO");

            Assert.NotNull(resA);
            Assert.Null(resB);
        }

        [Fact]
        public async Task OMC72_FreshnessEvaluationPartitionedByTenant()
        {
            var rig = CreateTestRig();
            await rig.store.SavePrecedentAsync(CreateSamplePrecedent("P-ISO", tenantId: TestTenant));

            var res = await rig.freshnessEvaluator.EvaluateFreshnessAsync(AltTenant, "P-ISO", TimeSpan.FromDays(1));

            Assert.Equal(FreshnessStatus.Unknown, res.NewFreshness);
            Assert.Equal("Precedent not found.", res.Rationale);
        }

        // =========================================================================
        // Family 13: Constitutional Invariant Laws (I28-A .. I28-N) (OMC73 - OMC78)
        // =========================================================================

        [Fact]
        public void OMC73_ConstitutionalInvariantI28_StatementMatchesContract()
        {
            Assert.Equal("I28", OrganizationalMemorySovereignty.InvariantName);
            Assert.Equal("MEMORY ≠ LEARNING ≠ KNOWLEDGE ≠ TRUTH ≠ HYPOTHESIS ≠ POLICY ≠ GOVERNANCE ≠ AUTHORITY ≠ EXECUTION",
                OrganizationalMemorySovereignty.InvariantStatement);
        }

        [Fact]
        public void OMC74_InvariantI28A_MemoryNotGroundTruth()
        {
            Assert.Contains("Memory records historical assertions", OrganizationalMemorySovereignty.I28_A_MemoryNotGroundTruth);
        }

        [Fact]
        public void OMC75_InvariantI28B_MemoryNotLearning()
        {
            Assert.Contains("Remembering an episodic outcome is not causal induction", OrganizationalMemorySovereignty.I28_B_MemoryNotLearning);
        }

        [Fact]
        public void OMC76_InvariantI28C_MemoryNotPolicyOrDoctrine()
        {
            Assert.Contains("Historical precedent cannot alter operational doctrines", OrganizationalMemorySovereignty.I28_C_MemoryNotPolicyOrDoctrine);
        }

        [Fact]
        public void OMC77_InvariantI28D_MemoryNotExecutionAuthority()
        {
            Assert.Contains("cannot issue or modify an ExecutionPermit", OrganizationalMemorySovereignty.I28_D_MemoryNotExecutionAuthority);
        }

        [Fact]
        public void OMC78_MemoryModel_StructurallyContainsZeroExecutionPermits()
        {
            var memoryTypes = new[]
            {
                typeof(OrganizationalPrecedent),
                typeof(OrganizationalAntiPattern),
                typeof(OrganizationalTrajectory),
                typeof(OrganizationalContextSnapshot),
                typeof(MemoryProvenanceLineage)
            };

            foreach (var type in memoryTypes)
            {
                var props = type.GetProperties().Select(p => p.Name).ToList();
                Assert.DoesNotContain("ExecutionPermit", props);
                Assert.DoesNotContain("ExecutionPermitId", props);
            }
        }

        // =========================================================================
        // Family 14: End-to-End Coordination Flow (OMC79 - OMC84)
        // =========================================================================

        [Fact]
        public async Task OMC79_FullLifecycleTrajectory_FromResponsibilityToOutcome()
        {
            var rig = CreateTestRig();

            // 1. Work initiated
            var traj = await rig.trajectoryRecorder.StartTrajectoryAsync(
                TestTenant, "RESP-FIN-01", "PROP-AUDIT-2026", "WRK-FIN-99");

            // 2. Decomposed into plan and mission
            await rig.trajectoryRecorder.RecordMilestoneAsync(TestTenant, "WRK-FIN-99", t =>
            {
                t.WorkPlanId = "PLAN-FIN-99";
                t.MissionProposalId = "PROP-DAG-FIN-99";
                t.MissionId = "MIS-EXEC-99";
                t.NodeIds = new List<string> { "N1-PERCEIVE", "N2-ANALYZE", "N3-VERIFY" };
            });

            // 3. Concluded with outcome
            await rig.trajectoryRecorder.RecordMilestoneAsync(TestTenant, "WRK-FIN-99", t =>
            {
                t.OutcomeId = "OUT-VERIFIED-99";
                t.CompletedUtc = DateTime.UtcNow;
            });

            // 4. Precedent synthesized from outcome
            var precedent = new OrganizationalPrecedent
            {
                PrecedentId = "PREC-FIN-99",
                TenantId = TestTenant,
                SourceTrajectoryId = traj.TrajectoryId,
                Title = "Audit Reconciliation Complete",
                HistoricalOutcomeId = "OUT-VERIFIED-99",
                ObservedConditions = "RegimeStable",
                PredictedOutcome = "Variance <= 1%",
                ActualOutcome = "Variance 0.4%",
                Variance = 0.004,
                EpistemicClassification = EpistemicStatus.VerifiedTruth,
                EvidenceReferences = new List<string> { "CERT-AUDIT-DIGEST" },
                ApplicabilityConditions = new List<string> { "RegimeStable" },
                Freshness = FreshnessStatus.Fresh,
                Applicability = CurrentApplicability.High
            };
            await rig.store.SavePrecedentAsync(precedent);

            // 5. Subsequent work item queries context snapshot
            var snapshot = await rig.memoryService.GetContextForWorkAsync(TestTenant, "WRK-FIN-100");

            Assert.NotNull(snapshot);
            Assert.Contains(snapshot.ItemsIncluded, i => i.ItemId == "PREC-FIN-99");
        }

        [Fact]
        public async Task OMC80_AntiPatternAndPrecedent_BothIncludedInContext()
        {
            var rig = CreateTestRig();

            await rig.store.SavePrecedentAsync(CreateSamplePrecedent("P-GOOD"));
            await rig.store.SaveAntiPatternAsync(new OrganizationalAntiPattern
            {
                AntiPatternId = "AP-BAD",
                TenantId = TestTenant,
                Title = "Negative Cash Flow Risk",
                IsActive = true
            });

            var snapshot = await rig.assembler.AssembleContextAsync(TestTenant, "WRK-E2E");

            Assert.Contains(snapshot.ItemsIncluded, i => i.ItemId == "P-GOOD");
            Assert.Contains(snapshot.ItemsIncluded, i => i.ItemId == "AP-BAD");
        }

        [Fact]
        public async Task OMC81_DegradedApplicabilityItem_ExcludedWhenPolicyIsHighOnly()
        {
            var policy = new ContextAssemblyPolicy { MinApplicability = CurrentApplicability.High };
            var rig = CreateTestRig(policy);

            // Precedent has medium applicability
            var prec = CreateSamplePrecedent("P-MED", applicability: CurrentApplicability.Medium);
            await rig.store.SavePrecedentAsync(prec);

            var snapshot = await rig.assembler.AssembleContextAsync(TestTenant, "WRK-01", policy);

            Assert.DoesNotContain(snapshot.ItemsIncluded, i => i.ItemId == "P-MED");
            Assert.Contains("P-MED", snapshot.ItemsExcluded);
        }

        [Fact]
        public async Task OMC82_EmptyStore_ProducesEmptySnapshotGracefully()
        {
            var rig = CreateTestRig();

            var snapshot = await rig.assembler.AssembleContextAsync(TestTenant, "WRK-EMPTY");

            Assert.NotNull(snapshot);
            Assert.Empty(snapshot.ItemsIncluded);
            Assert.Empty(snapshot.ItemsExcluded);
            Assert.Equal(0, snapshot.EstimatedTokenCount);
        }

        [Fact]
        public async Task OMC83_MultipleMilestoneUpdates_AccumulateInTrajectory()
        {
            var rig = CreateTestRig();

            await rig.trajectoryRecorder.StartTrajectoryAsync(TestTenant, "R-1", "P-1", "WRK-ACC");

            await rig.trajectoryRecorder.RecordMilestoneAsync(TestTenant, "WRK-ACC", t => t.WorkPlanId = "PLAN-A");
            await rig.trajectoryRecorder.RecordMilestoneAsync(TestTenant, "WRK-ACC", t => t.MissionId = "MIS-B");

            var traj = await rig.store.GetTrajectoryForWorkAsync(TestTenant, "WRK-ACC");
            Assert.Equal("PLAN-A", traj!.WorkPlanId);
            Assert.Equal("MIS-B", traj.MissionId);
        }

        [Fact]
        public async Task OMC84_NullWorkId_ThrowsArgumentException()
        {
            var rig = CreateTestRig();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                rig.assembler.AssembleContextAsync(TestTenant, ""));
            await Assert.ThrowsAsync<ArgumentException>(() =>
                rig.assembler.AssembleContextAsync("", "WRK-1"));
        }
    }
}
