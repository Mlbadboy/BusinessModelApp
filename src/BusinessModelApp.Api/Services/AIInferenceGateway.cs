using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.AI;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Infrastructure.AI.OmniRoute;
using BusinessModelApp.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace BusinessModelApp.Api.Services
{
    public class AIInferenceGateway : IAIInferenceGateway
    {
        private readonly IOmniRouteClient _omniRouteClient;
        private readonly IAIRoutingPolicyService _policyService;
        private readonly IUserContextService _userContext;
        private readonly AppDbContext _context;
        private readonly ILogger<AIInferenceGateway> _logger;

        public AIInferenceGateway(
            IOmniRouteClient omniRouteClient,
            IAIRoutingPolicyService policyService,
            IUserContextService userContext,
            AppDbContext context,
            ILogger<AIInferenceGateway> logger)
        {
            _omniRouteClient = omniRouteClient;
            _policyService = policyService;
            _userContext = userContext;
            _context = context;
            _logger = logger;
        }

        public async Task<AIResponse> ExecuteAsync(AIRequest request, CancellationToken ct = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            // 1. SERVER-SIDE TENANT AUTHORITY: Overwrite agent/client IDs with authenticated context
            try
            {
                var userId = await _userContext.GetCurrentUserIdAsync(ct);
                var orgId = await _userContext.GetCurrentOrganizationIdAsync(ct);
                var wsId = await _userContext.GetAuthorizedWorkspaceIdAsync(request.WorkspaceId, ct);

                request.UserId = userId;
                request.OrganizationId = orgId;
                request.WorkspaceId = wsId;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("AI request executing without authenticated user context: {Message}", ex.Message);
                // Fallback for background system tasks
                request.UserId ??= Guid.Empty;
                request.OrganizationId ??= Guid.Empty;
                request.WorkspaceId ??= Guid.Empty;
            }

            // 2. Resolve approved Routing Policy
            var policy = _policyService.ResolvePolicy(request.TaskType, request.Preference);

            // 3. Dispatch to OmniRoute Infrastructure Adapter
            var response = await _omniRouteClient.SendChatCompletionAsync(request, policy, ct);

            // 4. Record Telemetry (Append-only AICallRecord)
            try
            {
                var telemetryRecord = new AICallRecord
                {
                    OrganizationId = request.OrganizationId.GetValueOrDefault(),
                    WorkspaceId = request.WorkspaceId.GetValueOrDefault(),
                    UserId = request.UserId.GetValueOrDefault(),
                    AgentId = request.AgentId,
                    TaskType = request.TaskType,
                    Provider = response.ProviderUsed,
                    Model = response.ModelUsed,
                    PromptTokens = response.Usage.PromptTokens,
                    CompletionTokens = response.Usage.CompletionTokens,
                    EstimatedCost = response.EstimatedCost, // Nullable if not provided
                    LatencyMs = response.LatencyMs,
                    CacheHit = response.CacheHit,
                    FallbackAttempts = response.FallbackAttempts,
                    RequestCorrelationId = request.RequestCorrelationId,
                    OmniRouteRequestId = response.RequestId
                };

                _context.AICallRecords.Add(telemetryRecord);
                await _context.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                // Telemetry recording failure must never crash inference, but must be logged
                _logger.LogError(ex, "Failed to persist AICallRecord telemetry for correlation {CorrId}", request.RequestCorrelationId);
            }

            return response;
        }

        public async IAsyncEnumerable<AIStreamChunk> StreamAsync(AIRequest request, CancellationToken ct = default)
        {
            // First vertical slice delegates to synchronous execute for contract completeness
            var response = await ExecuteAsync(request, ct);
            yield return new AIStreamChunk
            {
                DeltaContent = response.Content,
                FinishReason = response.FinishReason
            };
        }

        public Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default)
        {
            // Placeholder for future embedding vertical slice
            return Task.FromResult(new float[1536]);
        }
    }
}
