using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Domain.Runtime.Organizational;
using BusinessModelApp.Core.Interfaces.Runtime;
using BusinessModelApp.Infrastructure.Runtime;
using BusinessModelApp.Infrastructure.Runtime.Organizational;
using BusinessModelApp.Infrastructure.Runtime.Reality;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public class Phase3Batch391SchedulerIntegrationTests
    {
        private const string TestTenant = "Tenant-Sched-Test";
        private readonly Guid _workspaceId = Guid.NewGuid();

        private (
            EnterpriseSchedulerEngine scheduler,
            AutonomousWorkManager manager,
            InMemoryOrganizationalWorkStore workStore,
            InMemoryWorkManagerRunStore runStore
        ) CreateTestRig()
        {
            var workStore = new InMemoryOrganizationalWorkStore();
            var runStore = new InMemoryWorkManagerRunStore();
            var priorityPolicy = new WorkPriorityPolicy();
            var schedulingPolicy = new PortfolioSchedulingPolicy();

            var admissionEngine = new OrganizationalWorkAdmissionEngine(workStore, priorityPolicy);
            var stateMachine = new OrganizationalStateMachine();
            var dependencyResolver = new WorkDependencyResolver(workStore);
            var commitmentMonitor = new WorkCommitmentMonitor(workStore);
            var prioritizer = new WorkPortfolioPrioritizer(schedulingPolicy);
            var decompositionEngine = new WorkDecompositionEngine(workStore, runStore);
            var prg1ApprovalManager = new HumanApprovalManager();

            var governanceQueueManager = new GovernanceQueueManager(
                runStore,
                workStore,
                stateMachine,
                prg1ApprovalManager);

            var manager = new AutonomousWorkManager(
                workStore,
                runStore,
                admissionEngine,
                stateMachine,
                dependencyResolver,
                commitmentMonitor,
                prioritizer,
                decompositionEngine,
                governanceQueueManager);

            var scheduler = new EnterpriseSchedulerEngine();

            return (scheduler, manager, workStore, runStore);
        }

        private async Task SeedResponsibilityAsync(InMemoryOrganizationalWorkStore workStore)
        {
            var resp = new OrganizationalResponsibility
            {
                ResponsibilityId = "RESP-FIN-01",
                TenantId = TestTenant,
                Title = "Financial Health Oversight",
                BusinessDomain = "Finance",
                AssignedLeadRole = "CFO",
                IsActive = true
            };
            await workStore.SaveResponsibilityAsync(resp);
        }

        [Fact]
        public async Task SCHED01_LiveSchedulerTick_TriggersAwmCycle_TransitionsWorkAndEmitsProposal()
        {
            var rig = CreateTestRig();
            await SeedResponsibilityAsync(rig.workStore);

            // Seed proposal to be admitted and decomposed
            var proposal = new WorkProposal
            {
                ProposalId = "PROP-SCHED-01",
                TenantId = TestTenant,
                ResponsibilityId = "RESP-FIN-01",
                Title = "Cash Flow Rebalancing",
                Objective = new WorkObjective { Statement = "Rebalance liquidity reserve" },
                EvidenceRefs = new List<string> { "EVID-BANK-01" },
                AdmissionStatus = ProposalAdmissionStatus.Pending
            };
            await rig.workStore.SaveProposalAsync(proposal);

            WorkManagerCycleResult? cycleResult = null;
            var tcs = new TaskCompletionSource<bool>();

            var jobId = ScheduledJobId.New();
            var jobRequest = new ScheduledJobRequest(_workspaceId, "AutonomousWorkManager-Tick", async ct =>
            {
                cycleResult = await rig.manager.ExecuteCycleAsync(
                    TestTenant,
                    ManagerTriggerType.ScheduledTick,
                    triggerId: jobId.ToString(),
                    ct: ct);
                tcs.SetResult(true);
            })
            {
                JobId = jobId,
                Timeout = TimeSpan.FromSeconds(5)
            };

            await rig.scheduler.ScheduleAsync(jobRequest);
            await tcs.Task;

            Assert.NotNull(cycleResult);
            Assert.True(cycleResult!.Success);
            Assert.Equal(1, cycleResult.ProposalsEvaluated);
            Assert.Equal(1, cycleResult.WorkItemsPlanned);
            Assert.Equal(1, cycleResult.MissionProposalsEmitted);

            // Verify work item was created, decomposed, and staged
            var items = await rig.workStore.ListWorkItemsAsync(TestTenant);
            Assert.Single(items);
            var item = items[0];
            Assert.Equal(WorkState.WaitingForGovernance, item.State);
            Assert.NotNull(item.MissionGraphId);

            // Verify MissionGraphProposal exists
            var propLink = await rig.runStore.GetMissionProposalLinkAsync(TestTenant, item.WorkId);
            Assert.NotNull(propLink);
            Assert.Equal(5, propLink!.ProposedNodes.Count);

            // Verify node 4 is ExecutionAuthorizationCheckpoint
            var checkpointNode = propLink.ProposedNodes.FirstOrDefault(n => n.NodeId.Contains("CHECKPOINT"));
            Assert.NotNull(checkpointNode);
            Assert.Equal(MissionNodeType.Approval, checkpointNode!.NodeType);
        }

        [Fact]
        public async Task SCHED02_DuplicateSchedulerTick_IdempotentResult_NoDuplicateRunsOrProposals()
        {
            var rig = CreateTestRig();
            await SeedResponsibilityAsync(rig.workStore);

            string sharedTriggerId = "SCHED-TRIGGER-FIXED-IDEM";

            // First scheduler tick
            var res1 = await rig.manager.ExecuteCycleAsync(TestTenant, ManagerTriggerType.ScheduledTick, sharedTriggerId);
            Assert.True(res1.Success);

            // Second duplicate scheduler tick with same TriggerId within window
            var res2 = await rig.manager.ExecuteCycleAsync(TestTenant, ManagerTriggerType.ScheduledTick, sharedTriggerId);
            Assert.True(res2.Success);

            // Assert exact same run returned without duplicate processing
            Assert.Equal(res1.RunId, res2.RunId);
            Assert.Equal(res1.ResultSummaryHash, res2.ResultSummaryHash);

            var runs = await rig.runStore.ListRunsAsync(TestTenant);
            Assert.Single(runs);
        }

        [Fact]
        public async Task SCHED03_SchedulerTick_ZeroConsequentialExecution_NoExecutionPermitIssued()
        {
            var rig = CreateTestRig();
            await SeedResponsibilityAsync(rig.workStore);

            var prop = new WorkProposal
            {
                ProposalId = "PROP-SCHED-R3",
                TenantId = TestTenant,
                ResponsibilityId = "RESP-FIN-01",
                Title = "High Risk Action",
                RiskTier = WorkRiskTier.R3_Consequential,
                Objective = new WorkObjective { Statement = "Wire funds" },
                EvidenceRefs = new List<string> { "EVID-WIRE" }
            };
            await rig.workStore.SaveProposalAsync(prop);

            var result = await rig.manager.ExecuteCycleAsync(TestTenant, ManagerTriggerType.ScheduledTick, "TRIG-WIRE-TEST");
            Assert.True(result.Success);

            // Verify work item is WaitingForGovernance
            var items = await rig.workStore.ListWorkItemsAsync(TestTenant);
            var item = items.First(i => i.Title == "High Risk Action");
            Assert.Equal(WorkState.WaitingForGovernance, item.State);

            // Verify WorkItem model structurally does NOT contain an ExecutionPermit property (I25/I26 Invariant)
            var props = typeof(WorkItem).GetProperties().Select(p => p.Name).ToList();
            Assert.DoesNotContain("ExecutionPermit", props);
            Assert.DoesNotContain("ExecutionPermitId", props);
            Assert.NotEqual(WorkState.Executing, item.State);
        }

        [Fact]
        public async Task SCHED04_SchedulerTimeout_FailClosed_ManagerRunIntegrityPreserved()
        {
            var rig = CreateTestRig();
            await SeedResponsibilityAsync(rig.workStore);

            // Trigger cycle with budget MaxManagerRuntimeMs = 0 to simulate deadline expiration
            var budget = new WorkManagerBudget { MaxManagerRuntimeMs = 0 };
            var result = await rig.manager.ExecuteCycleAsync(
                TestTenant,
                ManagerTriggerType.ScheduledTick,
                "TRIG-TIMEOUT",
                budget: budget);

            Assert.False(result.Success);
            Assert.Contains("exceeded MaxManagerRuntimeMs", result.ErrorMessage);

            var run = await rig.runStore.GetRunAsync(TestTenant, result.RunId);
            Assert.NotNull(run);
            Assert.Equal(ManagerRunStatus.BudgetExceeded, run!.Status);
            Assert.NotEmpty(run.ResultHash);
        }
    }
}
