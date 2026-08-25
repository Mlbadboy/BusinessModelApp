using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BusinessModelApp.Infrastructure.AI.OmniRoute
{
    public interface IOmniRouteClient
    {
        Task<AIResponse> SendChatCompletionAsync(
            AIRequest request,
            AIRoutingPolicy policy,
            CancellationToken ct = default);

        Task<bool> CheckHealthAsync(CancellationToken ct = default);
    }

    public class OmniRouteClient : IOmniRouteClient
    {
        private readonly HttpClient _httpClient;
        private readonly OmniRouteOptions _options;
        private readonly ILogger<OmniRouteClient> _logger;

        public OmniRouteClient(
            HttpClient httpClient,
            IOptions<OmniRouteOptions> options,
            ILogger<OmniRouteClient> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;

            var baseUrl = _options.BaseUrl?.TrimEnd('/') ?? "http://localhost:8000";
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
            {
                throw new InvalidOperationException($"Invalid OmniRoute BaseUrl configured: '{baseUrl}'");
            }

            _httpClient.BaseAddress = new Uri(baseUrl + "/");
            _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds > 0 ? _options.TimeoutSeconds : 60);

            if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            }
        }

        public async Task<AIResponse> SendChatCompletionAsync(
            AIRequest request,
            AIRoutingPolicy policy,
            CancellationToken ct = default)
        {
            var stopwatch = Stopwatch.StartNew();

            var messages = new List<object>();
            foreach (var msg in request.Messages)
            {
                messages.Add(new { role = msg.Role, content = msg.Content });
            }

            var model = policy.TargetModelStrategy 
                     ?? _options.DefaultModelMapping.GetValueOrDefault(policy.Preference.ToString(), "openai/gpt-4o-mini");

            var payload = new
            {
                model = model,
                messages = messages,
                temperature = policy.Temperature,
                max_tokens = policy.MaxTokens,
                stream = false
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions")
            {
                Content = jsonContent
            };

            // Attach server-derived correlation metadata (never agent-supplied or client-supplied)
            if (request.OrganizationId.HasValue)
            {
                httpRequest.Headers.TryAddWithoutValidation("X-Organization-Id", request.OrganizationId.Value.ToString());
            }

            if (request.WorkspaceId.HasValue)
            {
                httpRequest.Headers.TryAddWithoutValidation("X-Workspace-Id", request.WorkspaceId.Value.ToString());
            }

            if (request.UserId.HasValue)
            {
                httpRequest.Headers.TryAddWithoutValidation("X-User-Id", request.UserId.Value.ToString());
            }

            httpRequest.Headers.TryAddWithoutValidation("X-Correlation-Id", request.RequestCorrelationId);
            httpRequest.Headers.TryAddWithoutValidation("X-Task-Type", request.TaskType.ToString());

            _logger.LogInformation("Dispatching AI task {TaskType} to OmniRoute via strategy '{Model}' (CorrId: {CorrId})",
                request.TaskType, model, request.RequestCorrelationId);

            HttpResponseMessage responseMessage;
            try
            {
                responseMessage = await _httpClient.SendAsync(httpRequest, ct);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "OmniRoute request failed after {ElapsedMs}ms for task {TaskType}",
                    stopwatch.ElapsedMilliseconds, request.TaskType);
                throw new InvalidOperationException($"OmniRoute gateway communication failure: {ex.Message}", ex);
            }

            var responseBody = await responseMessage.Content.ReadAsStringAsync(ct);
            stopwatch.Stop();

            if (!responseMessage.IsSuccessStatusCode)
            {
                _logger.LogError("OmniRoute returned HTTP {StatusCode}: {ResponseBody}", responseMessage.StatusCode, responseBody);
                throw new HttpRequestException($"OmniRoute returned status code {responseMessage.StatusCode}: {responseBody}");
            }

            return OmniRouteResponseMapper.MapFromChatCompletionJson(responseBody, stopwatch.ElapsedMilliseconds);
        }

        public async Task<bool> CheckHealthAsync(CancellationToken ct = default)
        {
            try
            {
                var response = await _httpClient.GetAsync("health", ct);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
