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
    public sealed class Phase4Batch438RevenueMissionFactoryTests
    {
        private readonly InMemoryRevenueControlPlaneStore _store;
        private readonly RevenueControlPlaneAndFactoryService _service;
        private readonly RevenueControlPlaneController _controller;

        public Phase4Batch438RevenueMissionFactoryTests()
        {
            _store = new InMemoryRevenueControlPlaneStore();
            _service = new RevenueControlPlaneAndFactoryService(_store);
            _controller = new RevenueControlPlaneController(_service, _service);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.ControllerContext.HttpContext.Request.Headers["X-Tenant-ID"] = "tenant-rcp-438";
        }

        // =========================================================================
        // Family 1: Revenue Control Plane State & Pipeline Metrology
        // =========================================================================

        [Fact]
        public async Task RCP438_01_PipelineSeparation_DistinguishesPipelineFromCollectedRevenue()
        {
            await _service.UpdatePipelineMetricsAsync(
                "tenant-rcp-438",
                qualifiedPipeline: 500000m,
                weightedPipeline: 200000m,
                closedWon: 100000m,
                invoiced: 80000m,
                collected: 50000m,
                margin: 35000m);

            var state = await _service.GetCurrentStateAsync("tenant-rcp-438");

            state.QualifiedPipelineAmount.Should().Be(500000m);
            state.WeightedPipelineAmount.Should().Be(200000m);
            state.ClosedWonRevenue.Should().Be(100000m);
            state.InvoicedRevenue.Should().Be(80000m);
            state.CollectedCashRevenue.Should().Be(50000m); // Realized Cash != Pipeline
            state.RealizedGrossMargin.Should().Be(35000m);
        }

        // =========================================================================
        // Family 2: Revenue Mission Factory Templates & Risk Calibration
        // =========================================================================

        [Fact]
        public async Task FACTORY438_02_MissionFactory_CompilesCanonicalTemplates()
        {
            var oppId = "opp-retail-99";

            // 1. Qualify Account
            var qualify = await _service.CreateRevenueMissionAsync("tenant-rcp-438", oppId, RevenueMissionTemplateType.QUALIFY_ACCOUNT);
            qualify.RiskTier.Should().Be(1);
            qualify.RequiredTools.Should().Contain("Browser.Search");
            qualify.AssignedRoleTitle.Should().Be("RevenueProspector");

            // 2. Proposal
            var proposal = await _service.CreateRevenueMissionAsync("tenant-rcp-438", oppId, RevenueMissionTemplateType.PREPARE_PROPOSAL);
            proposal.RiskTier.Should().Be(2);
            proposal.RequiredTools.Should().Contain("Document.Generate");
            proposal.AssignedRoleTitle.Should().Be("ProposalArchitect");

            // 3. Negotiate (High Risk, Mandates PRG-1)
            var negotiate = await _service.CreateRevenueMissionAsync("tenant-rcp-438", oppId, RevenueMissionTemplateType.NEGOTIATE);
            negotiate.RiskTier.Should().Be(3);
            negotiate.AssignedRoleTitle.Should().Be("RevenueDirector");

            // 4. Invoicing (Financial Mutation, Mandates PRG-1)
            var invoice = await _service.CreateRevenueMissionAsync("tenant-rcp-438", oppId, RevenueMissionTemplateType.INVOICE);
            invoice.RiskTier.Should().Be(3);
            invoice.AssignedRoleTitle.Should().Be("FinanceOperations");

            var all = await _service.ListMissionsForOpportunityAsync("tenant-rcp-438", oppId);
            all.Should().HaveCount(4);
        }
    }
}
