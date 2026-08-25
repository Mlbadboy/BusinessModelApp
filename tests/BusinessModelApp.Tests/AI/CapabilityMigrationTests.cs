using System;
using System.Net.Http;
using System.Threading.Tasks;
using BusinessModelApp.Api.Services;
using BusinessModelApp.Core.AI;
using BusinessModelApp.Core.AI.Governance;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Infrastructure.AI.OmniRoute;
using BusinessModelApp.Infrastructure.Data;
using BusinessModelApp.Infrastructure.Interceptors;
using BusinessModelApp.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BusinessModelApp.Tests.AI
{
    public class CapabilityMigrationTests
    {
        private (AIInferenceGateway gateway, FakeOmniRouteHttpHandler fakeHandler, AppDbContext context, Guid orgId, Guid wsId) SetupEnvironment()
        {
            var interceptor = new AppendOnlyAuditInterceptor();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .AddInterceptors(interceptor)
                .Options;

            var context = new AppDbContext(options);
            var fakeHandler = new FakeOmniRouteHttpHandler();
            var httpClient = new HttpClient(fakeHandler);
            var omniOptions = Options.Create(new OmniRouteOptions { BaseUrl = "http://localhost:8000" });
            var omniClient = new OmniRouteClient(httpClient, omniOptions, NullLogger<OmniRouteClient>.Instance);

            var orgId = Guid.NewGuid();
            var wsId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var mockUserContext = new Mock<IUserContextService>();
            mockUserContext.Setup(u => u.GetCurrentUserIdAsync(default)).ReturnsAsync(userId);
            mockUserContext.Setup(u => u.GetCurrentOrganizationIdAsync(default)).ReturnsAsync(orgId);
            mockUserContext.Setup(u => u.GetAuthorizedWorkspaceIdAsync(It.IsAny<Guid?>(), default)).ReturnsAsync(wsId);

            var policyService = new AIRoutingPolicyService();
            var dataMinimizer = new AIDataMinimizationService();
            var budgetService = new BudgetReservationService(context, NullLogger<BudgetReservationService>.Instance);

            var gateway = new AIInferenceGateway(
                omniClient,
                policyService,
                mockUserContext.Object,
                dataMinimizer,
                budgetService,
                context,
                NullLogger<AIInferenceGateway>.Instance);

            return (gateway, fakeHandler, context, orgId, wsId);
        }

        [Fact]
        public async Task Capability_BusinessHealthExplanation_ShouldExecuteThroughGateway()
        {
            var (gateway, fakeHandler, context, orgId, wsId) = SetupEnvironment();
            fakeHandler.ResponseContent = "Pipeline coverage ratio of 2.4x indicates that weighted potential deals exceed targets by 140%.";

            var request = new AIRequest
            {
                TaskType = AITaskType.BusinessHealthExplanation,
                WorkspaceId = wsId,
                Messages = { AIMessage.User("Explain formula Σ(EstimatedValue * Probability) for EVD-PIPE-01.") }
            };

            var response = await gateway.ExecuteAsync(request);

            response.Should().NotBeNull();
            response.Content.Should().Contain("Pipeline coverage ratio of 2.4x");

            var record = await context.AICallRecords.SingleAsync();
            record.TaskType.Should().Be(AITaskType.BusinessHealthExplanation);
            record.OrganizationId.Should().Be(orgId);
        }

        [Fact]
        public async Task Capability_OpportunityAnalysis_ShouldExecuteThroughGateway()
        {
            var (gateway, fakeHandler, context, orgId, wsId) = SetupEnvironment();
            fakeHandler.ResponseContent = "Key risk: SLA terms pending. Next step: Schedule technical demo with CTO.";

            var request = new AIRequest
            {
                TaskType = AITaskType.OpportunityAnalysis,
                WorkspaceId = wsId,
                Messages = { AIMessage.User("Analyze Acme Corp Deal ($750,000 value, Stage: Discovery).") }
            };

            var response = await gateway.ExecuteAsync(request);

            response.Should().NotBeNull();
            response.Content.Should().Contain("Key risk: SLA terms pending");

            var record = await context.AICallRecords.SingleAsync();
            record.TaskType.Should().Be(AITaskType.OpportunityAnalysis);
        }

        [Fact]
        public async Task Capability_LeadQualification_ShouldExecuteThroughGateway()
        {
            var (gateway, fakeHandler, context, orgId, wsId) = SetupEnvironment();
            fakeHandler.ResponseContent = "Intent Score: 88/100. High-readiness enterprise inquiry.";

            var request = new AIRequest
            {
                TaskType = AITaskType.LeadQualification,
                WorkspaceId = wsId,
                Messages = { AIMessage.User("Score lead contact John Doe (john@acme.com) from Acme Corp.") }
            };

            var response = await gateway.ExecuteAsync(request);

            response.Should().NotBeNull();
            response.Content.Should().Contain("Intent Score: 88/100");

            var record = await context.AICallRecords.SingleAsync();
            record.TaskType.Should().Be(AITaskType.LeadQualification);
        }
    }
}
