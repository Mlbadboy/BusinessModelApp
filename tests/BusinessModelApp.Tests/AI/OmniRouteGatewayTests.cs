using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using BusinessModelApp.Api.Services;
using BusinessModelApp.Core.AI;
using BusinessModelApp.Core.Domain.Commercial;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Infrastructure.AI.OmniRoute;
using BusinessModelApp.Infrastructure.Data;
using BusinessModelApp.Infrastructure.Interceptors;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BusinessModelApp.Tests.AI
{
    public class OmniRouteGatewayTests
    {
        private AppDbContext CreateDbContext()
        {
            var interceptor = new AppendOnlyAuditInterceptor();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .AddInterceptors(interceptor)
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task Gateway_ShouldOverwriteAndEnforceServerSideTenantContext()
        {
            // Arrange
            using var context = CreateDbContext();
            var fakeHandler = new FakeOmniRouteHttpHandler();
            var httpClient = new HttpClient(fakeHandler);
            var options = Options.Create(new OmniRouteOptions { BaseUrl = "http://localhost:8000" });
            var omniClient = new OmniRouteClient(httpClient, options, NullLogger<OmniRouteClient>.Instance);

            var policyService = new AIRoutingPolicyService();

            // Authentic server context
            var actualOrgId = Guid.NewGuid();
            var actualWsId = Guid.NewGuid();
            var actualUserId = Guid.NewGuid();

            var mockUserContext = new Mock<IUserContextService>();
            mockUserContext.Setup(u => u.GetCurrentUserIdAsync(default)).ReturnsAsync(actualUserId);
            mockUserContext.Setup(u => u.GetCurrentOrganizationIdAsync(default)).ReturnsAsync(actualOrgId);
            mockUserContext.Setup(u => u.GetAuthorizedWorkspaceIdAsync(It.IsAny<Guid?>(), default)).ReturnsAsync(actualWsId);

            var gateway = new AIInferenceGateway(
                omniClient,
                policyService,
                mockUserContext.Object,
                context,
                NullLogger<AIInferenceGateway>.Instance);

            // Agent attempts to pass a spoofed OrganizationId and WorkspaceId
            var spoofedOrgId = Guid.NewGuid();
            var spoofedWsId = Guid.NewGuid();
            var request = new AIRequest
            {
                TaskType = AITaskType.ExecutiveBrief,
                OrganizationId = spoofedOrgId,
                WorkspaceId = spoofedWsId,
                Messages = { AIMessage.User("Summarize Q3 health.") }
            };

            // Act
            var response = await gateway.ExecuteAsync(request);

            // Assert: Response is normalized
            response.Should().NotBeNull();
            response.Content.Should().Be("Executive brief summary from OmniRoute.");
            response.ModelUsed.Should().Be("anthropic/claude-3-5-sonnet");

            // Assert: Outbound request headers received authentic server IDs, NOT spoofed agent IDs
            var capturedRequest = fakeHandler.CapturedRequests.Single();
            capturedRequest.Headers.GetValues("X-Organization-Id").Single().Should().Be(actualOrgId.ToString());
            capturedRequest.Headers.GetValues("X-Workspace-Id").Single().Should().Be(actualWsId.ToString());
            capturedRequest.Headers.GetValues("X-User-Id").Single().Should().Be(actualUserId.ToString());

            // Assert: Telemetry record was persisted with authentic server IDs
            var callRecord = await context.AICallRecords.SingleOrDefaultAsync();
            callRecord.Should().NotBeNull();
            callRecord!.OrganizationId.Should().Be(actualOrgId);
            callRecord.WorkspaceId.Should().Be(actualWsId);
            callRecord.UserId.Should().Be(actualUserId);
            callRecord.PromptTokens.Should().Be(150);
            callRecord.CompletionTokens.Should().Be(40);
            callRecord.EstimatedCost.Should().Be(0.0015m);
        }

        [Fact]
        public async Task AICallRecord_ShouldBeAppendOnly_AndRejectModifications()
        {
            // Arrange
            using var context = CreateDbContext();
            var record = new AICallRecord
            {
                OrganizationId = Guid.NewGuid(),
                WorkspaceId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                TaskType = AITaskType.ExecutiveBrief,
                Provider = "OmniRoute",
                Model = "anthropic/claude-3-5-sonnet",
                PromptTokens = 100,
                CompletionTokens = 50
            };

            context.AICallRecords.Add(record);
            await context.SaveChangesAsync();

            // Act: Attempt to modify telemetry record
            record.PromptTokens = 999;
            context.AICallRecords.Update(record);

            Func<Task> actUpdate = async () => await context.SaveChangesAsync();

            // Assert
            await actUpdate.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*immutable*Modifications are strictly forbidden*");
        }

        [Fact]
        public void RoutingPolicyService_ShouldMapTaskTypesCorrectly()
        {
            var policyService = new AIRoutingPolicyService();

            var briefPolicy = policyService.ResolvePolicy(AITaskType.ExecutiveBrief);
            briefPolicy.Preference.Should().Be(AIRoutingPreference.HighQuality);
            briefPolicy.TargetModelStrategy.Should().Be("anthropic/claude-3-5-sonnet");

            var voicePolicy = policyService.ResolvePolicy(AITaskType.VoiceQualification);
            voicePolicy.Preference.Should().Be(AIRoutingPreference.UltraLowLatency);
            voicePolicy.StreamingRequired.Should().BeTrue();

            var classPolicy = policyService.ResolvePolicy(AITaskType.Classification);
            classPolicy.Preference.Should().Be(AIRoutingPreference.CostControlled);
        }

        [Fact]
        public async Task Gateway_ShouldHandleNullCostWithoutFabricating()
        {
            // Arrange
            using var context = CreateDbContext();
            var fakeHandler = new FakeOmniRouteHttpHandler { CostReturned = null }; // OmniRoute omits cost
            var httpClient = new HttpClient(fakeHandler);
            var options = Options.Create(new OmniRouteOptions { BaseUrl = "http://localhost:8000" });
            var omniClient = new OmniRouteClient(httpClient, options, NullLogger<OmniRouteClient>.Instance);

            var mockUserContext = new Mock<IUserContextService>();
            mockUserContext.Setup(u => u.GetCurrentUserIdAsync(default)).ReturnsAsync(Guid.NewGuid());
            mockUserContext.Setup(u => u.GetCurrentOrganizationIdAsync(default)).ReturnsAsync(Guid.NewGuid());
            mockUserContext.Setup(u => u.GetAuthorizedWorkspaceIdAsync(It.IsAny<Guid?>(), default)).ReturnsAsync(Guid.NewGuid());

            var gateway = new AIInferenceGateway(
                omniClient,
                new AIRoutingPolicyService(),
                mockUserContext.Object,
                context,
                NullLogger<AIInferenceGateway>.Instance);

            // Act
            var response = await gateway.ExecuteAsync(new AIRequest { TaskType = AITaskType.GeneralAssistant });

            // Assert
            response.EstimatedCost.Should().BeNull();
            var record = await context.AICallRecords.SingleAsync();
            record.EstimatedCost.Should().BeNull();
        }

        [Fact]
        public async Task ExecutiveBriefService_ShouldIntegrateEndToEndWithGatewayAndPreserveEvidence()
        {
            // Arrange
            using var context = CreateDbContext();
            var fakeHandler = new FakeOmniRouteHttpHandler
            {
                ResponseContent = "Pipeline coverage is currently 0.85x. Immediate lead qualification is recommended."
            };
            var httpClient = new HttpClient(fakeHandler);
            var options = Options.Create(new OmniRouteOptions { BaseUrl = "http://localhost:8000" });
            var omniClient = new OmniRouteClient(httpClient, options, NullLogger<OmniRouteClient>.Instance);

            var orgId = Guid.NewGuid();
            var wsId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var mockUserContext = new Mock<IUserContextService>();
            mockUserContext.Setup(u => u.GetCurrentUserIdAsync(default)).ReturnsAsync(userId);
            mockUserContext.Setup(u => u.GetCurrentOrganizationIdAsync(default)).ReturnsAsync(orgId);
            mockUserContext.Setup(u => u.GetAuthorizedWorkspaceIdAsync(It.IsAny<Guid?>(), default)).ReturnsAsync(wsId);

            var gateway = new AIInferenceGateway(
                omniClient,
                new AIRoutingPolicyService(),
                mockUserContext.Object,
                context,
                NullLogger<AIInferenceGateway>.Instance);

            var briefService = new BusinessModelApp.Core.Services.ExecutiveBriefService(gateway);

            var health = new BusinessHealthResult
            {
                OverallHealthScore = 68.0,
                ConfidenceLevel = "Medium",
                ConfidenceScore = 0.65,
                PipelineCoverageRatio = 0.85, // Below 1.0 -> triggers recommendation
                WeightedForecastValue = 4250000m,
                QuarterlyTarget = 5000000m,
                StalledRiskIndex = 12.0
            };

            // Act
            var brief = await briefService.GenerateExecutiveBriefAsync(health);

            // Assert
            brief.IsActionRequired.Should().BeTrue();
            brief.Summary.Should().Contain("Pipeline coverage is currently 0.85x");
            brief.Recommendations.Should().Contain(r => r.CitedEvidenceId == "EVD-PIPE-01");

            // Assert Telemetry was logged
            var callRecord = await context.AICallRecords.SingleAsync();
            callRecord.TaskType.Should().Be(AITaskType.ExecutiveBrief);
            callRecord.OrganizationId.Should().Be(orgId);
            callRecord.WorkspaceId.Should().Be(wsId);
        }
    }
}
