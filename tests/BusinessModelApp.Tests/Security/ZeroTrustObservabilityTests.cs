using System;
using System.Collections.Generic;
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

namespace BusinessModelApp.Tests.Security
{
    public class ZeroTrustObservabilityTests
    {
        [Theory]
        [InlineData("Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.doNotLeakThisSignature123", "[REDACTED_JWT]")]
        [InlineData("sk-proj-1234567890abcdef1234567890abcdef", "[REDACTED_API_KEY]")]
        [InlineData("password='SuperSecretAdminPassword123!'", "password=[REDACTED]")]
        [InlineData("Server=prod-sql.db.net;Database=ProductionDB;User Id=sa;Password=SecretProdPassword123;", "[REDACTED_CONN_STR];")]
        public void TelemetrySanitizer_Proactively_Redacts_Sensitive_Patterns(string rawSecret, string expectedRedactedToken)
        {
            // Act
            var sanitized = TelemetrySanitizer.Sanitize(rawSecret);

            // Assert
            Assert.Contains(expectedRedactedToken, sanitized);
            Assert.DoesNotContain("SuperSecretAdminPassword123!", sanitized);
            Assert.DoesNotContain("SecretProdPassword123", sanitized);
        }

        [Fact]
        public async Task AIInferenceGateway_Sanitizes_Adversarial_Prompt_Secrets_Before_Execution()
        {
            // Arrange
            var mockOmniRoute = new Mock<IOmniRouteClient>();
            var mockPolicyService = new Mock<IAIRoutingPolicyService>();
            var mockUserContext = new Mock<IUserContextService>();
            var mockDataMinimizer = new Mock<IAIDataMinimizationService>();
            var mockBudgetService = new Mock<IBudgetReservationService>();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "ZeroTrust_Test_Db_" + Guid.NewGuid())
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
                .Returns(new AIRoutingPolicy { TaskType = AITaskType.ExecutiveBrief, MaxEstimatedCost = 0.05m });

            mockBudgetService.Setup(b => b.ReserveBudgetAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BudgetReservationResult { IsAllowed = true, ReservationId = Guid.NewGuid().ToString() });

            mockDataMinimizer.Setup(d => d.SanitizeMessages(It.IsAny<List<AIMessage>>(), It.IsAny<AITaskType>()))
                .Returns<List<AIMessage>, AITaskType>((msgs, _) => msgs);

            AIRequest? capturedRequest = null;
            mockOmniRoute.Setup(o => o.SendChatCompletionAsync(It.IsAny<AIRequest>(), It.IsAny<AIRoutingPolicy>(), It.IsAny<CancellationToken>()))
                .Callback<AIRequest, AIRoutingPolicy, CancellationToken>((req, _, _) => capturedRequest = req)
                .ReturnsAsync(new AIResponse
                {
                    Content = "Safe Response",
                    ProviderUsed = "anthropic",
                    ModelUsed = "claude-3-5-sonnet",
                    LatencyMs = 250,
                    EstimatedCost = 0.02m,
                    CacheHit = false,
                    Usage = new AIUsage { PromptTokens = 50, CompletionTokens = 20 }
                });

            var gateway = new AIInferenceGateway(
                mockOmniRoute.Object,
                mockPolicyService.Object,
                mockUserContext.Object,
                mockDataMinimizer.Object,
                mockBudgetService.Object,
                context,
                NullLogger<AIInferenceGateway>.Instance);

            var adversarialPrompt = "Here is my secret API key: sk-live-1234567890abcdef1234567890 and password='MySecretPassword123'";
            var request = new AIRequest
            {
                TaskType = AITaskType.ExecutiveBrief,
                Messages = new List<AIMessage>
                {
                    new AIMessage { Role = "user", Content = adversarialPrompt }
                }
            };

            // Act
            var response = await gateway.ExecuteAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.NotNull(capturedRequest);
            Assert.DoesNotContain("sk-live-1234567890abcdef1234567890", capturedRequest!.Messages[0].Content);
            Assert.DoesNotContain("MySecretPassword123", capturedRequest!.Messages[0].Content);
            Assert.Contains("[REDACTED_API_KEY]", capturedRequest!.Messages[0].Content);
            Assert.Contains("password=[REDACTED]", capturedRequest!.Messages[0].Content);
        }
    }
}
