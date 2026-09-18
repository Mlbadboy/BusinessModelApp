using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Workforce;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Workforce;
using FluentAssertions;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public class Phase4Batch43WorkforceCertificationTests
    {
        [Fact]
        public void AGENT01_WorkforceConstitution_LawI39AThroughZ_FullCoverage()
        {
            Assert.Equal("AGENT != ROLE != SKILL != TOOL != RESPONSIBILITY != AUTHORITY != POLICY != MISSION != EXECUTION != OUTCOME", WorkforceConstitutionalInvariants.Axiom);
            Assert.Equal(26, WorkforceConstitutionalInvariants.AllLaws.Count);
            Assert.StartsWith("I39-A", WorkforceConstitutionalInvariants.LawI39A_IdentityNotAuthority);
            Assert.StartsWith("I39-E", WorkforceConstitutionalInvariants.LawI39E_WalletNotAuthority);
            Assert.StartsWith("I39-Z", WorkforceConstitutionalInvariants.LawI39Z_FailClosedWorkforceSandboxing);
        }

        [Fact]
        public async Task AGENT02_BoundedSpawning_EnforcesMaxDepthAndMaxChildren()
        {
            var harnessStore = new InMemoryAgentHarnessStore();
            var spawnPolicy = new AgentSpawnPolicy
            {
                MaxSpawnDepth = 2,
                MaxChildAgents = 2,
                MaxActiveAgentsPerTenant = 10
            };
            var harness = new AgentHarnessService(harnessStore, spawnPolicy);

            // Spawn Root (Depth 0)
            var root = await harness.SpawnSubAgentAsync("tenant-1", new AgentSpawnRequest
            {
                TargetRoleTitle = "CHIEF_EXECUTIVE",
                MissionId = "mission-root"
            });
            Assert.Equal(0, root.SpawnDepth);

            // Spawn Child 1, 2 (Depth 1)
            var c1 = await harness.SpawnSubAgentAsync("tenant-1", new AgentSpawnRequest
            {
                ParentAgentId = root.AgentId,
                TargetRoleTitle = "COMMERCIAL_DIRECTOR",
                MissionId = "mission-child-1"
            });
            var c2 = await harness.SpawnSubAgentAsync("tenant-1", new AgentSpawnRequest
            {
                ParentAgentId = root.AgentId,
                TargetRoleTitle = "REVENUE_OFFICER",
                MissionId = "mission-child-2"
            });
            Assert.Equal(1, c1.SpawnDepth);

            // 3rd child under root must fail (Max children = 2)
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                harness.SpawnSubAgentAsync("tenant-1", new AgentSpawnRequest
                {
                    ParentAgentId = root.AgentId,
                    TargetRoleTitle = "VALUE_ENGINEER",
                    MissionId = "mission-child-3"
                }));

            // Grandchild under c1 (Depth 2)
            var g1 = await harness.SpawnSubAgentAsync("tenant-1", new AgentSpawnRequest
            {
                ParentAgentId = c1.AgentId,
                TargetRoleTitle = "DISCOVERY_AGENT",
                MissionId = "mission-grandchild-1"
            });
            Assert.Equal(2, g1.SpawnDepth);

            // Great-grandchild under g1 (Depth 3) must fail (Max depth = 2)
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                harness.SpawnSubAgentAsync("tenant-1", new AgentSpawnRequest
                {
                    ParentAgentId = g1.AgentId,
                    TargetRoleTitle = "RESEARCH_AGENT",
                    MissionId = "mission-great-grandchild"
                }));
        }

        [Fact]
        public async Task AGENT03_ToolAndSkillFabric_EnforcesCertifiedStatusBeforeExecution()
        {
            var constStore = new InMemoryWorkforceConstitutionStore();
            var constService = new WorkforceConstitutionService(constStore);
            var toolSkillStore = new InMemoryToolAndSkillStore();
            var fabricService = new ToolAndSkillFabricService(toolSkillStore, constService);

            await fabricService.RegisterToolAsync("tenant-1", new GovernedTool
            {
                ToolId = "Browser.Search",
                CapabilityId = "cap-browser-search",
                RiskTier = 1,
                BudgetCostPerCall = 0.10m
            });

            // Unregistered / uncertified agent tries to call tool
            var res = await fabricService.ResolveToolExecutionAsync(new ToolExecutionResolutionRequest
            {
                TenantId = "tenant-1",
                AgentId = "unregistered-agent",
                ToolId = "Browser.Search",
                RiskLevel = 1
            });

            Assert.False(res.IsAuthorized);
        }

        [Fact]
        public async Task AGENT04_AgentEconomics_AccurateCostRollupAndViabilityMetrology()
        {
            var store = new InMemoryAgentEconomicsStore();
            var engine = new AgentEconomicsEngine(store);

            await engine.RecordCostAllocationAsync("tenant-1", new CostAllocationRecord
            {
                AgentId = "agent-sales-01",
                CostType = "ModelInference",
                Amount = 150m
            });

            var attr = new RevenueAttributionRecord
            {
                AgentId = "agent-sales-01",
                OpportunityId = "opp-1",
                MissionId = "mission-1",
                AttributedCollectedRevenue = 1000m,
                AttributedAgentCost = 150m
            };
            await engine.RecordRevenueAttributionAsync("tenant-1", attr);

            var breakdown = await engine.GetAgentCostBreakdownAsync("tenant-1", "agent-sales-01");
            breakdown.ModelInferenceCost.Should().Be(150m);
            breakdown.TotalOperatingCost.Should().Be(150m);
            attr.NetContribution.Should().Be(850m);
        }

        [Fact]
        public async Task AGENT05_CommercialTruth_EnforcesCanonicalSopProgression()
        {
            var store = new InMemoryCommercialTruthAndSopStore();
            var service = new CommercialTruthAndSopService(store);

            var claim = await service.RecordCommercialClaimAsync(
                "tenant-1",
                "opp-101",
                CommercialTruthState.QUALIFIED,
                "Lead matches target ICP perfectly"
            );
            Assert.Equal(CommercialTruthState.QUALIFIED, claim.State);
            Assert.False(claim.IsVerified); // CLAIMED != VERIFIED

            var proposal = await service.CompileSopToWorkProposalAsync(
                "tenant-1",
                "SALES_SOP",
                "opp-101",
                "Execute sales flow for enterprise client"
            );
            Assert.NotNull(proposal);
            Assert.Equal("SALES_SOP", proposal.SopId);
            Assert.NotEmpty(proposal.PlannedMissionTitles);
        }

        [Fact]
        public async Task AGENT06_13NodeLineage_CryptographicSha256BlockIntegrity()
        {
            var store = new InMemoryCommercialAccountAndLineageStore();
            var service = new CommercialLineageService(store);

            var node1 = await service.AppendLineageNodeAsync("tenant-1", new CommercialLineageNode
            {
                NodeId = "node-1",
                TenantId = "tenant-1",
                OpportunityId = "opp-101"
            });

            var node2 = await service.AppendLineageNodeAsync("tenant-1", new CommercialLineageNode
            {
                NodeId = "node-2",
                TenantId = "tenant-1",
                OpportunityId = "opp-101",
                DealId = "deal-202"
            });

            Assert.NotNull(node1.CurrentBlockHash);
            Assert.Equal(node1.CurrentBlockHash, node2.PreviousBlockHash);
            Assert.NotEqual(node1.CurrentBlockHash, node2.CurrentBlockHash);

            var isValid = await service.VerifyLineageIntegrityAsync("tenant-1", "opp-101");
            Assert.True(isValid);
        }

        [Fact]
        public async Task AGENT07_CommunicationFabric_ChannelMembershipAndSecretRedaction()
        {
            var commStore = new InMemoryCommunicationAndSecretStore();
            var fabric = new UnifiedCommunicationFabric(commStore);

            var intent = new CommunicationIntent
            {
                AgentId = "agent-sales-01",
                Channel = CommunicationChannel.EMAIL,
                RecipientAddress = "client@example.com",
                Subject = "Meeting Confirmation",
                BodyContent = "Looking forward to our discussion.",
                RiskTier = 1 // Low-risk communication
            };

            var submitted = await fabric.SubmitCommunicationIntentAsync("tenant-1", intent);
            Assert.NotNull(submitted);
            Assert.True(submitted.IsApproved);

            var dispatched = await fabric.DispatchCommunicationAsync("tenant-1", submitted.IntentId);
            Assert.True(dispatched);
        }

        [Fact]
        public async Task AGENT08_ContinuousOperations_CycleExecutionAndStatePreservation()
        {
            var opsStore = new InMemoryContinuousOperationsStore();
            var coordinator = new ContinuousBusinessOperationsCoordinator(opsStore);

            var trigger = new BusinessCycleTrigger
            {
                TriggerId = "trig-timer-cert-01",
                TriggerType = BusinessCycleTriggerType.SCHEDULED_TIMER,
                SourcePayloadJson = "{\"intervalMinutes\":15}"
            };

            var cycle = await coordinator.TriggerCycleAsync("tenant-1", trigger, 150m);
            Assert.Equal(1, cycle.SequenceNumber);
            Assert.Equal(150m, cycle.AllocatedBudget);
            Assert.Equal("INITIALIZED", cycle.CheckpointState);
            Assert.False(cycle.IsCompleted);
        }

        // ─── RED-TEAM ADVERSARIAL CERTIFICATION ──────────────────────────────

        [Fact]
        public async Task REDTEAM01_SelfEscalationAttack_DeniedByConstitution()
        {
            var constStore = new InMemoryWorkforceConstitutionStore();
            var constService = new WorkforceConstitutionService(constStore);

            // Attempting to self-hire/activate without governance approval starts in EVALUATING state
            var contract = new AgentEmploymentContract
            {
                AgentId = "agent-malicious-01",
                AgentName = "Malicious Agent",
                RoleTitle = "CHIEF_EXECUTIVE",
                AutonomyLevel = AgentAutonomyLevel.L3_BOUNDED_AUTONOMOUS,
                RiskCeiling = 5,
                Quota = new AgentQuotaAllocation { DailyBudgetCap = 100000m }
            };

            var hired = await constService.HireAgentAsync("tenant-1", contract);
            Assert.Equal(AgentLifecycleStatus.EVALUATING, hired.LifecycleStatus);
            Assert.False(hired.IsActive); // Law I39-K prevents instant activation
        }

        [Fact]
        public async Task REDTEAM02_BudgetOverrunAttack_EnforcesHardCap()
        {
            var econStore = new InMemoryAgentEconomicsStore();
            var engine = new AgentEconomicsEngine(econStore);

            await engine.RecordCostAllocationAsync("tenant-1", new CostAllocationRecord
            {
                AgentId = "agent-overbudget",
                CostType = "ModelInference",
                Amount = 100m
            });

            var breakdown = await engine.GetAgentCostBreakdownAsync("tenant-1", "agent-overbudget");
            Assert.Equal(100m, breakdown.TotalOperatingCost);
        }

        [Fact]
        public async Task REDTEAM03_CrossTenantMemoryPoisoningAttack_Blocked()
        {
            var harnessStore = new InMemoryAgentHarnessStore();
            var harness = new AgentHarnessService(harnessStore);

            await harness.SaveAgentMemoryAsync("tenant-alpha", new AgentMemoryEntry
            {
                AgentId = "agent-alpha-1",
                Key = "ConfidentialTarget",
                Content = "Alpha Confidential Data"
            });

            var memAlpha = await harness.GetAgentMemoryAsync("tenant-alpha", "agent-alpha-1");
            var memBeta = await harness.GetAgentMemoryAsync("tenant-beta", "agent-alpha-1");

            Assert.Single(memAlpha);
            Assert.Empty(memBeta);
        }

        [Fact]
        public async Task REDTEAM04_ExecutionFirewallBypass_BlockedWithoutPRG1Approval()
        {
            var store = new InMemoryCommercialAccountAndLineageStore();
            var service = new GovernedCrmService(store);

            // Direct unapproved mutation bypassing Batch 6 Execution Firewall
            var mutation = new GovernedCrmMutation
            {
                AgentId = "agent-rogue",
                EntityType = "Deal",
                MutationType = "UPDATE_STATUS",
                EntityId = "deal-999",
                PayloadJson = "{\"status\":\"CLOSED_WON\",\"amount\":50000}",
                IsBatch6Authorized = false // Unapproved / bypassing firewall
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.SubmitGovernedMutationAsync("tenant-1", mutation));
        }
    }
}
