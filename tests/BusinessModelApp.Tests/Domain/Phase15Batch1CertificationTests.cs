using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessModelApp.Core.AI;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Objectives;
using BusinessModelApp.Core.Domain.Reality;
using BusinessModelApp.Core.Domain.WorldModel;
using BusinessModelApp.Infrastructure.AI;
using BusinessModelApp.Infrastructure.Reality;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public class Phase15Batch1CertificationTests
    {
        // -------------------------------------------------------------------------
        // GATE H0: Architecture Contract & Backward-Compatibility Gate
        // -------------------------------------------------------------------------
        [Fact]
        public void GateH0_ArchitectureContract_BackwardCompatibility_PreservesP1ThroughP14()
        {
            var guard = new Phase1BaselineGuard();

            // 1. Invariant: Baseline integrity holds
            Assert.True(guard.ValidateBaselineIntegrity());

            // 2. Invariant: Agent memory cannot claim to be verified fact without evidence
            var unverifiedMetric = new TruthMetric<int>
            {
                Value = 100,
                Provenance = MetricProvenanceSource.AI_ESTIMATE,
                VerificationStatus = VerificationStatus.VerifiedFact,
                EvidenceRecordIds = new List<Guid>() // No evidence!
            };
            Assert.False(guard.AssertMemoryIsNotTruth(unverifiedMetric));

            // Valid metric with evidence
            var validMetric = new TruthMetric<int>
            {
                Value = 100,
                Provenance = MetricProvenanceSource.VERIFIED_FACT,
                VerificationStatus = VerificationStatus.VerifiedFact,
                EvidenceRecordIds = new List<Guid> { Guid.NewGuid() }
            };
            Assert.True(guard.AssertMemoryIsNotTruth(validMetric));

            // 3. Invariant: Autonomous Gap is strictly a planning metric
            var baseline = new RevenueBaseline();
            baseline.ContractedGuaranteedRevenueINR = TruthMetric<decimal>.Grounded(1200000m, Guid.NewGuid(), "hash1");
            baseline.WeightedPipelineRevenueINR = TruthMetric<decimal>.Grounded(1400000m, Guid.NewGuid(), "hash2");
            baseline.HistoricalRunRateRevenueINR = TruthMetric<decimal>.Grounded(900000m, Guid.NewGuid(), "hash3");
            baseline.ComputeAutonomousGap(5000000m);
            Assert.True(guard.AssertRevenueGapIsPlanningOnly(baseline));

            // 4. Invariant: Execution wall is intact at ReadyForExecution
            var mission = new DurableMission
            {
                Id = Guid.NewGuid(),
                Title = "Generate ₹50L revenue",
                State = DurableMissionState.ReadyForExecution
            };
            Assert.True(guard.AssertExecutionWallEnforced(mission));
        }

        // -------------------------------------------------------------------------
        // GATE H1: AI Model Fabric & Provider-Neutral Inference Gateway
        // -------------------------------------------------------------------------
        [Fact]
        public void GateH1_ModelFabric_RegistryContainsAllFourTiers()
        {
            var registry = new ModelRegistry();

            var fastModels = registry.GetModelsByTier(ModelTier.FastAndCheap);
            var reasoningModels = registry.GetModelsByTier(ModelTier.ReasoningAndStrategy);
            var codingModels = registry.GetModelsByTier(ModelTier.CodingAndDelivery);
            var visionModels = registry.GetModelsByTier(ModelTier.VisionAndMultimodal);

            Assert.NotEmpty(fastModels);
            Assert.NotEmpty(reasoningModels);
            Assert.NotEmpty(codingModels);
            Assert.NotEmpty(visionModels);

            var claudeReasoning = registry.GetModel("reasoning-claude-3-5-sonnet");
            Assert.NotNull(claudeReasoning);
            Assert.Equal(PrivacyClassification.ConfidentialFinancial, claudeReasoning.PrivacyClass);
            Assert.True(claudeReasoning.ContextWindow >= 128000);
        }

        [Fact]
        public void GateH1_ModelFabric_RouterEnforcesPrivacyAndBudget()
        {
            var registry = new ModelRegistry();
            var router = new ModelRouter(registry);

            // Request requiring confidential financial privacy and reasoning tier
            var request = new ModelRoutingRequest
            {
                TaskType = AITaskType.StrategicPlanning,
                Prompt = "Formulate high-margin enterprise expansion strategy",
                EstimatedTokens = 2000,
                MaxBudgetINR = 5.0m,
                MaxLatencyMs = 3000,
                MinimumPrivacy = PrivacyClassification.ConfidentialFinancial,
                PreferredTier = ModelTier.ReasoningAndStrategy
            };

            var decision = router.Route(request);

            Assert.NotNull(decision);
            Assert.Equal(ModelTier.ReasoningAndStrategy, decision.Tier);
            Assert.True(decision.EstimatedCostINR <= request.MaxBudgetINR);
            Assert.NotEmpty(decision.FallbackModelIds);
            Assert.Contains("Matched tier", decision.RoutingReason);
        }

        [Fact]
        public async Task GateH1_ModelFabric_DeterministicMockGateway_Runs100PercentOffline()
        {
            IAIInferenceGateway gateway = new DeterministicMockInferenceGateway();

            var request = new AIRequest
            {
                TaskType = AITaskType.GeneralAssistant,
                RequestCorrelationId = "test-correlation-123"
            };

            var response = await gateway.ExecuteAsync(request);

            Assert.NotNull(response);
            Assert.Contains("DETERMINISTIC_MOCK_COMPLETION", response.Content);
            Assert.Equal("deterministic-mock-v1", response.ModelUsed);
            Assert.True(response.Usage.TotalTokens > 0);
            Assert.True(response.LatencyMs > 0);
        }

        // -------------------------------------------------------------------------
        // GATE H2: Evidence Graph & Claim Promotion Pipeline
        // -------------------------------------------------------------------------
        [Fact]
        public void GateH2_EvidenceGraph_CorroborationCalculatesMultiSourceConfidence()
        {
            var graph = new EvidenceGraphService();

            var filingSource = graph.AddSource("Registrar of Companies Filing", EvidenceGraphSourceType.OfficialFiling, 0.90);
            var linkedInSource = graph.AddSource("LinkedIn Headcount Audit", EvidenceGraphSourceType.LinkedIn, 0.60);

            var claim = graph.AddClaim("Acme Corp", "Headcount", "50");

            graph.LinkSourceToClaim(claim.ClaimId, filingSource.SourceId, isCorroborating: true);
            graph.LinkSourceToClaim(claim.ClaimId, linkedInSource.SourceId, isCorroborating: true);

            var confidence = graph.RecalculateClaimConfidence(claim.ClaimId);

            // Corroboration formula: 1 - (1 - 0.90)*(1 - 0.60) = 1 - 0.10 * 0.40 = 0.96
            Assert.True(confidence >= 0.95);
            Assert.Equal(ClaimPromotionStatus.Corroborated, claim.PromotionStatus);
        }

        [Fact]
        public void GateH2_EvidenceGraph_PromotionPipelinePromotesToTruthMetric()
        {
            var graph = new EvidenceGraphService();

            var auditSource = graph.AddSource("Statutory Audit 2025", EvidenceGraphSourceType.FinancialAudit, 0.95);
            var claim = graph.AddClaim("Acme Corp", "AnnualRevenueINR", "25000000");

            graph.LinkSourceToClaim(claim.ClaimId, auditSource.SourceId, isCorroborating: true);

            // Promote claim to TruthMetric
            var truthMetric = graph.PromoteToTruthMetric<decimal>(claim.ClaimId, 25000000m);

            Assert.NotNull(truthMetric);
            Assert.Equal(25000000m, truthMetric.Value);
            Assert.Equal(MetricProvenanceSource.VERIFIED_FACT, truthMetric.Provenance);
            Assert.Equal(VerificationStatus.VerifiedFact, truthMetric.VerificationStatus);
            Assert.Contains(auditSource.SourceId, truthMetric.EvidenceRecordIds);
            Assert.Equal(ClaimPromotionStatus.PromotedToTruthMetric, claim.PromotionStatus);
        }

        [Fact]
        public void GateH2_EvidenceGraph_RevocationDynamicallyDemotesClaim()
        {
            var graph = new EvidenceGraphService();

            var webSource = graph.AddSource("Scraped Blog Post", EvidenceGraphSourceType.CompanyWebsite, 0.75);
            var claim = graph.AddClaim("Beta Corp", "BranchCount", "12");

            graph.LinkSourceToClaim(claim.ClaimId, webSource.SourceId, isCorroborating: true);
            Assert.Equal(ClaimPromotionStatus.Corroborated, claim.PromotionStatus);

            // Revoke the source due to detected inaccuracy
            graph.RevokeSource(webSource.SourceId, "Blog post found to be outdated marketing projection");

            var updatedClaim = graph.GetClaim(claim.ClaimId);
            Assert.NotNull(updatedClaim);
            Assert.Equal(0.0, updatedClaim.ConfidenceScore);
        }

        // -------------------------------------------------------------------------
        // GATE H3: Reality Freshness & Decay Engine
        // -------------------------------------------------------------------------
        [Fact]
        public void GateH3_RealityDecay_FreshFactIsVerifiedAndUsable()
        {
            var decayEngine = new RealityDecayEngine();
            var now = DateTime.UtcNow;

            var metric = new TruthMetric<decimal>
            {
                Value = 5000000m,
                Confidence = 0.95,
                ObservedAt = now.AddDays(-5), // 5 days old
                Provenance = MetricProvenanceSource.VERIFIED_FACT,
                VerificationStatus = VerificationStatus.VerifiedFact
            };

            var evaluation = decayEngine.EvaluateFreshness(metric, now);

            Assert.Equal(FreshnessState.VERIFIED, evaluation.State);
            Assert.True(evaluation.IsUsableForPlanning);
            Assert.True(evaluation.EffectiveConfidence >= 0.90);
            Assert.True(decayEngine.CanUseForPlanning(metric, now));
        }

        [Fact]
        public void GateH3_RealityDecay_AgingFactDegradesConfidence()
        {
            var decayEngine = new RealityDecayEngine();
            var now = DateTime.UtcNow;

            var metric = new TruthMetric<decimal>
            {
                Value = 5000000m,
                Confidence = 0.90,
                ObservedAt = now.AddDays(-45), // 45 days old (Aging)
                Provenance = MetricProvenanceSource.VERIFIED_FACT,
                VerificationStatus = VerificationStatus.VerifiedFact
            };

            var evaluation = decayEngine.EvaluateFreshness(metric, now);

            Assert.Equal(FreshnessState.AGING, evaluation.State);
            Assert.True(evaluation.EffectiveConfidence < 0.90);
            Assert.True(evaluation.IsUsableForPlanning); // Usable but aging
        }

        [Fact]
        public void GateH3_RealityDecay_StaleFactIsStrictlyForbiddenForPlanning()
        {
            var decayEngine = new RealityDecayEngine();
            var now = DateTime.UtcNow;

            var metric = new TruthMetric<decimal>
            {
                Value = 5000000m,
                Confidence = 0.85,
                ObservedAt = now.AddDays(-75), // 75 days old (Stale)
                Provenance = MetricProvenanceSource.VERIFIED_FACT,
                VerificationStatus = VerificationStatus.VerifiedFact
            };

            var evaluation = decayEngine.EvaluateFreshness(metric, now);

            Assert.Equal(FreshnessState.STALE, evaluation.State);
            Assert.False(evaluation.IsUsableForPlanning); // Stale metrics strictly forbidden from planning
            Assert.False(decayEngine.CanUseForPlanning(metric, now));
        }

        [Fact]
        public void GateH3_RealityDecay_UnknownHorizonResetsProvenance()
        {
            var decayEngine = new RealityDecayEngine();
            var now = DateTime.UtcNow;

            var metric = new TruthMetric<decimal>
            {
                Value = 5000000m,
                Confidence = 0.80,
                ObservedAt = now.AddDays(-150), // 150 days old (Past unknown horizon)
                Provenance = MetricProvenanceSource.VERIFIED_FACT,
                VerificationStatus = VerificationStatus.VerifiedFact
            };

            var decayedMetric = decayEngine.ApplyDecay(metric, now);

            Assert.Equal(MetricProvenanceSource.UNKNOWN, decayedMetric.Provenance);
            Assert.Equal(VerificationStatus.Unverified, decayedMetric.VerificationStatus);
            Assert.False(decayEngine.CanUseForPlanning(decayedMetric, now));
        }
    }
}
