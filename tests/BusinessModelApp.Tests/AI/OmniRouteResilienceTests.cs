using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.AI;
using BusinessModelApp.Infrastructure.AI.OmniRoute;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace BusinessModelApp.Tests.AI
{
    public class OmniRouteResilienceTests
    {
        private class TransientFailHandler : HttpMessageHandler
        {
            public int AttemptCount { get; private set; } = 0;
            public int FailuresBeforeSuccess { get; set; } = 1;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                AttemptCount++;
                if (AttemptCount <= FailuresBeforeSuccess)
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.TooManyRequests)
                    {
                        Content = new StringContent("{\"error\":\"rate_limited\"}", Encoding.UTF8, "application/json")
                    });
                }

                var successJson = "{\"id\":\"chatcmpl-resilience\",\"model\":\"anthropic/claude-3-5-sonnet\",\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"Success after retry.\"}}],\"usage\":{\"prompt_tokens\":10,\"completion_tokens\":10}}";
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(successJson, Encoding.UTF8, "application/json")
                });
            }
        }

        [Fact]
        public async Task OmniRouteClient_ShouldRetryAndSucceedOnTransient429()
        {
            // Arrange
            var handler = new TransientFailHandler { FailuresBeforeSuccess = 1 };
            var httpClient = new HttpClient(handler);
            var options = Options.Create(new OmniRouteOptions { BaseUrl = "http://localhost:8000" });
            var client = new OmniRouteClient(httpClient, options, NullLogger<OmniRouteClient>.Instance);
            client.ResetCircuitBreaker();

            var request = new AIRequest { TaskType = AITaskType.ExecutiveBrief, Messages = { AIMessage.User("Test prompt") } };
            var policy = new AIRoutingPolicy { TaskType = AITaskType.ExecutiveBrief };

            // Act
            var response = await client.SendChatCompletionAsync(request, policy);

            // Assert: Attempted twice (1 retry) and succeeded
            handler.AttemptCount.Should().Be(2);
            response.Content.Should().Be("Success after retry.");
        }

        [Fact]
        public async Task OmniRouteClient_ShouldTripCircuitBreakerAfterConsecutiveFailures()
        {
            // Arrange: Handler that always fails with 500
            var handler = new TransientFailHandler { FailuresBeforeSuccess = 99 };
            var httpClient = new HttpClient(handler);
            var options = Options.Create(new OmniRouteOptions { BaseUrl = "http://localhost:8000" });
            var client = new OmniRouteClient(httpClient, options, NullLogger<OmniRouteClient>.Instance);
            client.ResetCircuitBreaker();

            var request = new AIRequest { TaskType = AITaskType.ExecutiveBrief, Messages = { AIMessage.User("Test") } };
            var policy = new AIRoutingPolicy { TaskType = AITaskType.ExecutiveBrief };

            // Act: Trigger 5 failures
            for (int i = 0; i < 5; i++)
            {
                try { await client.SendChatCompletionAsync(request, policy); } catch { }
            }

            // Assert: Circuit breaker is now OPEN
            client.IsCircuitBreakerOpen.Should().BeTrue();

            // Subsequent call fails fast without calling HTTP handler
            var attemptsBefore = handler.AttemptCount;
            Func<Task> act = async () => await client.SendChatCompletionAsync(request, policy);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Circuit Breaker OPEN*");

            handler.AttemptCount.Should().Be(attemptsBefore);
        }
    }
}
