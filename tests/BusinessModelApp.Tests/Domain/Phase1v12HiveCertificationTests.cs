using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using BusinessModelApp.Core.Agents;
using BusinessModelApp.Core.Constitution;
using BusinessModelApp.Core.Decisions;
using BusinessModelApp.Core.Domain.Decisions;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Objectives;
using BusinessModelApp.Core.Domain.Reality;
using BusinessModelApp.Core.Domain.Strategy;
using BusinessModelApp.Core.Domain.WorldModel;
using BusinessModelApp.Core.Missions;
using BusinessModelApp.Core.Objectives;
using BusinessModelApp.Core.Services;
using BusinessModelApp.Core.Strategy;
using BusinessModelApp.Core.WorldModel;
using BusinessModelApp.Infrastructure.Constitution;
using BusinessModelApp.Infrastructure.Data;
using BusinessModelApp.Infrastructure.Decisions;
using BusinessModelApp.Infrastructure.Missions;
using BusinessModelApp.Infrastructure.Objectives;
using BusinessModelApp.Infrastructure.Services;
using BusinessModelApp.Infrastructure.Strategy;
using BusinessModelApp.Infrastructure.WorldModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public class Phase1v12HiveCertificationTests
    {
        private AppDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"Phase1v12TestDb_{Guid.NewGuid()}")
                .Options;

            return new AppDbContext(options);
        }

        // =========================================================================
        // GATE P1: TruthMetric & Evidence Attribution + "Memory is NOT Truth" Invariant
        // =========================================================================
        [Fact]
        public void GateP1_TruthMetric_ShouldTrack_FieldLevelProvenance_AndRejectMemoryAsTruth()
        {
            // 1. Validated ground-truth metric
            var factId = Guid.NewGuid();
            var metric = TruthMetric<decimal>.Grounded(5000000m, factId, "sha256-hash-payment", 1.0, "Verified payment record.");
            Assert.True(metric.IsGroundedFact);
            Assert.Equal(MetricProvenanceSource.VerifiedFact, metric.Provenance);
            Assert.Equal(1.0, metric.ConfidenceScore);
            Assert.Equal(factId, metric.EvidenceRecordId);

            // 2. Memory != Truth invariant
            var memory = new AgentMemory();
            var hypothesis = memory.RecordHypothesis("Acme Corp has a ₹50L website budget", "Acme Corp", 0.85);

            // Stored in memory, but NOT a fact!
            Assert.False(hypothesis.IsVerifiedFact);
            Assert.DoesNotContain(hypothesis.Id.ToString(), memory.VerifiedEvidenceIds);

            // Attempting to create a TruthMetric from unverified memory must be an AI_ESTIMATE
            var unverifiedMetric = TruthMetric<decimal>.Estimated(5000000m, hypothesis.HypothesisText, hypothesis.SubjectiveConfidence);
            Assert.False(unverifiedMetric.IsGroundedFact);
            Assert.Equal(MetricProvenanceSource.AiEstimate, unverifiedMetric.Provenance);
            Assert.Equal(VerificationStatus.HypothesisOnly, unverifiedMetric.VerificationStatus);

            // Only after external evidence verification does it become a grounded fact
            var externalEvidenceId = Guid.NewGuid();
            bool promoted = memory.PromoteHypothesisToFact(hypothesis.Id, externalEvidenceId, "sha256-acme-po");
            Assert.True(promoted);
            Assert.True(hypothesis.IsVerifiedFact);
            Assert.Contains(externalEvidenceId.ToString(), memory.EvidenceReferences);
        }

        // =========================================================================
        // GATE P2: Unified Company World Model Sub-Realities
        // =========================================================================
        [Fact]
        public async Task GateP2_CompanyWorldModel_ShouldAggregate_AllFourRealities()
        {
            using var db = CreateInMemoryDbContext();
            var worldModel = new CompanyWorldModel(db, NullLogger<CompanyWorldModel>.Instance);
            var workspaceId = Guid.NewGuid();

            var snapshot = await worldModel.CaptureVerifiedSnapshotAsync(workspaceId);

            // Assert All Four Realities Exist
            Assert.NotNull(snapshot.Financial);
            Assert.NotNull(snapshot.Commercial);
            Assert.NotNull(snapshot.Delivery);
            Assert.NotNull(snapshot.Connectors);
            Assert.NotNull(snapshot.RevenueBaseline);

            // Delivery capability verification
            Assert.Equal(4, snapshot.Delivery.AvailableDeliverySlots.Value);
            Assert.Equal(MetricProvenanceSource.VerifiedFact, snapshot.Delivery.AvailableDeliverySlots.Provenance);
            Assert.Contains("Enterprise AI Solutions", snapshot.Delivery.CoreCapabilities);
        }

        // =========================================================================
        // GATE P3: Four-State Revenue Baseline
        // =========================================================================
        [Fact]
        public void GateP3_FourStateRevenueBaseline_ShouldCompute_AutonomousGap_AsPlanningMetricOnly()
        {
            var baseline = new RevenueBaseline();
            decimal targetRevenue = 5000000m; // ₹50L

            // Invariant: Contracted ₹0, Pipeline ₹0, RunRate ₹0 => Autonomous Gap = ₹50L
            baseline.ComputeAutonomousGap(targetRevenue);

            Assert.Equal(targetRevenue, baseline.AutonomousRevenueGapINR.Value);
            Assert.Contains("PLANNING METRIC ONLY", baseline.AutonomousRevenueGapINR.Note);
            Assert.Contains("not guaranteed revenue", baseline.AutonomousRevenueGapINR.Note.ToLowerInvariant());
        }

        // =========================================================================
        // GATE P4: Reverse Funnel Engine
        // =========================================================================
        [Fact]
        public void GateP4_ReverseFunnelEngine_ShouldCompute_Stages_WithTruthMetricConversionRates()
        {
            var funnelEngine = new ReverseFunnelEngine(NullLogger<ReverseFunnelEngine>.Instance);
            var snapshot = new CompanySnapshot();

            var acvMetric = TruthMetric<decimal>.Historical(2500000m, Guid.NewGuid(), "hash-acv", 0.9);
            var winRateMetric = TruthMetric<double>.Historical(0.25, Guid.NewGuid(), "hash-winrate", 0.9);

            var result = funnelEngine.CalculateFunnel(5000000m, acvMetric, winRateMetric, snapshot);

            // 50L / 25L = 2 deals
            Assert.Equal(2, result.RequiredClosedDeals);
            // 2 deals / 0.25 win rate = 8 opps
            Assert.Equal(8, result.RequiredOpportunities);
            Assert.Equal(20000000m, result.RequiredPipelineINR);
            // Funnel is evidence-backed
            Assert.True(result.IsEvidenceBacked);
            Assert.Equal(7, result.Stages.Count);
        }

        // =========================================================================
        // GATE P5 & P6: Deterministic and AI Strategy Simulators + Provenance
        // =========================================================================
        [Fact]
        public void GateP5_P6_StrategySimulators_ShouldDistinguish_DeterministicFromAIHypotheses()
        {
            var funnelEngine = new ReverseFunnelEngine(NullLogger<ReverseFunnelEngine>.Instance);
            var detSimulator = new DeterministicStrategySimulator(funnelEngine, NullLogger<DeterministicStrategySimulator>.Instance);
            var aiSimulator = new AIStrategySimulator(funnelEngine, NullLogger<AIStrategySimulator>.Instance);

            var objective = new BusinessObjective { Title = "Q4 Expansion", TargetRevenueINR = 5000000m };
            var snapshot = new CompanySnapshot { AvailableDeliverySlots = new TruthMetric<int> { Value = 4 } };

            // Deterministic candidate
            var detCandidate = detSimulator.SimulateDeterministicStrategy(
                "Deterministic Enterprise Route", objective, snapshot, 2500000m, 0.25, 2, 20000m);

            Assert.True(detCandidate.IsDeterministic);
            Assert.All(detCandidate.Assumptions, a => Assert.NotEqual(MetricProvenanceSource.AiEstimate, a.Origin));

            // AI Hypothesis candidate
            var aiCandidate = aiSimulator.FormulateAIHypothesisRoute(
                "AI HealthTech Modernization", "Dental Chains", "Automated booking AI", objective, snapshot, 1500000m, 0.30, 2, 25000m);

            Assert.False(aiCandidate.IsDeterministic);
            Assert.Equal(StrategyFeasibilityClassification.HypotheticalUnverified, aiCandidate.Feasibility);
            Assert.True(aiCandidate.HasUnverifiedAssumptions);
            Assert.Contains(aiCandidate.Assumptions, a => a.Origin == MetricProvenanceSource.AiEstimate);
        }

        // =========================================================================
        // GATE P7: Six-Rule Constitution Enforcement
        // =========================================================================
        [Fact]
        public void GateP7_ConstitutionPolicyEngine_ShouldEnforce_BudgetCap_And_ContactFatigue()
        {
            var constitution = new ConstitutionPolicyEngine(NullLogger<ConstitutionPolicyEngine>.Instance);

            // Operation 1: Budget violation (₹250K without human sign-off)
            var budgetViolation = constitution.EvaluateOperation(
                operationType: "PaidMarketingCampaign",
                monetaryCostINR: 250000m,
                targetDomain: "acme.com",
                recentContactCountInWindow: 1,
                requiresHumanSignoff: true,
                isApprovedByHuman: false,
                isReversible: true);

            Assert.False(budgetViolation.IsCompliant);
            Assert.Contains(budgetViolation.Violations, v => v.RuleCode == ConstitutionRuleCode.RULE_1_BUDGET_CAP);

            // Operation 2: Contact fatigue violation (4 touches in 7 days)
            var fatigueViolation = constitution.EvaluateOperation(
                operationType: "SendOutreachEmail",
                monetaryCostINR: 0m,
                targetDomain: "overcontacted.com",
                recentContactCountInWindow: 4,
                requiresHumanSignoff: false,
                isApprovedByHuman: true,
                isReversible: true);

            Assert.False(fatigueViolation.IsCompliant);
            Assert.Contains(fatigueViolation.Violations, v => v.RuleCode == ConstitutionRuleCode.RULE_2_CONTACT_FATIGUE);
        }

        // =========================================================================
        // GATE P8: Autonomous Agent Runtime (Phase 1 Boundary: Real Tools Disabled)
        // =========================================================================
        [Fact]
        public async Task GateP8_AgentRuntime_ShouldExecute_SimulatedPrep_AndDisallowRealMutations()
        {
            var policyEngine = new AgentPolicyEngine();
            var constitution = new ConstitutionPolicyEngine(NullLogger<ConstitutionPolicyEngine>.Instance);
            var runtime = new AgentRuntime(policyEngine, constitution, NullLogger<AgentRuntime>.Instance);

            var identity = AgentIdentity.Create(AgentRole.MarketIntelligence);
            var memory = new AgentMemory();
            var mailbox = new AgentMailbox(identity.AgentId);
            var context = new AgentContext(identity, memory, mailbox);
            var blackboard = new MissionBlackboard();

            // Act: Execute research step
            var result = await runtime.ExecuteStepAsync(
                context,
                blackboard,
                AgentActionType.ResearchCompany,
                "Analyze Target Companies in BFSI",
                estimatedCostINR: 200m);

            // Assert: Execution was simulated/prepared; real mutations disabled in Phase 1
            Assert.True(result.Succeeded);
            Assert.True(result.WasSimulated);
            Assert.Equal(9800m, context.Budget.RemainingBudgetINR); // 10000 - 200
            Assert.Single(context.Memory.EpisodicMemory);
            Assert.Single(blackboard.Tasks);
        }

        // =========================================================================
        // GATE P9: Business Hive Stigmergy & Agent Messaging
        // =========================================================================
        [Fact]
        public void GateP9_BusinessHive_ShouldEnable_Stigmergy_And_DurableMessaging()
        {
            var hive = new BusinessHive(Guid.NewGuid());
            var marketAgent = hive.RegisterAgent(AgentRole.MarketIntelligence);
            var qualAgent = hive.RegisterAgent(AgentRole.LeadQualification);

            var blackboard = hive.GetOrCreateBlackboard(Guid.NewGuid(), Guid.NewGuid(), "₹50L Revenue Mission", 5000000m);

            // Stigmergy: Market Agent posts fact to blackboard
            var evidenceId = Guid.NewGuid();
            blackboard.PostFact("Acme Corporation", "Verified Enterprise B2B SaaS with ₹100Cr+ turnover", evidenceId, "hash-acme-cin");

            // Direct message bus
            var message = new AgentMessage
            {
                SenderAgentId = marketAgent.Identity.AgentId,
                SenderRole = AgentRole.MarketIntelligence,
                RecipientAgentId = qualAgent.Identity.AgentId,
                RecipientRole = AgentRole.LeadQualification,
                Subject = "Qualify Account: Acme Corporation",
                BodyPayloadJson = "{\"company\":\"Acme Corporation\",\"evidence\":\"hash-acme-cin\"}"
            };
            hive.MessageBus.PostMessage(message);

            // Qualification agent checks inbox
            var pending = qualAgent.Mailbox.ReadPendingMessages();
            Assert.Single(pending);
            Assert.Equal("Qualify Account: Acme Corporation", pending[0].Subject);

            // Heartbeat check
            var heartbeat = new AgentHeartbeatService(NullLogger<AgentHeartbeatService>.Instance);
            var pulse = heartbeat.Pulse(hive);
            Assert.Equal(2, pulse.ActiveAgentCount);
            Assert.Equal(1, pulse.UnreadMessageCount);
        }

        // =========================================================================
        // GATE P10 & P11: Charlie Executive Supervisor & Immutable DecisionRecord
        // =========================================================================
        [Fact]
        public async Task GateP10_P11_CharlieExecutiveService_ShouldProduce_ImmutableDecisionRecord_WithHash()
        {
            using var db = CreateInMemoryDbContext();
            var worldModel = new CompanyWorldModel(db, NullLogger<CompanyWorldModel>.Instance);
            var objectiveEngine = new ObjectiveEngine(db, NullLogger<ObjectiveEngine>.Instance);
            var funnelEngine = new ReverseFunnelEngine(NullLogger<ReverseFunnelEngine>.Instance);
            var detSim = new DeterministicStrategySimulator(funnelEngine, NullLogger<DeterministicStrategySimulator>.Instance);
            var aiSim = new AIStrategySimulator(funnelEngine, NullLogger<AIStrategySimulator>.Instance);
            var constEngine = new ConstitutionPolicyEngine(NullLogger<ConstitutionPolicyEngine>.Instance);
            var stratEngine = new CommercialStrategyEngine(detSim, aiSim, constEngine, NullLogger<CommercialStrategyEngine>.Instance);
            var constitution = new CompanyConstitutionService(NullLogger<CompanyConstitutionService>.Instance);
            var decisionEngine = new DecisionEngine(db, constitution, NullLogger<DecisionEngine>.Instance);
            var missionOrchestrator = new DurableMissionOrchestrator(db, NullLogger<DurableMissionOrchestrator>.Instance);
            var agentRuntime = new AgentRuntime(new AgentPolicyEngine(), constEngine, NullLogger<AgentRuntime>.Instance);

            var executive = new CharlieExecutiveService(
                db, worldModel, objectiveEngine, stratEngine, constEngine, decisionEngine, missionOrchestrator, agentRuntime,
                NullLogger<CharlieExecutiveService>.Instance);

            var workspaceId = Guid.NewGuid();
            string mandate = "Generate ₹50L revenue in 60 days through Enterprise AI and Custom Software.";

            // Act
            var pipelineResult = await executive.ProcessExecutiveMandateAsync(mandate, workspaceId);

            // Assert
            Assert.NotNull(pipelineResult);
            Assert.NotNull(pipelineResult.Objective);
            Assert.NotNull(pipelineResult.RevenueBaseline);
            Assert.NotNull(pipelineResult.SelectedStrategy);
            Assert.NotNull(pipelineResult.DecisionRecord);
            Assert.NotNull(pipelineResult.Mission);
            Assert.True(pipelineResult.ExecutionWallEnforced); // Real execution wall maintained

            // DecisionRecord cryptographic immutability
            Assert.False(string.IsNullOrEmpty(pipelineResult.DecisionRecord.CryptographicHash));
            Assert.Equal(64, pipelineResult.DecisionRecord.CryptographicHash.Length); // SHA-256 hex string
        }

        // =========================================================================
        // GATE P12: Durable Mission 12-State Sequence & Checkpoint Resume
        // =========================================================================
        [Fact]
        public async Task GateP12_DurableMission_ShouldAdvance_ThroughFullLifecycle_AndResume()
        {
            using var db = CreateInMemoryDbContext();
            var missionOrchestrator = new DurableMissionOrchestrator(db, NullLogger<DurableMissionOrchestrator>.Instance);

            var decision = new DecisionRecord { Id = Guid.NewGuid(), ObjectiveId = Guid.NewGuid() };
            var strategy = new BusinessStrategy { StrategyName = "Enterprise AI Route" };

            var mission = await missionOrchestrator.CreateDurableMissionPlanAsync(decision, strategy);
            Assert.Equal(4, mission.Checkpoints.Count);

            // Advance through state sequence
            mission.TransitionTo(DurableMissionState.WorldModelBuilt);
            mission.TransitionTo(DurableMissionState.RevenueBaselineCalculated);
            mission.TransitionTo(DurableMissionState.StrategiesGenerated);
            mission.TransitionTo(DurableMissionState.SimulationComplete);
            mission.TransitionTo(DurableMissionState.FeasibilityChecked);
            mission.TransitionTo(DurableMissionState.ConstitutionValidated);
            mission.TransitionTo(DurableMissionState.DecisionRecorded);
            mission.TransitionTo(DurableMissionState.ExecutiveApproved);
            mission.TransitionTo(DurableMissionState.MissionPrepared);
            mission.TransitionTo(DurableMissionState.ReadyForExecution);

            Assert.Equal(DurableMissionState.ReadyForExecution, mission.State);

            // Record Checkpoint 1 completed
            await missionOrchestrator.RecordCheckpointAsync(
                mission.Id, mission.Checkpoints[0].StepName, "ProspectDiscovery", "{\"accounts\": 10}", true);

            // Recover and resume
            var resumed = await missionOrchestrator.ResumeMissionFromCheckpointAsync(mission.Id);
            Assert.Equal(1, resumed.CurrentCheckpointIndex);
            Assert.Equal(DurableMissionState.Running, resumed.State);
        }

        // =========================================================================
        // GATE P14: Antarctica Unknown-World Certification Test
        // =========================================================================
        [Fact]
        public async Task GateP14_Antarctica_UnknownWorldCertification_MustNotFabricateReality()
        {
            using var db = CreateInMemoryDbContext();
            var worldModel = new CompanyWorldModel(db, NullLogger<CompanyWorldModel>.Instance);
            var objectiveEngine = new ObjectiveEngine(db, NullLogger<ObjectiveEngine>.Instance);
            var funnelEngine = new ReverseFunnelEngine(NullLogger<ReverseFunnelEngine>.Instance);
            var detSim = new DeterministicStrategySimulator(funnelEngine, NullLogger<DeterministicStrategySimulator>.Instance);
            var aiSim = new AIStrategySimulator(funnelEngine, NullLogger<AIStrategySimulator>.Instance);
            var constEngine = new ConstitutionPolicyEngine(NullLogger<ConstitutionPolicyEngine>.Instance);
            var stratEngine = new CommercialStrategyEngine(detSim, aiSim, constEngine, NullLogger<CommercialStrategyEngine>.Instance);
            var constitution = new CompanyConstitutionService(NullLogger<CompanyConstitutionService>.Instance);
            var decisionEngine = new DecisionEngine(db, constitution, NullLogger<DecisionEngine>.Instance);
            var missionOrchestrator = new DurableMissionOrchestrator(db, NullLogger<DurableMissionOrchestrator>.Instance);
            var agentRuntime = new AgentRuntime(new AgentPolicyEngine(), constEngine, NullLogger<AgentRuntime>.Instance);

            var executive = new CharlieExecutiveService(
                db, worldModel, objectiveEngine, stratEngine, constEngine, decisionEngine, missionOrchestrator, agentRuntime,
                NullLogger<CharlieExecutiveService>.Instance);

            var workspaceId = Guid.NewGuid();
            string antarcticaMandate = "Generate ₹1 Crore selling Quantum Iceberg Monitoring in Antarctica within 30 days.";

            // Act
            var pipelineResult = await executive.ProcessExecutiveMandateAsync(antarcticaMandate, workspaceId);

            // Assert Invariants:
            // 1. Revenue baseline is UNAVAILABLE (not synthetic ₹0 or assumed cash)
            Assert.Equal(RevenueBaselineState.Unavailable, pipelineResult.Snapshot.RevenueBaselineState);

            // 2. Strategy is marked HYPOTHETICAL_UNVERIFIED
            Assert.Equal(StrategyFeasibilityClassification.HypotheticalUnverified, pipelineResult.SelectedStrategy.Feasibility);

            // 3. Human executive approval is MANDATORY because assumptions are unverified
            Assert.True(pipelineResult.DecisionRecord.HumanApprovalRequired);
            Assert.Equal(DecisionApprovalStatus.Pending, pipelineResult.DecisionRecord.ApprovalStatus);

            // 4. Mission cannot execute autonomously; transitions to ExecutiveApprovalRequired
            Assert.Equal(DurableMissionState.ExecutiveApprovalRequired, pipelineResult.Mission.State);

            // 5. Zero fake CRM entities created
            Assert.Equal(0, await db.Leads.CountAsync());
            Assert.Equal(0, await db.Opportunities.CountAsync());
        }
    }
}
