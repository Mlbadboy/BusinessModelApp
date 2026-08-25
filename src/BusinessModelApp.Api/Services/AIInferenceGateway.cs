using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.AI;
using BusinessModelApp.Core.AI.Governance;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Core.Observability;
using BusinessModelApp.Infrastructure.AI.OmniRoute;
using BusinessModelApp.Infrastructure.Data;
using BusinessModelApp.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
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

            using var activity = AITelemetryDiagnostics.ActivitySource.StartActivity("AIInferenceGateway.Execute", ActivityKind.Internal);

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
                _logger.LogWarning("AI request executing without authenticated user context: {Message}", TelemetrySanitizer.Sanitize(ex.Message));
                request.UserId ??= Guid.Empty;
                request.OrganizationId ??= Guid.Empty;
                request.WorkspaceId ??= Guid.Empty;
            }

            var orgIdVal = request.OrganizationId.GetValueOrDefault();
            var wsIdVal = request.WorkspaceId.GetValueOrDefault();

            // Set Zero-Trust Tracing Metadata (strictly non-sensitive identifiers)
            activity?.SetTag("ai.task_type", request.TaskType.ToString());
            activity?.SetTag("ai.organization_id", orgIdVal.ToString());
            activity?.SetTag("ai.workspace_id", wsIdVal.ToString());
            activity?.SetTag("ai.correlation_id", request.RequestCorrelationId);

            // 2. EMERGENCY KILL-SWITCH CHECK: Verify organization/workspace traffic status
            var trafficPolicy = await _context.AITrafficControlPolicies
                .FirstOrDefaultAsync(p => p.OrganizationId == orgIdVal && (p.WorkspaceId == wsIdVal || p.WorkspaceId == null), ct);

            if (trafficPolicy != null && trafficPolicy.Status == AITrafficStatus.EmergencyDisabled)
            {
                _logger.LogWarning("AI request blocked by Emergency Kill-Switch for Org {OrgId}. Reason: {Reason}",
                    orgIdVal, TelemetrySanitizer.Sanitize(trafficPolicy.DisabledReason));
                activity?.SetStatus(ActivityStatusCode.Error, "AI Kill-Switch Active");
                throw new AIKillSwitchActiveException(
                    orgIdVal, trafficPolicy.DisabledReason ?? "Administrator emergency disabled all AI execution.");
            }

            // 3. Resolve approved Routing Policy Profile
            var policy = _policyService.ResolvePolicy(request.TaskType, request.Preference);

            // 4. AI FINOPS GOVERNANCE: Atomic Budget Reservation
            var estimatedCost = policy.MaxEstimatedCost ?? 0.10m;
            var reservation = await _budgetService.ReserveBudgetAsync(orgIdVal, wsIdVal, estimatedCost, ct);
            if (!reservation.IsAllowed)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Budget Cap Exceeded");
                throw new AIBudgetExceededException(
                    orgIdVal,
                    wsIdVal,
                    estimatedCost,
                    reservation.RemainingMonthlyBudget,
                    reservation.RejectionReason ?? "Monthly AI budget cap exceeded.");
            }

            // 5. DATA MINIMIZATION & REDACTION: Task-specific scrubbing & secret redaction
            request.Messages = _dataMinimizer.SanitizeMessages(request.Messages, request.TaskType);
            for (int i = 0; i < request.Messages.Count; i++)
            {
                request.Messages[i].Content = TelemetrySanitizer.Sanitize(request.Messages[i].Content);
            }

            // 6. Dispatch to OmniRoute Infrastructure Adapter
            AIResponse response;
            try
            {
                response = await _omniRouteClient.SendChatCompletionAsync(request, policy, ct);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, TelemetrySanitizer.Sanitize(ex.Message));
                // Reconcile 0 cost on infrastructure failure to release in-flight reservation
                await _budgetService.ReconcileReservationAsync(
                    reservation.ReservationId, orgIdVal, wsIdVal, 0m, 0, 0, 0, false, false, ct);
                throw;
            }

            var actualCost = response.EstimatedCost ?? estimatedCost;

            // 7. Reconcile Budget Reservation & Upsert Fast AIUsageDaily Ledger
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

            // 8. OpenTelemetry Dimensional Metrics Emission
            var metricTags = new TagList
            {
                { "task_type", request.TaskType.ToString() },
                { "provider", response.ProviderUsed },
                { "model", response.ModelUsed },
                { "cache_hit", response.CacheHit }
            };

            AITelemetryDiagnostics.InferenceCounter.Add(1, metricTags);
            AITelemetryDiagnostics.SpendCounter.Add((double)actualCost, metricTags);
            AITelemetryDiagnostics.DurationHistogram.Record(response.LatencyMs, metricTags);
            if (response.CacheHit)
            {
                AITelemetryDiagnostics.CacheHitCounter.Add(1, metricTags);
            }

            // Span Enrichment
            activity?.SetTag("ai.provider", response.ProviderUsed);
            activity?.SetTag("ai.model", response.ModelUsed);
            activity?.SetTag("ai.actual_cost", actualCost.ToString("F4"));
            activity?.SetTag("ai.latency_ms", response.LatencyMs);
            activity?.SetTag("ai.cache_hit", response.CacheHit);

            // 9. Record Immutable Telemetry & Commercial Attribution (AICallRecord)
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
                    OmniRouteRequestId = response.RequestId,
                    LeadId = request.LeadId,
                    OpportunityId = request.OpportunityId
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

        public async IAsyncEnumerable<AIStreamChunk> StreamAsync(
            AIRequest request,
            [EnumeratorCancellation] CancellationToken ct = default)
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
