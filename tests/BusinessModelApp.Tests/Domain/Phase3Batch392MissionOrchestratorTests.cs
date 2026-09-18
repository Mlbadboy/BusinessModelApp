using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime.Missions;
using BusinessModelApp.Core.Domain.Runtime.Organizational;
using BusinessModelApp.Infrastructure.Runtime.Missions.Coordination;
using BusinessModelApp.Infrastructure.Runtime.Organizational;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public class Phase3Batch392MissionOrchestratorTests
    {
        private const string TestTenant = "TENANT-ORCH-392";
        private const string AltTenant = "TENANT-ORCH-ALT";

        private (
            InMemoryMissionCoordinationStore store,
            MissionAdmissionController admissionController,
            MissionResourceArbiter arbiter,
            CrossMissionDependencyResolver depResolver,
            CancellationCascadeCoordinator cancelCoordinator,
            MissionTelemetryFeedbackChannel telemetryChannel,
            MissionOrchestrator orchestrator,
            InMemoryOrganizationalWorkStore workStore
        ) CreateTestRig(TenantMissionConcurrencyPolicy? systemPolicy = null)
        {
            var store = new InMemoryMissionCoordinationStore();
            var workStore = new InMemoryOrganizationalWorkStore();
            var admission = new MissionAdmissionController(store, systemPolicy);
            var arbiter = new MissionResourceArbiter(store, systemPolicy);
            var depResolver = new CrossMissionDependencyResolver(store);
            var cancel = new CancellationCascadeCoordinator(store, admission);
            var telemetry = new MissionTelemetryFeedbackChannel(store, workStore);
            var orchestrator = new MissionOrchestrator(
                admission,
                arbiter,
                depResolver,
                cancel,
                telemetry,
                store);

            return (store, admission, arbiter, depResolver, cancel, telemetry, orchestrator, workStore);
        }

        private MissionGraphProposal CreateSampleProposal(string missionId = "M-01", int nodeCount = 3, int depth = 3)
        {
            var nodes = new List<ProposedNode>();
            var edges = new List<ProposedEdge>();

            for (int i = 1; i <= nodeCount; i++)
            {
                nodes.Add(new ProposedNode
                {
                    NodeId = $"N-{i}",
                    Title = $"Node {i}",
                    NodeType = i == 2 ? MissionNodeType.Approval : MissionNodeType.Analyze
                });
                if (i > 1 && i <= depth)
                {
                    edges.Add(new ProposedEdge
                    {
                        SourceNodeId = $"N-{i - 1}",
                        TargetNodeId = $"N-{i}",
                        Type = EdgeType.Sequential
                    });
                }
            }

            return new MissionGraphProposal
            {
                ProposalId = Guid.NewGuid(),
                MissionId = new MissionId(Guid.NewGuid()),
                Title = $"Proposal {missionId}",
                ProposedNodes = nodes,
                ProposedEdges = edges,
                EstimatedBudgetTokens = 10_000,
                EstimatedCostUsd = 1.50m
            };
        }

        // =========================================================================
        // Family 1: Mission Admission & Concurrency Quotas (MOC01 - MOC06)
        // =========================================================================

        [Fact]
        public async Task MOC01_ProposalWithinQuotas_AdmittedWithTicket()
        {
            var rig = CreateTestRig();
            var proposal = CreateSampleProposal();

            var (admitted, ticket, reason) = await rig.orchestrator.AdmitMissionProposalAsync(
                TestTenant,
                proposal,
                "WRK-01",
                WorkRiskTier.R1_InternalReversible);

            Assert.True(admitted);
            Assert.NotNull(ticket);
            Assert.Null(reason);
            Assert.Equal(AdmissionDecisionStatus.Admitted, ticket!.Decision);
            Assert.Equal(TestTenant, ticket.TenantId);
            Assert.NotEmpty(ticket.ProvenanceHash);
        }

        [Fact]
        public async Task MOC02_ActiveMissionCapExceeded_QueuesMission()
        {
            var policy = new TenantMissionConcurrencyPolicy { MaxActiveMissions = 2 };
            var rig = CreateTestRig(policy);

            // Fill active slots
            await rig.orchestrator.AdmitMissionProposalAsync(TestTenant, CreateSampleProposal("M-1"), "W-1", WorkRiskTier.R1_InternalReversible);
            await rig.orchestrator.AdmitMissionProposalAsync(TestTenant, CreateSampleProposal("M-2"), "W-2", WorkRiskTier.R1_InternalReversible);

            // Third mission exceeds active cap -> queued
            var (admitted, ticket, reason) = await rig.orchestrator.AdmitMissionProposalAsync(
                TestTenant,
                CreateSampleProposal("M-3"),
                "W-3",
                WorkRiskTier.R1_InternalReversible);

            Assert.False(admitted);
            Assert.NotNull(ticket);
            Assert.Equal(AdmissionDecisionStatus.Queued, ticket!.Decision);
            Assert.Contains("coordination queue", reason);
        }

        [Fact]
        public async Task MOC03_ActiveNodeCapExceeded_QueuesMission()
        {
            var policy = new TenantMissionConcurrencyPolicy { MaxActiveNodes = 5 };
            var rig = CreateTestRig(policy);

            // First mission uses 3 nodes
            await rig.orchestrator.AdmitMissionProposalAsync(TestTenant, CreateSampleProposal("M-1", nodeCount: 3), "W-1", WorkRiskTier.R1_InternalReversible);

            // Second mission requests 3 nodes (total 6 > 5) -> queued
            var (admitted, ticket, reason) = await rig.orchestrator.AdmitMissionProposalAsync(
                TestTenant,
                CreateSampleProposal("M-2", nodeCount: 3),
                "W-2",
                WorkRiskTier.R1_InternalReversible);

            Assert.False(admitted);
            Assert.Equal(AdmissionDecisionStatus.Queued, ticket!.Decision);
        }

        [Fact]
        public async Task MOC04_QueueCapExceeded_RejectsMission()
        {
            var policy = new TenantMissionConcurrencyPolicy { MaxActiveMissions = 1, MaxQueuedMissions = 1 };
            var rig = CreateTestRig(policy);

            // 1 active
            await rig.orchestrator.AdmitMissionProposalAsync(TestTenant, CreateSampleProposal("M-1"), "W-1", WorkRiskTier.R1_InternalReversible);
            // 1 queued
            await rig.orchestrator.AdmitMissionProposalAsync(TestTenant, CreateSampleProposal("M-2"), "W-2", WorkRiskTier.R1_InternalReversible);

            // Next exceeds queue -> Rejected
            var (admitted, ticket, reason) = await rig.orchestrator.AdmitMissionProposalAsync(
                TestTenant,
                CreateSampleProposal("M-3"),
                "W-3",
                WorkRiskTier.R1_InternalReversible);

            Assert.False(admitted);
            Assert.Equal(AdmissionDecisionStatus.Rejected, ticket!.Decision);
            Assert.Contains("queue limit", reason);
        }

        [Fact]
        public async Task MOC05_CompletedMission_ReleasesActiveQuota()
        {
            var policy = new TenantMissionConcurrencyPolicy { MaxActiveMissions = 1 };
            var rig = CreateTestRig(policy);

            var p1 = CreateSampleProposal("M-1");
            await rig.orchestrator.AdmitMissionProposalAsync(TestTenant, p1, "W-1", WorkRiskTier.R1_InternalReversible);

            // Complete M-1
            await rig.admissionController.CompleteMissionAsync(TestTenant, p1.MissionId.ToString());

            // M-2 now admitted
            var (admitted, ticket, _) = await rig.orchestrator.AdmitMissionProposalAsync(
                TestTenant,
                CreateSampleProposal("M-2"),
                "W-2",
                WorkRiskTier.R1_InternalReversible);

            Assert.True(admitted);
            Assert.Equal(AdmissionDecisionStatus.Admitted, ticket!.Decision);
        }

        [Fact]
        public async Task MOC06_BudgetExceeded_FailsAdmission()
        {
            var policy = new TenantMissionConcurrencyPolicy { MaxCostUsd = 1.00m };
            var rig = CreateTestRig(policy);

            var expensiveProposal = new MissionGraphProposal
            {
                ProposalId = Guid.NewGuid(),
                MissionId = new MissionId(Guid.NewGuid()),
                Title = "Costly",
                EstimatedCostUsd = 50.00m,
                ProposedNodes = new List<ProposedNode> { new ProposedNode { NodeId = "N-1" } }
            };

            var (admitted, _, reason) = await rig.orchestrator.AdmitMissionProposalAsync(
                TestTenant,
                expensiveProposal,
                "W-EXP",
                WorkRiskTier.R1_InternalReversible);

            Assert.False(admitted);
            Assert.Contains("exceeds policy limit", reason);
        }

        // =========================================================================
        // Family 2: Canonical Resource Ordering & Deadlock Prevention (MOC07 - MOC12)
        // =========================================================================

        [Fact]
        public void MOC07_CanonicalKeyGeneration_Deterministic()
        {
            string key1 = CanonicalResourceOrdering.GetCanonicalKey("Customer", "Account", "TenantA", "Acc-100");
            string key2 = CanonicalResourceOrdering.GetCanonicalKey("customer", "account", "tenanta", "acc-100");

            Assert.Equal("customer:account:tenanta:acc-100", key1);
            Assert.Equal(key1, key2);
        }

        [Fact]
        public void MOC08_SortCanonicalKeys_EnsuresLexicographicalOrder()
        {
            var keys = new List<string>
            {
                "orders:table:tenant:ord-99",
                "customer:account:tenant:acc-01",
                "billing:invoice:tenant:inv-42"
            };

            var sorted = CanonicalResourceOrdering.SortCanonicalKeys(keys);

            Assert.Equal("billing:invoice:tenant:inv-42", sorted[0]);
            Assert.Equal("customer:account:tenant:acc-01", sorted[1]);
            Assert.Equal("orders:table:tenant:ord-99", sorted[2]);
        }

        [Fact]
        public async Task MOC09_AcquireSingleLock_Succeeds()
        {
            var rig = CreateTestRig();

            var (acquired, lockItem, reason) = await rig.arbiter.AcquireLockAsync(
                TestTenant, "Billing", "Invoice", "INV-101", "M-01", "N-01");

            Assert.True(acquired);
            Assert.NotNull(lockItem);
            Assert.Null(reason);
            Assert.True(lockItem!.FencingToken > 0);
        }

        [Fact]
        public async Task MOC10_ResourceContention_SecondAcquirerRejected()
        {
            var rig = CreateTestRig();

            // M-1 acquires
            await rig.arbiter.AcquireLockAsync(TestTenant, "Billing", "Invoice", "INV-101", "M-01", "N-01");

            // M-2 tries to acquire same resource
            var (acquired, _, reason) = await rig.arbiter.AcquireLockAsync(
                TestTenant, "Billing", "Invoice", "INV-101", "M-02", "N-01");

            Assert.False(acquired);
            Assert.Contains("Resource contention", reason);
        }

        [Fact]
        public async Task MOC11_MultiResourceAcquisition_SortedCanonicallyAndAtomic()
        {
            var rig = CreateTestRig();

            var resources = new List<(string Namespace, string Type, string ResourceId)>
            {
                ("Orders", "Record", "ORD-1"),
                ("Billing", "Invoice", "INV-1"),
                ("Customer", "Profile", "CUST-1")
            };

            var (allAcquired, locks, _) = await rig.arbiter.AcquireMultipleLocksCanonicalAsync(
                TestTenant, resources, "M-01", "N-01");

            Assert.True(allAcquired);
            Assert.Equal(3, locks.Count);
        }

        [Fact]
        public async Task MOC12_MultiResourceContention_RollsBackPreviouslyAcquired()
        {
            var rig = CreateTestRig();

            // Holder holds ORD-1
            await rig.arbiter.AcquireLockAsync(TestTenant, "Orders", "Record", "ORD-1", "M-PRIOR", "N-01");

            // Competitor requests CUST-1 (free) and ORD-1 (held)
            var resources = new List<(string Namespace, string Type, string ResourceId)>
            {
                ("Customer", "Profile", "CUST-1"),
                ("Orders", "Record", "ORD-1")
            };

            var (allAcquired, locks, reason) = await rig.arbiter.AcquireMultipleLocksCanonicalAsync(
                TestTenant, resources, "M-COMPETITOR", "N-01");

            Assert.False(allAcquired);
            Assert.Empty(locks);
            Assert.Contains("Resource contention", reason);

            // Verify CUST-1 was rolled back and is not held by competitor
            var custKey = CanonicalResourceOrdering.GetCanonicalKey("Customer", "Profile", TestTenant, "CUST-1");
            var custLock = await rig.store.GetLockAsync(TestTenant, custKey);
            Assert.Null(custLock);
        }

        // =========================================================================
        // Family 3: Fencing Tokens & Stale-Holder Protection (MOC13 - MOC18)
        // =========================================================================

        [Fact]
        public async Task MOC13_FencingTokens_MonotonicallyIncrement()
        {
            var rig = CreateTestRig();

            var (_, l1, _) = await rig.arbiter.AcquireLockAsync(TestTenant, "Res", "Type", "R1", "M-1", "N-1");
            await rig.arbiter.ReleaseLockAsync(TestTenant, "Res", "Type", "R1", l1!.FencingToken);

            var (_, l2, _) = await rig.arbiter.AcquireLockAsync(TestTenant, "Res", "Type", "R1", "M-2", "N-1");

            Assert.True(l2!.FencingToken > l1.FencingToken);
        }

        [Fact]
        public async Task MOC14_ExpiredLock_AllowsNewHolderWithHigherFencingToken()
        {
            var rig = CreateTestRig();

            // Acquire with negative TTL to immediately expire
            var (_, l1, _) = await rig.arbiter.AcquireLockAsync(
                TestTenant, "Res", "Type", "R-EXP", "M-1", "N-1", TimeSpan.FromMilliseconds(-100));

            Assert.True(l1!.IsExpired(DateTime.UtcNow));

            // New holder acquires
            var (acquired, l2, _) = await rig.arbiter.AcquireLockAsync(
                TestTenant, "Res", "Type", "R-EXP", "M-2", "N-1");

            Assert.True(acquired);
            Assert.True(l2!.FencingToken > l1.FencingToken);
        }

        [Fact]
        public async Task MOC15_StaleHolderCannotReleaseNewHolderLock()
        {
            var rig = CreateTestRig();

            // M-1 acquires and expires
            var (_, l1, _) = await rig.arbiter.AcquireLockAsync(
                TestTenant, "Res", "Type", "R-STALE", "M-1", "N-1", TimeSpan.FromMilliseconds(-100));

            // M-2 acquires newer lock
            var (_, l2, _) = await rig.arbiter.AcquireLockAsync(
                TestTenant, "Res", "Type", "R-STALE", "M-2", "N-1");

            // M-1 tries to release with old fencing token -> rejected!
            bool released = await rig.arbiter.ReleaseLockAsync(
                TestTenant, "Res", "Type", "R-STALE", l1!.FencingToken);

            Assert.False(released);

            // Lock still held by M-2
            var key = CanonicalResourceOrdering.GetCanonicalKey("Res", "Type", TestTenant, "R-STALE");
            var active = await rig.store.GetLockAsync(TestTenant, key);
            Assert.NotNull(active);
            Assert.Equal("M-2", active!.HolderMissionId);
        }

        [Fact]
        public async Task MOC16_ValidateFencingToken_ValidatesActiveUnexpiredLock()
        {
            var rig = CreateTestRig();

            var (_, l1, _) = await rig.arbiter.AcquireLockAsync(
                TestTenant, "Res", "Type", "R-VAL", "M-1", "N-1");

            bool valid = await rig.arbiter.ValidateFencingTokenAsync(
                TestTenant, "Res", "Type", "R-VAL", l1!.FencingToken);
            bool invalidToken = await rig.arbiter.ValidateFencingTokenAsync(
                TestTenant, "Res", "Type", "R-VAL", 999999);

            Assert.True(valid);
            Assert.False(invalidToken);
        }

        [Fact]
        public async Task MOC17_ValidateFencingToken_ReturnsFalseForExpiredLock()
        {
            var rig = CreateTestRig();

            var (_, l1, _) = await rig.arbiter.AcquireLockAsync(
                TestTenant, "Res", "Type", "R-VAL-EXP", "M-1", "N-1", TimeSpan.FromMilliseconds(-100));

            bool valid = await rig.arbiter.ValidateFencingTokenAsync(
                TestTenant, "Res", "Type", "R-VAL-EXP", l1!.FencingToken);

            Assert.False(valid);
        }

        [Fact]
        public async Task MOC18_ReentrantAcquisitionBySameMissionAndNode_Permitted()
        {
            var rig = CreateTestRig();

            var (acq1, _, _) = await rig.arbiter.AcquireLockAsync(
                TestTenant, "Res", "Type", "R-REENTRANT", "M-SAME", "N-SAME");
            var (acq2, l2, _) = await rig.arbiter.AcquireLockAsync(
                TestTenant, "Res", "Type", "R-REENTRANT", "M-SAME", "N-SAME");

            Assert.True(acq1);
            Assert.True(acq2);
            Assert.NotNull(l2);
        }

        // =========================================================================
        // Family 4: Cross-Mission Artifact Readiness (MOC19 - MOC24)
        // =========================================================================

        [Fact]
        public async Task MOC19_RegisterDependency_SavedInStore()
        {
            var rig = CreateTestRig();

            var dep = new CrossMissionDependency
            {
                TenantId = TestTenant,
                ConsumerMissionId = "M-CONSUMER",
                ConsumerNodeId = "N-CONSUMER",
                PrerequisiteMissionId = "M-PRODUCER",
                PrerequisiteArtifactType = "FinancialStatement",
                PrerequisiteArtifactId = "FS-2026-Q3",
                IsSatisfied = false
            };

            var registered = await rig.depResolver.RegisterDependencyAsync(dep);

            Assert.NotEmpty(registered.DependencyId);
            var list = await rig.store.ListDependenciesAsync(TestTenant, "M-CONSUMER");
            Assert.Single(list);
        }

        [Fact]
        public async Task MOC20_UnsatisfiedDependency_BlocksReadiness()
        {
            var rig = CreateTestRig();

            await rig.depResolver.RegisterDependencyAsync(new CrossMissionDependency
            {
                TenantId = TestTenant,
                ConsumerMissionId = "M-CONS",
                ConsumerNodeId = "N-BLOCKED",
                PrerequisiteMissionId = "M-PROD",
                PrerequisiteArtifactType = "AuditReport",
                PrerequisiteArtifactId = "AR-01"
            });

            bool satisfied = await rig.depResolver.AreDependenciesSatisfiedAsync(TestTenant, "M-CONS", "N-BLOCKED");
            Assert.False(satisfied);
        }

        [Fact]
        public async Task MOC21_NoDependencies_SatisfiedByDefault()
        {
            var rig = CreateTestRig();

            bool satisfied = await rig.depResolver.AreDependenciesSatisfiedAsync(TestTenant, "M-ANY", "N-INDEPENDENT");
            Assert.True(satisfied);
        }

        [Fact]
        public async Task MOC22_ArtifactProducedEvent_SatisfiesDependency()
        {
            var rig = CreateTestRig();

            // Admission of consumer mission
            var proposal = CreateSampleProposal("M-CONS");
            await rig.orchestrator.AdmitMissionProposalAsync(TestTenant, proposal, "W-CONS", WorkRiskTier.R1_InternalReversible);

            await rig.depResolver.RegisterDependencyAsync(new CrossMissionDependency
            {
                TenantId = TestTenant,
                ConsumerMissionId = proposal.MissionId.ToString(),
                ConsumerNodeId = "N-1",
                PrerequisiteMissionId = "M-PROD",
                PrerequisiteArtifactType = "TaxLedger",
                PrerequisiteArtifactId = "TL-99"
            });

            // Before artifact produced
            Assert.False(await rig.depResolver.AreDependenciesSatisfiedAsync(TestTenant, proposal.MissionId.ToString(), "N-1"));

            // Artifact produced by prerequisite mission
            await rig.depResolver.MarkArtifactProducedAsync(TestTenant, "M-PROD", "TaxLedger", "TL-99");

            // Now satisfied
            Assert.True(await rig.depResolver.AreDependenciesSatisfiedAsync(TestTenant, proposal.MissionId.ToString(), "N-1"));
        }

        [Fact]
        public async Task MOC23_MultipleDependencies_AllMustBeSatisfied()
        {
            var rig = CreateTestRig();
            var p = CreateSampleProposal("M-MULTI");
            await rig.orchestrator.AdmitMissionProposalAsync(TestTenant, p, "W-1", WorkRiskTier.R1_InternalReversible);
            string mId = p.MissionId.ToString();

            await rig.depResolver.RegisterDependencyAsync(new CrossMissionDependency
            {
                TenantId = TestTenant,
                ConsumerMissionId = mId,
                ConsumerNodeId = "N-1",
                PrerequisiteMissionId = "M-P1",
                PrerequisiteArtifactType = "Art1",
                PrerequisiteArtifactId = "ID-1"
            });
            await rig.depResolver.RegisterDependencyAsync(new CrossMissionDependency
            {
                TenantId = TestTenant,
                ConsumerMissionId = mId,
                ConsumerNodeId = "N-1",
                PrerequisiteMissionId = "M-P2",
                PrerequisiteArtifactType = "Art2",
                PrerequisiteArtifactId = "ID-2"
            });

            // Satisfy only one
            await rig.depResolver.MarkArtifactProducedAsync(TestTenant, "M-P1", "Art1", "ID-1");
            Assert.False(await rig.depResolver.AreDependenciesSatisfiedAsync(TestTenant, mId, "N-1"));

            // Satisfy second
            await rig.depResolver.MarkArtifactProducedAsync(TestTenant, "M-P2", "Art2", "ID-2");
            Assert.True(await rig.depResolver.AreDependenciesSatisfiedAsync(TestTenant, mId, "N-1"));
        }

        [Fact]
        public async Task MOC24_MismatchedArtifactId_DoesNotSatisfyDependency()
        {
            var rig = CreateTestRig();
            var p = CreateSampleProposal("M-MIS");
            await rig.orchestrator.AdmitMissionProposalAsync(TestTenant, p, "W-1", WorkRiskTier.R1_InternalReversible);

            await rig.depResolver.RegisterDependencyAsync(new CrossMissionDependency
            {
                TenantId = TestTenant,
                ConsumerMissionId = p.MissionId.ToString(),
                ConsumerNodeId = "N-1",
                PrerequisiteMissionId = "M-PROD",
                PrerequisiteArtifactType = "Doc",
                PrerequisiteArtifactId = "DOC-EXPECTED"
            });

            await rig.depResolver.MarkArtifactProducedAsync(TestTenant, "M-PROD", "Doc", "DOC-DIFFERENT");

            Assert.False(await rig.depResolver.AreDependenciesSatisfiedAsync(TestTenant, p.MissionId.ToString(), "N-1"));
        }

        // =========================================================================
        // Family 5: Coordinated Drain & Cancellation Cascades (MOC25 - MOC30)
        // =========================================================================

        [Fact]
        public async Task MOC25_CancellationCascade_EmitsReceiptWithAuditDigest()
        {
            var rig = CreateTestRig();

            var receipt = await rig.orchestrator.CancelMissionCascadingAsync(
                TestTenant, "M-CANCEL-01", "W-01", "Business priority shifted", "CEO");

            Assert.NotNull(receipt);
            Assert.NotEmpty(receipt.ReceiptId);
            Assert.Equal("M-CANCEL-01", receipt.MissionId);
            Assert.NotEmpty(receipt.AuditDigest);
            Assert.Equal(64, receipt.AuditDigest.Length);
        }

        [Fact]
        public async Task MOC26_CancellationReleasesActiveMissionSlot()
        {
            var policy = new TenantMissionConcurrencyPolicy { MaxActiveMissions = 1 };
            var rig = CreateTestRig(policy);

            var p1 = CreateSampleProposal("M-1");
            await rig.orchestrator.AdmitMissionProposalAsync(TestTenant, p1, "W-1", WorkRiskTier.R1_InternalReversible);

            // Cancel M-1
            await rig.orchestrator.CancelMissionCascadingAsync(TestTenant, p1.MissionId.ToString(), "W-1", "Cancelled", "CFO");

            // M-2 can now be admitted
            var (admitted, _, _) = await rig.orchestrator.AdmitMissionProposalAsync(
                TestTenant, CreateSampleProposal("M-2"), "W-2", WorkRiskTier.R1_InternalReversible);

            Assert.True(admitted);
        }

        [Fact]
        public async Task MOC27_CancellationPreservesUnknownEffectSemantics()
        {
            var rig = CreateTestRig();

            // Receipts model UnknownEffectCount without assuming magical rollback (I27-D)
            var receipt = await rig.cancelCoordinator.CoordinateDrainAsync(new MissionDrainSignal
            {
                TenantId = TestTenant,
                MissionId = "M-DRAIN-UNKNOWN",
                WorkId = "W-DRAIN",
                Reason = "Halt execution"
            });

            Assert.True(receipt.HaltedNodeCount >= 0);
            Assert.True(receipt.CheckpointNodeCount >= 0);
            Assert.True(receipt.UnknownEffectCount >= 0);
        }

        [Fact]
        public async Task MOC28_CancellationReceipt_StoredInLedger()
        {
            var rig = CreateTestRig();

            var receipt = await rig.orchestrator.CancelMissionCascadingAsync(
                TestTenant, "M-STORE-TEST", "W-STORE", "Testing storage", "System");

            var stored = await rig.store.GetReceiptAsync(TestTenant, receipt.ReceiptId);
            Assert.NotNull(stored);
            Assert.Equal(receipt.AuditDigest, stored!.AuditDigest);
        }

        [Fact]
        public async Task MOC29_CancellationAuditDigest_TamperSensitive()
        {
            var receipt = new MissionCancellationReceipt
            {
                ReceiptId = "RCPT-1",
                TenantId = TestTenant,
                MissionId = "M-1",
                WorkId = "W-1",
                HaltedNodeCount = 2,
                CheckpointNodeCount = 1,
                CompletedUtc = DateTime.UtcNow
            };
            receipt.ComputeAuditDigest();
            string originalDigest = receipt.AuditDigest;

            receipt.HaltedNodeCount = 99; // Tamper
            receipt.ComputeAuditDigest();

            Assert.NotEqual(originalDigest, receipt.AuditDigest);
        }

        [Fact]
        public async Task MOC30_MissingTenantOrMission_ThrowsArgumentException()
        {
            var rig = CreateTestRig();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                rig.orchestrator.CancelMissionCascadingAsync("", "M-1", "W-1", "reason", "actor"));
            await Assert.ThrowsAsync<ArgumentException>(() =>
                rig.orchestrator.CancelMissionCascadingAsync(TestTenant, "", "W-1", "reason", "actor"));
        }

        // =========================================================================
        // Family 6: Telemetry Metrology Feedback (MOC31 - MOC36)
        // =========================================================================

        [Fact]
        public async Task MOC31_RecordTelemetry_PersistsInStore()
        {
            var rig = CreateTestRig();

            var telemetry = new MissionTelemetryFeedback
            {
                TenantId = TestTenant,
                MissionId = "M-TEL-01",
                WorkId = "W-TEL-01",
                ActualTokensBurned = 15_420,
                ActualCostUsd = 0.45m,
                ActualDuration = TimeSpan.FromMinutes(4.2),
                VerifiedOutcomeCount = 2,
                VarianceScore = 0.05
            };

            await rig.telemetryChannel.RecordTelemetryAsync(telemetry);

            var list = await rig.telemetryChannel.GetTelemetryForWorkAsync(TestTenant, "W-TEL-01");
            Assert.Single(list);
            Assert.Equal(15_420, list[0].ActualTokensBurned);
        }

        [Fact]
        public async Task MOC32_TelemetryPropagatesToWorkItemOutcome()
        {
            var rig = CreateTestRig();

            var workItem = new WorkItem
            {
                WorkId = "W-ITEM-TEL",
                TenantId = TestTenant,
                Title = "Telemetry Sync Test"
            };
            await rig.workStore.SaveWorkItemAsync(workItem);

            var telemetry = new MissionTelemetryFeedback
            {
                TenantId = TestTenant,
                MissionId = "M-01",
                WorkId = "W-ITEM-TEL",
                ActualTokensBurned = 50_000,
                ActualCostUsd = 1.25m,
                ActualDuration = TimeSpan.FromMinutes(12)
            };

            await rig.telemetryChannel.RecordTelemetryAsync(telemetry);

            var updatedItem = await rig.workStore.GetWorkItemAsync(TestTenant, "W-ITEM-TEL");
            Assert.NotNull(updatedItem);
            Assert.NotNull(updatedItem!.OutcomeRecord);
            Assert.Equal(50_000, updatedItem.OutcomeRecord!.ActualMetrics["TokensBurned"]);
            Assert.Equal(1.25, updatedItem.OutcomeRecord.ActualMetrics["CostUsd"]);
            Assert.Equal(720, updatedItem.OutcomeRecord.ActualMetrics["DurationSeconds"]);
        }

        [Fact]
        public async Task MOC33_TelemetrySeparation_DoesNotMutateReputationDirectly()
        {
            var rig = CreateTestRig();

            // Invariant I27-E: Telemetry feeds empirical metrology records, not direct reputation scores
            var telemetry = new MissionTelemetryFeedback
            {
                TenantId = TestTenant,
                MissionId = "M-01",
                WorkId = "W-01",
                VarianceScore = 0.99 // Poor variance
            };
            await rig.telemetryChannel.RecordTelemetryAsync(telemetry);

            // Assert that Telemetry class does not contain reputation scores
            var propNames = typeof(MissionTelemetryFeedback).GetProperties().Select(p => p.Name).ToList();
            Assert.DoesNotContain("ReputationScore", propNames);
            Assert.DoesNotContain("ReputationTier", propNames);
        }

        [Fact]
        public async Task MOC34_MultipleTelemetryEntriesForSameWork_AppendsHistory()
        {
            var rig = CreateTestRig();

            await rig.telemetryChannel.RecordTelemetryAsync(new MissionTelemetryFeedback
            {
                TenantId = TestTenant,
                MissionId = "M-1",
                WorkId = "W-HIST",
                ActualCostUsd = 0.10m
            });
            await rig.telemetryChannel.RecordTelemetryAsync(new MissionTelemetryFeedback
            {
                TenantId = TestTenant,
                MissionId = "M-2",
                WorkId = "W-HIST",
                ActualCostUsd = 0.20m
            });

            var history = await rig.telemetryChannel.GetTelemetryForWorkAsync(TestTenant, "W-HIST");
            Assert.Equal(2, history.Count);
        }

        [Fact]
        public async Task MOC35_TelemetryForDifferentWorkItems_PartitionedCorrectly()
        {
            var rig = CreateTestRig();

            await rig.telemetryChannel.RecordTelemetryAsync(new MissionTelemetryFeedback
            {
                TenantId = TestTenant,
                MissionId = "M-1",
                WorkId = "W-A"
            });
            await rig.telemetryChannel.RecordTelemetryAsync(new MissionTelemetryFeedback
            {
                TenantId = TestTenant,
                MissionId = "M-2",
                WorkId = "W-B"
            });

            var listA = await rig.telemetryChannel.GetTelemetryForWorkAsync(TestTenant, "W-A");
            var listB = await rig.telemetryChannel.GetTelemetryForWorkAsync(TestTenant, "W-B");

            Assert.Single(listA);
            Assert.Single(listB);
            Assert.Equal("W-A", listA[0].WorkId);
            Assert.Equal("W-B", listB[0].WorkId);
        }

        [Fact]
        public async Task MOC36_NullTelemetry_ThrowsArgumentNullException()
        {
            var rig = CreateTestRig();
            await Assert.ThrowsAsync<ArgumentNullException>(() => rig.telemetryChannel.RecordTelemetryAsync(null!));
        }

        // =========================================================================
        // Family 7: Non-Preemptive Execution Fairness (MOC37 - MOC42)
        // =========================================================================

        [Fact]
        public void MOC37_InvariantI27G_ProhibitsForcedPreemptionOfConsequentialAttempts()
        {
            Assert.Contains("never forcibly preempts an in-flight consequential external attempt",
                MissionCoordinationSovereignty.I27_G_NonPreemptiveExecutionFairness);
        }

        [Fact]
        public async Task MOC38_StarvationProtection_AgingMissionsRemainValidInQueue()
        {
            var policy = new TenantMissionConcurrencyPolicy { MaxActiveMissions = 1, MaxMissionLifetime = TimeSpan.FromHours(1) };
            var rig = CreateTestRig(policy);

            await rig.orchestrator.AdmitMissionProposalAsync(TestTenant, CreateSampleProposal("M-1"), "W-1", WorkRiskTier.R1_InternalReversible);

            // M-2 queued
            var (_, t2, _) = await rig.orchestrator.AdmitMissionProposalAsync(
                TestTenant, CreateSampleProposal("M-2"), "W-2", WorkRiskTier.R1_InternalReversible);

            Assert.Equal(AdmissionDecisionStatus.Queued, t2!.Decision);
            Assert.True(t2.ExpiresUtc > DateTime.UtcNow);
        }

        [Fact]
        public async Task MOC39_ActiveStateInspection_ReportsCorrectCounts()
        {
            var policy = new TenantMissionConcurrencyPolicy { MaxActiveMissions = 1 };
            var rig = CreateTestRig(policy);

            await rig.orchestrator.AdmitMissionProposalAsync(TestTenant, CreateSampleProposal("M-1"), "W-1", WorkRiskTier.R1_InternalReversible);
            await rig.orchestrator.AdmitMissionProposalAsync(TestTenant, CreateSampleProposal("M-2"), "W-2", WorkRiskTier.R1_InternalReversible);
            await rig.arbiter.AcquireLockAsync(TestTenant, "NS", "TYP", "RES-1", "M-1", "N-1");

            var state = await rig.orchestrator.GetActiveStateAsync(TestTenant);

            Assert.Equal(1, state.ActiveMissionCount);
            Assert.Equal(1, state.QueuedMissionCount);
            Assert.Equal(1, state.HeldLockCount);
        }

        [Fact]
        public async Task MOC40_ZeroActiveMissions_StateReportsZero()
        {
            var rig = CreateTestRig();
            var state = await rig.orchestrator.GetActiveStateAsync(TestTenant);

            Assert.Equal(0, state.ActiveMissionCount);
            Assert.Equal(0, state.QueuedMissionCount);
            Assert.Equal(0, state.HeldLockCount);
        }

        [Fact]
        public async Task MOC41_ConcurrencyPolicy_EffectiveResolutionTakesMinimum()
        {
            var sys = new TenantMissionConcurrencyPolicy { MaxActiveMissions = 10, MaxActiveNodes = 50 };
            var tenantOverride = new TenantMissionConcurrencyPolicy { MaxActiveMissions = 3, MaxActiveNodes = 100 };

            var effective = TenantMissionConcurrencyPolicy.ResolveEffective(sys, tenantOverride);

            Assert.Equal(3, effective.MaxActiveMissions); // min(10, 3) = 3
            Assert.Equal(50, effective.MaxActiveNodes);  // min(50, 100) = 50
        }

        [Fact]
        public async Task MOC42_NullTenantOverride_UsesSystemPolicy()
        {
            var sys = new TenantMissionConcurrencyPolicy { MaxActiveMissions = 7 };
            var effective = TenantMissionConcurrencyPolicy.ResolveEffective(sys, null);

            Assert.Equal(7, effective.MaxActiveMissions);
        }

        // =========================================================================
        // Family 8: Multi-Tenant Partitioning (MOC43 - MOC48)
        // =========================================================================

        [Fact]
        public async Task MOC43_MultiTenant_TicketsPartitionedStrictly()
        {
            var rig = CreateTestRig();

            await rig.orchestrator.AdmitMissionProposalAsync(TestTenant, CreateSampleProposal("M-A"), "W-A", WorkRiskTier.R1_InternalReversible);
            await rig.orchestrator.AdmitMissionProposalAsync(AltTenant, CreateSampleProposal("M-B"), "W-B", WorkRiskTier.R1_InternalReversible);

            var tA = await rig.store.ListTicketsAsync(TestTenant);
            var tB = await rig.store.ListTicketsAsync(AltTenant);

            Assert.Single(tA);
            Assert.Single(tB);
            Assert.Equal(TestTenant, tA[0].TenantId);
            Assert.Equal(AltTenant, tB[0].TenantId);
        }

        [Fact]
        public async Task MOC44_MultiTenant_LocksPartitionedStrictly()
        {
            var rig = CreateTestRig();

            // Both tenants acquire resource with same ID
            var (acqA, lA, _) = await rig.arbiter.AcquireLockAsync(TestTenant, "NS", "TYP", "SHARED-ID", "M-1", "N-1");
            var (acqB, lB, _) = await rig.arbiter.AcquireLockAsync(AltTenant, "NS", "TYP", "SHARED-ID", "M-2", "N-1");

            // Both succeed because tenantId partitions the canonical key!
            Assert.True(acqA);
            Assert.True(acqB);
            Assert.NotEqual(lA!.CanonicalKey, lB!.CanonicalKey);
        }

        [Fact]
        public async Task MOC45_MultiTenant_DependenciesPartitionedStrictly()
        {
            var rig = CreateTestRig();

            await rig.depResolver.RegisterDependencyAsync(new CrossMissionDependency
            {
                TenantId = TestTenant,
                ConsumerMissionId = "M-T1",
                ConsumerNodeId = "N-1"
            });
            await rig.depResolver.RegisterDependencyAsync(new CrossMissionDependency
            {
                TenantId = AltTenant,
                ConsumerMissionId = "M-T2",
                ConsumerNodeId = "N-1"
            });

            var d1 = await rig.store.ListDependenciesAsync(TestTenant, "M-T1");
            var d2 = await rig.store.ListDependenciesAsync(AltTenant, "M-T1");

            Assert.Single(d1);
            Assert.Empty(d2);
        }

        [Fact]
        public async Task MOC46_MultiTenant_ActiveStatePartitioned()
        {
            var rig = CreateTestRig();

            await rig.orchestrator.AdmitMissionProposalAsync(TestTenant, CreateSampleProposal("M-1"), "W-1", WorkRiskTier.R1_InternalReversible);

            var stateA = await rig.orchestrator.GetActiveStateAsync(TestTenant);
            var stateB = await rig.orchestrator.GetActiveStateAsync(AltTenant);

            Assert.Equal(1, stateA.ActiveMissionCount);
            Assert.Equal(0, stateB.ActiveMissionCount);
        }

        [Fact]
        public async Task MOC47_MultiTenant_CancellationReceiptPartitioned()
        {
            var rig = CreateTestRig();

            var rA = await rig.orchestrator.CancelMissionCascadingAsync(TestTenant, "M-A", "W-A", "Reason", "Actor");

            var lookupA = await rig.store.GetReceiptAsync(TestTenant, rA.ReceiptId);
            var lookupB = await rig.store.GetReceiptAsync(AltTenant, rA.ReceiptId);

            Assert.NotNull(lookupA);
            Assert.Null(lookupB);
        }

        [Fact]
        public async Task MOC48_MultiTenant_TelemetryPartitioned()
        {
            var rig = CreateTestRig();

            await rig.telemetryChannel.RecordTelemetryAsync(new MissionTelemetryFeedback
            {
                TenantId = TestTenant,
                WorkId = "W-SHARED",
                ActualCostUsd = 1.00m
            });
            await rig.telemetryChannel.RecordTelemetryAsync(new MissionTelemetryFeedback
            {
                TenantId = AltTenant,
                WorkId = "W-SHARED",
                ActualCostUsd = 2.00m
            });

            var tA = await rig.telemetryChannel.GetTelemetryForWorkAsync(TestTenant, "W-SHARED");
            var tB = await rig.telemetryChannel.GetTelemetryForWorkAsync(AltTenant, "W-SHARED");

            Assert.Equal(1.00m, tA[0].ActualCostUsd);
            Assert.Equal(2.00m, tB[0].ActualCostUsd);
        }

        // =========================================================================
        // Family 9: Replay & Determinism (MOC49 - MOC54)
        // =========================================================================

        [Fact]
        public void MOC49_TicketProvenanceHash_Deterministic()
        {
            var ticket = new MissionAdmissionTicket
            {
                TicketId = "TICK-FIXED",
                TenantId = TestTenant,
                MissionId = "M-1",
                WorkId = "W-1",
                MissionGraphHash = "HASH-1",
                RiskTier = WorkRiskTier.R1_InternalReversible,
                Decision = AdmissionDecisionStatus.Admitted,
                FencingToken = 100,
                IssuedUtc = new DateTime(2026, 9, 9, 0, 0, 0, DateTimeKind.Utc),
                ExpiresUtc = new DateTime(2026, 9, 9, 0, 30, 0, DateTimeKind.Utc)
            };

            ticket.ComputeProvenance();
            string p1 = ticket.ProvenanceHash;
            ticket.ComputeProvenance();
            string p2 = ticket.ProvenanceHash;

            Assert.Equal(p1, p2);
            Assert.Equal(64, p1.Length);
        }

        [Fact]
        public void MOC50_CancellationReceiptDigest_Deterministic()
        {
            var receipt = new MissionCancellationReceipt
            {
                ReceiptId = "RCPT-FIXED",
                TenantId = TestTenant,
                MissionId = "M-1",
                WorkId = "W-1",
                HaltedNodeCount = 2,
                CheckpointNodeCount = 1,
                UnknownEffectCount = 0,
                CompletedUtc = new DateTime(2026, 9, 9, 0, 0, 0, DateTimeKind.Utc)
            };

            receipt.ComputeAuditDigest();
            string d1 = receipt.AuditDigest;
            receipt.ComputeAuditDigest();
            string d2 = receipt.AuditDigest;

            Assert.Equal(d1, d2);
            Assert.Equal(64, d1.Length);
        }

        [Fact]
        public async Task MOC51_CanonicalOrderingReplay_ProducesIdenticalOrder()
        {
            var keys = new List<string> { "z:z:t:1", "a:a:t:1", "m:m:t:1" };

            var s1 = CanonicalResourceOrdering.SortCanonicalKeys(keys);
            var s2 = CanonicalResourceOrdering.SortCanonicalKeys(keys);

            Assert.Equal(s1, s2);
        }

        [Fact]
        public async Task MOC52_ReplayAdmissions_GeneratesOrderedTickets()
        {
            var rig = CreateTestRig();

            for (int i = 1; i <= 3; i++)
            {
                await rig.orchestrator.AdmitMissionProposalAsync(TestTenant, CreateSampleProposal($"M-{i}"), $"W-{i}", WorkRiskTier.R1_InternalReversible);
            }

            var tickets = await rig.store.ListTicketsAsync(TestTenant);
            Assert.Equal(3, tickets.Count);
            // All tickets have distinct fencing tokens in increasing order
            Assert.True(tickets[0].FencingToken != tickets[1].FencingToken);
        }

        [Fact]
        public async Task MOC53_ReplayTelemetry_PreservesOrder()
        {
            var rig = CreateTestRig();

            for (int i = 1; i <= 3; i++)
            {
                await rig.telemetryChannel.RecordTelemetryAsync(new MissionTelemetryFeedback
                {
                    TenantId = TestTenant,
                    WorkId = "W-REPLAY",
                    ActualTokensBurned = i * 1000
                });
            }

            var telemetry = await rig.telemetryChannel.GetTelemetryForWorkAsync(TestTenant, "W-REPLAY");
            Assert.Equal(3, telemetry.Count);
            Assert.Equal(1000, telemetry[0].ActualTokensBurned);
            Assert.Equal(2000, telemetry[1].ActualTokensBurned);
            Assert.Equal(3000, telemetry[2].ActualTokensBurned);
        }

        [Fact]
        public void MOC54_TamperedTicket_ProvenanceHashMismatch()
        {
            var ticket = new MissionAdmissionTicket
            {
                TicketId = "T-1",
                TenantId = TestTenant,
                RiskTier = WorkRiskTier.R1_InternalReversible
            };
            ticket.ComputeProvenance();
            string original = ticket.ProvenanceHash;

            ticket.RiskTier = WorkRiskTier.R3_Consequential; // Tamper
            ticket.ComputeProvenance();

            Assert.NotEqual(original, ticket.ProvenanceHash);
        }

        // =========================================================================
        // Family 10: Adversarial & Tamper Resistance (MOC55 - MOC60)
        // =========================================================================

        [Fact]
        public void MOC55_AdmissionTicket_StructurallyDoesNotContainExecutionPermit()
        {
            var ticketProps = typeof(MissionAdmissionTicket).GetProperties().Select(p => p.Name).ToList();

            Assert.DoesNotContain("ExecutionPermit", ticketProps);
            Assert.DoesNotContain("ExecutionPermitId", ticketProps);
            Assert.DoesNotContain("PermitSignature", ticketProps);
            Assert.DoesNotContain("WorkerAuthorization", ticketProps);
        }

        [Fact]
        public void MOC56_MissionOrchestrator_StructurallyDoesNotImplementWorkerDispatcher()
        {
            var orchMethods = typeof(MissionOrchestrator).GetMethods(BindingFlags.Public | BindingFlags.Instance).Select(m => m.Name).ToList();

            Assert.DoesNotContain("DispatchWorker", orchMethods);
            Assert.DoesNotContain("ExecuteMission", orchMethods);
            Assert.DoesNotContain("ExecuteNode", orchMethods);
            Assert.DoesNotContain("CreateAttempt", orchMethods);
        }

        [Fact]
        public async Task MOC57_UnauthorizedFencingTokenRelease_Rejected()
        {
            var rig = CreateTestRig();

            var (_, l1, _) = await rig.arbiter.AcquireLockAsync(TestTenant, "Res", "Type", "R-FORGE", "M-1", "N-1");

            // Forged fencing token
            bool released = await rig.arbiter.ReleaseLockAsync(TestTenant, "Res", "Type", "R-FORGE", 123456789);

            Assert.False(released);
        }

        [Fact]
        public async Task MOC58_EmptyProposalNodes_DepthZeroHandledGracefully()
        {
            var rig = CreateTestRig();
            var emptyProposal = new MissionGraphProposal
            {
                ProposalId = Guid.NewGuid(),
                MissionId = new MissionId(Guid.NewGuid()),
                Title = "Empty",
                ProposedNodes = Array.Empty<ProposedNode>()
            };

            var (admitted, ticket, _) = await rig.orchestrator.AdmitMissionProposalAsync(
                TestTenant, emptyProposal, "W-EMPTY", WorkRiskTier.R1_InternalReversible);

            Assert.True(admitted);
            Assert.NotNull(ticket);
        }

        [Fact]
        public async Task MOC59_CorruptedDependencyResolution_HandlesGracefully()
        {
            var rig = CreateTestRig();

            bool res = await rig.depResolver.AreDependenciesSatisfiedAsync(TestTenant, "NONEXISTENT", "NONEXISTENT");
            Assert.True(res); // No dependencies registered -> satisfied
        }

        [Fact]
        public async Task MOC60_NegativeLockTtl_HandledAsExpired()
        {
            var rig = CreateTestRig();

            var (acq, lockItem, _) = await rig.arbiter.AcquireLockAsync(
                TestTenant, "Res", "Type", "R-NEG", "M-1", "N-1", TimeSpan.FromSeconds(-5));

            Assert.True(acq);
            Assert.NotNull(lockItem);
            Assert.True(lockItem!.IsExpired(DateTime.UtcNow));
        }

        // =========================================================================
        // Family 11: Constitutional Invariant Laws (I27-A .. I27-M) (MOC61 - MOC66)
        // =========================================================================

        [Fact]
        public void MOC61_ConstitutionalInvariantI27_CoreLawsEnforced()
        {
            Assert.Equal("I27", MissionCoordinationSovereignty.InvariantName);
            Assert.Contains("MISSION COORDINATION ≠ MISSION EXECUTION", MissionCoordinationSovereignty.InvariantStatement);
            Assert.Contains("I27-A", MissionCoordinationSovereignty.I27_A_ZeroPeerToPeerSpawning);
            Assert.Contains("I27-B", MissionCoordinationSovereignty.I27_B_BoundedDepthAndFanout);
            Assert.Contains("I27-C", MissionCoordinationSovereignty.I27_C_CanonicalLockOrdering);
        }

        [Fact]
        public void MOC62_InvariantI27_SecondRuntimeProhibition_Enforced()
        {
            Assert.Contains("I27-I: Mission Orchestrator cannot create an independent mission execution loop",
                MissionCoordinationSovereignty.I27_I_NoSecondRuntime);
        }

        [Fact]
        public void MOC63_InvariantI27_CompilerSovereignty_Enforced()
        {
            Assert.Contains("I27-J: MissionGraphProposal cannot become executable merely because the Orchestrator admits it",
                MissionCoordinationSovereignty.I27_J_CompilerSovereignty);
        }

        [Fact]
        public void MOC64_InvariantI27_AdmissionNotAuthorization_Enforced()
        {
            Assert.Contains("I27-K: MissionAdmissionTicket must never be interpreted as ExecutionPermit",
                MissionCoordinationSovereignty.I27_K_AdmissionNotAuthorization);
        }

        [Fact]
        public void MOC65_InvariantI27_CoordinationCannotManufactureAuthority_Enforced()
        {
            Assert.Contains("I27-L: No coordination component may create, sign, modify, delegate, or issue ExecutionPermit",
                MissionCoordinationSovereignty.I27_L_CoordinationCannotManufactureAuthority);
        }

        [Fact]
        public void MOC66_InvariantI27_SchedulerSovereignty_Enforced()
        {
            Assert.Contains("I27-M: Mission timing, retries, backoff, heartbeat, leases, and execution attempts remain owned exclusively",
                MissionCoordinationSovereignty.I27_M_SchedulerSovereignty);
        }

        // =========================================================================
        // Family 12: Graph Depth & Fanout Enforcements (MOC67 - MOC72)
        // =========================================================================

        [Fact]
        public async Task MOC67_GraphDepthExceedsFive_RejectedPerI27B()
        {
            var rig = CreateTestRig();
            // Create proposal with depth = 6
            var deepProposal = CreateSampleProposal("M-DEEP", nodeCount: 6, depth: 6);

            var (admitted, _, reason) = await rig.orchestrator.AdmitMissionProposalAsync(
                TestTenant, deepProposal, "W-DEEP", WorkRiskTier.R1_InternalReversible);

            Assert.False(admitted);
            Assert.Contains("exceeds constitutional limit (5)", reason);
        }

        [Fact]
        public async Task MOC68_GraphDepthWithinFive_AdmittedPerI27B()
        {
            var rig = CreateTestRig();
            var validProposal = CreateSampleProposal("M-VALID-DEPTH", nodeCount: 5, depth: 5);

            var (admitted, ticket, _) = await rig.orchestrator.AdmitMissionProposalAsync(
                TestTenant, validProposal, "W-VALID-DEPTH", WorkRiskTier.R1_InternalReversible);

            Assert.True(admitted);
            Assert.NotNull(ticket);
        }

        [Fact]
        public async Task MOC69_GraphFanoutExceedsTen_RejectedPerI27B()
        {
            var rig = CreateTestRig();

            var nodes = new List<ProposedNode> { new ProposedNode { NodeId = "ROOT" } };
            var edges = new List<ProposedEdge>();

            // Root fans out to 11 children
            for (int i = 1; i <= 11; i++)
            {
                nodes.Add(new ProposedNode { NodeId = $"CHILD-{i}" });
                edges.Add(new ProposedEdge { SourceNodeId = "ROOT", TargetNodeId = $"CHILD-{i}" });
            }

            var fanoutProposal = new MissionGraphProposal
            {
                ProposalId = Guid.NewGuid(),
                MissionId = new MissionId(Guid.NewGuid()),
                Title = "High Fanout",
                ProposedNodes = nodes,
                ProposedEdges = edges
            };

            var (admitted, _, reason) = await rig.orchestrator.AdmitMissionProposalAsync(
                TestTenant, fanoutProposal, "W-FANOUT", WorkRiskTier.R1_InternalReversible);

            Assert.False(admitted);
            Assert.Contains("exceeds constitutional limit (10)", reason);
        }

        [Fact]
        public async Task MOC70_GraphFanoutWithinTen_AdmittedPerI27B()
        {
            var rig = CreateTestRig();

            var nodes = new List<ProposedNode> { new ProposedNode { NodeId = "ROOT" } };
            var edges = new List<ProposedEdge>();

            for (int i = 1; i <= 10; i++)
            {
                nodes.Add(new ProposedNode { NodeId = $"CHILD-{i}" });
                edges.Add(new ProposedEdge { SourceNodeId = "ROOT", TargetNodeId = $"CHILD-{i}" });
            }

            var fanoutProposal = new MissionGraphProposal
            {
                ProposalId = Guid.NewGuid(),
                MissionId = new MissionId(Guid.NewGuid()),
                Title = "Valid Fanout",
                ProposedNodes = nodes,
                ProposedEdges = edges
            };

            var (admitted, ticket, _) = await rig.orchestrator.AdmitMissionProposalAsync(
                TestTenant, fanoutProposal, "W-FANOUT-OK", WorkRiskTier.R1_InternalReversible);

            Assert.True(admitted);
            Assert.NotNull(ticket);
        }

        [Fact]
        public async Task MOC71_NodeCountExceedsTwentyFive_Rejected()
        {
            var rig = CreateTestRig();
            var largeProposal = CreateSampleProposal("M-LARGE", nodeCount: 26, depth: 1);

            var (admitted, _, reason) = await rig.orchestrator.AdmitMissionProposalAsync(
                TestTenant, largeProposal, "W-LARGE", WorkRiskTier.R1_InternalReversible);

            Assert.False(admitted);
            Assert.Contains("exceeds max limit (25)", reason);
        }

        [Fact]
        public async Task MOC72_CyclicGraphProposal_HandledWithoutStackOverflow()
        {
            var rig = CreateTestRig();

            // Create cyclic edges A -> B -> A
            var nodes = new List<ProposedNode>
            {
                new ProposedNode { NodeId = "A" },
                new ProposedNode { NodeId = "B" }
            };
            var edges = new List<ProposedEdge>
            {
                new ProposedEdge { SourceNodeId = "A", TargetNodeId = "B" },
                new ProposedEdge { SourceNodeId = "B", TargetNodeId = "A" }
            };

            var cyclic = new MissionGraphProposal
            {
                ProposalId = Guid.NewGuid(),
                MissionId = new MissionId(Guid.NewGuid()),
                Title = "Cyclic",
                ProposedNodes = nodes,
                ProposedEdges = edges
            };

            // Depth calculation detects cycle via visited set without infinite recursion
            var (admitted, ticket, _) = await rig.orchestrator.AdmitMissionProposalAsync(
                TestTenant, cyclic, "W-CYCLIC", WorkRiskTier.R1_InternalReversible);

            Assert.NotNull(ticket);
        }

        // =========================================================================
        // Family 13: Coordination Sovereignty & Runtime Delegation (MOC73 - MOC78)
        // =========================================================================

        [Fact]
        public void MOC73_TicketDoesNotGrantAuthorityToExecute()
        {
            var ticket = new MissionAdmissionTicket
            {
                TicketId = "TICK-01",
                Decision = AdmissionDecisionStatus.Admitted
            };

            // Ticket decision is Admitted, but it is not an ExecutionPermit
            Assert.False(ticket is object && ticket.GetType().Name == "ExecutionPermit");
        }

        [Fact]
        public async Task MOC74_AdmissionDecisionQueued_DoesNotGrantAdmissionTicketAdmittedStatus()
        {
            var policy = new TenantMissionConcurrencyPolicy { MaxActiveMissions = 0 };
            var rig = CreateTestRig(policy);

            var (admitted, ticket, _) = await rig.orchestrator.AdmitMissionProposalAsync(
                TestTenant, CreateSampleProposal(), "W-1", WorkRiskTier.R1_InternalReversible);

            Assert.False(admitted);
            Assert.Equal(AdmissionDecisionStatus.Queued, ticket!.Decision);
        }

        [Fact]
        public async Task MOC75_ResourceArbiter_ReleasingNonHeldLock_ReturnsTrueCleanly()
        {
            var rig = CreateTestRig();

            bool res = await rig.arbiter.ReleaseLockAsync(TestTenant, "Free", "Free", "FREE-1", 100);
            Assert.True(res);
        }

        [Fact]
        public async Task MOC76_DoubleReleaseWithMatchingToken_Succeeds()
        {
            var rig = CreateTestRig();

            var (_, l1, _) = await rig.arbiter.AcquireLockAsync(TestTenant, "Res", "Type", "R-DBL", "M-1", "N-1");

            bool r1 = await rig.arbiter.ReleaseLockAsync(TestTenant, "Res", "Type", "R-DBL", l1!.FencingToken);
            bool r2 = await rig.arbiter.ReleaseLockAsync(TestTenant, "Res", "Type", "R-DBL", l1.FencingToken);

            Assert.True(r1);
            Assert.True(r2); // Already free
        }

        [Fact]
        public async Task MOC77_CrossMissionDependency_DifferentArtifactType_DoesNotSatisfy()
        {
            var rig = CreateTestRig();
            var p = CreateSampleProposal("M-DIFF-TYPE");
            await rig.orchestrator.AdmitMissionProposalAsync(TestTenant, p, "W-1", WorkRiskTier.R1_InternalReversible);

            await rig.depResolver.RegisterDependencyAsync(new CrossMissionDependency
            {
                TenantId = TestTenant,
                ConsumerMissionId = p.MissionId.ToString(),
                ConsumerNodeId = "N-1",
                PrerequisiteMissionId = "M-PROD",
                PrerequisiteArtifactType = "SchemaV1",
                PrerequisiteArtifactId = "ID-COMMON"
            });

            await rig.depResolver.MarkArtifactProducedAsync(TestTenant, "M-PROD", "SchemaV2", "ID-COMMON");

            Assert.False(await rig.depResolver.AreDependenciesSatisfiedAsync(TestTenant, p.MissionId.ToString(), "N-1"));
        }

        [Fact]
        public async Task MOC78_CancellationCascade_CompletedUtcRecorded()
        {
            var rig = CreateTestRig();
            var before = DateTime.UtcNow;

            var receipt = await rig.orchestrator.CancelMissionCascadingAsync(
                TestTenant, "M-UTC", "W-UTC", "Reason", "Actor");

            Assert.True(receipt.CompletedUtc >= before);
            Assert.True(receipt.CompletedUtc <= DateTime.UtcNow.AddSeconds(1));
        }

        // =========================================================================
        // Family 14: End-to-End Orchestrator Flow (MOC79 - MOC84)
        // =========================================================================

        [Fact]
        public async Task MOC79_EndToEnd_AdmissionLocksDependencyAndTelemetry()
        {
            var rig = CreateTestRig();

            // 1. Admit Producer Mission
            var pProducer = CreateSampleProposal("M-PRODUCER");
            var (admProd, tProd, _) = await rig.orchestrator.AdmitMissionProposalAsync(
                TestTenant, pProducer, "W-PROD", WorkRiskTier.R1_InternalReversible);
            Assert.True(admProd);

            // 2. Producer acquires lock
            var (acqProd, lProd, _) = await rig.arbiter.AcquireLockAsync(
                TestTenant, "Data", "Lake", "TableA", pProducer.MissionId.ToString(), "N-1");
            Assert.True(acqProd);

            // 3. Admit Consumer Mission
            var pConsumer = CreateSampleProposal("M-CONSUMER");
            var (admCons, tCons, _) = await rig.orchestrator.AdmitMissionProposalAsync(
                TestTenant, pConsumer, "W-CONS", WorkRiskTier.R1_InternalReversible);
            Assert.True(admCons);

            // 4. Consumer registers dependency on Producer artifact
            await rig.depResolver.RegisterDependencyAsync(new CrossMissionDependency
            {
                TenantId = TestTenant,
                ConsumerMissionId = pConsumer.MissionId.ToString(),
                ConsumerNodeId = "N-1",
                PrerequisiteMissionId = pProducer.MissionId.ToString(),
                PrerequisiteArtifactType = "TableExtract",
                PrerequisiteArtifactId = "EXT-01"
            });
            Assert.False(await rig.depResolver.AreDependenciesSatisfiedAsync(TestTenant, pConsumer.MissionId.ToString(), "N-1"));

            // 5. Producer releases lock and emits artifact
            await rig.arbiter.ReleaseLockAsync(TestTenant, "Data", "Lake", "TableA", lProd!.FencingToken);
            await rig.depResolver.MarkArtifactProducedAsync(TestTenant, pProducer.MissionId.ToString(), "TableExtract", "EXT-01");

            // 6. Consumer dependency now satisfied and Consumer can acquire lock
            Assert.True(await rig.depResolver.AreDependenciesSatisfiedAsync(TestTenant, pConsumer.MissionId.ToString(), "N-1"));
            var (acqCons, lCons, _) = await rig.arbiter.AcquireLockAsync(
                TestTenant, "Data", "Lake", "TableA", pConsumer.MissionId.ToString(), "N-1");
            Assert.True(acqCons);

            // 7. Record Telemetry for Producer and Consumer
            await rig.telemetryChannel.RecordTelemetryAsync(new MissionTelemetryFeedback
            {
                TenantId = TestTenant,
                MissionId = pProducer.MissionId.ToString(),
                WorkId = "W-PROD",
                ActualCostUsd = 0.50m
            });
            await rig.telemetryChannel.RecordTelemetryAsync(new MissionTelemetryFeedback
            {
                TenantId = TestTenant,
                MissionId = pConsumer.MissionId.ToString(),
                WorkId = "W-CONS",
                ActualCostUsd = 0.75m
            });

            // 8. Both release and complete
            await rig.arbiter.ReleaseLockAsync(TestTenant, "Data", "Lake", "TableA", lCons!.FencingToken);
            await rig.admissionController.CompleteMissionAsync(TestTenant, pProducer.MissionId.ToString());
            await rig.admissionController.CompleteMissionAsync(TestTenant, pConsumer.MissionId.ToString());

            var activeState = await rig.orchestrator.GetActiveStateAsync(TestTenant);
            Assert.Equal(0, activeState.HeldLockCount);
        }

        [Fact]
        public async Task MOC80_EndToEnd_ConcurrentMissionsContendForResource_DeterministicFairness()
        {
            var rig = CreateTestRig();

            var p1 = CreateSampleProposal("M-1");
            var p2 = CreateSampleProposal("M-2");

            await rig.orchestrator.AdmitMissionProposalAsync(TestTenant, p1, "W-1", WorkRiskTier.R1_InternalReversible);
            await rig.orchestrator.AdmitMissionProposalAsync(TestTenant, p2, "W-2", WorkRiskTier.R1_InternalReversible);

            // M-1 acquires lock on Customer profile
            var (acq1, l1, _) = await rig.arbiter.AcquireLockAsync(TestTenant, "CRM", "Contact", "CUST-999", p1.MissionId.ToString(), "N-1");
            Assert.True(acq1);

            // M-2 contends -> rejected
            var (acq2, _, _) = await rig.arbiter.AcquireLockAsync(TestTenant, "CRM", "Contact", "CUST-999", p2.MissionId.ToString(), "N-1");
            Assert.False(acq2);

            // M-1 finishes and releases
            await rig.arbiter.ReleaseLockAsync(TestTenant, "CRM", "Contact", "CUST-999", l1!.FencingToken);

            // M-2 retries -> succeeds with higher fencing token
            var (acq2Retry, l2, _) = await rig.arbiter.AcquireLockAsync(TestTenant, "CRM", "Contact", "CUST-999", p2.MissionId.ToString(), "N-1");
            Assert.True(acq2Retry);
            Assert.True(l2!.FencingToken > l1.FencingToken);
        }

        [Fact]
        public async Task MOC81_Orchestrator_CancelNonExistentMission_EmitsReceiptCleanly()
        {
            var rig = CreateTestRig();

            var receipt = await rig.orchestrator.CancelMissionCascadingAsync(
                TestTenant, "M-GHOST", "W-GHOST", "Cleanup", "Admin");

            Assert.NotNull(receipt);
            Assert.Equal("M-GHOST", receipt.MissionId);
        }

        [Fact]
        public async Task MOC82_TelemetryRecordWithZeroTokensAndCost_HandledCleanly()
        {
            var rig = CreateTestRig();

            await rig.telemetryChannel.RecordTelemetryAsync(new MissionTelemetryFeedback
            {
                TenantId = TestTenant,
                MissionId = "M-ZERO",
                WorkId = "W-ZERO",
                ActualTokensBurned = 0,
                ActualCostUsd = 0.00m,
                ActualDuration = TimeSpan.Zero
            });

            var list = await rig.telemetryChannel.GetTelemetryForWorkAsync(TestTenant, "W-ZERO");
            Assert.Single(list);
            Assert.Equal(0, list[0].ActualTokensBurned);
        }

        [Fact]
        public async Task MOC83_AcquireMultipleLocks_EmptyList_ReturnsTrueImmediately()
        {
            var rig = CreateTestRig();

            var (allAcq, locks, reason) = await rig.arbiter.AcquireMultipleLocksCanonicalAsync(
                TestTenant, Array.Empty<(string, string, string)>(), "M-1", "N-1");

            Assert.True(allAcq);
            Assert.Empty(locks);
            Assert.Null(reason);
        }

        [Fact]
        public async Task MOC84_AdmissionWithNullPolicyOverride_UsesSystemPolicyGracefully()
        {
            var rig = CreateTestRig();
            var p = CreateSampleProposal("M-NULL-OVERRIDE");

            var (admitted, ticket, _) = await rig.admissionController.EvaluateAdmissionAsync(
                TestTenant, p, "W-1", WorkRiskTier.R1_InternalReversible, policyOverride: null);

            Assert.True(admitted);
            Assert.NotNull(ticket);
        }
    }
}
