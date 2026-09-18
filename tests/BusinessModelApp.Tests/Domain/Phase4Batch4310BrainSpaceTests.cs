using System;
using System.Threading.Tasks;
using BusinessModelApp.Api.Controllers;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Workforce;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public sealed class Phase4Batch4310BrainSpaceTests
    {
        private readonly InMemoryWorkforceConstitutionStore _constStore;
        private readonly WorkforceConstitutionService _constitutionService;

        private readonly InMemoryRevenueControlPlaneStore _rcpStore;
        private readonly RevenueControlPlaneAndFactoryService _rcpService;

        private readonly InMemoryCommunicationAndSecretStore _commStore;
        private readonly UnifiedCommunicationFabric _commFabric;

        private readonly InMemoryAgentHarnessStore _harnessStore;
        private readonly AgentHarnessService _harnessService;

        private readonly BrainSpaceTelemetryService _telemetryService;
        private readonly BrainSpaceController _controller;

        public Phase4Batch4310BrainSpaceTests()
        {
            _constStore = new InMemoryWorkforceConstitutionStore();
            _constitutionService = new WorkforceConstitutionService(_constStore);

            _rcpStore = new InMemoryRevenueControlPlaneStore();
            _rcpService = new RevenueControlPlaneAndFactoryService(_rcpStore);

            _commStore = new InMemoryCommunicationAndSecretStore();
            _commFabric = new UnifiedCommunicationFabric(_commStore);

            _harnessStore = new InMemoryAgentHarnessStore();
            _harnessService = new AgentHarnessService(_harnessStore);

            _telemetryService = new BrainSpaceTelemetryService(
                _constitutionService,
                _rcpService,
                _commFabric,
                _harnessService);

            _controller = new BrainSpaceController(_telemetryService);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.ControllerContext.HttpContext.Request.Headers["X-Tenant-ID"] = "tenant-brain-4310";
        }

        // =========================================================================
        // Family 1: Telemetry Separation & Real Backend Synthesis
        // =========================================================================

        [Fact]
        public async Task BRAIN4310_01_CaptureSnapshot_SynthesizesRealSystemAndBusinessTelemetry()
        {
            // 1. Setup Revenue Control Plane state
            await _rcpService.UpdatePipelineMetricsAsync(
                "tenant-brain-4310",
                qualifiedPipeline: 400000m,
                weightedPipeline: 180000m,
                closedWon: 90000m,
                invoiced: 75000m,
                collected: 60000m,
                margin: 42000m);

            // 2. Hire active agent
            var contract = new AgentEmploymentContract
            {
                AgentId = "agent-brain-01",
                AgentName = "Chief Commercial Agent",
                RoleTitle = "RevenueDirector",
                DepartmentId = "Revenue",
                Quota = new AgentQuotaAllocation { DailyBudgetCap = 5000m, DailySpent = 350m },
                RiskCeiling = 3
            };
            await _constitutionService.HireAgentAsync("tenant-brain-4310", contract);
            await _constitutionService.PromoteAgentAsync("tenant-brain-4310", "agent-brain-01", AgentLifecycleStatus.CERTIFIED, "ChiefRiskOfficer");
            await _constitutionService.PromoteAgentAsync("tenant-brain-4310", "agent-brain-01", AgentLifecycleStatus.ACTIVE, "HumanSupervisor");

            // 3. Submit high risk communication intent (creates pending approval)
            await _commFabric.SubmitCommunicationIntentAsync("tenant-brain-4310", new CommunicationIntent
            {
                AgentId = "agent-brain-01",
                Channel = CommunicationChannel.EMAIL,
                Subject = "Commercial Terms",
                RiskTier = 3 // Pending approval
            });

            // Capture real snapshot
            var snapshot = await _telemetryService.CaptureSnapshotAsync("tenant-brain-4310");

            snapshot.TenantId.Should().Be("tenant-brain-4310");

            // Verify Business Telemetry
            snapshot.BusinessTelemetry.QualifiedPipeline.Should().Be(400000m);
            snapshot.BusinessTelemetry.CollectedCashRevenue.Should().Be(60000m);
            snapshot.BusinessTelemetry.RealizedGrossMargin.Should().Be(42000m);

            // Verify System Telemetry
            snapshot.SystemTelemetry.ActiveQueueDepth.Should().Be(1); // 1 pending approval

            // Verify Agent Nodes
            snapshot.AgentNodes.Should().HaveCount(1);
            var node = snapshot.AgentNodes[0];
            node.AgentId.Should().Be("agent-brain-01");
            node.DailyBudget.Should().Be(5000m);
            node.DailySpent.Should().Be(350m);
            node.PendingApprovalCount.Should().Be(1);
            node.HealthStatus.Should().Be("HEALTHY");
        }

        [Fact]
        public async Task BRAIN4310_02_Controller_ReturnsSnapshotResponse()
        {
            var result = await _controller.GetTelemetrySnapshot() as OkObjectResult;
            result.Should().NotBeNull();
            var snapshot = result!.Value as BrainSpaceTelemetrySnapshot;
            snapshot.Should().NotBeNull();
        }
    }
}
