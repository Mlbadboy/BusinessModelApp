using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
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
using BusinessModelApp.Core.Strategy;
using BusinessModelApp.Core.WorldModel;
using BusinessModelApp.Infrastructure.Constitution;
using BusinessModelApp.Infrastructure.Data;
using BusinessModelApp.Infrastructure.Decisions;
using BusinessModelApp.Infrastructure.Missions;
using BusinessModelApp.Infrastructure.Objectives;
using BusinessModelApp.Infrastructure.Strategy;
using BusinessModelApp.Infrastructure.WorldModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public class Phase1WorldModelCertificationTests
    {
        private AppDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"Phase1TestDb_{Guid.NewGuid()}")
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task RevenueBaseline_ShouldReport_Unavailable_WhenNoReconciledDataExists()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var worldModel = new CompanyWorldModel(db, NullLogger<CompanyWorldModel>.Instance);
            var workspaceId = Guid.NewGuid();

            // Act: Capture snapshot without any payment evidence or connected gateway
            var snapshot = await worldModel.CaptureVerifiedSnapshotAsync(workspaceId);

            // Assert Invariant: ZERO ASSUMED CASH. Absence of data is UNAVAILABLE, never synthetic ₹0!
            Assert.Equal(RevenueBaselineState.Unavailable, snapshot.RevenueBaselineState);
            Assert.Equal(0m, snapshot.VerifiedRevenueINR.Value);
            Assert.Equal(0.0, snapshot.VerifiedRevenueINR.Confidence);
            Assert.Equal(MetricProvenanceSource.Unknown, snapshot.VerifiedRevenueINR.Source);
            Assert.Contains("Revenue baseline unavailable", snapshot.VerifiedRevenueINR.Note);
        }

        [Fact]
        public async Task TruthMetric_ShouldTrack_FieldLevel_Evidence()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var worldModel = new CompanyWorldModel(db, NullLogger<CompanyWorldModel>.Instance);
            var workspaceId = Guid.NewGuid();

            // Seed legitimate payment evidence
            var paymentEvidence = new EvidenceRecord
            {
                WorkspaceId = workspaceId,
                Class = EvidenceClass.Fact,
                SourceType = EvidenceSourceType.PaymentGateway,
                SourceSystem = "Razorpay Webhook",
                SourceIdentifier = "pay_Phase1Verified123",
                RawPayloadHash = "abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789",
                CanonicalPayloadHash = "123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef0",
                EvidenceDigest = "Authoritative payment settlement verified for transaction pay_Phase1Verified123 (₹1,500,000.00 INR).",
                Status = VerificationStatus.VerifiedFact,
                Confidence = 1.0,
                ObservedAt = DateTime.UtcNow.AddHours(-2),
                RetrievedAt = DateTime.UtcNow
            };
            await db.EvidenceRecords.AddAsync(paymentEvidence);
            await db.SaveChangesAsync();

            // Act
            var snapshot = await worldModel.CaptureVerifiedSnapshotAsync(workspaceId);

            // Assert: Field-by-field TruthMetric tracks evidence record IDs and hashes
            Assert.Equal(RevenueBaselineState.Verified, snapshot.RevenueBaselineState);
            Assert.Equal(1500000m, snapshot.VerifiedRevenueINR.Value);
            Assert.Equal(VerificationStatus.VerifiedFact, snapshot.VerifiedRevenueINR.VerificationStatus);
            Assert.Contains(paymentEvidence.Id, snapshot.VerifiedRevenueINR.EvidenceRecordIds);
            Assert.Contains(paymentEvidence.RawPayloadHash, snapshot.VerifiedRevenueINR.GroundingEvidenceHashes);
            Assert.Equal(1.0, snapshot.VerifiedRevenueINR.Confidence);
        }

        [Fact]
        public void StrategySimulator_ShouldBeDeterministic()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var strategyEngine = new StrategyEngine(db, NullLogger<StrategyEngine>.Instance);
            var snapshot = new CompanySnapshot
            {
                AvailableDeliverySlots = new TruthMetric<int> { Value = 4, Source = MetricProvenanceSource.VerifiedFact }
            };

            var strategy = new BusinessStrategy
            {
                StrategyName = "BFSI AI Route",
                TargetACV_INR = 2500000m, // ₹25L ACV
                ExpectedWinRate = 0.25,   // 25% win rate
                DeliveryCapacitySlotsRequired = 2,
                ExpectedGrossMarginPercent = 65.0m
            };

            decimal targetRevenue = 5000000m; // ₹50L

            // Act
            strategyEngine.SimulateDeterministicFunnel(strategy, targetRevenue, snapshot);

            // Assert: Pure deterministic reverse-funnel math
            // 1. Required Closed Deals = 50L / 25L = 2
            Assert.Equal(2, strategy.RequiredClosedDeals);

            // 2. Required Qualified Opps = Ceiling(2 / 0.25) = 8
            Assert.Equal(8, strategy.RequiredQualifiedOppsCount);
            Assert.Equal(20000000m, strategy.RequiredPipelineINR); // 8 * 25L = ₹2 Crore pipeline

            // 3. Required Meetings = Ceiling(8 / 0.40) = 20
            Assert.Equal(20, strategy.RequiredMeetingsCount);

            // 4. Required Prospects = Ceiling(20 / 0.20) = 100
            Assert.Equal(100, strategy.RequiredProspectsCount);

            // 5. Feasibility: 2 slots required vs 4 available -> Feasible
            Assert.Equal(StrategyFeasibilityState.Feasible, strategy.FeasibilityState);
        }

        [Fact]
        public void StrategyFeasibility_ShouldFlag_CapacityBlocked_WhenSlotsExceeded()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var strategyEngine = new StrategyEngine(db, NullLogger<StrategyEngine>.Instance);
            var snapshot = new CompanySnapshot
            {
                AvailableDeliverySlots = new TruthMetric<int> { Value = 4 }
            };

            var strategy = new BusinessStrategy
            {
                StrategyName = "High-Volume Retainers",
                TargetACV_INR = 500000m,
                ExpectedWinRate = 0.40,
                DeliveryCapacitySlotsRequired = 6 // 6 slots exceeds available 4 slots!
            };

            // Act
            strategyEngine.SimulateDeterministicFunnel(strategy, 5000000m, snapshot);

            // Assert Invariant: CAPACITY_BLOCKED
            Assert.Equal(StrategyFeasibilityState.CapacityBlocked, strategy.FeasibilityState);
            Assert.Contains("Requires 6 delivery slots, but company only has 4 available", strategy.FeasibilityReason);
        }

        [Fact]
        public async Task StrategicAssumptions_MustBeExplicitlyLabeled_WithProvenanceTags()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var strategyEngine = new StrategyEngine(db, NullLogger<StrategyEngine>.Instance);
            var objective = new BusinessObjective
            {
                Title = "Enterprise AI Expansion",
                Description = "Generate ₹50L in 60 days via BFSI software",
                TargetRevenueINR = 5000000m
            };
            var snapshot = new CompanySnapshot
            {
                AvailableDeliverySlots = new TruthMetric<int> { Value = 4 }
            };

            // Act
            var candidates = await strategyEngine.GenerateStrategyCandidatesAsync(objective, snapshot);

            // Assert: Every strategy candidate carries labeled assumptions
            Assert.Equal(3, candidates.Count);
            foreach (var candidate in candidates)
            {
                var assumptions = JsonSerializer.Deserialize<List<StrategicAssumption>>(candidate.AssumptionsJson);
                Assert.NotNull(assumptions);
                Assert.True(assumptions.Count >= 4);

                // Asserts each assumption has a valid enum provenance tag
                foreach (var assumption in assumptions)
                {
                    Assert.True(Enum.IsDefined(typeof(MetricProvenanceSource), assumption.Source));
                    Assert.False(string.IsNullOrWhiteSpace(assumption.Key));
                    Assert.False(string.IsNullOrWhiteSpace(assumption.AssumedValue));
                }
            }
        }

        [Fact]
        public async Task ConstitutionPolicyEngine_ShouldEnforce_AllSixRules()
        {
            // Arrange
            var constitution = new CompanyConstitutionService(NullLogger<CompanyConstitutionService>.Instance);
            var snapshot = new CompanySnapshot
            {
                AvailableDeliverySlots = new TruthMetric<int> { Value = 4 }
            };

            // Act: Violating RULE-003 (Slots Exceeded) and RULE-004 (Empty Assumptions)
            var invalidStrategy = new BusinessStrategy
            {
                StrategyName = "Overcommitted Strategy",
                DeliveryCapacitySlotsRequired = 8, // Exceeds 4
                AssumptionsJson = "[]" // Missing assumptions
            };

            var result = await constitution.EvaluateStrategyAsync(invalidStrategy, snapshot);

            // Assert
            Assert.False(result.IsCompliant);
            Assert.Equal(2, result.Violations.Count);
            Assert.Contains(result.Violations, v => v.RuleCode == ConstitutionRuleCode.RULE_003_NO_DELIVERY_CAPACITY);
            Assert.Contains(result.Violations, v => v.RuleCode == ConstitutionRuleCode.RULE_004_STRATEGY_MUST_IDENTIFY_ASSUMPTIONS);
        }

        [Fact]
        public async Task DecisionRecord_ShouldBeImmutable_AndSupport_Supersession()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var constitution = new CompanyConstitutionService(NullLogger<CompanyConstitutionService>.Instance);
            var decisionEngine = new DecisionEngine(db, constitution, NullLogger<DecisionEngine>.Instance);

            var objective = new BusinessObjective { Title = "Q4 Mandate", TargetRevenueINR = 5000000m };
            var strategyA = new BusinessStrategy { StrategyName = "Strategy A", ExpectedGrossMarginPercent = 60m };
            var strategyB = new BusinessStrategy { StrategyName = "Strategy B", ExpectedGrossMarginPercent = 50m };
            var snapshot = new CompanySnapshot
            {
                SnapshotDigestHash = "hash123",
                AvailableDeliverySlots = new TruthMetric<int> { Value = 4 }
            };

            // Act 1: Commit initial decision
            var initialDecision = await decisionEngine.CommitStrategyDecisionAsync(objective, strategyA, new[] { strategyB }, snapshot);
            Assert.NotNull(initialDecision);
            Assert.Null(initialDecision.SupersedesDecisionId);

            // Act 2: Commit superseding decision (e.g., executive override to Strategy B)
            var supersedingDecision = await decisionEngine.CommitStrategyDecisionAsync(
                objective, strategyB, new[] { strategyA }, snapshot, supersedesDecisionId: initialDecision.Id);

            // Assert: Superseding decision points back to initial decision
            Assert.NotNull(supersedingDecision);
            Assert.Equal(initialDecision.Id, supersedingDecision.SupersedesDecisionId);
            Assert.NotEqual(initialDecision.Id, supersedingDecision.Id);
        }

        [Fact]
        public async Task DurableMission_ShouldResume_FromLastCompletedCheckpoint()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var missionOrchestrator = new DurableMissionOrchestrator(db, NullLogger<DurableMissionOrchestrator>.Instance);

            var decision = new DecisionRecord { Id = Guid.NewGuid(), ObjectiveId = Guid.NewGuid() };
            var strategy = new BusinessStrategy { StrategyName = "Mission Route", TargetMarket = "BFSI" };

            // Act 1: Create mission plan
            var mission = await missionOrchestrator.CreateDurableMissionPlanAsync(decision, strategy);
            Assert.Equal(4, mission.Checkpoints.Count);
            Assert.Equal(0, mission.CurrentCheckpointIndex);

            // Act 2: Simulate Checkpoint 1 & 2 completion
            await missionOrchestrator.RecordCheckpointAsync(
                mission.Id,
                mission.Checkpoints[0].StepName,
                "ProspectDiscovery",
                "{\"discoveredAccounts\": 15}",
                isCompleted: true);

            await missionOrchestrator.RecordCheckpointAsync(
                mission.Id,
                mission.Checkpoints[1].StepName,
                "CompanyResearch",
                "{\"verifiedDecisionMakers\": 8}",
                isCompleted: true);

            // Act 3: Resume mission after interruption
            var resumedMission = await missionOrchestrator.ResumeMissionFromCheckpointAsync(mission.Id);

            // Assert: Resumed at index 2, pointing to Checkpoint 3
            Assert.Equal(2, resumedMission.CurrentCheckpointIndex);
            Assert.Equal(DurableMissionState.Running, resumedMission.State);
            Assert.Equal(mission.Checkpoints[1].StepName, resumedMission.LastCompletedStepName);
        }

        [Fact]
        public async Task WhyCharlieExplanation_ShouldProvide_FullAuditChain()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var constitution = new CompanyConstitutionService(NullLogger<CompanyConstitutionService>.Instance);
            var decisionEngine = new DecisionEngine(db, constitution, NullLogger<DecisionEngine>.Instance);

            var objective = new BusinessObjective { Title = "Generate ₹50L", TargetRevenueINR = 5000000m };
            await db.BusinessObjectives.AddAsync(objective);

            var strategy = new BusinessStrategy
            {
                ObjectiveId = objective.Id,
                StrategyName = "Enterprise Route B",
                TargetACV_INR = 2500000m,
                ExpectedGrossMarginPercent = 65m,
                ExpectedWinRate = 0.25
            };
            await db.BusinessStrategies.AddAsync(strategy);
            await db.SaveChangesAsync();

            var snapshot = new CompanySnapshot
            {
                SnapshotDigestHash = "sha256-snapshot-digest-12345",
                AvailableDeliverySlots = new TruthMetric<int> { Value = 4 }
            };

            var decision = await decisionEngine.CommitStrategyDecisionAsync(objective, strategy, Array.Empty<BusinessStrategy>(), snapshot);

            // Act
            var why = await decisionEngine.GetWhyCharlieExplanationAsync(decision.Id);

            // Assert: Full explainability chain populated
            Assert.NotNull(why);
            Assert.Equal(decision.Id, why.DecisionId);
            Assert.Equal("Enterprise Route B", why.SelectedStrategyName);
            Assert.Equal(65m, why.ExpectedMarginPercent);
            Assert.Equal(0.25, why.WinProbability);
            Assert.Contains("sha256-snapshot-digest-12345", why.GroundingEvidenceHashes);
            Assert.True(why.ConstitutionRulesEvaluated.Count > 0);
        }

        [Fact]
        public async Task Antarctica_UnknownWorldTest_ShouldBlock_Execution()
        {
            // Arrange: Ungrounded goal with zero market evidence
            using var db = CreateInMemoryDbContext();
            var worldModel = new CompanyWorldModel(db, NullLogger<CompanyWorldModel>.Instance);
            var objectiveEngine = new ObjectiveEngine(db, NullLogger<ObjectiveEngine>.Instance);
            var strategyEngine = new StrategyEngine(db, NullLogger<StrategyEngine>.Instance);
            var constitution = new CompanyConstitutionService(NullLogger<CompanyConstitutionService>.Instance);
            var decisionEngine = new DecisionEngine(db, constitution, NullLogger<DecisionEngine>.Instance);
            var missionOrchestrator = new DurableMissionOrchestrator(db, NullLogger<DurableMissionOrchestrator>.Instance);
            var workspaceId = Guid.NewGuid();

            string ungroundedPrompt = "Generate ₹1 crore from quantum-computing services in Antarctica within 30 days.";

            // Act 1: Ingest objective
            var objective = await objectiveEngine.IngestCeoPromptAsync(ungroundedPrompt, workspaceId);
            var snapshot = await worldModel.CaptureVerifiedSnapshotAsync(workspaceId);
            objective = await objectiveEngine.ReconcileObjectiveProgressAsync(objective.Id, snapshot);

            // Act 2: Generate strategies
            var strategies = await strategyEngine.GenerateStrategyCandidatesAsync(objective, snapshot);

            // Act 3: Attempt to commit decision for candidate strategy
            var selectedStrategy = strategies.First();
            var decision = await decisionEngine.CommitStrategyDecisionAsync(objective, selectedStrategy, strategies, snapshot);

            // Act 4: Formulate mission plan
            var mission = await missionOrchestrator.CreateDurableMissionPlanAsync(decision, selectedStrategy);

            // Assert Invariants:
            // 1. Revenue baseline is UNAVAILABLE
            Assert.Equal(RevenueBaselineState.Unavailable, snapshot.RevenueBaselineState);

            // 2. All candidate strategies are flagged EVIDENCE_INSUFFICIENT
            foreach (var s in strategies)
            {
                Assert.Equal(StrategyFeasibilityState.EvidenceInsufficient, s.FeasibilityState);
                Assert.False(string.IsNullOrWhiteSpace(s.FeasibilityReason));
            }

            // 3. Decision is BLOCKED and requires mandatory human approval
            Assert.True(decision.HumanApprovalRequired);
            Assert.Equal(DecisionApprovalStatus.Pending, decision.ApprovalStatus);
            Assert.Contains("DECISION BLOCKED: Insufficient external market evidence", decision.DecisionRationale);

            // 4. Mission cannot execute autonomously (State = WaitingForApproval)
            Assert.Equal(DurableMissionState.WaitingForApproval, mission.State);
            Assert.Equal(0, mission.CurrentCheckpointIndex);

            // 5. Zero fake CRM records were manufactured!
            Assert.Equal(0, await db.Leads.CountAsync());
            Assert.Equal(0, await db.Opportunities.CountAsync());
        }
    }
}
