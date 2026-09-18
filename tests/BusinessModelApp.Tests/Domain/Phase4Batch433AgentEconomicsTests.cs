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
    public sealed class Phase4Batch433AgentEconomicsTests
    {
        private readonly InMemoryAgentEconomicsStore _store;
        private readonly AgentEconomicsEngine _engine;
        private readonly AgentEconomicsController _controller;

        public Phase4Batch433AgentEconomicsTests()
        {
            _store = new InMemoryAgentEconomicsStore();
            _engine = new AgentEconomicsEngine(_store);
            _controller = new AgentEconomicsController(_engine);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.ControllerContext.HttpContext.Request.Headers["X-Tenant-ID"] = "tenant-econ-433";
        }

        // =========================================================================
        // Family 1: Agent Cost Allocation & Breakdown Rollup
        // =========================================================================

        [Fact]
        public async Task ECON433_01_CostAllocationRollup_ComputesTotalOperatingCostAccurately()
        {
            var agentId = "agent-researcher-433";

            await _engine.RecordCostAllocationAsync("tenant-econ-433", new CostAllocationRecord
            {
                AgentId = agentId,
                CostType = "ModelInference",
                Amount = 120m
            });
            await _engine.RecordCostAllocationAsync("tenant-econ-433", new CostAllocationRecord
            {
                AgentId = agentId,
                CostType = "ToolUsage",
                Amount = 30m
            });
            await _engine.RecordCostAllocationAsync("tenant-econ-433", new CostAllocationRecord
            {
                AgentId = agentId,
                CostType = "BrowserCompute",
                Amount = 50m
            });
            await _engine.RecordCostAllocationAsync("tenant-econ-433", new CostAllocationRecord
            {
                AgentId = agentId,
                CostType = "HumanAttention",
                Amount = 200m
            });

            var breakdown = await _engine.GetAgentCostBreakdownAsync("tenant-econ-433", agentId);

            breakdown.ModelInferenceCost.Should().Be(120m);
            breakdown.ToolUsageCost.Should().Be(30m);
            breakdown.BrowserComputeCost.Should().Be(50m);
            breakdown.HumanAttentionCost.Should().Be(200m);
            breakdown.TotalOperatingCost.Should().Be(400m);
        }

        // =========================================================================
        // Family 2: Revenue Attribution & P&L Contribution
        // =========================================================================

        [Fact]
        public async Task ECON433_02_RevenueAttribution_CalculatesNetContributionAndContributionRatio()
        {
            var agentId = "agent-sales-433";

            var attr = new RevenueAttributionRecord
            {
                AgentId = agentId,
                OpportunityId = "opp-cloud-01",
                MissionId = "mission-q4-close",
                AttributedCollectedRevenue = 50000m,
                AttributedDeliveryCost = 15000m,
                AttributedAgentCost = 2500m,
                AttributedAcquisitionCost = 2000m,
                AttributedPaymentCost = 500m
            };

            await _engine.RecordRevenueAttributionAsync("tenant-econ-433", attr);

            attr.NetContribution.Should().Be(30000m); // 50000 - 15000 - 2500 - 2000 - 500 = 30000
            attr.ContributionRatio.Should().Be(12.0m); // 30000 / 2500 = 12.0x

            var list = await _engine.GetAgentRevenueAttributionsAsync("tenant-econ-433", agentId);
            list.Should().HaveCount(1);
            list[0].NetContribution.Should().Be(30000m);
        }

        // =========================================================================
        // Family 3: Metrology Performance & Task Routing (Law I39-G)
        // =========================================================================

        [Fact]
        public async Task ECON433_03_MetrologyPerformance_OrdersAgentsByRoutingScore()
        {
            await _engine.UpdateAgentMetrologyPerformanceAsync("tenant-econ-433", new AgentMetrologyPerformance
            {
                AgentId = "agent-prospector-A",
                ResearchAccuracyPercent = 92m,
                RoutingScore = 0.85m
            });

            await _engine.UpdateAgentMetrologyPerformanceAsync("tenant-econ-433", new AgentMetrologyPerformance
            {
                AgentId = "agent-prospector-B",
                ResearchAccuracyPercent = 98m,
                RoutingScore = 0.96m
            });

            var best = await _engine.ListBestAgentsForRoutingAsync("tenant-econ-433", "RevenueProspector");

            best.Should().HaveCount(2);
            best[0].AgentId.Should().Be("agent-prospector-B"); // Higher routing score first
            best[1].AgentId.Should().Be("agent-prospector-A");
        }

        // =========================================================================
        // Family 4: Economic Outcome & Realized ROI
        // =========================================================================

        [Fact]
        public async Task ECON433_04_ComputeEconomicOutcome_CalculatesRealizedROI()
        {
            var objectiveId = "obj-q4-enterprise";

            // Total revenue = 100,000, Total delivery = 30,000
            await _engine.RecordRevenueAttributionAsync("tenant-econ-433", new RevenueAttributionRecord
            {
                AttributedCollectedRevenue = 100000m,
                AttributedDeliveryCost = 30000m
            });

            // Workforce operating costs = 10,000
            await _engine.RecordCostAllocationAsync("tenant-econ-433", new CostAllocationRecord
            {
                Amount = 10000m
            });

            var outcome = await _engine.ComputeEconomicOutcomeAsync("tenant-econ-433", objectiveId);

            outcome.TotalCollectedRevenue.Should().Be(100000m);
            outcome.TotalDeliveryCost.Should().Be(30000m);
            outcome.TotalWorkforceCost.Should().Be(10000m);
            outcome.NetGrossMargin.Should().Be(60000m); // 100000 - 40000
            outcome.RealizedROI.Should().Be(1.5m); // 60000 / 40000 = 1.5 (150% ROI)
        }

        // =========================================================================
        // Family 5: Controller API Endpoints
        // =========================================================================

        [Fact]
        public async Task ECON433_05_ControllerEndpoints_RecordAndRetrieveEconomics()
        {
            var costResult = await _controller.RecordCost(new CostAllocationRecord
            {
                AgentId = "agent-api-econ",
                CostType = "ModelInference",
                Amount = 50m
            }) as OkObjectResult;
            costResult.Should().NotBeNull();

            var breakdownResult = await _controller.GetCostBreakdown("agent-api-econ") as OkObjectResult;
            breakdownResult.Should().NotBeNull();
        }
    }
}
