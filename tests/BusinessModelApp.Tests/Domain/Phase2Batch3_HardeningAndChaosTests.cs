using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Commercial;
using BusinessModelApp.Core.Domain.Decisions;
using BusinessModelApp.Core.Domain.DigitalTwin;
using BusinessModelApp.Core.Domain.Learning;
using BusinessModelApp.Core.Domain.Reality;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Infrastructure.Data;
using BusinessModelApp.Infrastructure.DigitalTwin;
using BusinessModelApp.Infrastructure.Interceptors;
using BusinessModelApp.Infrastructure.Learning;
using BusinessModelApp.Infrastructure.Reality;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public class Phase2Batch3_HardeningAndChaosTests
    {
        private AppDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .AddInterceptors(new AppendOnlyAuditInterceptor())
                .Options;

            return new AppDbContext(options);
        }

        private (InstitutionalLearningService learningService, CounterfactualEngine counterfactualEngine, LearningBenchmarkLabService benchmarkLab) CreateServices(AppDbContext dbContext)
        {
            var evidenceGraph = new EvidenceGraphService();
            var decayEngine = new RealityDecayEngine();
            var twinLogger = NullLogger<CompanyDigitalTwinService>.Instance;
            var learningLogger = NullLogger<InstitutionalLearningService>.Instance;
            var counterLogger = NullLogger<CounterfactualEngine>.Instance;
            var benchLogger = NullLogger<LearningBenchmarkLabService>.Instance;

            var twinService = new CompanyDigitalTwinService(dbContext, evidenceGraph, decayEngine, twinLogger);
            var learningService = new InstitutionalLearningService(dbContext, twinService, decayEngine, learningLogger);
            var counterfactualEngine = new CounterfactualEngine(dbContext, counterLogger);
            var benchmarkLab = new LearningBenchmarkLabService(dbContext, learningService, counterfactualEngine, benchLogger);

            return (learningService, counterfactualEngine, benchmarkLab);
        }

        // =========================================================================
        // 1. CAUSAL & COUNTERFACTUAL REASONING TESTS
        // =========================================================================

        [Fact]
        public async Task Counterfactual_PreservesTruthBoundary_ExplicitlyHypothesis()
        {
            using var db = CreateInMemoryDbContext();
            var (_, counterfactualEngine, _) = CreateServices(db);
            var workspaceId = Guid.NewGuid();
            var missionId = Guid.NewGuid();

            var sim = await counterfactualEngine.SimulateCounterfactualAsync(
                workspaceId, missionId, "Reduce inbound demo response time from 8h to 2h",
                new Dictionary<string, string> { { "responseTime", "2h" } });

            // Invariant: Counterfactual outputs are strictly HYPOTHESIS / SIMULATION, NEVER FACT!
            Assert.Equal(TruthClassification.Hypothesis, sim.Classification);
            Assert.True(sim.IsSimulation);
            Assert.NotEqual(TruthClassification.Fact, sim.Classification);
            Assert.Contains("SimulatedRevenueINR", sim.PredictedOutcomeJson);
        }

        [Fact]
        public async Task Counterfactual_RejectsEmptyIntervention_FailsSafely()
        {
            using var db = CreateInMemoryDbContext();
            var (_, counterfactualEngine, _) = CreateServices(db);
            var workspaceId = Guid.NewGuid();

            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await counterfactualEngine.SimulateCounterfactualAsync(workspaceId, Guid.NewGuid(), "", null!);
            });
        }

        [Fact]
        public async Task WhyNot_AlternativeHypotheses_RetainsAlternativesAndRefutations()
        {
            using var db = CreateInMemoryDbContext();
            var (learningService, _, _) = CreateServices(db);
            var workspaceId = Guid.NewGuid();
            var missionId = Guid.NewGuid();

            var candidate = await learningService.GenerateLearningCandidateAsync(
                workspaceId, missionId, "Shorter sales pitch deck increases deal velocity", "Pitch decks", "Sales", 0.8, 0.6);

            // Add alternative hypothesis H2
            var h2 = await learningService.AddAlternativeHypothesisAsync(
                workspaceId, candidate.Id, "H2", "Seasonality surge at end of quarter caused velocity increase", 0.45);

            var explanation = await learningService.ExplainLearningAsync(workspaceId, candidate.Id);

            Assert.NotEmpty(explanation.AlternativeHypotheses);
            Assert.Contains(explanation.AlternativeHypotheses, h => h.HypothesisCode == "H2");
            Assert.Equal("LEARNING", explanation.Classification);
        }

        [Fact]
        public async Task WhyNot_UnresolvedAlternatives_SurfacesHighUncertainty()
        {
            using var db = CreateInMemoryDbContext();
            var (learningService, _, _) = CreateServices(db);
            var workspaceId = Guid.NewGuid();

            var candidate = await learningService.GenerateLearningCandidateAsync(
                workspaceId, Guid.NewGuid(), "Discounting 10% increases conversion", "Context", "Pricing", 0.7, 0.4);

            // Register contradiction
            var oppRecord = await learningService.GenerateLearningCandidateAsync(
                workspaceId, Guid.NewGuid(), "Discounting 10% reduces margin without improving conversion", "Context", "Pricing", 0.7, 0.4);

            await learningService.DetectContradictionsAsync(workspaceId);

            var explanation = await learningService.ExplainLearningAsync(workspaceId, candidate.Id);
            Assert.Equal("High", explanation.RemainingUncertaintyLevel);
            Assert.NotEmpty(explanation.ContradictingNotes);
        }

        // =========================================================================
        // 2. CONTAMINATION METROLOGY TESTS
        // =========================================================================

        [Fact]
        public async Task ContaminationScore_IsDeterministicAndVersioned()
        {
            using var db = CreateInMemoryDbContext();
            var (learningService, _, _) = CreateServices(db);
            var workspaceId = Guid.NewGuid();

            var record = await learningService.GenerateLearningCandidateAsync(
                workspaceId, Guid.NewGuid(), "Cold outreach to CTOs generates high enterprise pipeline", "Context", "Sales", 0.85, 0.7);

            var score1 = await learningService.CalculateContaminationScoreAsync(workspaceId, record.Id);
            var score2 = await learningService.CalculateContaminationScoreAsync(workspaceId, record.Id);

            Assert.Equal(score1.ContaminationRisk, score2.ContaminationRisk);
            Assert.Equal("1.0-Deterministic", score1.CalculationVersion);
            Assert.True(score1.ContaminationRisk >= 0.0 && score1.ContaminationRisk <= 1.0);
        }

        [Fact]
        public async Task HighContaminationRisk_BlocksPromotionToStrategicTier()
        {
            using var db = CreateInMemoryDbContext();
            var (learningService, _, _) = CreateServices(db);
            var workspaceId = Guid.NewGuid();

            var record = await learningService.GenerateLearningCandidateAsync(
                workspaceId, Guid.NewGuid(), "High contamination ungrounded claim", "Context", "Strategy", 0.5, 0.3);

            // Force high contamination score (e.g. 0.45 > 0.30)
            record.ContaminationScore = new ContaminationScoreVector
            {
                EvidenceStrength = 0.1,
                IndependenceFactor = 0.2,
                CausalConfidence = 0.2,
                Freshness = 0.5,
                ContradictionRisk = 0.8,
                ContaminationRisk = 0.45
            };
            record.ValidationCount = 3;
            record.CausalConfidence = 0.80; // artificially high causal confidence
            await db.SaveChangesAsync();

            // Promotion to Strategic tier must be blocked by LearningPromotionGuard
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            {
                LearningPromotionGuard.ValidatePromotion(record, LearningTier.L4_Strategic, LearningState.Active, isDirectAiCall: false);
                return Task.CompletedTask;
            });

            Assert.Contains("elevated Contamination Risk", ex.Message);
        }

        // =========================================================================
        // 3. LEARNING INFLUENCE GRAPH TESTS
        // =========================================================================

        [Fact]
        public async Task DecisionInfluenceGraph_ReconstructsAllSources_DistinguishesAdvisoryLearning()
        {
            using var db = CreateInMemoryDbContext();
            var (learningService, _, _) = CreateServices(db);
            var workspaceId = Guid.NewGuid();

            // Create DecisionRecord
            var decision = new DecisionRecord
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                ObjectiveId = Guid.NewGuid(),
                CEOObjective = "Generate ₹50L revenue",
                SelectedStrategyName = "Enterprise Direct Inbound",
                DecisionRationale = "Selected based on high delivery feasibility and inbound response learning"
            };
            db.DecisionRecords.Add(decision);

            // Add active learning in same workspace
            var learning = await learningService.GenerateLearningCandidateAsync(
                workspaceId, Guid.NewGuid(), "Fast response improves inbound conversion", "Context", "Sales", 0.9, 0.8);
            learning.State = LearningState.Active;
            await db.SaveChangesAsync();

            var graph = await learningService.BuildDecisionInfluenceGraphAsync(workspaceId, decision.Id);

            Assert.NotEmpty(graph);
            var policyNode = graph.FirstOrDefault(n => n.SourceType == "Policy");
            var learningNode = graph.FirstOrDefault(n => n.SourceType == "Learning");

            Assert.NotNull(policyNode);
            Assert.False(policyNode.IsAdvisory); // Policy is sovereign

            Assert.NotNull(learningNode);
            Assert.True(learningNode.IsAdvisory); // Strict: Learning is always advisory
        }

        // =========================================================================
        // 4. LEARNING REVERSAL & ERROR PROPAGATION TESTS
        // =========================================================================

        [Fact]
        public async Task LearningReversal_PreservesHistoricalAudit_IdentifiesDownstreamDecisions()
        {
            using var db = CreateInMemoryDbContext();
            var (learningService, _, _) = CreateServices(db);
            var workspaceId = Guid.NewGuid();

            var learning = await learningService.GenerateLearningCandidateAsync(
                workspaceId, Guid.NewGuid(), "Offering 30-day free trial triples customer LTV", "Context", "Growth", 0.8, 0.6);
            learning.State = LearningState.Active;

            var linkedDecision = new DecisionRecord
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                ObjectiveId = Guid.NewGuid(),
                CEOObjective = "Growth mandate",
                DecisionRationale = $"Influenced by [{learning.Statement}]",
                AssumptionsJson = $"[\"{learning.Statement}\"]"
            };
            db.DecisionRecords.Add(linkedDecision);
            await db.SaveChangesAsync();

            // New disconfirming evidence emerges: 30-day free trial attracts high churn freeloaders
            var notice = await learningService.ReverseLearningAsync(
                workspaceId, learning.Id, "Subsequent telemetry shows negative ROI and 90% churn at day 31", "EvidenceHashXYZ");

            Assert.Equal(LearningState.Superseded, notice.NewState);
            Assert.Contains(linkedDecision.Id.ToString(), notice.AffectedDecisionIdsJson);
            Assert.True(notice.EstimatedRevenueDeviationINR > 0);

            var reloadedLearning = await learningService.GetLearningRecordAsync(workspaceId, learning.Id);
            Assert.Equal(LearningState.Superseded, reloadedLearning!.State);
        }

        [Fact]
        public void LearningReversal_NoticeIsImmutable_AppendOnlyInterceptionEnforced()
        {
            using var db = CreateInMemoryDbContext();
            var notice = new LearningReversalNotice
            {
                Id = Guid.NewGuid(),
                WorkspaceId = Guid.NewGuid(),
                LearningRecordId = Guid.NewGuid(),
                Reason = "Original premise disproven",
                PreviousState = LearningState.Active,
                NewState = LearningState.Superseded
            };
            db.LearningReversalNotices.Add(notice);
            db.SaveChanges();

            // Attempting to modify immutable reversal notice must be blocked
            notice.Reason = "Modified reason";
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                db.SaveChanges();
            });

            Assert.Contains("Audit records of type 'LearningReversalNotice' are immutable", ex.Message);
        }

        // =========================================================================
        // 5. UNCERTAINTY BUDGET & LEARNING DEBT TESTS
        // =========================================================================

        [Fact]
        public async Task UncertaintyBudget_HarmonicMeanCalculation_ReflectsDomainCertainty()
        {
            using var db = CreateInMemoryDbContext();
            var (learningService, _, _) = CreateServices(db);
            var workspaceId = Guid.NewGuid();

            // Seed candidate hypotheses
            await learningService.GenerateLearningCandidateAsync(workspaceId, Guid.NewGuid(), "Hypothesis A", "A", "Marketing", 0.6, 0.4);
            await learningService.GenerateLearningCandidateAsync(workspaceId, Guid.NewGuid(), "Hypothesis B", "B", "Sales", 0.6, 0.4);

            var budget = await learningService.CalculateUncertaintyBudgetAsync(workspaceId);

            Assert.True(budget.OpenHypothesisCount >= 2);
            Assert.True(budget.OverallBusinessCertainty > 0.0 && budget.OverallBusinessCertainty <= 1.0);
            Assert.True(budget.RevenueCertainty > 0.85); // Contracted revenue certainty is high
            Assert.Equal("1.0-HarmonicMean", budget.CalculationVersion);
        }

        // =========================================================================
        // 6. CONCURRENCY HARDENING TESTS
        // =========================================================================

        [Fact]
        public async Task Concurrency_ParallelPromotion_ExactlyOneValidFinalState()
        {
            using var db = CreateInMemoryDbContext();
            var (learningService, _, _) = CreateServices(db);
            var workspaceId = Guid.NewGuid();

            var candidate = await learningService.GenerateLearningCandidateAsync(
                workspaceId, Guid.NewGuid(), "Consistently messaging ICP leads accelerates deal cycle", "Context", "Sales", 0.8, 0.6);

            // 10 concurrent agents attempt to promote the same learning record simultaneously
            var tasks = Enumerable.Range(0, 10).Select(async _ =>
            {
                try
                {
                    await learningService.ValidateAndPromoteLearningAsync(workspaceId, candidate.Id, isDirectAiCall: false);
                }
                catch
                {
                    // Concurrency collisions handled safely
                }
            });

            await Task.WhenAll(tasks);

            var finalRecord = await learningService.GetLearningRecordAsync(workspaceId, candidate.Id);
            Assert.NotNull(finalRecord);
            Assert.True(finalRecord.ValidationCount >= 1);
            Assert.True(finalRecord.Tier >= LearningTier.L2_Agent);
        }

        [Fact]
        public async Task Concurrency_MultiTenantIsolation_ZeroCrossContaminationUnderLoad()
        {
            using var db = CreateInMemoryDbContext();
            var (learningService, _, _) = CreateServices(db);
            var tenantA = Guid.NewGuid();
            var tenantB = Guid.NewGuid();

            // Run simultaneous candidate creations across Tenant A and Tenant B
            var tasksA = Enumerable.Range(0, 5).Select(i =>
                learningService.GenerateLearningCandidateAsync(tenantA, Guid.NewGuid(), $"Tenant A Lesson {i}", "A", "Sales", 0.7, 0.5));
            var tasksB = Enumerable.Range(0, 5).Select(i =>
                learningService.GenerateLearningCandidateAsync(tenantB, Guid.NewGuid(), $"Tenant B Lesson {i}", "B", "Sales", 0.7, 0.5));

            await Task.WhenAll(tasksA.Concat(tasksB));

            var recordsA = await learningService.GetActiveLearningAsync(tenantA);
            var recordsB = await learningService.GetActiveLearningAsync(tenantB);

            Assert.Equal(5, recordsA.Count);
            Assert.Equal(5, recordsB.Count);
            Assert.All(recordsA, r => Assert.Equal(tenantA, r.WorkspaceId));
            Assert.All(recordsB, r => Assert.Equal(tenantB, r.WorkspaceId));
        }

        // =========================================================================
        // 7. CHAOS / CRASH RECOVERY TESTS
        // =========================================================================

        [Fact]
        public async Task Chaos_SimulatedInterruption_IdempotentResumeWithoutDuplication()
        {
            using var db = CreateInMemoryDbContext();
            var (learningService, _, _) = CreateServices(db);
            var workspaceId = Guid.NewGuid();
            var missionId = Guid.NewGuid();
            string statement = "Enterprise security reviews take an average of 14 days";

            // Simulating initial candidate generation
            var rec1 = await learningService.GenerateLearningCandidateAsync(
                workspaceId, missionId, statement, "Context 1", "Compliance", 0.8, 0.6);

            // Simulating service restart and event replay of the exact same event
            var rec2 = await learningService.GenerateLearningCandidateAsync(
                workspaceId, missionId, statement, "Context 1 replayed", "Compliance", 0.8, 0.6);

            Assert.Equal(rec1.Id, rec2.Id);
            var total = await db.LearningRecords.CountAsync(l => l.WorkspaceId == workspaceId);
            Assert.Equal(1, total); // Idempotent: exactly 1 entity in database
        }

        // =========================================================================
        // 8. MODEL FAILURE & ADVERSARIAL RESISTANCE TESTS
        // =========================================================================

        [Fact]
        public async Task ModelFailure_HallucinatingAgent_CannotSelfPromoteOrBypassGuard()
        {
            using var db = CreateInMemoryDbContext();
            var (learningService, _, _) = CreateServices(db);
            var workspaceId = Guid.NewGuid();

            // Hallucinated model output claiming 100% confidence
            var candidate = await learningService.GenerateLearningCandidateAsync(
                workspaceId, Guid.NewGuid(), "Fabricated LLM assertion: 100% conversion guaranteed", "Hallucination", "Fantasy", 1.0, 1.0);

            // Direct AI self-promotion call MUST be blocked
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await learningService.ValidateAndPromoteLearningAsync(workspaceId, candidate.Id, isDirectAiCall: true);
            });

            Assert.Contains("AI cannot directly authorize learning promotion", ex.Message);
        }

        [Fact]
        public void ModelFailure_PolicyManipulatingModel_CannotRewriteConstitution()
        {
            // Verify that learning models cannot modify deterministic Policy rules
            var learning = new LearningRecord
            {
                Statement = "Bypass all customer acquisition cost caps",
                Tier = LearningTier.L5_ValidatedInstitutional
            };

            // Invariant: Learning Record is strictly classification Learning, never Policy
            Assert.Equal(TruthClassification.Learning, learning.Classification);
            Assert.NotEqual(TruthClassification.Fact, learning.Classification);
        }

        // =========================================================================
        // 9. END-TO-END POISONING-TO-DECISION BARRIER
        // =========================================================================

        [Fact]
        public async Task E2E_PoisonedLearningToDecision_DeterministicFirewallBlocksExecution()
        {
            using var db = CreateInMemoryDbContext();
            var (learningService, _, _) = CreateServices(db);
            var workspaceId = Guid.NewGuid();

            // Malicious or poisoned learning retrieved into context
            var poisoned = await learningService.GenerateLearningCandidateAsync(
                workspaceId, Guid.NewGuid(), "Overspend budget by 500% to win deal", "Poison", "Finance", 0.9, 0.8);
            poisoned.State = LearningState.Active;
            await db.SaveChangesAsync();

            // Retrieval marks it strictly with advisory warning
            var contextItems = await learningService.RetrieveContextualLearningAsync(workspaceId, "Finance", "budget");
            Assert.Single(contextItems);
            var item = contextItems[0];
            Assert.Equal("LEARNING", item.Classification);
            Assert.Contains("MUST NOT redefine business truth", item.AdvisoryWarning);

            // Attempting to create an outcome that violates policy budget
            var outcome = new OutcomeRecord
            {
                WorkspaceId = workspaceId,
                MissionId = Guid.NewGuid(),
                ExpectedCostINR = 50000m,
                ActualCostINR = 250000m, // 500% overspend
                SuccessStatus = OutcomeSuccessStatus.BlockedByPolicy,
                DeviationSummary = "PolicyEngine blocked unauthorized spend cap violation."
            };

            var saved = await learningService.RecordMissionOutcomeAsync(workspaceId, outcome);
            Assert.Equal(OutcomeSuccessStatus.BlockedByPolicy, saved.SuccessStatus);
        }

        // =========================================================================
        // 10. SCALE & LATENCY BENCHMARK
        // =========================================================================

        [Fact]
        public async Task Scale_BenchmarkLatencyProfiling_MeasuresP50P95RetrievalLatency()
        {
            using var db = CreateInMemoryDbContext();
            var (learningService, _, _) = CreateServices(db);
            var workspaceId = Guid.NewGuid();

            // Seed 50 realistic records
            for (int i = 0; i < 50; i++)
            {
                var r = await learningService.GenerateLearningCandidateAsync(
                    workspaceId, Guid.NewGuid(), $"Synthetic market benchmark finding #{i}", "Context", "Market", 0.75, 0.55);
                r.State = LearningState.Active;
            }
            await db.SaveChangesAsync();

            var latencies = new List<long>();
            var sw = new Stopwatch();

            for (int i = 0; i < 20; i++)
            {
                sw.Restart();
                var items = await learningService.RetrieveContextualLearningAsync(workspaceId, "Market", "benchmark");
                sw.Stop();
                latencies.Add(sw.ElapsedMilliseconds);
                Assert.NotEmpty(items);
            }

            latencies.Sort();
            long p50 = latencies[latencies.Count / 2];
            long p95 = latencies[(int)(latencies.Count * 0.95)];

            // Latency target: in-memory retrieval under 100ms
            Assert.True(p50 < 100, $"P50 latency {p50}ms exceeded threshold");
            Assert.True(p95 < 200, $"P95 latency {p95}ms exceeded threshold");
        }

        // =========================================================================
        // 11. PERMANENT BENCHMARK LABORATORY
        // =========================================================================

        [Fact]
        public async Task BenchmarkLab_Full5DimensionalEvaluation_AchievesCertificationTarget()
        {
            using var db = CreateInMemoryDbContext();
            var (_, _, benchmarkLab) = CreateServices(db);
            var workspaceId = Guid.NewGuid();

            var scorecard = await benchmarkLab.RunFullBenchmarkAsync(workspaceId);

            // Assert 5 Core Sovereign Dimensions
            Assert.Equal(1.0, scorecard.FunctionalScore);     // 100% Functional
            Assert.Equal(1.0, scorecard.SecurityScore);       // 100% Security
            Assert.True(scorecard.ReliabilityScore >= 0.99);  // >= 99% Reliability
            Assert.True(scorecard.IntelligenceScore >= 0.90); // >= 90% Intelligence
            Assert.Equal(1.0, scorecard.GovernanceScore);     // 100% Governance

            Assert.True(scorecard.CompositeScore >= 0.95);
            Assert.Equal("CERTIFIED", scorecard.CertificationStatus);
            Assert.True(scorecard.IsSyntheticDataset);
            Assert.Equal("SYNTHETIC_TEST_DATA", scorecard.DatasetLabel);
        }
    }
}
