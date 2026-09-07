using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Causal;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Kernel;
using BusinessModelApp.Infrastructure.Runtime.Intelligence.Causal;
using BusinessModelApp.Infrastructure.Runtime.Intelligence.Kernel;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public class Phase3Batch381CausalIntelligenceTests
    {
        private const string TenantA = "tenant-alpha";
        private const string TenantB = "tenant-beta";

        private static (
            CausalGraphEngine graph,
            CausalHypothesisEngine hypotheses,
            ConfounderDetector confounders,
            TemporalCausalInvestigator temporal,
            InterventionSimulator interventions,
            CausalEvidenceEvaluator evaluator,
            CausalIntelligenceEngine orchestrator,
            InMemoryKpiObservationStore observationStore,
            InMemoryKpiRegistry registry) CreateFixture()
        {
            var registry = new InMemoryKpiRegistry();
            var observationStore = new InMemoryKpiObservationStore(registry);
            var graph = new CausalGraphEngine();
            var hypotheses = new CausalHypothesisEngine();
            var confounders = new ConfounderDetector(graph);
            var temporal = new TemporalCausalInvestigator(observationStore);
            var interventions = new InterventionSimulator(graph, confounders);
            var evaluator = new CausalEvidenceEvaluator(hypotheses, confounders);
            var orchestrator = new CausalIntelligenceEngine(graph, hypotheses, confounders, temporal, interventions, evaluator);

            return (graph, hypotheses, confounders, temporal, interventions, evaluator, orchestrator, observationStore, registry);
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
        // CIE-01: Causal DAG Graph Construction & Cycle Prevention
        // =====================================================================

        [Fact]
        public async Task CIE01_01_AddNodesAndEdges_ConstructsValidDAG()
        {
            var (graph, _, _, _, _, _, _, _, _) = CreateFixture();
            var now = DateTime.UtcNow;

            await graph.AddNodeAsync(new CausalNode("ad_spend", TenantA, "ad_spend", "Ad Spend", CausalNodeType.Metric, EpistemicKind.Fact, now));
            await graph.AddNodeAsync(new CausalNode("traffic", TenantA, "traffic", "Web Traffic", CausalNodeType.Metric, EpistemicKind.Fact, now));
            await graph.AddNodeAsync(new CausalNode("sales", TenantA, "sales", "Total Sales", CausalNodeType.Metric, EpistemicKind.Fact, now));

            await graph.AddEdgeAsync(new CausalEdge("e1", TenantA, "ad_spend", "traffic", 0.85m, "Paid advertising drives impressions and visits", CausalEdgeStatus.Observed, 0.90m));
            await graph.AddEdgeAsync(new CausalEdge("e2", TenantA, "traffic", "sales", 0.40m, "Traffic converts to orders", CausalEdgeStatus.Observed, 0.85m));

            var fullGraph = await graph.GetGraphAsync(TenantA);
            Assert.Equal(3, fullGraph.Nodes.Count);
            Assert.Equal(2, fullGraph.Edges.Count);

            bool isAcyclic = await graph.ValidateAcyclicAsync(TenantA);
            Assert.True(isAcyclic);
        }

        [Fact]
        public async Task CIE01_02_AddCyclicEdge_ThrowsInvalidOperationException()
        {
            var (graph, _, _, _, _, _, _, _, _) = CreateFixture();
            var now = DateTime.UtcNow;

            await graph.AddNodeAsync(new CausalNode("A", TenantA, "A", "Node A", CausalNodeType.Metric, EpistemicKind.Fact, now));
            await graph.AddNodeAsync(new CausalNode("B", TenantA, "B", "Node B", CausalNodeType.Metric, EpistemicKind.Fact, now));
            await graph.AddNodeAsync(new CausalNode("C", TenantA, "C", "Node C", CausalNodeType.Metric, EpistemicKind.Fact, now));

            await graph.AddEdgeAsync(new CausalEdge("e1", TenantA, "A", "B", 0.5m, "A -> B", CausalEdgeStatus.Inferred, 0.8m));
            await graph.AddEdgeAsync(new CausalEdge("e2", TenantA, "B", "C", 0.5m, "B -> C", CausalEdgeStatus.Inferred, 0.8m));

            // Attempt to create cycle: C -> A
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                graph.AddEdgeAsync(new CausalEdge("e3", TenantA, "C", "A", 0.5m, "C -> A (Cycle!)", CausalEdgeStatus.Inferred, 0.8m)));

            Assert.Contains("creates a cycle", ex.Message);
        }

        [Fact]
        public async Task CIE01_03_SelfLoopEdge_ThrowsInvalidOperationException()
        {
            var (graph, _, _, _, _, _, _, _, _) = CreateFixture();
            var now = DateTime.UtcNow;

            await graph.AddNodeAsync(new CausalNode("A", TenantA, "A", "Node A", CausalNodeType.Metric, EpistemicKind.Fact, now));

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                graph.AddEdgeAsync(new CausalEdge("e1", TenantA, "A", "A", 0.5m, "Self-loop", CausalEdgeStatus.Inferred, 0.8m)));

            Assert.Contains("Self-loops are forbidden", ex.Message);
        }

        [Fact]
        public async Task CIE01_04_TopologicalSort_ReturnsValidCausalOrder()
        {
            var (graph, _, _, _, _, _, _, _, _) = CreateFixture();
            var now = DateTime.UtcNow;

            await graph.AddNodeAsync(new CausalNode("A", TenantA, "A", "A", CausalNodeType.Metric, EpistemicKind.Fact, now));
            await graph.AddNodeAsync(new CausalNode("B", TenantA, "B", "B", CausalNodeType.Metric, EpistemicKind.Fact, now));
            await graph.AddNodeAsync(new CausalNode("C", TenantA, "C", "C", CausalNodeType.Metric, EpistemicKind.Fact, now));

            await graph.AddEdgeAsync(new CausalEdge("e1", TenantA, "A", "B", 1.0m, "A -> B", CausalEdgeStatus.Observed, 0.9m));
            await graph.AddEdgeAsync(new CausalEdge("e2", TenantA, "B", "C", 1.0m, "B -> C", CausalEdgeStatus.Observed, 0.9m));

            var order = (await graph.GetTopologicalSortAsync(TenantA)).ToList();
            Assert.Equal(3, order.Count);
            Assert.True(order.IndexOf("A") < order.IndexOf("B"));
            Assert.True(order.IndexOf("B") < order.IndexOf("C"));
        }

        [Fact]
        public async Task CIE01_05_AncestorsAndDescendants_AccuratelyIdentifiesLineage()
        {
            var (graph, _, _, _, _, _, _, _, _) = CreateFixture();
            var now = DateTime.UtcNow;

            await graph.AddNodeAsync(new CausalNode("A", TenantA, "A", "A", CausalNodeType.Metric, EpistemicKind.Fact, now));
            await graph.AddNodeAsync(new CausalNode("B", TenantA, "B", "B", CausalNodeType.Metric, EpistemicKind.Fact, now));
            await graph.AddNodeAsync(new CausalNode("C", TenantA, "C", "C", CausalNodeType.Metric, EpistemicKind.Fact, now));

            await graph.AddEdgeAsync(new CausalEdge("e1", TenantA, "A", "B", 1.0m, "A -> B", CausalEdgeStatus.Observed, 0.9m));
            await graph.AddEdgeAsync(new CausalEdge("e2", TenantA, "B", "C", 1.0m, "B -> C", CausalEdgeStatus.Observed, 0.9m));

            var ancestorsOfC = await graph.GetAncestorsAsync(TenantA, "C");
            Assert.Contains(ancestorsOfC, a => a.NodeId == "A");
            Assert.Contains(ancestorsOfC, a => a.NodeId == "B");

            var descendantsOfA = await graph.GetDescendantsAsync(TenantA, "A");
            Assert.Contains(descendantsOfA, d => d.NodeId == "B");
            Assert.Contains(descendantsOfA, d => d.NodeId == "C");
        }

        // =====================================================================
        // CIE-02: Correlation Does Not Equal Causation (Invariant I19-A)
        // =====================================================================

        [Fact]
        public async Task CIE02_01_HighCorrelation_GeneratesHypothesis_NeverFact()
        {
            var (_, hypotheses, _, _, _, _, _, _, _) = CreateFixture();

            var hyp = await hypotheses.FormulateHypothesisAsync(
                TenantA, "ice_cream_sales", "drowning_incidents", "Correlation observed during summer",
                statedAssumptions: new[] { "Observational correlation only" });

            Assert.Equal(CausalHypothesisStatus.Hypothesized, hyp.Status);
            Assert.NotEqual(CausalHypothesisStatus.Plausible, hyp.Status);
        }

        [Fact]
        public async Task CIE02_02_ScorecardFailsGating_WhenOnlyCorrelationTierExists()
        {
            var (_, hypotheses, _, _, _, evaluator, _, _, _) = CreateFixture();

            var hyp = await hypotheses.FormulateHypothesisAsync(
                TenantA, "metric_x", "metric_y", "Strong Pearson correlation r=0.95");

            var scorecard = await evaluator.EvaluateEvidenceAsync(TenantA, hyp.HypothesisId);

            Assert.False(scorecard.GatingPassed);
            Assert.Equal(CausalHypothesisStatus.Hypothesized, scorecard.RecommendedStatus);
            Assert.Contains("Invariant I19-A", scorecard.GatingRationale);
        }

        [Fact]
        public async Task CIE02_03_AttachingObservationalEvidence_DoesNotPromoteToPlausible()
        {
            var (_, hypotheses, _, _, _, evaluator, _, _, _) = CreateFixture();

            var hyp = await hypotheses.FormulateHypothesisAsync(TenantA, "x", "y", "Correlation test");
            hyp = await hypotheses.AttachEvidenceAsync(hyp.HypothesisId, "obs-corr-1", isSupporting: true, EvidenceTier.ObservationalCorrelation);

            var scorecard = await evaluator.EvaluateEvidenceAsync(TenantA, hyp.HypothesisId);
            Assert.False(scorecard.GatingPassed);
            Assert.NotEqual(CausalHypothesisStatus.Plausible, scorecard.RecommendedStatus);
        }

        // =====================================================================
        // CIE-03: Temporal Precedence Metrology (Invariant I19-B)
        // =====================================================================

        [Fact]
        public async Task CIE03_01_LeadingMetric_AccuratelyIdentifiesPrecedenceAndDirection()
        {
            var (_, _, _, temporal, _, _, _, observationStore, registry) = CreateFixture();
            var now = DateTime.UtcNow;

            await registry.RegisterKpiAsync(new KpiDefinition("lead_metric", TenantA, "Lead", "", "", KpiNature.Leading, TimeSpan.FromDays(1)));
            await registry.RegisterKpiAsync(new KpiDefinition("lag_metric", TenantA, "Lag", "", "", KpiNature.Lagging, TimeSpan.FromDays(1)));

            // Metric A leads Metric B by 1 step
            for (int i = 0; i < 8; i++)
            {
                var time = now.AddHours(-16 + i * 2);
                await observationStore.RecordObservationAsync(CreateObservation(TenantA, "lead_metric", 100m + i * 10m, time));
                await observationStore.RecordObservationAsync(CreateObservation(TenantA, "lag_metric", 50m + Math.Max(0, (i - 1)) * 10m, time));
            }

            var result = await temporal.InvestigateTemporalPrecedenceAsync(TenantA, "lead_metric", "lag_metric");

            Assert.True(result.MeetsCandidatePrecedenceThreshold);
            Assert.Equal(TemporalPrecedenceDirection.Leading, result.Direction);
            Assert.Contains("Invariant I19-B", result.CaveatMessage);
        }

        [Fact]
        public async Task CIE03_02_TemporalPrecedence_ExplicitlyCaveatsPostHocErgoPropterHoc()
        {
            var (_, _, _, temporal, _, _, _, observationStore, registry) = CreateFixture();
            var now = DateTime.UtcNow;

            await registry.RegisterKpiAsync(new KpiDefinition("A", TenantA, "A", "", "", KpiNature.Leading, TimeSpan.FromDays(1)));
            await registry.RegisterKpiAsync(new KpiDefinition("B", TenantA, "B", "", "", KpiNature.Lagging, TimeSpan.FromDays(1)));

            for (int i = 0; i < 6; i++)
            {
                var time = now.AddHours(-12 + i * 2);
                await observationStore.RecordObservationAsync(CreateObservation(TenantA, "A", 10m * i, time));
                await observationStore.RecordObservationAsync(CreateObservation(TenantA, "B", 10m * Math.Max(0, i - 1), time));
            }

            var result = await temporal.InvestigateTemporalPrecedenceAsync(TenantA, "A", "B");
            Assert.Contains("post hoc ergo propter hoc fallacy is rejected", result.CaveatMessage);
        }

        [Fact]
        public async Task CIE03_03_InsufficientObservations_PreservesUnknown()
        {
            var (_, _, _, temporal, _, _, _, observationStore, registry) = CreateFixture();
            var now = DateTime.UtcNow;

            await registry.RegisterKpiAsync(new KpiDefinition("A", TenantA, "A", "", "", KpiNature.Leading, TimeSpan.FromDays(1)));
            await registry.RegisterKpiAsync(new KpiDefinition("B", TenantA, "B", "", "", KpiNature.Lagging, TimeSpan.FromDays(1)));

            await observationStore.RecordObservationAsync(CreateObservation(TenantA, "A", 10m, now));
            await observationStore.RecordObservationAsync(CreateObservation(TenantA, "B", 10m, now));

            var result = await temporal.InvestigateTemporalPrecedenceAsync(TenantA, "A", "B");
            Assert.False(result.MeetsCandidatePrecedenceThreshold);
            Assert.Equal(TemporalPrecedenceDirection.Indeterminate, result.Direction);
            Assert.Contains("UNKNOWN preserved", result.CaveatMessage);
        }

        // =====================================================================
        // CIE-04: Confounder & Common Cause Detection
        // =====================================================================

        [Fact]
        public async Task CIE04_01_CommonCause_DetectedAsConfounder()
        {
            var (graph, _, confounders, _, _, _, _, _, _) = CreateFixture();
            var now = DateTime.UtcNow;

            // Seasonality (Z) drives both Ad Spend (X) and Sales (Y)
            await graph.AddNodeAsync(new CausalNode("seasonality", TenantA, "seasonality", "Seasonality", CausalNodeType.ExternalFactor, EpistemicKind.Fact, now));
            await graph.AddNodeAsync(new CausalNode("ad_spend", TenantA, "ad_spend", "Ad Spend", CausalNodeType.Metric, EpistemicKind.Fact, now));
            await graph.AddNodeAsync(new CausalNode("sales", TenantA, "sales", "Sales", CausalNodeType.Metric, EpistemicKind.Fact, now));

            await graph.AddEdgeAsync(new CausalEdge("e1", TenantA, "seasonality", "ad_spend", 0.8m, "Holiday campaign budget surge", CausalEdgeStatus.Observed, 0.9m));
            await graph.AddEdgeAsync(new CausalEdge("e2", TenantA, "seasonality", "sales", 0.9m, "Holiday consumer demand surge", CausalEdgeStatus.Observed, 0.95m));

            var assessment = await confounders.AssessConfoundersAsync(TenantA, "ad_spend", "sales");

            Assert.True(assessment.IsConfounded);
            Assert.Contains("seasonality", assessment.ConfounderMetricIds);
            Assert.Contains("seasonality", assessment.RequiredAdjustmentSet);
            Assert.NotEqual(ConfounderRiskLevel.None, assessment.RiskLevel);
        }

        [Fact]
        public async Task CIE04_02_UnconfoundedPath_EvaluatesAsNone()
        {
            var (graph, _, confounders, _, _, _, _, _, _) = CreateFixture();
            var now = DateTime.UtcNow;

            await graph.AddNodeAsync(new CausalNode("X", TenantA, "X", "X", CausalNodeType.Metric, EpistemicKind.Fact, now));
            await graph.AddNodeAsync(new CausalNode("Y", TenantA, "Y", "Y", CausalNodeType.Metric, EpistemicKind.Fact, now));
            await graph.AddEdgeAsync(new CausalEdge("e1", TenantA, "X", "Y", 0.7m, "Direct connection", CausalEdgeStatus.Observed, 0.85m));

            var assessment = await confounders.AssessConfoundersAsync(TenantA, "X", "Y");
            Assert.False(assessment.IsConfounded);
            Assert.Equal(ConfounderRiskLevel.None, assessment.RiskLevel);
            Assert.Empty(assessment.ConfounderMetricIds);
        }

        // =====================================================================
        // CIE-05: Causal Hypothesis Engine & Competing Explanations
        // =====================================================================

        [Fact]
        public async Task CIE05_01_FormulateHypothesis_RecordsStatedAssumptionsAndAlternatives()
        {
            var (_, hypotheses, _, _, _, _, _, _, _) = CreateFixture();

            var hyp = await hypotheses.FormulateHypothesisAsync(
                TenantA,
                "cac",
                "margin_drop",
                "CAC surge increases marketing OPEX, depressing operating margin",
                statedAssumptions: new[] { "Pricing remains constant", "COGS unchanged" },
                alternativeExplanationIds: new[] { "hyp-cogs-surge", "hyp-discount-pressure" });

            Assert.NotNull(hyp);
            Assert.Equal("cac", hyp.CauseMetricId);
            Assert.Equal("margin_drop", hyp.EffectMetricId);
            Assert.Equal(2, hyp.StatedAssumptions.Count);
            Assert.Equal(2, hyp.AlternativeExplanationIds.Count);
            Assert.NotEmpty(hyp.IntegrityHash);
        }

        [Fact]
        public async Task CIE05_02_AttachingContradictoryEvidence_TransitionsToWeakenedOrRefuted()
        {
            var (_, hypotheses, _, _, _, _, _, _, _) = CreateFixture();

            var hyp = await hypotheses.FormulateHypothesisAsync(TenantA, "feature_x", "churn_reduction", "Feature X directly stops churn");

            // Attach 3 contradictory evidence items
            hyp = await hypotheses.AttachEvidenceAsync(hyp.HypothesisId, "churn-cohort-test-1", isSupporting: false, EvidenceTier.ControlledExperiment);
            hyp = await hypotheses.AttachEvidenceAsync(hyp.HypothesisId, "churn-cohort-test-2", isSupporting: false, EvidenceTier.ControlledExperiment);

            Assert.True(hyp.Status == CausalHypothesisStatus.Weakened || hyp.Status == CausalHypothesisStatus.Refuted);
            Assert.Equal(2, hyp.ContradictoryEvidenceIds.Count);
        }

        // =====================================================================
        // CIE-06: Multi-Dimensional Evidence Scorecard & Gating (Hardening Amendment 1)
        // =====================================================================

        [Fact]
        public async Task CIE06_01_ConfoundedBackdoorPath_ForcesGatingRejection_PreservesAsUnknown()
        {
            var (graph, hypotheses, _, _, _, evaluator, _, _, _) = CreateFixture();
            var now = DateTime.UtcNow;

            // Introduce strong confounder
            await graph.AddNodeAsync(new CausalNode("Z", TenantA, "Z", "Confounder Z", CausalNodeType.ExternalFactor, EpistemicKind.Fact, now));
            await graph.AddNodeAsync(new CausalNode("X", TenantA, "X", "Metric X", CausalNodeType.Metric, EpistemicKind.Fact, now));
            await graph.AddNodeAsync(new CausalNode("Y", TenantA, "Y", "Metric Y", CausalNodeType.Metric, EpistemicKind.Fact, now));

            await graph.AddEdgeAsync(new CausalEdge("e1", TenantA, "Z", "X", 0.9m, "Z -> X", CausalEdgeStatus.Observed, 0.95m));
            await graph.AddEdgeAsync(new CausalEdge("e2", TenantA, "Z", "Y", 0.9m, "Z -> Y", CausalEdgeStatus.Observed, 0.95m));

            var hyp = await hypotheses.FormulateHypothesisAsync(TenantA, "X", "Y", "Spurious X -> Y link");
            // Give high observational correlation evidence
            hyp = await hypotheses.AttachEvidenceAsync(hyp.HypothesisId, "ev-1", isSupporting: true, EvidenceTier.TemporalPrecedence);

            var scorecard = await evaluator.EvaluateEvidenceAsync(TenantA, hyp.HypothesisId);

            // Hardening Rule 1: Confounder cannot be overcome by high score!
            Assert.False(scorecard.GatingPassed);
            Assert.Equal(CausalHypothesisStatus.PreservedAsUnknown, scorecard.RecommendedStatus);
            Assert.True(scorecard.ConfounderPenalty > 0);
            Assert.Contains("Confounded backdoor path detected", scorecard.GatingRationale);
        }

        [Fact]
        public async Task CIE06_02_HighScoreAlone_CannotMechanicallyPromoteToPlausible_WithoutExperimentalTier()
        {
            var (_, hypotheses, _, _, _, evaluator, _, _, _) = CreateFixture();

            var hyp = await hypotheses.FormulateHypothesisAsync(TenantA, "X", "Y", "High correlation test");
            // Attach 4 observational pieces of evidence
            hyp = await hypotheses.AttachEvidenceAsync(hyp.HypothesisId, "ev-1", isSupporting: true, EvidenceTier.ObservationalCorrelation);
            hyp = await hypotheses.AttachEvidenceAsync(hyp.HypothesisId, "ev-2", isSupporting: true, EvidenceTier.ObservationalCorrelation);
            hyp = await hypotheses.AttachEvidenceAsync(hyp.HypothesisId, "ev-3", isSupporting: true, EvidenceTier.ObservationalCorrelation);

            var scorecard = await evaluator.EvaluateEvidenceAsync(TenantA, hyp.HypothesisId);

            // Scorecard is decision support, NOT causal truth machine
            Assert.False(scorecard.GatingPassed);
            Assert.NotEqual(CausalHypothesisStatus.Plausible, scorecard.RecommendedStatus);
            Assert.Contains("Invariant I19-A", scorecard.GatingRationale);
        }

        [Fact]
        public async Task CIE06_03_UnconfoundedWithControlledExperiment_PassesGateToPlausible()
        {
            var (graph, hypotheses, _, _, _, evaluator, _, _, _) = CreateFixture();
            var now = DateTime.UtcNow;

            await graph.AddNodeAsync(new CausalNode("treatment", TenantA, "treatment", "Treatment", CausalNodeType.Intervention, EpistemicKind.Fact, now));
            await graph.AddNodeAsync(new CausalNode("outcome", TenantA, "outcome", "Outcome", CausalNodeType.Metric, EpistemicKind.Fact, now));
            await graph.AddEdgeAsync(new CausalEdge("e1", TenantA, "treatment", "outcome", 0.65m, "Direct treatment effect", CausalEdgeStatus.Observed, 0.95m));

            var hyp = await hypotheses.FormulateHypothesisAsync(TenantA, "treatment", "outcome", "A/B test on checkout optimization");
            hyp = await hypotheses.AttachEvidenceAsync(hyp.HypothesisId, "ab-test-stat-sig", isSupporting: true, EvidenceTier.ControlledExperiment);

            var scorecard = await evaluator.EvaluateEvidenceAsync(TenantA, hyp.HypothesisId);

            Assert.True(scorecard.GatingPassed);
            Assert.Equal(CausalHypothesisStatus.Plausible, scorecard.RecommendedStatus);
            Assert.True(scorecard.ExperimentalEvidenceScore > 0.80m);
        }

        // =====================================================================
        // CIE-07: Insufficient Evidence Fail-Closed Invariance (Invariant I19-H)
        // =====================================================================

        [Fact]
        public async Task CIE07_01_EmptyEvidenceHypothesis_FailsClosedToHypothesized()
        {
            var (_, hypotheses, _, _, _, evaluator, _, _, _) = CreateFixture();

            var hyp = await hypotheses.FormulateHypothesisAsync(TenantA, "A", "B", "Speculative link");
            var scorecard = await evaluator.EvaluateEvidenceAsync(TenantA, hyp.HypothesisId);

            Assert.False(scorecard.GatingPassed);
            Assert.Equal(CausalHypothesisStatus.Hypothesized, scorecard.RecommendedStatus);
        }

        // =====================================================================
        // CIE-08: Multi-Model Agreement Is Not Proof (Invariant I19-D)
        // =====================================================================

        [Fact]
        public async Task CIE08_01_MultiModelAgreement_WithoutEmpiricalTier_RemainsHypothesis()
        {
            var (_, hypotheses, _, _, _, evaluator, _, _, _) = CreateFixture();

            var hyp = await hypotheses.FormulateHypothesisAsync(
                TenantA, "pricing_strategy", "retention", "GPT-4, Claude 3.5, and Gemini all agree pricing reduces retention",
                statedAssumptions: new[] { "All LLMs surveyed agree on causal direction" });

            var scorecard = await evaluator.EvaluateEvidenceAsync(TenantA, hyp.HypothesisId);

            // Invariant I19-D: Model consensus is not causal proof
            Assert.False(scorecard.GatingPassed);
            Assert.Equal(CausalHypothesisStatus.Hypothesized, scorecard.RecommendedStatus);
        }

        // =====================================================================
        // CIE-09: Counterfactual Intervention Simulator (Hardening Amendment 2 & Invariant I19-F)
        // =====================================================================

        [Fact]
        public async Task CIE09_01_CheckIdentifiability_ReturnsNotIdentified_WhenBackdoorPathUnblocked()
        {
            var (graph, _, _, _, interventions, _, _, _, _) = CreateFixture();
            var now = DateTime.UtcNow;

            await graph.AddNodeAsync(new CausalNode("Z", TenantA, "Z", "Z", CausalNodeType.ExternalFactor, EpistemicKind.Fact, now));
            await graph.AddNodeAsync(new CausalNode("X", TenantA, "X", "X", CausalNodeType.Metric, EpistemicKind.Fact, now));
            // Backdoor edge into X
            await graph.AddEdgeAsync(new CausalEdge("e1", TenantA, "Z", "X", 0.8m, "Backdoor", CausalEdgeStatus.Observed, 0.9m, IsBackdoorPath: true));

            var identifiability = await interventions.CheckIdentifiabilityAsync(TenantA, "X", conditioningNodes: Array.Empty<string>());
            Assert.Equal(CausalIdentifiabilityStatus.NotIdentified, identifiability);
        }

        [Fact]
        public async Task CIE09_02_SimulateIntervention_OnUnidentifiableGraph_FailsClosedWithoutFalseClaims()
        {
            var (graph, _, _, _, interventions, _, _, _, _) = CreateFixture();
            var now = DateTime.UtcNow;

            await graph.AddNodeAsync(new CausalNode("Z", TenantA, "Z", "Z", CausalNodeType.ExternalFactor, EpistemicKind.Fact, now));
            await graph.AddNodeAsync(new CausalNode("X", TenantA, "X", "X", CausalNodeType.Metric, EpistemicKind.Fact, now));
            await graph.AddEdgeAsync(new CausalEdge("e1", TenantA, "Z", "X", 0.8m, "Backdoor", CausalEdgeStatus.Observed, 0.9m, IsBackdoorPath: true));

            var spec = new InterventionSpec("spec-1", TenantA, "X", 10.0m, 50.0m, ConditioningNodes: Array.Empty<string>());
            var result = await interventions.SimulateInterventionAsync(spec);

            // Hardening Rule 2: Fails closed, does NOT generate unjustified counterfactual claims
            Assert.Equal(CausalIdentifiabilityStatus.NotIdentified, result.IdentifiabilityStatus);
            Assert.Empty(result.PredictedMetricDeltas);
            Assert.Equal(1.00m, result.UncertaintyRadius);
            Assert.Contains("Invariant I19-F", result.Explanation);
        }

        [Fact]
        public async Task CIE09_03_ConditioningOnBackdoor_RendersEffectIdentified()
        {
            var (graph, _, _, _, interventions, _, _, _, _) = CreateFixture();
            var now = DateTime.UtcNow;

            await graph.AddNodeAsync(new CausalNode("Z", TenantA, "Z", "Z", CausalNodeType.ExternalFactor, EpistemicKind.Fact, now));
            await graph.AddNodeAsync(new CausalNode("X", TenantA, "X", "X", CausalNodeType.Metric, EpistemicKind.Fact, now));
            await graph.AddEdgeAsync(new CausalEdge("e1", TenantA, "Z", "X", 0.8m, "Backdoor", CausalEdgeStatus.Observed, 0.9m, IsBackdoorPath: true));

            var identifiability = await interventions.CheckIdentifiabilityAsync(TenantA, "X", conditioningNodes: new[] { "Z" });
            Assert.Equal(CausalIdentifiabilityStatus.Identified, identifiability);
        }

        [Fact]
        public async Task CIE09_04_IdentifiedIntervention_SimulatesDownstreamDeltas_TaggedSimulation()
        {
            var (graph, _, _, _, interventions, _, _, _, _) = CreateFixture();
            var now = DateTime.UtcNow;

            await graph.AddNodeAsync(new CausalNode("discount", TenantA, "discount", "Discount Rate", CausalNodeType.Intervention, EpistemicKind.Fact, now));
            await graph.AddNodeAsync(new CausalNode("conversion", TenantA, "conversion", "Conversion", CausalNodeType.Metric, EpistemicKind.Fact, now));
            await graph.AddEdgeAsync(new CausalEdge("e1", TenantA, "discount", "conversion", 0.50m, "Price sensitivity elasticity", CausalEdgeStatus.Observed, 0.90m));

            var spec = new InterventionSpec("spec-discount", TenantA, "discount", 5.0m, 10.0m, ConditioningNodes: Array.Empty<string>());
            var sim = await interventions.SimulateInterventionAsync(spec);

            Assert.Equal(CausalIdentifiabilityStatus.Identified, sim.IdentifiabilityStatus);
            Assert.True(sim.PredictedMetricDeltas.ContainsKey("conversion"));
            Assert.Equal(2.50m, sim.PredictedMetricDeltas["conversion"]); // 5.0 * 0.50 = 2.50
            Assert.Equal(EpistemicKind.Simulation, sim.EpistemicKind);
        }

        // =====================================================================
        // CIE-10: Causal Recommendation Does Not Equal Authority (Invariant I19-G)
        // =====================================================================

        [Fact]
        public void CIE10_01_CausalTypes_HaveZeroExecutionPermitMethods()
        {
            var causalTypes = typeof(CausalHypothesis).Assembly.GetTypes()
                .Where(t => t.Namespace != null && t.Namespace.Contains("Intelligence.Causal"))
                .ToList();

            foreach (var type in causalTypes)
            {
                var methods = type.GetMethods();
                Assert.DoesNotContain(methods, m => m.Name.Contains("IssuePermit"));
                Assert.DoesNotContain(methods, m => m.Name.Contains("AuthorizeExecution"));
                Assert.DoesNotContain(methods, m => m.Name.Contains("BypassFirewall"));
            }
        }

        // =====================================================================
        // CIE-11: Content-Addressed Cryptographic Integrity of Hypotheses
        // =====================================================================

        [Fact]
        public async Task CIE11_01_IntegrityHash_ChangesDeterministicallyWhenEvidenceAttached()
        {
            var (_, hypotheses, _, _, _, _, _, _, _) = CreateFixture();

            var hyp = await hypotheses.FormulateHypothesisAsync(TenantA, "A", "B", "Initial mechanism");
            var originalHash = hyp.IntegrityHash;

            hyp = await hypotheses.AttachEvidenceAsync(hyp.HypothesisId, "ev-1", isSupporting: true, EvidenceTier.TemporalPrecedence);
            var updatedHash = hyp.IntegrityHash;

            Assert.NotEqual(originalHash, updatedHash);
        }

        [Fact]
        public void CIE11_02_IdenticalParameters_ProduceIdenticalHash()
        {
            var hash1 = CausalHypothesis.ComputeIntegrityHash("T1", "X", "Y", "mech", 0.75m, CausalHypothesisStatus.Hypothesized, EvidenceTier.TemporalPrecedence, new[] { "ev1", "ev2" });
            var hash2 = CausalHypothesis.ComputeIntegrityHash("T1", "X", "Y", "mech", 0.75m, CausalHypothesisStatus.Hypothesized, EvidenceTier.TemporalPrecedence, new[] { "ev1", "ev2" });

            Assert.Equal(hash1, hash2);
        }

        // =====================================================================
        // CIE-12: Full End-to-End Orchestration & Multi-Tenant Isolation
        // =====================================================================

        [Fact]
        public async Task CIE12_01_FullOrchestrator_IngestToCausalExplanationLifecycle()
        {
            var (graph, _, _, _, _, _, orchestrator, observationStore, registry) = CreateFixture();
            var now = DateTime.UtcNow;

            await registry.RegisterKpiAsync(new KpiDefinition("cac", TenantA, "CAC", "", "", KpiNature.Leading, TimeSpan.FromDays(1)));
            await registry.RegisterKpiAsync(new KpiDefinition("ebitda", TenantA, "EBITDA", "", "", KpiNature.Lagging, TimeSpan.FromDays(1)));

            for (int i = 0; i < 6; i++)
            {
                var time = now.AddHours(-12 + i * 2);
                await observationStore.RecordObservationAsync(CreateObservation(TenantA, "cac", 1000m + i * 100m, time));
                await observationStore.RecordObservationAsync(CreateObservation(TenantA, "ebitda", 50000m - i * 2000m, time));
            }

            var explained = await orchestrator.EvaluateFullCausalExplanationAsync(
                TenantA, "cac", "ebitda", "CAC inflation drives margin compression");

            Assert.NotNull(explained);
            Assert.Equal("cac", explained.CauseMetricId);
            Assert.Equal("ebitda", explained.EffectMetricId);
            Assert.NotEmpty(explained.SupportingEvidenceIds);
            Assert.NotEqual(CausalHypothesisStatus.Refuted, explained.Status);
        }

        [Fact]
        public async Task CIE12_02_MultiTenant_StrictDataIsolation()
        {
            var (graph, hypotheses, _, _, _, _, _, _, _) = CreateFixture();
            var now = DateTime.UtcNow;

            await graph.AddNodeAsync(new CausalNode("nodeA", TenantA, "nodeA", "Node A", CausalNodeType.Metric, EpistemicKind.Fact, now));
            await graph.AddNodeAsync(new CausalNode("nodeB", TenantB, "nodeB", "Node B", CausalNodeType.Metric, EpistemicKind.Fact, now));

            var graphA = await graph.GetGraphAsync(TenantA);
            var graphB = await graph.GetGraphAsync(TenantB);

            Assert.Single(graphA.Nodes);
            Assert.Equal("nodeA", graphA.Nodes[0].NodeId);

            Assert.Single(graphB.Nodes);
            Assert.Equal("nodeB", graphB.Nodes[0].NodeId);

            await hypotheses.FormulateHypothesisAsync(TenantA, "mA", "mB", "Mechanism A");
            await hypotheses.FormulateHypothesisAsync(TenantB, "mC", "mD", "Mechanism B");

            var hypA = await hypotheses.GetAllHypothesesAsync(TenantA);
            var hypB = await hypotheses.GetAllHypothesesAsync(TenantB);

            Assert.Single(hypA);
            Assert.Single(hypB);
            Assert.NotEqual(hypA[0].HypothesisId, hypB[0].HypothesisId);
        }
    }
}
