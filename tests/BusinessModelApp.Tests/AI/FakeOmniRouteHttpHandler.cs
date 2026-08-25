using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessModelApp.Tests.AI
{
    public class FakeOmniRouteHttpHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> CapturedRequests { get; } = new List<HttpRequestMessage>();
        public List<string> CapturedRequestBodies { get; } = new List<string>();
        public string ResponseContent { get; set; } = "Executive brief summary from OmniRoute.";
        public string ModelReturned { get; set; } = "anthropic/claude-3-5-sonnet";
        public string ProviderReturned { get; set; } = "OmniRoute";
        public int PromptTokens { get; set; } = 150;
        public int CompletionTokens { get; set; } = 40;
        public decimal? CostReturned { get; set; } = 0.0015m;
        public HttpStatusCode StatusCodeToReturn { get; set; } = HttpStatusCode.OK;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequests.Add(request);
            if (request.Content != null)
            {
                var body = request.Content.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
                CapturedRequestBodies.Add(body);
            }

            if (request.RequestUri?.AbsolutePath.EndsWith("health") == true)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"status\":\"healthy\"}", Encoding.UTF8, "application/json")
                });
            }

            if (StatusCodeToReturn != HttpStatusCode.OK)
            {
                return Task.FromResult(new HttpResponseMessage(StatusCodeToReturn)
                {
                    Content = new StringContent("{\"error\":\"OmniRoute test error\"}", Encoding.UTF8, "application/json")
                });
            }

            var responseObj = new
            {
                id = "chatcmpl-test-12345",
                @object = "chat.completion",
                created = 1724590000,
                model = ModelReturned,
                provider = ProviderReturned,
                choices = new[]
                {
                    new
                    {
                        index = 0,
                        message = new
                        {
                            role = "assistant",
                            content = ResponseContent
                        },
                        finish_reason = "stop"
                    }
                },
                usage = new
                {
                    prompt_tokens = PromptTokens,
                    completion_tokens = CompletionTokens,
                    total_tokens = PromptTokens + CompletionTokens,
                    estimated_cost = CostReturned
                },
                cache_hit = false,
                fallback_attempts = 0
            };

            var json = JsonSerializer.Serialize(responseObj);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }
}
