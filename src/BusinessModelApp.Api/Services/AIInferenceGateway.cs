using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.AI;
using BusinessModelApp.Core.AI.Governance;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Infrastructure.AI.OmniRoute;
using BusinessModelApp.Infrastructure.Data;
using BusinessModelApp.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace BusinessModelApp.Api.Services
{
    public class AIInferenceGateway : IAIInferenceGateway
    {
        private readonly IOmniRouteClient _omniRouteClient;
        private readonly IAIRoutingPolicyService _policyService;
        private readonly IUserContextService _userContext;
        private readonly IAIDataMinimizationService _dataMinimizer;
        private readonly IBudgetReservationService _budgetService;
        private readonly AppDbContext _context;
        private readonly ILogger<AIInferenceGateway> _logger;

        public AIInferenceGateway(
            IOmniRouteClient omniRouteClient,
            IAIRoutingPolicyService policyService,
            IUserContextService userContext,
            IAIDataMinimizationService dataMinimizer,
            IBudgetReservationService budgetService,
            AppDbContext context,
            ILogger<AIInferenceGateway> logger)
        {
            _omniRouteClient = omniRouteClient;
            _policyService = policyService;
            _userContext = userContext;
            _dataMinimizer = dataMinimizer;
            _budgetService = budgetService;
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
                request.UserId ??= Guid.Empty;
                request.OrganizationId ??= Guid.Empty;
                request.WorkspaceId ??= Guid.Empty;
            }

            var orgIdVal = request.OrganizationId.GetValueOrDefault();
            var wsIdVal = request.WorkspaceId.GetValueOrDefault();

            // 2. Resolve approved Routing Policy Profile
            var policy = _policyService.ResolvePolicy(request.TaskType, request.Preference);

            // 3. AI FINOPS GOVERNANCE: Atomic Budget Reservation
            var estimatedCost = policy.MaxEstimatedCost ?? 0.10m;
            var reservation = await _budgetService.ReserveBudgetAsync(orgIdVal, wsIdVal, estimatedCost, ct);
            if (!reservation.IsAllowed)
            {
                throw new AIBudgetExceededException(
                    orgIdVal,
                    wsIdVal,
                    estimatedCost,
                    reservation.RemainingMonthlyBudget,
                    reservation.RejectionReason ?? "Monthly AI budget cap exceeded.");
            }

            // 4. DATA MINIMIZATION & REDACTION: Task-specific scrubbing
            request.Messages = _dataMinimizer.SanitizeMessages(request.Messages, request.TaskType);

            // 5. Dispatch to OmniRoute Infrastructure Adapter
            AIResponse response;
            try
            {
                response = await _omniRouteClient.SendChatCompletionAsync(request, policy, ct);
            }
            catch (Exception ex)
            {
                // Reconcile 0 cost on infrastructure failure to free reservation
                await _budgetService.ReconcileReservationAsync(
                    reservation.ReservationId, orgIdVal, wsIdVal, 0m, 0, 0, 0, false, false, ct);
                throw;
            }

            var actualCost = response.EstimatedCost ?? estimatedCost;

            // 6. Reconcile Budget Reservation & Upsert Fast AIUsageDaily Ledger
            await _budgetService.ReconcileReservationAsync(
                reservation.ReservationId,
                orgIdVal,
                wsIdVal,
                actualCost,
                response.Usage.PromptTokens,
                response.Usage.CompletionTokens,
                response.LatencyMs,
                response.CacheHit,
                response.FallbackAttempts > 0,
                ct);

            // 7. Record Immutable Telemetry (AICallRecord)
            try
            {
                var telemetryRecord = new AICallRecord
                {
                    OrganizationId = orgIdVal,
                    WorkspaceId = wsIdVal,
                    UserId = request.UserId.GetValueOrDefault(),
                    AgentId = request.AgentId,
                    TaskType = request.TaskType,
                    Provider = response.ProviderUsed,
                    Model = response.ModelUsed,
                    PromptTokens = response.Usage.PromptTokens,
                    CompletionTokens = response.Usage.CompletionTokens,
                    EstimatedCost = response.EstimatedCost,
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
                _logger.LogError(ex, "Failed to persist AICallRecord telemetry for correlation {CorrId}", request.RequestCorrelationId);
            }

            return response;
        }

        public async IAsyncEnumerable<AIStreamChunk> StreamAsync(AIRequest request, CancellationToken ct = default)
        {
            var response = await ExecuteAsync(request, ct);
            yield return new AIStreamChunk
            {
                DeltaContent = response.Content,
                FinishReason = response.FinishReason
            };
        }

        public Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default)
        {
            return Task.FromResult(new float[1536]);
        }
    }
}
