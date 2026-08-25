using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
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
        bool IsCircuitBreakerOpen { get; }
        void ResetCircuitBreaker();
    }

    public class OmniRouteClient : IOmniRouteClient
    {
        private readonly HttpClient _httpClient;
        private readonly OmniRouteOptions _options;
        private readonly ILogger<OmniRouteClient> _logger;

        private int _consecutiveFailures = 0;
        private DateTime? _circuitBreakerTripTime = null;
        private const int CircuitBreakerThreshold = 5;
        private static readonly TimeSpan CircuitBreakerCooldown = TimeSpan.FromSeconds(30);

        public bool IsCircuitBreakerOpen
        {
            get
            {
                if (_consecutiveFailures >= CircuitBreakerThreshold)
                {
                    if (_circuitBreakerTripTime.HasValue &&
                        DateTime.UtcNow - _circuitBreakerTripTime.Value < CircuitBreakerCooldown)
                    {
                        return true;
                    }
                    // Cooldown expired: half-open probe
                    return false;
                }
                return false;
            }
        }

        public void ResetCircuitBreaker()
        {
            _consecutiveFailures = 0;
            _circuitBreakerTripTime = null;
        }

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
            if (IsCircuitBreakerOpen)
            {
                _logger.LogWarning("OmniRoute Circuit Breaker is OPEN due to recent upstream failures.");
                throw new InvalidOperationException("OmniRoute gateway is temporarily unavailable (Circuit Breaker OPEN).");
            }

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

            var jsonPayload = JsonSerializer.Serialize(payload);

            // Retry policy: up to 3 attempts with exponential backoff & jitter for transient 429/503
            const int maxRetries = 3;
            HttpResponseMessage? responseMessage = null;
            string responseBody = string.Empty;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions")
                {
                    Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
                };

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

                try
                {
                    responseMessage = await _httpClient.SendAsync(httpRequest, ct);
                    responseBody = await responseMessage.Content.ReadAsStringAsync(ct);

                    if (responseMessage.IsSuccessStatusCode)
                    {
                        // Successful response: reset failure count
                        ResetCircuitBreaker();
                        break;
                    }

                    // Transient status codes eligible for retry
                    if ((responseMessage.StatusCode == HttpStatusCode.TooManyRequests || 
                         responseMessage.StatusCode == HttpStatusCode.ServiceUnavailable ||
                         responseMessage.StatusCode == HttpStatusCode.GatewayTimeout) && attempt < maxRetries)
                    {
                        var jitterMs = Random.Shared.Next(50, 200);
                        var delayMs = (int)(Math.Pow(2, attempt) * 100) + jitterMs;
                        _logger.LogWarning("OmniRoute transient status {Status} on attempt {Attempt}. Retrying in {Delay}ms...",
                            responseMessage.StatusCode, attempt, delayMs);
                        await Task.Delay(delayMs, ct);
                        continue;
                    }

                    // Non-transient failure
                    break;
                }
                catch (Exception ex) when (attempt < maxRetries && !ct.IsCancellationRequested)
                {
                    var delayMs = (int)(Math.Pow(2, attempt) * 100) + Random.Shared.Next(50, 150);
                    _logger.LogWarning(ex, "OmniRoute transient transport exception on attempt {Attempt}. Retrying in {Delay}ms...", attempt, delayMs);
                    await Task.Delay(delayMs, ct);
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    RecordFailure();
                    _logger.LogError(ex, "OmniRoute request failed after {ElapsedMs}ms for task {TaskType}",
                        stopwatch.ElapsedMilliseconds, request.TaskType);
                    throw new InvalidOperationException($"OmniRoute gateway communication failure: {ex.Message}", ex);
                }
            }

            stopwatch.Stop();

            if (responseMessage == null || !responseMessage.IsSuccessStatusCode)
            {
                RecordFailure();
                var status = responseMessage?.StatusCode.ToString() ?? "Unknown";
                _logger.LogError("OmniRoute returned HTTP {StatusCode}: {ResponseBody}", status, responseBody);
                throw new HttpRequestException($"OmniRoute returned status code {status}: {responseBody}");
            }

            return OmniRouteResponseMapper.MapFromChatCompletionJson(responseBody, stopwatch.ElapsedMilliseconds);
        }

        private void RecordFailure()
        {
            Interlocked.Increment(ref _consecutiveFailures);
            if (_consecutiveFailures >= CircuitBreakerThreshold)
            {
                _circuitBreakerTripTime = DateTime.UtcNow;
            }
        }

        public async Task<bool> CheckHealthAsync(CancellationToken ct = default)
        {
            if (IsCircuitBreakerOpen) return false;
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
