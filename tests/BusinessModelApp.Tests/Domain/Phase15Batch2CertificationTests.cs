using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessModelApp.Core.Agents;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Infrastructure.Agents;
using BusinessModelApp.Infrastructure.Missions;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public class Phase15Batch2CertificationTests
    {
        // -------------------------------------------------------------------------
        // GATE H4: Agent Cards, Dynamic Trust Scores & 5-Layer Authority Hierarchy
        // -------------------------------------------------------------------------
        [Fact]
        public void GateH4_AgentCard_AuthorityChain_ConstitutionStrictlyOverridesTrust()
        {
            var engine = new TrustScoreEngine();

            var card = new AgentCard
            {
                AgentId = "outreach-specialist-01",
                Role = "Outreach Specialist",
                Department = "Commercial",
                Capabilities = new List<string> { "SEND_OUTREACH", "ANALYZE_PROSPECT" },
                TrustScore = new AgentTrustScore
                {
                    OverallScore = 0.99 // Near-perfect trust
                }
            };

            // Constitutional policy fails (e.g. contact fatigue breached or budget cap hit)
            bool constitutionAllows = false;

            bool canExecute = engine.CanExecuteCapability(card, "SEND_OUTREACH", constitutionAllows);

            // Invariant: Constitution overrides trust score completely
            Assert.False(canExecute);
        }

        [Fact]
        public void GateH4_AgentCard_ForbiddenCapability_TakesStrictPrecedence()
        {
            var engine = new TrustScoreEngine();

            var card = new AgentCard
            {
                AgentId = "researcher-02",
                Role = "Market Researcher",
                Department = "Commercial",
                Capabilities = new List<string> { "READ_PUBLIC_DATA", "SEND_EMAIL" },
                ForbiddenCapabilities = new List<string> { "SEND_EMAIL", "EXECUTE_PAYMENT" },
                TrustScore = new AgentTrustScore { OverallScore = 0.95 }
            };

            // Forbidden capability must return false
            Assert.False(card.IsCapabilityPermitted("SEND_EMAIL"));
            Assert.False(engine.CanExecuteCapability(card, "SEND_EMAIL", constitutionalPolicyAllows: true));

            // Allowed capability must return true
            Assert.True(card.IsCapabilityPermitted("READ_PUBLIC_DATA"));
            Assert.True(engine.CanExecuteCapability(card, "READ_PUBLIC_DATA", constitutionalPolicyAllows: true));
        }

        [Fact]
        public void GateH4_AgentCard_TrustScore_DegradedTrustBlocksAutonomousExecution()
        {
            var engine = new TrustScoreEngine();

            var card = new AgentCard
            {
                AgentId = "junior-sales-03",
                Role = "Junior SDR",
                Capabilities = new List<string> { "BOOK_MEETING" },
                TrustScore = new AgentTrustScore { OverallScore = 0.85 }
            };

            // Agent commits repeated policy violations and hallucinations
            for (int i = 0; i < 5; i++)
            {
                engine.UpdateTrustScore(card.AgentId, taskSucceeded: false, budgetAdhered: false, policyComplied: false, hallucinationDetected: true);
            }

            var updatedScore = engine.UpdateTrustScore(card.AgentId, taskSucceeded: false, budgetAdhered: false, policyComplied: false, hallucinationDetected: true);
            card.TrustScore = updatedScore;

            Assert.True(card.TrustScore.OverallScore < 0.70);
            // Invariant: Trust below 0.70 blocks autonomous execution
            Assert.False(engine.CanExecuteCapability(card, "BOOK_MEETING", constitutionalPolicyAllows: true));
        }

        // -------------------------------------------------------------------------
        // GATE H5: Idempotent Hierarchical Wallets
        // -------------------------------------------------------------------------
        [Fact]
        public void GateH5_HierarchicalWallet_ThreePhaseCommit_ReservesCommitsAndSettles()
        {
            var manager = new HierarchicalWalletManager();

            var missionWallet = manager.CreateWallet("mission-101", WalletScope.Mission, 50000m);
            var agentWallet = manager.CreateWallet("agent-outreach", WalletScope.Agent, 10000m, parentWalletId: missionWallet.WalletId);

            string txnId = "txn-unique-001";
            decimal reserveAmount = 2500m;
            decimal actualSpend = 2100m;

            // 1. Reserve
            bool reserved = manager.TryReserveIdempotent(txnId, agentWallet.WalletId, "agent-outreach", reserveAmount);
            Assert.True(reserved);
            Assert.Equal(7500m, agentWallet.RemainingINR);
            Assert.Equal(47500m, missionWallet.RemainingINR);

            // 2. Commit
            bool committed = manager.CommitIdempotent(txnId, actualSpend);
            Assert.True(committed);
            Assert.Equal(7900m, agentWallet.RemainingINR); // 10000 - 2100 = 7900
            Assert.Equal(47900m, missionWallet.RemainingINR); // 50000 - 2100 = 47900

            // 3. Reconcile / Settle
            bool settled = manager.ReconcileIdempotent(txnId);
            Assert.True(settled);

            var txn = manager.GetTransaction(txnId);
            Assert.NotNull(txn);
            Assert.Equal(WalletPhase.Settled, txn.Phase);
        }

        [Fact]
        public void GateH5_HierarchicalWallet_Idempotency_DuplicateTxnDoesNotDoubleSpend()
        {
            var manager = new HierarchicalWalletManager();
            var wallet = manager.CreateWallet("agent-wallet-01", WalletScope.Agent, 10000m);

            string txnId = "idempotent-txn-555";

            // First reservation
            bool firstAttempt = manager.TryReserveIdempotent(txnId, wallet.WalletId, "agent-01", 3000m);
            Assert.True(firstAttempt);
            Assert.Equal(7000m, wallet.RemainingINR);

            // Duplicate reservation with same txnId (e.g. network retry)
            bool secondAttempt = manager.TryReserveIdempotent(txnId, wallet.WalletId, "agent-01", 3000m);
            Assert.True(secondAttempt);
            // Invariant: Remaining budget is NOT deducted a second time
            Assert.Equal(7000m, wallet.RemainingINR);
        }

        [Fact]
        public void GateH5_HierarchicalWallet_Overdraft_HardBlocksWhenBudgetExceeded()
        {
            var manager = new HierarchicalWalletManager();
            var wallet = manager.CreateWallet("agent-wallet-02", WalletScope.Agent, 5000m);

            // Attempt to reserve more than total budget
            bool overdrawn = manager.TryReserveIdempotent("txn-overdraft-1", wallet.WalletId, "agent-02", 6000m);

            // Invariant: Overdraft is hard-blocked
            Assert.False(overdrawn);
            Assert.Equal(5000m, wallet.RemainingINR);
        }

        // -------------------------------------------------------------------------
        // GATE H6: Idempotent Event-Sourced Mission Ledger
        // -------------------------------------------------------------------------
        [Fact]
        public async Task GateH6_EventSourcedLedger_AppendAndReplay_ReconstructsMissionState()
        {
            var store = new EventSourcedMissionStore();
            var missionId = Guid.NewGuid();

            // Append sequential events
            await store.AppendEventAsync(new MissionCreatedEvent
            {
                MissionId = missionId,
                SequenceNumber = 1,
                Objective = "Generate ₹50L revenue in 60 days",
                TargetRevenueINR = 5000000m,
                MaxBudgetINR = 75000m
            });

            await store.AppendEventAsync(new RevenueBaselineCalculatedEvent
            {
                MissionId = missionId,
                SequenceNumber = 2,
                ContractedRevenueINR = 1200000m,
                WeightedPipelineINR = 1400000m,
                AutonomousGapINR = 2400000m
            });

            await store.AppendEventAsync(new AgentDispatchedEvent
            {
                MissionId = missionId,
                SequenceNumber = 3,
                AgentId = "commercial-cbo-agent",
                Role = "Commercial Chief",
                AllocatedBudgetINR = 25000m
            });

            await store.AppendEventAsync(new MissionStateTransitionedEvent
            {
                MissionId = missionId,
                SequenceNumber = 4,
                PreviousState = DurableMissionState.RevenueBaselineCalculated,
                NewState = DurableMissionState.ReadyForExecution
            });

            // Replay from stream
            var projection = await store.ReplayMissionStateAsync(missionId);

            Assert.NotNull(projection);
            Assert.Equal(missionId, projection.MissionId);
            Assert.Equal("Generate ₹50L revenue in 60 days", projection.Objective);
            Assert.Equal(5000000m, projection.TargetRevenueINR);
            Assert.Equal(2400000m, projection.AutonomousGapINR);
            Assert.Equal(DurableMissionState.ReadyForExecution, projection.CurrentState);
            Assert.Contains("commercial-cbo-agent", projection.DispatchedAgentIds);
            Assert.Equal(4, projection.EventCount);
        }

        [Fact]
        public async Task GateH6_EventSourcedLedger_Idempotency_DuplicateSequenceIsDeduplicated()
        {
            var store = new EventSourcedMissionStore();
            var missionId = Guid.NewGuid();

            var evt = new MissionCreatedEvent
            {
                EventId = Guid.NewGuid(),
                MissionId = missionId,
                SequenceNumber = 1,
                Objective = "Idempotency Test Objective",
                TargetRevenueINR = 1000000m
            };

            // First append
            bool firstResult = await store.AppendEventAsync(evt);
            Assert.True(firstResult);

            // Duplicate append with same EventId and SequenceNumber
            bool secondResult = await store.AppendEventAsync(evt);
            Assert.True(secondResult);

            var stream = await store.GetEventStreamAsync(missionId);
            // Invariant: Event was not duplicated in the stream
            Assert.Single(stream);
        }

        // -------------------------------------------------------------------------
        // GATE H7: Counterfactual Mission Forking Engine
        // -------------------------------------------------------------------------
        [Fact]
        public async Task GateH7_MissionForking_CounterfactualScenario_LeavesParentImmutable()
        {
            var store = new EventSourcedMissionStore();
            var missionId = Guid.NewGuid();

            await store.AppendEventAsync(new MissionCreatedEvent
            {
                MissionId = missionId,
                SequenceNumber = 1,
                Objective = "Canonical Baseline Mission",
                TargetRevenueINR = 5000000m,
                MaxBudgetINR = 50000m
            });

            var forkEngine = new MissionForkEngine(store);

            // Scenario: 20% conversion drop, 25% deal size increase
            var scenario = new CounterfactualScenario
            {
                Name = "Stress Test: -20% Conversion, +25% Deal Size",
                ConversionRateMultiplier = 0.80,
                DealSizeMultiplier = 1.25,
                BudgetMultiplier = 1.0
            };

            var forkResult = await forkEngine.CreateForkAsync(missionId, scenario);

            Assert.NotNull(forkResult);
            Assert.Equal(missionId, forkResult.ParentMissionId);
            Assert.Equal(5000000m, forkResult.SimulatedRevenueINR); // 50L * 1.25 * 0.80 = 50L
            Assert.True(forkResult.EstimatedSuccessProbability > 0.0);
            Assert.Equal(1.25, forkResult.RequiredOutreachMultiplier); // 1 / 0.80 = 1.25
            Assert.True(forkResult.IsFeasibleWithinBudget);

            // Invariant: Parent stream remains untouched
            var parentStream = await store.GetEventStreamAsync(missionId);
            Assert.Single(parentStream);
        }

        [Fact]
        public async Task GateH7_MissionForking_CompareForks_RanksForksByProbabilityAndFeasibility()
        {
            var store = new EventSourcedMissionStore();
            var missionId = Guid.NewGuid();

            await store.AppendEventAsync(new MissionCreatedEvent
            {
                MissionId = missionId,
                SequenceNumber = 1,
                Objective = "Comparative Multi-Fork Mission",
                TargetRevenueINR = 5000000m
            });

            var forkEngine = new MissionForkEngine(store);

            var scenarios = new List<CounterfactualScenario>
            {
                new() { Name = "Conservative", ConversionRateMultiplier = 0.60, DealSizeMultiplier = 1.0, BudgetMultiplier = 0.80 },
                new() { Name = "Optimistic", ConversionRateMultiplier = 1.10, DealSizeMultiplier = 1.20, BudgetMultiplier = 1.0 },
                new() { Name = "Severe Downturn", ConversionRateMultiplier = 0.30, DealSizeMultiplier = 0.70, BudgetMultiplier = 0.40 }
            };

            var report = await forkEngine.CompareForksAsync(missionId, scenarios);

            Assert.NotNull(report);
            Assert.Equal(3, report.EvaluatedForks.Count);
            // Ranked descending by probability
            Assert.True(report.EvaluatedForks[0].EstimatedSuccessProbability >= report.EvaluatedForks[1].EstimatedSuccessProbability);
            Assert.Contains("Optimistic", report.RecommendationSummary);
            Assert.False(report.EvaluatedForks.First(f => f.Scenario.Name == "Severe Downturn").IsFeasibleWithinBudget);
        }
    }
}
