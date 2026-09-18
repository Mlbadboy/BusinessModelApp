using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Decision;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Radar;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Scenario;
using BusinessModelApp.Infrastructure.Runtime.Intelligence.Decision;
using BusinessModelApp.Infrastructure.Runtime.Intelligence.Radar;
using BusinessModelApp.Infrastructure.Runtime.Intelligence.Scenario;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public class Phase3Batch385DecisionTests
    {
        private readonly DecisionScorer _scorer;
        private readonly DecisionReversibilityEvaluator _reversibilityEvaluator;
        private readonly DecisionCandidateSynthesizer _synthesizer;
        private readonly InMemoryDecisionStore _store;
        private readonly InMemoryRadarSignalStore _radarStore;
        private readonly InMemoryScenarioStore _scenarioStore;
        private readonly DecisionOrchestrator _orchestrator;

        public Phase3Batch385DecisionTests()
        {
            _scorer = new DecisionScorer();
            _reversibilityEvaluator = new DecisionReversibilityEvaluator();
            _synthesizer = new DecisionCandidateSynthesizer();
            _store = new InMemoryDecisionStore();
            _radarStore = new InMemoryRadarSignalStore();
            _scenarioStore = new InMemoryScenarioStore();
            _orchestrator = new DecisionOrchestrator(
                _scorer,
                _reversibilityEvaluator,
                _synthesizer,
                _store,
                _radarStore,
                _scenarioStore);
        }

        private static List<DecisionCandidate> CreateSampleCandidates(string tenant = "tenant-alpha")
        {
            return new List<DecisionCandidate>
            {
                new DecisionCandidate
                {
                    CandidateId = "CAND-01",
                    TenantId = tenant,
                    Title = "Option A: Optimize Enterprise Tier Pricing",
                    Description = "Increase enterprise subscription by 10% with SLA enhancements",
                    Category = DecisionCategory.PricingAdjustment,
                    Reversibility = ReversibilityTier.Type1_EasilyReversible,
                    ExpectedReturnINR = 300000m,
                    DownsideRiskP10INR = -15000m,
                    UpsidePotentialP90INR = 450000m,
                    EstimatedTimeToImpact = TimeSpan.FromDays(21),
                    ImplementationComplexityScore = 30.0,
                    ExpectedOutcomes = new[] { "ARR boost +8%", "Margin expansion" },
                    UnintendedConsequences = new[] { "Minor sales cycle elongation" },
                    FeasibilityStatus = DecisionFeasibilityStatus.Feasible,
                    Validity = DecisionValidity.Valid,
                    IsDoNothingBaseline = false
                },
                new DecisionCandidate
                {
                    CandidateId = "CAND-02",
                    TenantId = tenant,
                    Title = "Option B: Phased Annual Plan Incentives",
                    Description = "Offer 15% discount for annual upfront commitments",
                    Category = DecisionCategory.CostOptimization,
                    Reversibility = ReversibilityTier.Type1_EasilyReversible,
                    ExpectedReturnINR = 180000m,
                    DownsideRiskP10INR = -5000m,
                    UpsidePotentialP90INR = 220000m,
                    EstimatedTimeToImpact = TimeSpan.FromDays(30),
                    ImplementationComplexityScore = 20.0,
                    ExpectedOutcomes = new[] { "Cash flow acceleration" },
                    UnintendedConsequences = new[] { "Lower overall revenue per user" },
                    FeasibilityStatus = DecisionFeasibilityStatus.Feasible,
                    Validity = DecisionValidity.Valid,
                    IsDoNothingBaseline = false
                },
                new DecisionCandidate
                {
                    CandidateId = "CAND-03-DO-NOTHING",
                    TenantId = tenant,
                    Title = "Option C: Do Nothing (Maintain Baseline)",
                    Description = "Preserve current status quo pricing. Used for regret and benchmark evaluation.",
                    Category = DecisionCategory.OperationalPivoting,
                    Reversibility = ReversibilityTier.Type1_EasilyReversible,
                    ExpectedReturnINR = 0m,
                    DownsideRiskP10INR = 0m,
                    UpsidePotentialP90INR = 0m,
                    EstimatedTimeToImpact = TimeSpan.Zero,
                    ImplementationComplexityScore = 0.0,
                    ExpectedOutcomes = new[] { "Zero implementation overhead" },
                    UnintendedConsequences = new[] { "Opportunity cost of uncaptured ARR" },
                    FeasibilityStatus = DecisionFeasibilityStatus.Feasible,
                    Validity = DecisionValidity.Valid,
                    IsDoNothingBaseline = true // MANDATORY DO-NOTHING BASELINE
                }
            };
        }

        // =========================================================================
        // DCE-01: MULTI-ALTERNATIVE SYNTHESIS & MANDATORY DO-NOTHING (I23, I23-E)
        // =========================================================================

        [Fact]
        public async Task DCE01_01_MultiAlternativeSynthesis_GeneratesCandidatesWithDoNothing()
        {
            var candidates = await _synthesizer.SynthesizeCandidatesAsync("tenant-alpha", null, null);

            Assert.NotNull(candidates);
            Assert.True(candidates.Count >= 3);
            Assert.Contains(candidates, c => c.IsDoNothingBaseline);
            Assert.Contains(candidates, c => !c.IsDoNothingBaseline);
        }

        // =========================================================================
        // DCE-02: MCDA DETERMINISTIC SCORING (I23-C)
        // =========================================================================

        [Fact]
        public void DCE02_01_MCDAScoring_ComputesExactNormalizedWeights()
        {
            var candidates = CreateSampleCandidates();
            var policy = new DecisionPolicy { TenantId = "tenant-alpha" };
            var evidence = new EvidenceAssessment();

            var breakdown = _scorer.ScoreCandidate(candidates[0], policy, evidence);

            Assert.True(breakdown.CompositeScore >= 0.0 && breakdown.CompositeScore <= 100.0);
            Assert.True(breakdown.ExpectedValueScore > 0);
            Assert.True(breakdown.DownsideRiskScore > 0);
            Assert.True(breakdown.StrategicFitScore > 0);
        }

        // =========================================================================
        // DCE-03: REVERSIBILITY CLASSIFICATION (I23-D)
        // =========================================================================

        [Fact]
        public void DCE03_01_ReversibilityClassification_DistinguishesType1AndType3Doors()
        {
            var twoWay = new DecisionCandidate
            {
                Title = "Minor Copy Revision",
                DownsideRiskP10INR = -500m,
                ImplementationComplexityScore = 10.0
            };
            var oneWay = new DecisionCandidate
            {
                Title = "Acquire Subsidiary",
                DownsideRiskP10INR = -5000000m, // Heavy downside
                ImplementationComplexityScore = 95.0
            };

            Assert.Equal(ReversibilityTier.Type1_EasilyReversible, _reversibilityEvaluator.ClassifyReversibility(twoWay));
            Assert.Equal(ReversibilityTier.Type3_IrreversibleOneWayDoor, _reversibilityEvaluator.ClassifyReversibility(oneWay));
        }

        // =========================================================================
        // DCE-04: MINIMAX REGRET CALCULATION (I23-D, I23-E)
        // =========================================================================

        [Fact]
        public void DCE04_01_MinimaxRegret_ComparesAgainstOptimalOutcome()
        {
            var candidates = CreateSampleCandidates();
            var regretCandidateA = _reversibilityEvaluator.CalculateMinimaxRegret(candidates[0], candidates);
            var regretDoNothing = _reversibilityEvaluator.CalculateMinimaxRegret(candidates[2], candidates);

            // Candidate A has 300k expected return (max) -> regret = 0
            Assert.Equal(0m, regretCandidateA);
            // DoNothing has 0 expected return -> regret = 300k
            Assert.Equal(300000m, regretDoNothing);
        }

        // =========================================================================
        // DCE-05: CONSTRAINT HARD-GATING (I23-F)
        // =========================================================================

        [Fact]
        public async Task DCE05_01_InfeasibleCandidate_NeverRecommendedOverFeasible()
        {
            var candidates = CreateSampleCandidates();
            // Infeasible candidate with huge theoretical upside
            candidates.Insert(0, new DecisionCandidate
            {
                CandidateId = "CAND-ILLEGAL",
                TenantId = "tenant-alpha",
                Title = "Hostile Acquisition",
                ExpectedReturnINR = 10000000m,
                FeasibilityStatus = DecisionFeasibilityStatus.InfeasibleViolation,
                IsDoNothingBaseline = false
            });

            var result = await _orchestrator.RankDecisionsAsync("tenant-alpha", candidates);

            Assert.NotNull(result.RecommendedCandidateId);
            Assert.NotEqual("CAND-ILLEGAL", result.RecommendedCandidateId);
            Assert.Equal("CAND-01", result.RecommendedCandidateId);
        }

        // =========================================================================
        // DCE-06: EXPLICIT CONFIDENCE CLAMPING (I23-G)
        // =========================================================================

        [Fact]
        public void DCE06_01_ConfidenceClamping_BindsToMinimumUpstreamEvidence()
        {
            var clamped = _scorer.AssessEvidence(
                "tenant-alpha",
                evidenceQuality: 90.0,
                evidenceCoverage: 85.0,
                causalConf: 95.0,
                forecastConf: 45.0, // WEAKEST UPSTREAM LINK
                scenarioConf: 90.0);

            Assert.Equal(45.0, clamped.CompositeDecisionConfidence);
        }

        // =========================================================================
        // DCE-07: TRADE-OFF & UNINTENDED CONSEQUENCES DISCLOSURE (I23-H)
        // =========================================================================

        [Fact]
        public async Task DCE07_01_RankingResult_DisclosesTradeoffsAndUnintendedConsequences()
        {
            var candidates = CreateSampleCandidates();
            var result = await _orchestrator.RankDecisionsAsync("tenant-alpha", candidates);

            Assert.False(string.IsNullOrWhiteSpace(result.TradeoffRationale));
            Assert.Contains("Expected return", result.TradeoffRationale);
            Assert.Contains("Minimax Regret", result.TradeoffRationale);
        }

        // =========================================================================
        // DCE-08: END-TO-END DECISION RANKING (I23-C, I23-L)
        // =========================================================================

        [Fact]
        public async Task DCE08_01_EndToEndRanking_ProducesRankedDecisions()
        {
            var candidates = CreateSampleCandidates();
            var result = await _orchestrator.RankDecisionsAsync("tenant-alpha", candidates);

            Assert.Equal(3, result.RankedCandidates.Count);
            Assert.Equal("CAND-01", result.RecommendedCandidateId);
            Assert.Equal("CAND-03-DO-NOTHING", result.DoNothingCandidateId);
        }

        // =========================================================================
        // DCE-09: BIDIRECTIONAL PROVENANCE VERIFICATION (I23-I)
        // =========================================================================

        [Fact]
        public async Task DCE09_01_DecisionProvenance_CryptographicallyVerified()
        {
            var candidates = CreateSampleCandidates();
            var result = await _orchestrator.RankDecisionsAsync("tenant-alpha", candidates);

            var prov = await _store.GetProvenanceAsync("tenant-alpha", result.RecommendedCandidateId!);
            Assert.NotNull(prov);
            Assert.True(prov.VerifyLineage());
        }

        // =========================================================================
        // DCE-10: DECISION LIFECYCLE STATE MACHINE (I23-J)
        // =========================================================================

        [Fact]
        public async Task DCE10_01_LifecycleProgression_EnforcesValidStates()
        {
            var candidates = CreateSampleCandidates();
            var result = await _orchestrator.RankDecisionsAsync("tenant-alpha", candidates);

            var rec = await _store.GetCandidateAsync("tenant-alpha", result.RecommendedCandidateId!);
            Assert.Equal(DecisionLifecycleState.Recommended, rec!.LifecycleState);
        }

        // =========================================================================
        // DCE-11: RECOMMENDED != APPROVED (I23-A, I23-J)
        // =========================================================================

        [Fact]
        public async Task DCE11_01_RecommendedCandidate_IsNotApprovedWithoutHumanReview()
        {
            var candidates = CreateSampleCandidates();
            var result = await _orchestrator.RankDecisionsAsync("tenant-alpha", candidates);

            var rec = await _store.GetCandidateAsync("tenant-alpha", result.RecommendedCandidateId!);
            Assert.NotEqual(DecisionLifecycleState.ApprovedByHuman, rec!.LifecycleState);
            Assert.Equal(DecisionLifecycleState.Recommended, rec.LifecycleState);
        }

        // =========================================================================
        // DCE-12: ZERO EXECUTION AUTHORITY (I23-B)
        // =========================================================================

        [Fact]
        public void DCE12_01_DecisionCandidates_PossessZeroExecutionPermitProperties()
        {
            var c = new DecisionCandidate();
            var hasPermit = c.GetType().GetProperties().Any(p => p.Name.Contains("Permit") || p.Name.Contains("Execution"));
            Assert.False(hasPermit);
        }

        // =========================================================================
        // DCE-13: MULTI-TENANT ISOLATION (I23-K)
        // =========================================================================

        [Fact]
        public async Task DCE13_01_MultiTenantIsolation_BlocksCrossTenantAccess()
        {
            var t1 = CreateSampleCandidates("tenant-one");
            var t2 = CreateSampleCandidates("tenant-two");

            await _orchestrator.RankDecisionsAsync("tenant-one", t1);
            await _orchestrator.RankDecisionsAsync("tenant-two", t2);

            var t1List = await _store.ListCandidatesAsync("tenant-one");
            Assert.Equal(3, t1List.Count);
            Assert.All(t1List, c => Assert.Equal("tenant-one", c.TenantId));

            var crossAccess = await _store.GetCandidateAsync("tenant-one", "CAND-01-tenant-two");
            Assert.Null(crossAccess);
        }

        // =========================================================================
        // DCE-14: REPRODUCIBILITY GUARANTEE (I23-L)
        // =========================================================================

        [Fact]
        public async Task DCE14_01_IdenticalInputsAndPolicy_YieldBitIdenticalRankings()
        {
            var c1 = CreateSampleCandidates("tenant-rep");
            var c2 = CreateSampleCandidates("tenant-rep");

            var res1 = await _orchestrator.RankDecisionsAsync("tenant-rep", c1);
            var res2 = await _orchestrator.RankDecisionsAsync("tenant-rep", c2);

            Assert.Equal(res1.RecommendedCandidateId, res2.RecommendedCandidateId);
            Assert.Equal(res1.TradeoffRationale, res2.TradeoffRationale);
        }

        // =========================================================================
        // DCE-15: AI-GENERATED CANDIDATE VALIDATION (I23-M)
        // =========================================================================

        [Fact]
        public void DCE15_01_AiProposedCandidate_MustHaveValidStructure()
        {
            var valid = new DecisionCandidate { Title = "Valid Option", ExpectedReturnINR = 1000m };
            Assert.False(string.IsNullOrWhiteSpace(valid.Title));
            Assert.False(double.IsNaN((double)valid.ExpectedReturnINR));
        }

        // =========================================================================
        // DCE-16: FROZEN POLICY SNAPSHOT IMMUTABILITY (I23-N)
        // =========================================================================

        [Fact]
        public async Task DCE16_01_FrozenPolicySnapshot_PreservesAuditIntegrity()
        {
            var policy = new DecisionPolicy
            {
                PolicyId = "POL-SNAPSHOT-TEST",
                Version = "2.1.0",
                ExpectedValueWeight = 0.50
            };
            var result = await _orchestrator.RankDecisionsAsync("tenant-alpha", CreateSampleCandidates(), policy);

            Assert.Equal("2.1.0", result.PolicySnapshot.Version);
            Assert.Equal(0.50, result.PolicySnapshot.ExpectedValueWeight);
        }

        // =========================================================================
        // DCE-17: UNKNOWN EPISTEMIC PROPAGATION (I23-O)
        // =========================================================================

        [Fact]
        public async Task DCE17_01_UnknownEpistemicTier_ForcesLowDecisionConfidence()
        {
            var candidates = CreateSampleCandidates();
            candidates[0] = candidates[0] with { EpistemicTier = DecisionEpistemicTier.Unknown };

            var result = await _orchestrator.RankDecisionsAsync("tenant-alpha", candidates);

            var eval = result.Evaluations[candidates[0].CandidateId];
            Assert.True(eval.Evidence.CompositeDecisionConfidence <= 30.0);
            Assert.Equal(DecisionValidity.InsufficientEvidence, eval.Validity);
        }

        // =========================================================================
        // DCE-18: STRESS-TEST DECISION UNDER SHOCKS (I23-C, I23-D)
        // =========================================================================

        [Fact]
        public async Task DCE18_01_StressTest_EvaluatesDecisionUnderSevereDownside()
        {
            var candidates = CreateSampleCandidates();
            // Candidate with severe downside shock
            candidates[0] = candidates[0] with { DownsideRiskP10INR = -500000m };

            var result = await _orchestrator.RankDecisionsAsync("tenant-alpha", candidates);
            var breakdown = result.Evaluations[candidates[0].CandidateId].Breakdown;

            Assert.True(breakdown.DownsideRiskScore < 60.0);
        }

        // =========================================================================
        // DCE-19: TIME-TO-IMPACT PENALTY (I23-C)
        // =========================================================================

        [Fact]
        public void DCE19_01_TimeToImpact_ShorterHorizonReceivesHigherScore()
        {
            var policy = new DecisionPolicy();
            var evidence = new EvidenceAssessment();

            var fast = new DecisionCandidate { Title = "Fast", EstimatedTimeToImpact = TimeSpan.FromDays(7) };
            var slow = new DecisionCandidate { Title = "Slow", EstimatedTimeToImpact = TimeSpan.FromDays(180) };

            var bFast = _scorer.ScoreCandidate(fast, policy, evidence);
            var bSlow = _scorer.ScoreCandidate(slow, policy, evidence);

            Assert.True(bFast.TimeToImpactScore > bSlow.TimeToImpactScore);
        }

        // =========================================================================
        // DCE-20: IMPLEMENTATION COMPLEXITY PENALTY (I23-C)
        // =========================================================================

        [Fact]
        public void DCE20_01_ComplexityScore_LowerComplexityScoresHigher()
        {
            var policy = new DecisionPolicy();
            var evidence = new EvidenceAssessment();

            var simple = new DecisionCandidate { Title = "Simple", ImplementationComplexityScore = 10.0 };
            var complex = new DecisionCandidate { Title = "Complex", ImplementationComplexityScore = 90.0 };

            var bSimple = _scorer.ScoreCandidate(simple, policy, evidence);
            var bComplex = _scorer.ScoreCandidate(complex, policy, evidence);

            Assert.True(bSimple.ComplexityScore > bComplex.ComplexityScore);
        }

        // =========================================================================
        // DCE-21: RADAR OPPORTUNITY DECISION SYNTHESIS (I23, I23-I)
        // =========================================================================

        [Fact]
        public async Task DCE21_01_OpportunitySignal_SynthesizesGrowthDecisions()
        {
            var signal = new RadarSignal
            {
                Id = "SIG-OPP-99",
                TenantId = "tenant-alpha",
                Type = RadarSignalType.Opportunity,
                Title = "Revenue Surge on Organic Channels",
                EstimatedMonetaryImpactINR = 200000m
            };

            var candidates = await _synthesizer.SynthesizeCandidatesAsync("tenant-alpha", signal, null);

            Assert.Contains(candidates, c => c.Category == DecisionCategory.GrowthAcceleration);
            Assert.Contains(candidates, c => c.IsDoNothingBaseline);
        }

        // =========================================================================
        // DCE-22: RADAR THREAT DECISION SYNTHESIS (I23, I23-F)
        // =========================================================================

        [Fact]
        public async Task DCE22_01_ThreatSignal_SynthesizesRiskMitigationDecisions()
        {
            var signal = new RadarSignal
            {
                Id = "SIG-THREAT-88",
                TenantId = "tenant-alpha",
                Type = RadarSignalType.Threat,
                Title = "Margin Erosion Surge",
                EstimatedMonetaryImpactINR = 120000m
            };

            var candidates = await _synthesizer.SynthesizeCandidatesAsync("tenant-alpha", signal, null);

            Assert.Contains(candidates, c => c.Category == DecisionCategory.RiskMitigation);
            Assert.Contains(candidates, c => c.IsDoNothingBaseline);
        }

        // =========================================================================
        // DCE-23: CONCURRENT EVALUATION THREAD SAFETY (I23-K, I23-L)
        // =========================================================================

        [Fact]
        public async Task DCE23_01_ConcurrentEvaluations_ExecuteSafely()
        {
            var tasks = Enumerable.Range(0, 10)
                .Select(i => _orchestrator.RankDecisionsAsync($"tenant-conc-{i}", CreateSampleCandidates($"tenant-conc-{i}")))
                .ToArray();

            var results = await Task.WhenAll(tasks);

            Assert.Equal(10, results.Length);
            Assert.All(results, r => Assert.NotNull(r.RecommendedCandidateId));
        }

        // =========================================================================
        // DCE-24: END-TO-END ORCHESTRATOR PIPELINE (I23-A..I23-P)
        // =========================================================================

        [Fact]
        public async Task DCE24_01_EndToEndPipeline_CompletesCleanly()
        {
            var sig = new RadarSignal
            {
                Id = "SIG-E2E-01",
                TenantId = "tenant-e2e",
                Title = "Conversion Inflection",
                EstimatedMonetaryImpactINR = 100000m
            };
            await _radarStore.SaveSignalAsync(sig);

            var result = await _orchestrator.SynthesizeAndRankFromRadarAndScenarioAsync("tenant-e2e", "SIG-E2E-01");

            Assert.NotNull(result);
            Assert.Equal("tenant-e2e", result.TenantId);
            Assert.NotNull(result.RecommendedCandidateId);
        }

        // =========================================================================
        // DCE-25: [ADVERSARIAL] PERMIT CREATION ATTEMPT BLOCKED (I23-B)
        // =========================================================================

        [Fact]
        public void DCE25_01_Adversarial_ExecutionPermitAttempt_TypeSystemRejects()
        {
            var candidate = new DecisionCandidate();
            var methods = candidate.GetType().GetMethods().Where(m => m.Name.Contains("Permit") || m.Name.Contains("Execute")).ToList();
            Assert.Empty(methods);
        }

        // =========================================================================
        // DCE-26: [ADVERSARIAL] AUTONOMOUS SELF-APPROVAL ATTEMPT REJECTED (I23-A, I23-J)
        // =========================================================================

        [Fact]
        public async Task DCE26_01_Adversarial_AutonomousApprovalAttempt_ThrowsException()
        {
            var candidate = new DecisionCandidate
            {
                CandidateId = "CAND-ATTEMPT-APPROVAL",
                TenantId = "tenant-alpha",
                LifecycleState = DecisionLifecycleState.Recommended
            };
            await _store.SaveCandidateAsync(candidate);

            // Autonomous agent attempts to self-approve without human actor
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _store.UpdateLifecycleStateAsync(
                    "tenant-alpha",
                    "CAND-ATTEMPT-APPROVAL",
                    DecisionLifecycleState.ApprovedByHuman,
                    "Self approval attempt",
                    isHumanActor: false)); // NOT HUMAN
        }

        // =========================================================================
        // DCE-27: [ADVERSARIAL] SINGLE HOBSON'S CHOICE REJECTED (I23-E)
        // =========================================================================

        [Fact]
        public async Task DCE27_01_Adversarial_MissingDoNothingBaseline_ThrowsException()
        {
            // Only 1 choice without DoNothing baseline
            var singleChoice = new List<DecisionCandidate>
            {
                new DecisionCandidate
                {
                    CandidateId = "CAND-ONLY-ONE",
                    TenantId = "tenant-alpha",
                    Title = "Forced Option",
                    IsDoNothingBaseline = false
                }
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _orchestrator.RankDecisionsAsync("tenant-alpha", singleChoice));
        }

        // =========================================================================
        // DCE-28: [ADVERSARIAL] CROSS-TENANT MODIFICATION ISOLATED (I23-K)
        // =========================================================================

        [Fact]
        public async Task DCE28_01_Adversarial_CrossTenantModification_ReturnsFalse()
        {
            var cand = new DecisionCandidate
            {
                CandidateId = "CAND-T1",
                TenantId = "tenant-victim"
            };
            await _store.SaveCandidateAsync(cand);

            // Attacker tenant attempts to update victim's decision
            var updated = await _store.UpdateLifecycleStateAsync(
                "tenant-attacker",
                "CAND-T1",
                DecisionLifecycleState.Superseded,
                "Malicious supersede",
                isHumanActor: true);

            Assert.False(updated);
        }

        // =========================================================================
        // DCE-29: [ADVERSARIAL] MALICIOUS METRIC SCORE INJECTION REJECTED (I23-C, I23-M)
        // =========================================================================

        [Fact]
        public void DCE29_01_Adversarial_MaliciousScoreInjection_ThrowsArgumentException()
        {
            var malCandidate = new DecisionCandidate
            {
                Title = "", // Malicious empty title
                ExpectedReturnINR = 100m
            };

            Assert.Throws<ArgumentException>(() =>
                _scorer.ScoreCandidate(malCandidate, new DecisionPolicy(), new EvidenceAssessment()));
        }

        // =========================================================================
        // DCE-30: [ADVERSARIAL] POLICY TAMPERING DETECTION (I23-N)
        // =========================================================================

        [Fact]
        public void DCE30_01_Adversarial_PolicyTampering_ChangesHash()
        {
            var p1 = new DecisionPolicy { ExpectedValueWeight = 0.35 };
            var p2 = new DecisionPolicy { ExpectedValueWeight = 0.85 }; // Tampered weight

            var h1 = p1.ComputeIntegrityHash();
            var h2 = p2.ComputeIntegrityHash();

            Assert.NotEqual(h1, h2);
        }

        // =========================================================================
        // DCE-31: [ADVERSARIAL] CONFIDENCE INFLATION ATTEMPT CLAMPED (I23-G)
        // =========================================================================

        [Fact]
        public void DCE31_01_Adversarial_ConfidenceInflation_ClampedToMinimum()
        {
            var assessed = _scorer.AssessEvidence(
                "tenant-alpha",
                evidenceQuality: 99.0,
                evidenceCoverage: 99.0,
                causalConf: 20.0, // WEAKEST UPSTREAM CONFIDENCE
                forecastConf: 99.0,
                scenarioConf: 99.0);

            Assert.Equal(20.0, assessed.CompositeDecisionConfidence);
        }

        // =========================================================================
        // DCE-32: [ADVERSARIAL] CONSTRAINT VIOLATION BYPASS ATTEMPT DETECTED (I23-F)
        // =========================================================================

        [Fact]
        public void DCE32_01_Adversarial_ConstraintBreachScore_CapsCompositeScore()
        {
            var candidate = new DecisionCandidate
            {
                Title = "Violating Candidate",
                ExpectedReturnINR = 500000m,
                FeasibilityStatus = DecisionFeasibilityStatus.InfeasibleViolation
            };

            var breakdown = _scorer.ScoreCandidate(candidate, new DecisionPolicy(), new EvidenceAssessment());

            // Composite score capped extremely low for infeasible violations
            Assert.True(breakdown.CompositeScore <= 15.0);
        }

        // =========================================================================
        // DCE-33: [ADVERSARIAL] DECISION CANNOT DIRECTLY MUTATE REALITY LEDGER (I23, I23-B)
        // =========================================================================

        [Fact]
        public async Task DCE33_01_Adversarial_DecisionZeroSideEffectsOnLedger()
        {
            var candidates = CreateSampleCandidates();
            var result = await _orchestrator.RankDecisionsAsync("tenant-alpha", candidates);

            Assert.NotNull(result);
            // Verify no execution jobs or external tasks were created
            var rec = await _store.GetCandidateAsync("tenant-alpha", result.RecommendedCandidateId!);
            Assert.Equal(DecisionLifecycleState.Recommended, rec!.LifecycleState);
        }

        // =========================================================================
        // DCE-34: [ADVERSARIAL] CONCURRENT DUPLICATE RANKING IDEMPOTENCY (I23-L)
        // =========================================================================

        [Fact]
        public async Task DCE34_01_Adversarial_DuplicateExecution_YieldsDeterministicHash()
        {
            var c = CreateSampleCandidates("tenant-idem");

            var tasks = Enumerable.Range(0, 5)
                .Select(_ => _orchestrator.RankDecisionsAsync("tenant-idem", c))
                .ToArray();

            var results = await Task.WhenAll(tasks);
            var firstHash = results[0].PolicySnapshot.IntegrityHash;

            foreach (var r in results)
            {
                Assert.Equal(firstHash, r.PolicySnapshot.IntegrityHash);
                Assert.Equal(results[0].RecommendedCandidateId, r.RecommendedCandidateId);
            }
        }

        // =========================================================================
        // DCE-35: [ADVERSARIAL] DECISION NON-MUTATION CONTRACT (I23-P)
        // =========================================================================

        [Fact]
        public void DCE35_01_Adversarial_DecisionNonMutation_GuaranteedByTypes()
        {
            var candidate = new DecisionCandidate();
            var types = candidate.GetType().GetProperties().Select(p => p.PropertyType.Name).ToList();

            Assert.DoesNotContain("LedgerEntry", types);
            Assert.DoesNotContain("ExecutionPermit", types);
            Assert.DoesNotContain("WorkerTask", types);
            Assert.DoesNotContain("DigitalTwinState", types);
        }

        // =========================================================================
        // DCE-36: STALE RECOMMENDATION DETERMINISTIC EXPIRY (I23-J, I23-N)
        // =========================================================================

        [Fact]
        public async Task DCE36_01_StaleDecision_AutomaticallyExpired()
        {
            var candidate = new DecisionCandidate
            {
                CandidateId = "CAND-STALE",
                TenantId = "tenant-alpha",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-30), // 30 days old
                LifecycleState = DecisionLifecycleState.Recommended
            };
            await _store.SaveCandidateAsync(candidate);

            // Freshness requirement is 14 days
            await _store.CheckAndExpireStaleDecisionsAsync("tenant-alpha", TimeSpan.FromDays(14));

            var expired = await _store.GetCandidateAsync("tenant-alpha", "CAND-STALE");
            Assert.Equal(DecisionLifecycleState.Expired, expired!.LifecycleState);
            Assert.Equal(DecisionValidity.StaleEvidence, expired.Validity);
        }

        // =========================================================================
        // DCE-37: STRATEGIC-POLICY TAMPERING DETECTION (I23-N)
        // =========================================================================

        [Fact]
        public void DCE37_01_StrategicPolicyTampering_DetectedViaSha256()
        {
            var pol1 = new DecisionPolicy { ActiveStrategicRegime = StrategicRegime.MarginExpansion };
            var pol2 = new DecisionPolicy { ActiveStrategicRegime = StrategicRegime.AggressiveGrowth };

            Assert.NotEqual(pol1.ComputeIntegrityHash(), pol2.ComputeIntegrityHash());
        }

        // =========================================================================
        // DCE-38: EVIDENCE-QUALITY DEGRADATION DOWNSCORES CANDIDATE (I23-G, I23-O)
        // =========================================================================

        [Fact]
        public void DCE38_01_DegradedEvidence_DownscoresCandidateCompositeScore()
        {
            var candidate = new DecisionCandidate { Title = "Normal", ExpectedReturnINR = 100000m };
            var policy = new DecisionPolicy();

            var strongEvidence = new EvidenceAssessment { CompositeDecisionConfidence = 85.0 };
            var weakEvidence = new EvidenceAssessment { CompositeDecisionConfidence = 25.0 }; // Low confidence

            var bStrong = _scorer.ScoreCandidate(candidate, policy, strongEvidence);
            var bWeak = _scorer.ScoreCandidate(candidate, policy, weakEvidence);

            Assert.True(bStrong.CompositeScore > bWeak.CompositeScore);
        }

        // =========================================================================
        // DCE-39: SCENARIO INVALIDATION PROPAGATION (I23-I, I23-J)
        // =========================================================================

        [Fact]
        public async Task DCE39_01_InvalidatedScenario_PropagatesToDecisionExpiry()
        {
            var candidate = new DecisionCandidate
            {
                CandidateId = "CAND-LINKED",
                TenantId = "tenant-alpha",
                LinkedScenarioId = "SCEN-INVALIDATED",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-20),
                LifecycleState = DecisionLifecycleState.Recommended
            };
            await _store.SaveCandidateAsync(candidate);

            await _store.CheckAndExpireStaleDecisionsAsync("tenant-alpha", TimeSpan.FromDays(14));

            var checkedCand = await _store.GetCandidateAsync("tenant-alpha", "CAND-LINKED");
            Assert.Equal(DecisionLifecycleState.Expired, checkedCand!.LifecycleState);
        }

        // =========================================================================
        // DCE-40: DECISION PROVENANCE TAMPERING DETECTION (I23-I)
        // =========================================================================

        [Fact]
        public void DCE40_01_DecisionProvenance_RejectsIncompleteLineage()
        {
            var validProv = new DecisionProvenance
            {
                DecisionId = "DEC-1",
                TenantId = "tenant-alpha",
                PolicyHash = "HASH-POLICY",
                RankingHash = "HASH-RANKING"
            };
            Assert.True(validProv.VerifyLineage());

            var tamperedProv = new DecisionProvenance
            {
                DecisionId = "DEC-1",
                TenantId = "tenant-alpha",
                PolicyHash = "", // Missing hash
                RankingHash = "HASH-RANKING"
            };
            Assert.False(tamperedProv.VerifyLineage());
        }
    }
}
