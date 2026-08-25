using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Api.Services;
using BusinessModelApp.Core.AI;
using BusinessModelApp.Core.AI.Governance;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Core.Observability;
using BusinessModelApp.Infrastructure.AI.OmniRoute;
using BusinessModelApp.Infrastructure.Data;
using BusinessModelApp.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BusinessModelApp.Tests.Observability
{
    public class OpenTelemetryMetricsTests
    {
        private readonly Mock<IOmniRouteClient> _mockOmniRoute;
        private readonly Mock<IAIRoutingPolicyService> _mockPolicyService;
        private readonly Mock<IUserContextService> _mockUserContext;
        private readonly Mock<IAIDataMinimizationService> _mockDataMinimizer;
        private readonly Mock<IBudgetReservationService> _mockBudgetService;
        private readonly AppDbContext _context;

        public OpenTelemetryMetricsTests()
        {
            _mockOmniRoute = new Mock<IOmniRouteClient>();
            _mockPolicyService = new Mock<IAIRoutingPolicyService>();
            _mockUserContext = new Mock<IUserContextService>();
            _mockDataMinimizer = new Mock<IAIDataMinimizationService>();
            _mockBudgetService = new Mock<IBudgetReservationService>();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "OTel_Test_Db_" + Guid.NewGuid())
                .Options;
            _context = new AppDbContext(options);

            // Default Setup
            var orgId = Guid.NewGuid();
            var wsId = Guid.NewGuid();
            _mockUserContext.Setup(u => u.GetCurrentUserIdAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Guid.NewGuid());
            _mockUserContext.Setup(u => u.GetCurrentOrganizationIdAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(orgId);
            _mockUserContext.Setup(u => u.GetAuthorizedWorkspaceIdAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(wsId);

            _mockPolicyService.Setup(p => p.ResolvePolicy(It.IsAny<AITaskType>(), It.IsAny<AIRoutingPreference>()))
                .Returns(new AIRoutingPolicy { TaskType = AITaskType.ExecutiveBrief, MaxEstimatedCost = 0.05m });

            _mockBudgetService.Setup(b => b.ReserveBudgetAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BudgetReservationResult { IsAllowed = true, ReservationId = Guid.NewGuid().ToString() });

            _mockDataMinimizer.Setup(d => d.SanitizeMessages(It.IsAny<List<AIMessage>>(), It.IsAny<AITaskType>()))
                .Returns<List<AIMessage>, AITaskType>((msgs, _) => msgs);

            _mockOmniRoute.Setup(o => o.SendChatCompletionAsync(It.IsAny<AIRequest>(), It.IsAny<AIRoutingPolicy>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIResponse
                {
                    Content = "Commercial Briefing Generated",
                    ProviderUsed = "anthropic",
                    ModelUsed = "claude-3-5-sonnet",
                    LatencyMs = 380,
                    EstimatedCost = 0.035m,
                    CacheHit = false,
                    Usage = new AIUsage { PromptTokens = 120, CompletionTokens = 80 }
                });
        }

        [Fact]
        public async Task ExecuteAsync_Emits_OpenTelemetry_Activity_With_Correct_SpanTags()
        {
            // Arrange
            var activityListener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == AITelemetryDiagnostics.ActivitySourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(activityListener);

            var gateway = new AIInferenceGateway(
                _mockOmniRoute.Object,
                _mockPolicyService.Object,
                _mockUserContext.Object,
                _mockDataMinimizer.Object,
                _mockBudgetService.Object,
                _context,
                NullLogger<AIInferenceGateway>.Instance);

            var request = new AIRequest
            {
                TaskType = AITaskType.ExecutiveBrief,
                Messages = new List<AIMessage> { new AIMessage { Role = "user", Content = "Summarize health" } }
            };

            // Act
            var response = await gateway.ExecuteAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.Equal("anthropic", response.ProviderUsed);
            Assert.Equal("claude-3-5-sonnet", response.ModelUsed);
            Assert.Equal(380, response.LatencyMs);
        }

        [Fact]
        public void AITelemetryDiagnostics_Exposes_Active_Instruments()
        {
            // Assert
            Assert.NotNull(AITelemetryDiagnostics.InferenceCounter);
            Assert.NotNull(AITelemetryDiagnostics.SpendCounter);
            Assert.NotNull(AITelemetryDiagnostics.CacheHitCounter);
            Assert.NotNull(AITelemetryDiagnostics.DurationHistogram);
            Assert.NotNull(AITelemetryDiagnostics.CircuitBreakerGauge);
            Assert.Equal("BusinessModelApp.AI.Metrics", AITelemetryDiagnostics.Meter.Name);
        }
    }
}
