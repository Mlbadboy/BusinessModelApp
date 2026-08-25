using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Api.Health;
using BusinessModelApp.Core.Observability;
using BusinessModelApp.Infrastructure.AI.OmniRoute;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Xunit;

namespace BusinessModelApp.Tests.Health
{
    public class HealthCheckTests
    {
        [Fact]
        public async Task OmniRouteHealthCheck_Returns_Healthy_Status_Without_Exposing_Secrets()
        {
            // Arrange
            var mockClient = new Mock<IOmniRouteClient>();
            var healthCheck = new OmniRouteHealthCheck(mockClient.Object);
            var context = new HealthCheckContext();

            // Act
            var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None);

            // Assert
            Assert.Equal(HealthStatus.Healthy, result.Status);
            Assert.Contains("operational", result.Description, StringComparison.OrdinalIgnoreCase);

            // Verify Zero-Trust: no secret keys in description or data
            Assert.False(TelemetrySanitizer.ContainsSecret(result.Description));
            foreach (var kvp in result.Data)
            {
                Assert.False(TelemetrySanitizer.ContainsSecret(kvp.Value?.ToString()));
            }
        }
    }
}
