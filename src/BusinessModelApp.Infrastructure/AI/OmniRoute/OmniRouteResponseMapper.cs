using System;
using System.Text.Json;
using BusinessModelApp.Core.AI;

namespace BusinessModelApp.Infrastructure.AI.OmniRoute
{
    public static class OmniRouteResponseMapper
    {
        public static AIResponse MapFromChatCompletionJson(string json, long latencyMs)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var response = new AIResponse
            {
                LatencyMs = latencyMs,
                RequestId = root.TryGetProperty("id", out var idElem) ? idElem.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString(),
                ModelUsed = root.TryGetProperty("model", out var modelElem) ? modelElem.GetString() ?? "unknown" : "unknown",
                ProviderUsed = root.TryGetProperty("provider", out var provElem) ? provElem.GetString() ?? "OmniRoute" : "OmniRoute"
            };

            // Choices & Content
            if (root.TryGetProperty("choices", out var choicesElem) && choicesElem.GetArrayLength() > 0)
            {
                var firstChoice = choicesElem[0];
                if (firstChoice.TryGetProperty("message", out var msgElem) &&
                    msgElem.TryGetProperty("content", out var contentElem))
                {
                    response.Content = contentElem.GetString() ?? string.Empty;
                }

                if (firstChoice.TryGetProperty("finish_reason", out var finishElem))
                {
                    response.FinishReason = finishElem.GetString() ?? "stop";
                }
            }

            // Usage Tokens
            if (root.TryGetProperty("usage", out var usageElem))
            {
                response.Usage = new AIUsage
                {
                    PromptTokens = usageElem.TryGetProperty("prompt_tokens", out var pt) ? pt.GetInt32() : 0,
                    CompletionTokens = usageElem.TryGetProperty("completion_tokens", out var ct) ? ct.GetInt32() : 0
                };

                // Authoritative Cost (only if returned upstream as a number; otherwise null)
                if (usageElem.TryGetProperty("estimated_cost", out var costElem) && 
                    costElem.ValueKind == JsonValueKind.Number && 
                    costElem.TryGetDecimal(out var cost))
                {
                    response.EstimatedCost = cost;
                }
                else if (usageElem.TryGetProperty("cost", out var costElem2) && 
                         costElem2.ValueKind == JsonValueKind.Number && 
                         costElem2.TryGetDecimal(out var cost2))
                {
                    response.EstimatedCost = cost2;
                }
            }

            // Telemetry Flags
            if (root.TryGetProperty("cache_hit", out var cacheElem))
            {
                response.CacheHit = cacheElem.GetBoolean();
            }

            if (root.TryGetProperty("fallback_attempts", out var fallbackElem))
            {
                response.FallbackAttempts = fallbackElem.GetInt32();
            }

            return response;
        }
    }
}
