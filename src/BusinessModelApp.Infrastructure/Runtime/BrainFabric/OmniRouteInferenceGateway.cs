using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Interfaces.Runtime;

namespace BusinessModelApp.Infrastructure.Runtime.BrainFabric
{
    public class OmniRouteInferenceGateway : IOmniRouteGateway
    {
        public ModelProviderKind GatewayKind => ModelProviderKind.OmniRouteGateway;

        private readonly ConcurrentDictionary<string, ProviderQuotaStatus> _quotaStatuses = new();
        private readonly ConcurrentDictionary<string, bool> _outageFlags = new();

        public void SetQuotaStatus(ProviderId providerId, ProviderQuotaStatus status)
        {
            _quotaStatuses[providerId.Value] = status;
        }

        public void SetOutage(ProviderId providerId, bool isOutage)
        {
            _outageFlags[providerId.Value] = isOutage;
        }

        public Task<List<ModelRoute>> DiscoverQuotaRoutesAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        {
            var routes = new List<ModelRoute>
            {
                new ModelRoute
                {
                    RouteId = ModelRouteId.From("omniroute-free-qwen"),
                    ProviderId = ProviderId.From("omniroute-pool"),
                    ModelId = ModelId.From("qwen-2.5-72b-free"),
                    GatewayKind = ModelProviderKind.OmniRouteGateway,
                    Priority = 10,
                    IsActive = true,
                    QuotaStatus = _quotaStatuses.GetValueOrDefault("omniroute-pool", ProviderQuotaStatus.Available)
                },
                new ModelRoute
                {
                    RouteId = ModelRouteId.From("omniroute-free-llama"),
                    ProviderId = ProviderId.From("omniroute-pool"),
                    ModelId = ModelId.From("llama-3.3-70b-free"),
                    GatewayKind = ModelProviderKind.OmniRouteGateway,
                    Priority = 20,
                    IsActive = true,
                    QuotaStatus = _quotaStatuses.GetValueOrDefault("omniroute-pool", ProviderQuotaStatus.Available)
                }
            };

            return Task.FromResult(routes);
        }

        public Task<BrainInferenceResult> DispatchInferenceAsync(BrainInferenceRequest request, ModelRoute route, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (route == null) throw new ArgumentNullException(nameof(route));

            // Check provider outage simulation
            if (_outageFlags.TryGetValue(route.ProviderId.Value, out var isOutage) && isOutage)
            {
                return Task.FromResult(new BrainInferenceResult
                {
                    InferenceId = InferenceRequestId.New(),
                    RequestId = request.RequestId,
                    ProviderId = route.ProviderId,
                    ModelId = route.ModelId,
                    Status = InferenceLifecycleState.ProviderUnavailable,
                    ErrorMessage = $"Provider '{route.ProviderId.Value}' is currently unavailable (simulated outage)."
                });
            }

            // Check quota exhaustion
            if (_quotaStatuses.TryGetValue(route.ProviderId.Value, out var quotaStatus) &&
                quotaStatus == ProviderQuotaStatus.Exhausted)
            {
                return Task.FromResult(new BrainInferenceResult
                {
                    InferenceId = InferenceRequestId.New(),
                    RequestId = request.RequestId,
                    ProviderId = route.ProviderId,
                    ModelId = route.ModelId,
                    Status = InferenceLifecycleState.QuotaExhausted,
                    ErrorMessage = $"Inference quota for provider '{route.ProviderId.Value}' is exhausted."
                });
            }

            var inputHash = ComputeSha256(request.Prompt);
            var responseText = $"[OmniRoute - {route.ModelId.Value}]: Inference delivered via aggregated route {route.RouteId.Value}";

            string? structuredJson = null;
            if (!string.IsNullOrWhiteSpace(request.StructuredOutputSchemaJson))
            {
                structuredJson = $"{{\"status\":\"success\",\"gateway\":\"omniroute\",\"model\":\"{route.ModelId.Value}\",\"quota_compliant\":true}}";
            }

            var outputHash = ComputeSha256(responseText);

            var result = new BrainInferenceResult
            {
                InferenceId = InferenceRequestId.New(),
                RequestId = request.RequestId,
                ProviderId = route.ProviderId,
                ModelId = route.ModelId,
                ModelVersionId = ModelVersionId.From("v1.0-omni"),
                RawOutput = responseText,
                ParsedStructuredJson = structuredJson,
                InputTokens = Math.Max(1, request.Prompt.Length / 4),
                OutputTokens = Math.Max(1, responseText.Length / 4),
                LatencyMs = 120,
                CostUsd = 0m, // Legitimately free/quota-based capacity
                Status = InferenceLifecycleState.Completed,
                Provenance = new ModelProvenance
                {
                    ProviderId = route.ProviderId,
                    ModelId = route.ModelId,
                    ModelVersionId = ModelVersionId.From("v1.0-omni"),
                    InputPromptHash = inputHash,
                    OutputHash = outputHash,
                    SchemaVersion = "1.0",
                    TimestampUtc = DateTime.UtcNow,
                    WorkspaceId = request.WorkspaceId,
                    MissionRunId = request.MissionRunId
                }
            };

            return Task.FromResult(result);
        }

        private static string ComputeSha256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input ?? string.Empty));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
