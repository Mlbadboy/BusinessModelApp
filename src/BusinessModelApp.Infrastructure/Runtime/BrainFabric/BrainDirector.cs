using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Interfaces.Runtime;

namespace BusinessModelApp.Infrastructure.Runtime.BrainFabric
{
    public class BrainDirector : IBrainDirector
    {
        private readonly IModelRouter _router;
        private readonly ILocalInferenceGateway _localGateway;
        private readonly IOmniRouteGateway _omniRouteGateway;
        private readonly IOpenRouterGateway _openRouterGateway;
        private readonly IDirectApiGateway _directApiGateway;
        private readonly IInferenceBudgetGuard _budgetGuard;
        private readonly IProviderCircuitBreaker _circuitBreaker;
        private readonly IDataEgressClassifier _egressClassifier;
        private readonly IContextOptimizer _contextOptimizer;
        private readonly IInferenceAuditLedger _auditLedger;
        private readonly IEventBus? _eventBus;

        public BrainDirector(
            IModelRouter router,
            ILocalInferenceGateway localGateway,
            IOmniRouteGateway omniRouteGateway,
            IOpenRouterGateway openRouterGateway,
            IDirectApiGateway directApiGateway,
            IInferenceBudgetGuard budgetGuard,
            IProviderCircuitBreaker circuitBreaker,
            IDataEgressClassifier egressClassifier,
            IContextOptimizer contextOptimizer,
            IInferenceAuditLedger auditLedger,
            IEventBus? eventBus = null)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _localGateway = localGateway ?? throw new ArgumentNullException(nameof(localGateway));
            _omniRouteGateway = omniRouteGateway ?? throw new ArgumentNullException(nameof(omniRouteGateway));
            _openRouterGateway = openRouterGateway ?? throw new ArgumentNullException(nameof(openRouterGateway));
            _directApiGateway = directApiGateway ?? throw new ArgumentNullException(nameof(directApiGateway));
            _budgetGuard = budgetGuard ?? throw new ArgumentNullException(nameof(budgetGuard));
            _circuitBreaker = circuitBreaker ?? throw new ArgumentNullException(nameof(circuitBreaker));
            _egressClassifier = egressClassifier ?? throw new ArgumentNullException(nameof(egressClassifier));
            _contextOptimizer = contextOptimizer ?? throw new ArgumentNullException(nameof(contextOptimizer));
            _auditLedger = auditLedger ?? throw new ArgumentNullException(nameof(auditLedger));
            _eventBus = eventBus;
        }

        public async Task<BrainInferenceResult> RequestInferenceAsync(BrainInferenceRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            // Invariant: Tenant isolation is mandatory
            if (request.WorkspaceId == Guid.Empty)
            {
                return new BrainInferenceResult
                {
                    RequestId = request.RequestId,
                    Status = InferenceLifecycleState.PolicyDenied,
                    ErrorMessage = "Tenant isolation violation: WorkspaceId cannot be empty."
                };
            }

            // Invariant: Deterministic Replay safety: External inference is FORBIDDEN during historical replay
            if (request.IsReplay)
            {
                return new BrainInferenceResult
                {
                    RequestId = request.RequestId,
                    ProviderId = ProviderId.From("historical-replay-engine"),
                    ModelId = ModelId.From("replayed-provenance-model"),
                    ModelVersionId = ModelVersionId.From("v1.0-replay"),
                    RawOutput = "[Replay Simulation]: State reconstructed strictly from durable event history without live inference call.",
                    Status = InferenceLifecycleState.Completed,
                    CostUsd = 0m,
                    Provenance = new ModelProvenance
                    {
                        ProviderId = ProviderId.From("historical-replay-engine"),
                        ModelId = ModelId.From("replayed-provenance-model"),
                        ModelVersionId = ModelVersionId.From("v1.0-replay"),
                        InputPromptHash = "replay_hash",
                        OutputHash = "replay_hash",
                        TimestampUtc = DateTime.UtcNow,
                        WorkspaceId = request.WorkspaceId,
                        MissionRunId = request.MissionRunId
                    }
                };
            }

            // 1. Data Egress Classification
            var egressTier = _egressClassifier.ClassifyTaskDataSensitivity(request.Prompt, request.SystemPrompt);
            var effectiveRequest = request.DataEgressTier == DataEgressTier.LocalOnly
                ? request
                : request with { DataEgressTier = egressTier };

            // 2. Context Optimization (Preserving underlying source records)
            var optimization = await _contextOptimizer.OptimizeContextAsync(effectiveRequest.Prompt, effectiveRequest.MinimumContextWindow, cancellationToken);
            var optimizedRequest = effectiveRequest with { Prompt = optimization.CompressedPrompt };

            if (_eventBus != null)
            {
                var evt = new BrainRequestCreatedEvent(
                    optimizedRequest.RequestId,
                    optimizedRequest.MissionRunId,
                    "cognitive_reasoning") { WorkspaceId = optimizedRequest.WorkspaceId };
                var envelope = RuntimeEventEnvelope.Create(evt, "initial_hash", 1);
                await _eventBus.PublishAsync(envelope, cancellationToken);
            }

            // 3. Select Eligible Route via 10-Point Formula
            var route = await _router.SelectEligibleRouteAsync(optimizedRequest, cancellationToken);
            if (route == null)
            {
                return new BrainInferenceResult
                {
                    RequestId = optimizedRequest.RequestId,
                    Status = InferenceLifecycleState.PolicyDenied,
                    ErrorMessage = "No eligible inference route satisfies capability, privacy, quota, and budget constraints."
                };
            }

            // 4. Dispatch with Circuit-Breaker Protected Fallback
            var result = await DispatchWithFallbackAsync(optimizedRequest, route, cancellationToken);

            // 5. Schema Validation Gate (If structured output requested)
            if (result.Status == InferenceLifecycleState.Completed &&
                !string.IsNullOrWhiteSpace(optimizedRequest.StructuredOutputSchemaJson))
            {
                var isValidJson = IsValidJson(result.ParsedStructuredJson ?? result.RawOutput);
                if (!isValidJson)
                {
                    result = result with
                    {
                        Status = InferenceLifecycleState.SchemaInvalid,
                        ErrorMessage = "Model output failed structured schema validation."
                    };

                    if (_eventBus != null)
                    {
                        var schemaEvt = new InferenceSchemaRejectedEvent(
                            optimizedRequest.RequestId,
                            "Malformed JSON structured output") { WorkspaceId = optimizedRequest.WorkspaceId };
                        var envelope = RuntimeEventEnvelope.Create(schemaEvt, "prev_hash", 2);
                        await _eventBus.PublishAsync(envelope, cancellationToken);
                    }
                    return result;
                }
            }

            // 6. Record Budget & Provenance Audit
            if (result.Status == InferenceLifecycleState.Completed)
            {
                await _budgetGuard.RecordUsageAsync(
                    optimizedRequest.WorkspaceId,
                    optimizedRequest.MissionRunId,
                    result.CostUsd,
                    result.InputTokens + result.OutputTokens,
                    cancellationToken);

                await _auditLedger.RecordInferenceAuditAsync(result, cancellationToken);

                if (_eventBus != null)
                {
                    var completedEvt = new InferenceCompletedEvent(
                        optimizedRequest.RequestId,
                        result.InferenceId,
                        result.ProviderId,
                        result.ModelId,
                        result.InputTokens + result.OutputTokens,
                        result.CostUsd,
                        result.Provenance.OutputHash) { WorkspaceId = optimizedRequest.WorkspaceId };
                    var envelope = RuntimeEventEnvelope.Create(completedEvt, "prev_hash", 3);
                    await _eventBus.PublishAsync(envelope, cancellationToken);
                }
            }

            return result;
        }

        private async Task<BrainInferenceResult> DispatchWithFallbackAsync(
            BrainInferenceRequest request,
            ModelRoute primaryRoute,
            CancellationToken cancellationToken)
        {
            var result = await ExecuteGatewayAsync(request, primaryRoute, cancellationToken);

            if (result.Status == InferenceLifecycleState.Completed)
            {
                _circuitBreaker.RecordSuccess(primaryRoute.ProviderId);
                return result;
            }

            // Record failure on primary provider
            _circuitBreaker.RecordFailure(primaryRoute.ProviderId, result.ErrorMessage ?? "Inference error");

            // Attempt fallback across alternative registered eligible routes
            var allRoutes = await _router.GetRegisteredRoutesAsync(cancellationToken);
            var fallbackCandidates = allRoutes
                .Where(r => r.IsActive && r.RouteId.Value != primaryRoute.RouteId.Value && r.ProviderId.Value != primaryRoute.ProviderId.Value)
                .OrderBy(r => r.Priority);

            foreach (var fallbackRoute in fallbackCandidates)
            {
                var candidateRequest = request with { PreferredRouteId = fallbackRoute.RouteId };
                var eligible = await _router.SelectEligibleRouteAsync(candidateRequest, cancellationToken);
                if (eligible == null || eligible.RouteId.Value != fallbackRoute.RouteId.Value)
                    continue;

                if (_eventBus != null)
                {
                    var fallbackEvt = new InferenceFallbackTriggeredEvent(
                        request.RequestId,
                        primaryRoute.RouteId,
                        fallbackRoute.RouteId,
                        result.ErrorMessage ?? "Provider failure") { WorkspaceId = request.WorkspaceId };
                    var envelope = RuntimeEventEnvelope.Create(fallbackEvt, "prev_hash", 4);
                    await _eventBus.PublishAsync(envelope, cancellationToken);
                }

                var fallbackResult = await ExecuteGatewayAsync(request, fallbackRoute, cancellationToken);
                if (fallbackResult.Status == InferenceLifecycleState.Completed)
                {
                    _circuitBreaker.RecordSuccess(fallbackRoute.ProviderId);
                    return fallbackResult;
                }

                _circuitBreaker.RecordFailure(fallbackRoute.ProviderId, fallbackResult.ErrorMessage ?? "Fallback error");
            }

            return result;
        }

        private Task<BrainInferenceResult> ExecuteGatewayAsync(
            BrainInferenceRequest request,
            ModelRoute route,
            CancellationToken cancellationToken)
        {
            return route.GatewayKind switch
            {
                ModelProviderKind.LocalOffline => _localGateway.DispatchInferenceAsync(request, route, cancellationToken),
                ModelProviderKind.OmniRouteGateway => _omniRouteGateway.DispatchInferenceAsync(request, route, cancellationToken),
                ModelProviderKind.OpenRouterGateway => _openRouterGateway.DispatchInferenceAsync(request, route, cancellationToken),
                ModelProviderKind.DirectVendorApi => _directApiGateway.DispatchInferenceAsync(request, route, cancellationToken),
                _ => throw new NotSupportedException($"Gateway kind '{route.GatewayKind}' is not supported.")
            };
        }

        private static bool IsValidJson(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            try
            {
                using var doc = JsonDocument.Parse(input);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
