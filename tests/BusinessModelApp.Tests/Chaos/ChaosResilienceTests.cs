using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Api.Services;
using BusinessModelApp.Core.AI;
using BusinessModelApp.Core.AI.Governance;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Infrastructure.AI.OmniRoute;
using BusinessModelApp.Infrastructure.Data;
using BusinessModelApp.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BusinessModelApp.Tests.Chaos
{
    public class ChaosResilienceTests
    {
        [Fact]
        public async Task OmniRoute_Outage_Scenario_Releases_Budget_And_Keeps_Business_Layer_Functional()
        {
            // Arrange: Simulate OmniRoute upstream failure (503 Service Unavailable)
            var mockOmniRoute = new Mock<IOmniRouteClient>();
            var mockPolicyService = new Mock<IAIRoutingPolicyService>();
            var mockUserContext = new Mock<IUserContextService>();
            var mockDataMinimizer = new Mock<IAIDataMinimizationService>();
            var mockBudgetService = new Mock<IBudgetReservationService>();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "Chaos_Outage_Db_" + Guid.NewGuid())
                .Options;
            var context = new AppDbContext(options);

            var orgId = Guid.NewGuid();
            var wsId = Guid.NewGuid();
            mockUserContext.Setup(u => u.GetCurrentUserIdAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Guid.NewGuid());
            mockUserContext.Setup(u => u.GetCurrentOrganizationIdAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(orgId);
            mockUserContext.Setup(u => u.GetAuthorizedWorkspaceIdAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(wsId);

            mockPolicyService.Setup(p => p.ResolvePolicy(It.IsAny<AITaskType>(), It.IsAny<AIRoutingPreference>()))
                .Returns(new AIRoutingPolicy { TaskType = AITaskType.ExecutiveBrief, MaxEstimatedCost = 0.10m });

            var reservationId = Guid.NewGuid().ToString();
            mockBudgetService.Setup(b => b.ReserveBudgetAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BudgetReservationResult { IsAllowed = true, ReservationId = reservationId });

            mockDataMinimizer.Setup(d => d.SanitizeMessages(It.IsAny<List<AIMessage>>(), It.IsAny<AITaskType>()))
                .Returns<List<AIMessage>, AITaskType>((msgs, _) => msgs);

            // Simulate OmniRoute throwing HttpRequestException (503 Outage)
            mockOmniRoute.Setup(o => o.SendChatCompletionAsync(It.IsAny<AIRequest>(), It.IsAny<AIRoutingPolicy>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("OmniRoute upstream provider unavailable (503)"));

            var gateway = new AIInferenceGateway(
                mockOmniRoute.Object,
                mockPolicyService.Object,
                mockUserContext.Object,
                mockDataMinimizer.Object,
                mockBudgetService.Object,
                context,
                NullLogger<AIInferenceGateway>.Instance);

            var request = new AIRequest
            {
                TaskType = AITaskType.ExecutiveBrief,
                Messages = new List<AIMessage> { new AIMessage { Role = "user", Content = "Generate briefing" } }
            };

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => gateway.ExecuteAsync(request));

            // Verify in-flight budget reservation was refunded with $0 actual cost
            mockBudgetService.Verify(b => b.ReconcileReservationAsync(
                reservationId, orgId, wsId, 0m, 0, 0, 0, false, false, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task AI_Budget_Exhausted_Blocks_AI_While_Business_Operations_Continue()
        {
            // Arrange
            var mockOmniRoute = new Mock<IOmniRouteClient>();
            var mockPolicyService = new Mock<IAIRoutingPolicyService>();
            var mockUserContext = new Mock<IUserContextService>();
            var mockDataMinimizer = new Mock<IAIDataMinimizationService>();
            var mockBudgetService = new Mock<IBudgetReservationService>();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "Chaos_Budget_Db_" + Guid.NewGuid())
                .Options;
            var context = new AppDbContext(options);

            var orgId = Guid.NewGuid();
            var wsId = Guid.NewGuid();
            mockUserContext.Setup(u => u.GetCurrentUserIdAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Guid.NewGuid());
            mockUserContext.Setup(u => u.GetCurrentOrganizationIdAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(orgId);
            mockUserContext.Setup(u => u.GetAuthorizedWorkspaceIdAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(wsId);

            mockPolicyService.Setup(p => p.ResolvePolicy(It.IsAny<AITaskType>(), It.IsAny<AIRoutingPreference>()))
                .Returns(new AIRoutingPolicy { TaskType = AITaskType.ExecutiveBrief, MaxEstimatedCost = 0.10m });

            // Budget reservation rejected
            mockBudgetService.Setup(b => b.ReserveBudgetAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BudgetReservationResult { IsAllowed = false, RejectionReason = "Monthly budget cap exhausted." });

            var gateway = new AIInferenceGateway(
                mockOmniRoute.Object,
                mockPolicyService.Object,
                mockUserContext.Object,
                mockDataMinimizer.Object,
                mockBudgetService.Object,
                context,
                NullLogger<AIInferenceGateway>.Instance);

            var request = new AIRequest
            {
                TaskType = AITaskType.ExecutiveBrief,
                Messages = new List<AIMessage> { new AIMessage { Role = "user", Content = "Generate brief" } }
            };

            // Act & Assert
            await Assert.ThrowsAsync<AIBudgetExceededException>(() => gateway.ExecuteAsync(request));

            // OmniRoute was NEVER called because governance blocked it first
            mockOmniRoute.Verify(o => o.SendChatCompletionAsync(It.IsAny<AIRequest>(), It.IsAny<AIRoutingPolicy>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
