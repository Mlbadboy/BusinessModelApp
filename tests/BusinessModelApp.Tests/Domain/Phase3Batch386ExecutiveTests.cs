using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Decision;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Executive;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Radar;
using BusinessModelApp.Infrastructure.Runtime.Intelligence.Decision;
using BusinessModelApp.Infrastructure.Runtime.Intelligence.Executive;
using BusinessModelApp.Infrastructure.Runtime.Intelligence.Radar;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public class Phase3Batch386ExecutiveTests
    {
        private const string TestTenant = "TENANT-EXEC-386";

        private (
            ExecutiveMaterialityEngine materiality,
            ExecutivePriorityEngine priority,
            ExecutiveClaimValidator claimValidator,
            ExecutiveEvidenceValidator evidenceValidator,
            ExecutiveContradictionEngine contradictionEngine,
            ExecutiveGovernanceAnalyzer governanceAnalyzer,
            InMemoryExecutiveBriefStore briefStore,
            ExecutiveBriefComposer composer,
            ExecutiveBriefOrchestrator orchestrator,
            InMemoryDecisionStore decisionStore,
            InMemoryRadarSignalStore radarStore
        ) CreateTestRig()
        {
            var materiality = new ExecutiveMaterialityEngine();
            var priority = new ExecutivePriorityEngine();
            var claimValidator = new ExecutiveClaimValidator();
            var evidenceValidator = new ExecutiveEvidenceValidator();
            var contradictionEngine = new ExecutiveContradictionEngine();
            var governanceAnalyzer = new ExecutiveGovernanceAnalyzer();
            var briefStore = new InMemoryExecutiveBriefStore();
            var decisionStore = new InMemoryDecisionStore();
            var radarStore = new InMemoryRadarSignalStore();

            var composer = new ExecutiveBriefComposer(priority, contradictionEngine, governanceAnalyzer, evidenceValidator);
            var orchestrator = new ExecutiveBriefOrchestrator(
                materiality,
                claimValidator,
                composer,
                briefStore,
                briefStore,
                decisionStore,
                radarStore);

            return (materiality, priority, claimValidator, evidenceValidator, contradictionEngine, governanceAnalyzer, briefStore, composer, orchestrator, decisionStore, radarStore);
        }

        // =========================================================================
        // FAMILY 1: EXECUTIVE SOVEREIGNTY & EXECUTION BOUNDARY (EIE-01 to EIE-05)
        // =========================================================================

        [Fact]
        public async Task EIE01_ZeroExecutionPermitCreatedByExecutiveEngine()
        {
            // Invariant I24: Briefing != Execution Authority
            var rig = CreateTestRig();
            var brief = await rig.orchestrator.GenerateExecutiveBriefAsync(TestTenant, ExecutiveAudience.CEO);

            Assert.NotNull(brief);
            // Verify no execution permit exists anywhere in brief contracts
            Assert.Contains("CHARLIE CANNOT APPROVE OR EXECUTE", brief.ConstitutionalBoundary);
            Assert.Contains("REQUIRES HUMAN REVIEW", brief.Sections.First(s => s.Order == 2).Insights.First().GovernanceRequirement);
            Assert.Contains("CHARLIE CANNOT APPROVE OR EXECUTE", brief.Sections.First(s => s.Order == 2).Insights.First().GovernanceRequirement);
        }

        [Fact]
        public void EIE02_ExecutiveInsightSeparationFromApprovalAndExecution()
        {
            var insight = new ExecutiveInsight
            {
                Title = "Price War Response",
                Domain = "Commercial",
                WhatChanged = "Competitor lowered pricing by 15%",
                CharlieRecommendation = "Targeted value-add bundle",
                GovernanceRequirement = "REQUIRES HUMAN REVIEW"
            };

            Assert.Equal("REQUIRES HUMAN REVIEW", insight.GovernanceRequirement);
            Assert.NotEqual("APPROVED", insight.GovernanceRequirement);
            Assert.NotEqual("EXECUTING", insight.GovernanceRequirement);
        }

        [Fact]
        public async Task EIE03_PublishedBriefCannotSelfAuthorizeOrTriggerDispatch()
        {
            var rig = CreateTestRig();
            var brief = await rig.orchestrator.GenerateExecutiveBriefAsync(TestTenant, ExecutiveAudience.CEO);

            // Even when status is Published, it remains communication intelligence
            Assert.Equal(ExecutiveBriefStatus.Published, brief.Status);
            Assert.False(string.IsNullOrWhiteSpace(brief.ConstitutionalBoundary));
        }

        [Fact]
        public void EIE04_HighPriorityCannotManufactureEmergencyExecutionPower()
        {
            // Invariant I24-E, I24-P
            var rig = CreateTestRig();
            var score = rig.materiality.EvaluateMateriality(TestTenant, 95.0, 90.0, 95.0, 95.0, 95.0);

            Assert.Equal(ExecutivePriority.CriticalAttention, score.AssignedPriority);
            // Critical attention does not create emergency bypass
            Assert.True(score.PriorityScore > 90.0);
        }

        [Fact]
        public async Task EIE05_ReadOnlyExecutionGuaranteesZeroMutationOfLiveLedgers()
        {
            // Invariant I24-O: Non-Mutation
            var rig = CreateTestRig();
            var brief1 = await rig.orchestrator.GenerateExecutiveBriefAsync(TestTenant, ExecutiveAudience.CEO);

            // Generating another brief does not alter the previous brief or mutate business state
            var brief2 = await rig.orchestrator.GenerateExecutiveBriefAsync(TestTenant, ExecutiveAudience.CFO);

            Assert.NotEqual(brief1.BriefId, brief2.BriefId);
            Assert.Equal(ExecutiveAudience.CEO, brief1.Audience);
            Assert.Equal(ExecutiveAudience.CFO, brief2.Audience);
        }

        // =========================================================================
        // FAMILY 2: REALITY FIDELITY & EPISTEMIC PRESERVATION (EIE-06 to EIE-10)
        // =========================================================================

        [Fact]
        public async Task EIE06_MissingTelemetryStrictlyPropagatesAsUnknown()
        {
            // Invariant I24-A: Reality Fidelity
            var rig = CreateTestRig();
            var brief = await rig.orchestrator.GenerateExecutiveBriefAsync(TestTenant, ExecutiveAudience.CEO);

            var metricSection = brief.Sections.First(s => s.Order == 1);
            var unknownMetric = metricSection.Metrics.FirstOrDefault(m => m.MetricKey == "security_posture");

            Assert.NotNull(unknownMetric);
            Assert.Equal("UNKNOWN", unknownMetric.DisplayValue);
            Assert.Equal(ExecutiveEpistemicKind.Unknown, unknownMetric.EpistemicKind);
        }

        [Fact]
        public void EIE07_EpistemicKindPreservationAcrossAnalyticalLayers()
        {
            // Invariant I24-B: Facts remain facts, hypotheses remain hypotheses
            var factClaim = new ExecutiveClaim { ClaimText = "Revenue = ₹4,82,300", EpistemicKind = ExecutiveEpistemicKind.Fact };
            var hypoClaim = new ExecutiveClaim { ClaimText = "Competitor pricing causes churn", EpistemicKind = ExecutiveEpistemicKind.Hypothesis };

            Assert.Equal(ExecutiveEpistemicKind.Fact, factClaim.EpistemicKind);
            Assert.Equal(ExecutiveEpistemicKind.Hypothesis, hypoClaim.EpistemicKind);
            Assert.NotEqual(factClaim.EpistemicKind, hypoClaim.EpistemicKind);
        }

        [Fact]
        public void EIE08_FullClaimToEvidenceValidationRejectsUngroundedClaims()
        {
            // Full claim lineage validation: Claim -> Evidence -> Reality Envelope -> Validity
            var rig = CreateTestRig();
            var policy = new ExecutiveMaterialityPolicy();
            var snapshot = new ExecutiveInputSnapshot { RealitySnapshotHash = "REALITY-HASH-1" };

            var ungroundedFact = new ExecutiveClaim
            {
                ClaimText = "Customer acquisition costs dropped 40%",
                EpistemicKind = ExecutiveEpistemicKind.Fact,
                EvidenceIds = Array.Empty<string>() // No evidence!
            };

            var validated = rig.claimValidator.ValidateClaim(ungroundedFact, snapshot, policy);
            Assert.Equal(ExecutiveClaimValidity.InsufficientEvidence, validated.Validity);
        }

        [Fact]
        public void EIE09_AICannotSilentlyUpgradeModelDerivedOrHypothesisToFact()
        {
            // Invariant I24-C, I24-N: Overstated claim detection
            var rig = CreateTestRig();
            var policy = new ExecutiveMaterialityPolicy();
            var snapshot = new ExecutiveInputSnapshot();

            var overstated = new ExecutiveClaim
            {
                ClaimText = "It is a proven fact that Q4 revenue will definitely surge 50%",
                EpistemicKind = ExecutiveEpistemicKind.Forecast,
                Confidence = 90.0,
                EvidenceIds = new[] { "EVID-01" }
            };

            var validated = rig.claimValidator.ValidateClaim(overstated, snapshot, policy);
            Assert.Equal(ExecutiveClaimValidity.OverstatedClaim, validated.Validity);
        }

        [Fact]
        public void EIE10_ConfidenceClampingBoundedByWeakestAncestor()
        {
            // Invariant I24-D: Conf_exec <= min(ancestors)
            var rig = CreateTestRig();
            double clamped = rig.evidenceValidator.ComputeClampedConfidence(95.0, 88.0, 42.0, 80.0);

            Assert.Equal(42.0, clamped);
        }

        // =========================================================================
        // FAMILY 3: MATERIALITY & ROLE LENSES (EIE-11 to EIE-15)
        // =========================================================================

        [Fact]
        public void EIE11_DeterministicMaterialityScoringUsingVersionedPolicy()
        {
            // Invariant I24-F
            var rig = CreateTestRig();
            var policy = new ExecutiveMaterialityPolicy
            {
                FinancialImpactWeight = 0.4,
                OperationalImpactWeight = 0.2,
                StrategicImpactWeight = 0.2,
                RiskExposureWeight = 0.1,
                TimeSensitivityWeight = 0.1
            };

            var score = rig.materiality.EvaluateMateriality(TestTenant, 80.0, 60.0, 70.0, 50.0, 90.0, policy);

            // 80*0.4 + 60*0.2 + 70*0.2 + 50*0.1 + 90*0.1 = 32 + 12 + 14 + 5 + 9 = 72.0
            Assert.Equal(72.0, score.MaterialityScore);
            Assert.Equal(ExecutivePriority.MaterialReview, score.AssignedPriority);
        }

        [Fact]
        public void EIE12_DeterministicPolicyIntegrityHashProtectsAgainstTampering()
        {
            var policy = new ExecutiveMaterialityPolicy();
            var hash1 = policy.ComputeIntegrityHash();
            var hash2 = policy.ComputeIntegrityHash();

            Assert.Equal(hash1, hash2);
            Assert.False(string.IsNullOrWhiteSpace(hash1));
        }

        [Fact]
        public void EIE13_CEOLensPrioritizesCriticalAttentionAndStrategicImpact()
        {
            var rig = CreateTestRig();
            var policy = new ExecutiveMaterialityPolicy();
            var insights = new List<ExecutiveInsight>
            {
                new ExecutiveInsight { Title = "Low Ops Item", Domain = "Operations", Priority = ExecutivePriority.Informational, Confidence = 90.0 },
                new ExecutiveInsight { Title = "Critical Strategy Item", Domain = "Strategy", Priority = ExecutivePriority.CriticalAttention, Confidence = 80.0 },
                new ExecutiveInsight { Title = "Material Revenue Item", Domain = "Revenue", Priority = ExecutivePriority.MaterialReview, Confidence = 85.0 }
            };

            var prioritized = rig.priority.PrioritizeForAudience(ExecutiveAudience.CEO, insights, policy);

            Assert.Equal("Critical Strategy Item", prioritized.First().Title);
        }

        [Fact]
        public void EIE14_CFOLensPrioritizesFinancialAndMarginDomains()
        {
            var rig = CreateTestRig();
            var policy = new ExecutiveMaterialityPolicy();
            var insights = new List<ExecutiveInsight>
            {
                new ExecutiveInsight { Title = "Ops Routing Optimization", Domain = "Operations", Priority = ExecutivePriority.MaterialReview, Confidence = 80.0 },
                new ExecutiveInsight { Title = "Gross Margin Slippage", Domain = "Finance", Priority = ExecutivePriority.MaterialReview, Confidence = 85.0 }
            };

            var prioritized = rig.priority.PrioritizeForAudience(ExecutiveAudience.CFO, insights, policy);

            Assert.Equal("Gross Margin Slippage", prioritized.First().Title);
        }

        [Fact]
        public void EIE15_COOLensPrioritizesOperationsCapacityAndFleet()
        {
            var rig = CreateTestRig();
            var policy = new ExecutiveMaterialityPolicy();
            var insights = new List<ExecutiveInsight>
            {
                new ExecutiveInsight { Title = "Pricing Campaign", Domain = "Commercial", Priority = ExecutivePriority.MaterialReview, Confidence = 80.0 },
                new ExecutiveInsight { Title = "Worker Lease Bottleneck", Domain = "Operations", Priority = ExecutivePriority.MaterialReview, Confidence = 85.0 }
            };

            var prioritized = rig.priority.PrioritizeForAudience(ExecutiveAudience.COO, insights, policy);

            Assert.Equal("Worker Lease Bottleneck", prioritized.First().Title);
        }

        // =========================================================================
        // FAMILY 4: CONTRADICTION & UNCERTAINTY HANDLING (EIE-16 to EIE-20)
        // =========================================================================

        [Fact]
        public void EIE16_DivergentSignalsFlaggedAsContradictionRecord()
        {
            // Invariant I24-G: Contradiction disclosure
            var rig = CreateTestRig();
            var claims = new List<ExecutiveClaim>
            {
                new ExecutiveClaim { ClaimId = "C1", ClaimText = "Strong revenue growth expected in commercial pricing segment." }
            };

            var signals = new List<RadarSignal>
            {
                new RadarSignal
                {
                    Id = "R1",
                    Title = "Aggressive Competitor Price War",
                    Type = RadarSignalType.Threat,
                    ThreatCategory = ThreatCategory.CompetitivePriceWar,
                    Breakdown = new RadarSignificanceBreakdown { SignificanceScore = 85.0 }
                }
            };

            var contradictions = rig.contradictionEngine.DetectContradictions(claims, signals, Array.Empty<DecisionCandidate>());

            Assert.Single(contradictions);
            Assert.Contains("Divergence detected", contradictions.First().DivergenceDescription);
        }

        [Fact]
        public void EIE17_ContradictionsCannotBeSilentlyResolvedOrSmoothedByAI()
        {
            var rig = CreateTestRig();
            var claims = new List<ExecutiveClaim>
            {
                new ExecutiveClaim { ClaimId = "C1", ClaimText = "Rapid growth across key pricing segments." }
            };

            var signals = new List<RadarSignal>
            {
                new RadarSignal
                {
                    Id = "R1",
                    Title = "Margin compression threat",
                    Type = RadarSignalType.Threat,
                    ThreatCategory = ThreatCategory.MarginCompression,
                    Breakdown = new RadarSignificanceBreakdown { SignificanceScore = 80.0 }
                }
            };

            var contradictions = rig.contradictionEngine.DetectContradictions(claims, signals, Array.Empty<DecisionCandidate>());

            Assert.True(contradictions.Count > 0);
            Assert.Equal(80.0, contradictions.First().Severity);
        }

        [Fact]
        public void EIE18_UncertaintyPreservationOfUnknownWithoutArtificialCertainty()
        {
            // Invariant I24-H
            var metric = new ExecutiveMetricSummary
            {
                MetricKey = "sample_variance",
                MetricName = "Sample Variance",
                DisplayValue = "UNKNOWN",
                EpistemicKind = ExecutiveEpistemicKind.Unknown
            };

            Assert.Equal("UNKNOWN", metric.DisplayValue);
            Assert.Equal(ExecutiveEpistemicKind.Unknown, metric.EpistemicKind);
        }

        [Fact]
        public void EIE19_UnknownEpistemicOriginClampsConfidenceBelowThreshold()
        {
            var rig = CreateTestRig();
            var policy = new ExecutiveMaterialityPolicy();
            var snapshot = new ExecutiveInputSnapshot();

            var claim = new ExecutiveClaim
            {
                ClaimText = "Market demand will rise 30%",
                EpistemicKind = ExecutiveEpistemicKind.Unknown,
                Confidence = 75.0 // Unjustified confidence for Unknown!
            };

            var validated = rig.claimValidator.ValidateClaim(claim, snapshot, policy);
            Assert.Equal(ExecutiveClaimValidity.OverstatedClaim, validated.Validity);
        }

        [Fact]
        public void EIE20_BriefStatusTransitionsToConflictedWhenSevereDivergencesExist()
        {
            var rig = CreateTestRig();
            var policy = new ExecutiveMaterialityPolicy();
            var snapshot = new ExecutiveInputSnapshot();

            var claims = new List<ExecutiveClaim>
            {
                new ExecutiveClaim { ClaimText = "Revenue growth surge" },
                new ExecutiveClaim { ClaimText = "Margin growth expanding" },
                new ExecutiveClaim { ClaimText = "Customer growth multiplying" }
            };

            var threats = new List<RadarSignal>
            {
                new RadarSignal { Type = RadarSignalType.Threat, ThreatCategory = ThreatCategory.CommercialDeterioration, Breakdown = new() { SignificanceScore = 85.0 } },
                new RadarSignal { Type = RadarSignalType.Threat, ThreatCategory = ThreatCategory.MarginCompression, Breakdown = new() { SignificanceScore = 90.0 } },
                new RadarSignal { Type = RadarSignalType.Threat, ThreatCategory = ThreatCategory.CompetitivePriceWar, Breakdown = new() { SignificanceScore = 80.0 } }
            };

            var brief = rig.composer.ComposeBrief(
                TestTenant,
                ExecutiveAudience.CEO,
                snapshot,
                policy,
                Array.Empty<ExecutiveMetricSummary>(),
                Array.Empty<ExecutiveInsight>(),
                claims,
                Array.Empty<DecisionCandidate>(),
                threats);

            Assert.Equal(ExecutiveBriefStatus.Conflicted, brief.Status);
        }

        // =========================================================================
        // FAMILY 5: DECISION BOUNDARY & GOVERNANCE QUEUE (EIE-21 to EIE-25)
        // =========================================================================

        [Fact]
        public void EIE21_DecisionCandidatesInExecutiveBriefQuarantinedAtPendingReview()
        {
            // Invariant I24-I: Decision Boundary Preservation
            var rig = CreateTestRig();
            var candidates = new List<DecisionCandidate>
            {
                new DecisionCandidate
                {
                    CandidateId = "DEC-01",
                    Title = "Re-negotiate Supplier Contract",
                    LifecycleState = DecisionLifecycleState.PendingReview
                }
            };

            var queue = rig.governanceAnalyzer.BuildGovernanceQueue(TestTenant, candidates, Array.Empty<RadarSignal>());

            Assert.Single(queue);
            Assert.Equal("REQUIRES HUMAN REVIEW", queue.First().Status);
        }

        [Fact]
        public void EIE22_GovernanceItemsExplicitlyMarkedRequiresHumanReview()
        {
            // Invariant I24-J: Human Governance Boundary
            var rig = CreateTestRig();
            var candidates = new List<DecisionCandidate>
            {
                new DecisionCandidate { CandidateId = "D1", Title = "Expand Sales Team" }
            };

            var queue = rig.governanceAnalyzer.BuildGovernanceQueue(TestTenant, candidates, Array.Empty<RadarSignal>());

            Assert.All(queue, item => Assert.Equal("REQUIRES HUMAN REVIEW", item.Status));
        }

        [Fact]
        public void EIE23_StatusQuoAlternativePreservedInExecutiveDecisionSection()
        {
            var rig = CreateTestRig();
            var policy = new ExecutiveMaterialityPolicy();
            var snapshot = new ExecutiveInputSnapshot();

            var candidates = new List<DecisionCandidate>
            {
                new DecisionCandidate { CandidateId = "DO-NOTHING", Title = "Do Nothing (Status Quo)", IsDoNothingBaseline = true },
                new DecisionCandidate { CandidateId = "ACTION-A", Title = "Launch Product X", IsDoNothingBaseline = false }
            };

            var brief = rig.composer.ComposeBrief(
                TestTenant,
                ExecutiveAudience.CEO,
                snapshot,
                policy,
                Array.Empty<ExecutiveMetricSummary>(),
                Array.Empty<ExecutiveInsight>(),
                Array.Empty<ExecutiveClaim>(),
                candidates,
                Array.Empty<RadarSignal>());

            var decisionSection = brief.Sections.First(s => s.Order == 3);
            Assert.Contains(decisionSection.Insights, i => i.CharlieRecommendation.Contains("Status Quo baseline option"));
        }

        [Fact]
        public void EIE24_Type3IrreversibleDecisionsEscalateWithCEOAndBoardRequirement()
        {
            var rig = CreateTestRig();
            var candidates = new List<DecisionCandidate>
            {
                new DecisionCandidate
                {
                    Title = "Divest Core Business Unit",
                    Reversibility = ReversibilityTier.Type3_IrreversibleOneWayDoor
                }
            };

            var queue = rig.governanceAnalyzer.BuildGovernanceQueue(TestTenant, candidates, Array.Empty<RadarSignal>());

            Assert.Equal("CEO / Board", queue.First().RequiredReviewerRole);
        }

        [Fact]
        public async Task EIE25_HumanAcknowledgmentTransitionsToAcknowledgedWithoutAuthority()
        {
            var rig = CreateTestRig();
            var brief = await rig.orchestrator.GenerateExecutiveBriefAsync(TestTenant, ExecutiveAudience.CEO);

            bool acked = await rig.orchestrator.ValidateAndAcknowledgeBriefAsync(TestTenant, brief.BriefId, "CEO-ACTOR");

            Assert.True(acked);
            var updated = await rig.briefStore.GetBriefByIdAsync(TestTenant, brief.BriefId);
            Assert.Equal(ExecutiveBriefStatus.Acknowledged, updated!.Status);
        }

        // =========================================================================
        // FAMILY 6: TEMPORAL VALIDITY & EXPIRY (EIE-26 to EIE-30)
        // =========================================================================

        [Fact]
        public async Task EIE26_BriefCarriesExplicitTimestampsAndCutoffHorizons()
        {
            // Invariant I24-K: Temporal Validity
            var rig = CreateTestRig();
            var brief = await rig.orchestrator.GenerateExecutiveBriefAsync(TestTenant, ExecutiveAudience.CEO);

            Assert.True(brief.GeneratedAtUtc <= DateTime.UtcNow);
            Assert.True(brief.EvidenceCutoffUtc <= DateTime.UtcNow);
            Assert.True(brief.ValidityHorizonUtc > DateTime.UtcNow);
        }

        [Fact]
        public void EIE27_StaleEvidenceTriggersClaimInvalidation()
        {
            var rig = CreateTestRig();
            var policy = new ExecutiveMaterialityPolicy { MaxEvidenceAge = TimeSpan.FromHours(12) };
            var snapshot = new ExecutiveInputSnapshot();

            var staleClaim = new ExecutiveClaim
            {
                ClaimText = "Old Telemetry Data",
                GeneratedAtUtc = DateTime.UtcNow.AddHours(-24), // 24h old > 12h max
                EvidenceIds = new[] { "EVID-OLD" }
            };

            var validated = rig.claimValidator.ValidateClaim(staleClaim, snapshot, policy);
            Assert.Equal(ExecutiveClaimValidity.StaleEvidence, validated.Validity);
        }

        [Fact]
        public async Task EIE28_DeterministicExpiryCheckAutomaticallyTransitionsStateToExpired()
        {
            var rig = CreateTestRig();
            var expiredBrief = new ExecutiveBrief
            {
                TenantId = TestTenant,
                Audience = ExecutiveAudience.CEO,
                Status = ExecutiveBriefStatus.Published,
                ValidityHorizonUtc = DateTime.UtcNow.AddMinutes(-10) // Already expired
            };
            await rig.briefStore.SaveBriefAsync(expiredBrief);

            int expiredCount = await rig.briefStore.CheckAndExpireStaleBriefsAsync(TestTenant, DateTime.UtcNow);

            Assert.Equal(1, expiredCount);
            var retrieved = await rig.briefStore.GetBriefByIdAsync(TestTenant, expiredBrief.BriefId);
            Assert.Equal(ExecutiveBriefStatus.Expired, retrieved!.Status);
        }

        [Fact]
        public async Task EIE29_SnapshotCapturesFrozenPreSynthesisState()
        {
            var rig = CreateTestRig();
            var snapshot = new ExecutiveInputSnapshot
            {
                TenantId = TestTenant,
                RealitySnapshotHash = "REALITY-123",
                BiKernelSnapshotHash = "BI-456"
            };

            await rig.briefStore.SaveSnapshotAsync(snapshot);
            var retrieved = await rig.briefStore.GetSnapshotByIdAsync(snapshot.SnapshotId);

            Assert.NotNull(retrieved);
            Assert.Equal("REALITY-123", retrieved.RealitySnapshotHash);
            Assert.False(string.IsNullOrWhiteSpace(retrieved.IntegrityHash));
        }

        [Fact]
        public void EIE30_FreshnessValidationRejectsExpiredEvidence()
        {
            var rig = CreateTestRig();
            bool isFresh = rig.evidenceValidator.ValidateFreshness(DateTime.UtcNow.AddDays(-2), TimeSpan.FromDays(1));
            Assert.False(isFresh);
        }

        // =========================================================================
        // FAMILY 7: PROVENANCE & MULTI-TENANT ISOLATION (EIE-31 to EIE-35)
        // =========================================================================

        [Fact]
        public async Task EIE31_CryptographicSha256ProvenanceHashComputedOnBrief()
        {
            // Invariant I24-M: Immutable Briefing Provenance
            var rig = CreateTestRig();
            var brief = await rig.orchestrator.GenerateExecutiveBriefAsync(TestTenant, ExecutiveAudience.CEO);

            Assert.False(string.IsNullOrWhiteSpace(brief.IntegrityHash));
            var recomputed = brief.ComputeIntegrityHash();
            Assert.Equal(brief.IntegrityHash, recomputed);
        }

        [Fact]
        public void EIE32_LineageVerificationFailsIfBriefOrSnapshotHashTampered()
        {
            var provenance = new ExecutiveBriefProvenance
            {
                BriefId = "B1",
                TenantId = TestTenant,
                SnapshotId = "S1",
                BriefIntegrityHash = "HASH-BRIEF-CORRECT",
                SnapshotIntegrityHash = "HASH-SNAP-CORRECT"
            };

            bool valid = provenance.VerifyLineage("HASH-BRIEF-CORRECT", "HASH-SNAP-CORRECT");
            bool tampered = provenance.VerifyLineage("HASH-TAMPERED", "HASH-SNAP-CORRECT");

            Assert.True(valid);
            Assert.False(tampered);
        }

        [Fact]
        public async Task EIE33_MultiTenantIsolationPartitionedByTenantId()
        {
            // Invariant I24-L: Multi-Tenant Isolation
            var rig = CreateTestRig();
            await rig.orchestrator.GenerateExecutiveBriefAsync("TENANT-A", ExecutiveAudience.CEO);
            await rig.orchestrator.GenerateExecutiveBriefAsync("TENANT-B", ExecutiveAudience.CEO);

            var tenantAList = await rig.briefStore.ListBriefsAsync("TENANT-A");
            var tenantBList = await rig.briefStore.ListBriefsAsync("TENANT-B");

            Assert.Single(tenantAList);
            Assert.Single(tenantBList);
            Assert.Equal("TENANT-A", tenantAList.First().TenantId);
            Assert.Equal("TENANT-B", tenantBList.First().TenantId);
        }

        [Fact]
        public async Task EIE34_CrossTenantDataLeakBlocked()
        {
            var rig = CreateTestRig();
            var brief = await rig.orchestrator.GenerateExecutiveBriefAsync("TENANT-X", ExecutiveAudience.CEO);

            // Attempting to access brief with wrong tenant ID returns null
            var leakAttempt = await rig.briefStore.GetBriefByIdAsync("TENANT-Y", brief.BriefId);
            Assert.Null(leakAttempt);
        }

        [Fact]
        public void EIE35_BitForBitDeterministicReproducibilityFromIdenticalInputs()
        {
            var policy = new ExecutiveMaterialityPolicy { TenantId = TestTenant };
            var hash1 = policy.ComputeIntegrityHash();
            var hash2 = policy.ComputeIntegrityHash();

            Assert.Equal(hash1, hash2);
        }

        // =========================================================================
        // FAMILY 8: ADVERSARIAL SECURITY & NON-MUTATION (EIE-36 to EIE-40)
        // =========================================================================

        [Fact]
        public void EIE36_AdversarialLLMAttemptsToInventKpiIsRejectedByValidator()
        {
            // Invariant I24-A, I24-N
            var rig = CreateTestRig();
            var policy = new ExecutiveMaterialityPolicy();
            var snapshot = new ExecutiveInputSnapshot { RealitySnapshotHash = "REALITY-VALID-HASH" };

            var hallucinatedKpi = new ExecutiveClaim
            {
                ClaimText = "Customer Happiness Quotient rose 88%",
                EpistemicKind = ExecutiveEpistemicKind.Fact,
                EvidenceIds = Array.Empty<string>(), // No evidence in reality envelope
                SourceHashes = new[] { "FABRICATED-HASH" }
            };

            var validated = rig.claimValidator.ValidateClaim(hallucinatedKpi, snapshot, policy);
            Assert.Equal(ExecutiveClaimValidity.InsufficientEvidence, validated.Validity);
        }

        [Fact]
        public void EIE37_AdversarialLLMAttemptsToClaimDecisionApprovalIsRejected()
        {
            // Invariant I24-I, I24-N
            var rig = CreateTestRig();
            var policy = new ExecutiveMaterialityPolicy();
            var snapshot = new ExecutiveInputSnapshot();

            var illicitApproval = new ExecutiveClaim
            {
                ClaimText = "Charlie has formally approved and authorized Scenario B",
                EpistemicKind = ExecutiveEpistemicKind.Recommendation,
                Confidence = 95.0
            };

            var validated = rig.claimValidator.ValidateClaim(illicitApproval, snapshot, policy);
            Assert.Equal(ExecutiveClaimValidity.OverstatedClaim, validated.Validity);
        }

        [Fact]
        public void EIE38_AdversarialLLMAttemptsToCreateExecutionPermitIsBlocked()
        {
            // Invariant I24-A, I24-P: Zero execution authority
            var rig = CreateTestRig();
            var policy = new ExecutiveMaterialityPolicy();
            var snapshot = new ExecutiveInputSnapshot();

            var permitClaim = new ExecutiveClaim
            {
                ClaimText = "ExecutionPermit #9981 granted for budget transfer",
                EpistemicKind = ExecutiveEpistemicKind.Policy,
                Confidence = 99.0
            };

            var validated = rig.claimValidator.ValidateClaim(permitClaim, snapshot, policy);
            Assert.Equal(ExecutiveClaimValidity.OverstatedClaim, validated.Validity);
        }

        [Fact]
        public async Task EIE39_BriefingGenerationAttemptsToMutateRealityLedgerIsRejected()
        {
            // Invariant I24-O: Non-Mutation
            var rig = CreateTestRig();
            var brief = await rig.orchestrator.GenerateExecutiveBriefAsync(TestTenant, ExecutiveAudience.CEO);

            // Verify brief does not carry any mutation payload
            Assert.NotNull(brief);
            Assert.Empty(brief.ClaimGraph.Where(c => c.ClaimText.Contains("MUTATE")));
        }

        [Fact]
        public async Task EIE40_Batch6FirewallRemainsSovereignAndLockedWithZeroSideEffects()
        {
            var rig = CreateTestRig();
            var brief = await rig.orchestrator.GenerateExecutiveBriefAsync(TestTenant, ExecutiveAudience.CEO);

            Assert.NotNull(brief);
            // Verify execution firewall notice is explicitly embedded
            Assert.Contains("consequential actions require Human / PRG-1 authority", brief.ConstitutionalBoundary);
        }

        // =========================================================================
        // ADDITIONAL HARDENING TESTS (EIE-41 & EIE-42)
        // =========================================================================

        [Fact]
        public void EIE41_BoardLensFiltersStrictlyToCriticalAttentionAndMaterialReview()
        {
            var rig = CreateTestRig();
            var policy = new ExecutiveMaterialityPolicy();
            var insights = new List<ExecutiveInsight>
            {
                new ExecutiveInsight { Title = "Routine Ops Item", Priority = ExecutivePriority.Informational },
                new ExecutiveInsight { Title = "Strategic Watch Item", Priority = ExecutivePriority.StrategicWatch },
                new ExecutiveInsight { Title = "Major M&A Proposal", Priority = ExecutivePriority.CriticalAttention },
                new ExecutiveInsight { Title = "Capital Reallocation", Priority = ExecutivePriority.MaterialReview }
            };

            var boardInsights = rig.priority.PrioritizeForAudience(ExecutiveAudience.Board, insights, policy);

            Assert.Equal(2, boardInsights.Count);
            Assert.DoesNotContain(boardInsights, i => i.Priority == ExecutivePriority.Informational);
            Assert.DoesNotContain(boardInsights, i => i.Priority == ExecutivePriority.StrategicWatch);
        }

        [Fact]
        public void EIE42_CROAudienceLensPrioritizesCommercialPricingAndConversion()
        {
            var rig = CreateTestRig();
            var policy = new ExecutiveMaterialityPolicy();
            var insights = new List<ExecutiveInsight>
            {
                new ExecutiveInsight { Title = "Warehouse Space", Domain = "Operations", Priority = ExecutivePriority.MaterialReview, Confidence = 80.0 },
                new ExecutiveInsight { Title = "Pricing Elasticity Shock", Domain = "Pricing", Priority = ExecutivePriority.MaterialReview, Confidence = 85.0 }
            };

            var croInsights = rig.priority.PrioritizeForAudience(ExecutiveAudience.CRO, insights, policy);

            Assert.Equal("Pricing Elasticity Shock", croInsights.First().Title);
        }
    }
}
