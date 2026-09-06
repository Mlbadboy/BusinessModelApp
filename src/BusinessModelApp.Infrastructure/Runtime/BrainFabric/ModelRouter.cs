using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Interfaces.Runtime;

namespace BusinessModelApp.Infrastructure.Runtime.BrainFabric
{
    public class ModelRouter : IModelRouter
    {
        private readonly IModelEvaluationGate _evaluationGate;
        private readonly IProviderCircuitBreaker _circuitBreaker;
        private readonly IInferenceBudgetGuard _budgetGuard;
        private readonly ConcurrentDictionary<string, ModelRoute> _routes = new(StringComparer.OrdinalIgnoreCase);

        public ModelRouter(
            IModelEvaluationGate evaluationGate,
            IProviderCircuitBreaker circuitBreaker,
            IInferenceBudgetGuard budgetGuard)
        {
            _evaluationGate = evaluationGate ?? throw new ArgumentNullException(nameof(evaluationGate));
            _circuitBreaker = circuitBreaker ?? throw new ArgumentNullException(nameof(circuitBreaker));
            _budgetGuard = budgetGuard ?? throw new ArgumentNullException(nameof(budgetGuard));
        }

        public Task RegisterRouteAsync(ModelRoute route, CancellationToken cancellationToken = default)
        {
            if (route == null) throw new ArgumentNullException(nameof(route));
            _routes[route.RouteId.Value] = route;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ModelRoute>> GetRegisteredRoutesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ModelRoute>>(_routes.Values.ToList());
        }

        public async Task<ModelRoute?> SelectEligibleRouteAsync(BrainInferenceRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            // Tenant context must be valid
            if (request.WorkspaceId == Guid.Empty)
                return null;

            // Sort routes by priority (lower number = higher priority)
            var candidates = _routes.Values
                .Where(r => r.IsActive)
                .OrderBy(r => r.Priority)
                .ToList();

            // If a preferred route was requested, evaluate it first
            if (request.PreferredRouteId.HasValue &&
                _routes.TryGetValue(request.PreferredRouteId.Value.Value, out var preferred))
            {
                candidates.Remove(preferred);
                candidates.Insert(0, preferred);
            }

            foreach (var route in candidates)
            {
                var isEligible = await EvaluateRouteEligibilityAsync(request, route, cancellationToken);
                if (isEligible)
                {
                    return route;
                }
            }

            return null;
        }

        private async Task<bool> EvaluateRouteEligibilityAsync(
            BrainInferenceRequest request,
            ModelRoute route,
            CancellationToken cancellationToken)
        {
            // 1. Provider Healthy & Circuit Breaker Closed
            if (_circuitBreaker.IsCircuitOpen(route.ProviderId))
                return false;

            // 2. Model Approved by ModelEvaluationGate
            var isApproved = await _evaluationGate.IsModelApprovedAsync(route.ModelId, cancellationToken);
            if (!isApproved)
                return false;

            var modelDef = await _evaluationGate.GetModelDefinitionAsync(route.ModelId, cancellationToken);
            if (modelDef == null)
                return false;

            // 3. Privacy & Data Egress Satisfied
            // If request requires LocalOnly, only LocalOffline gateway is eligible
            if (request.DataEgressTier == DataEgressTier.LocalOnly &&
                route.GatewayKind != ModelProviderKind.LocalOffline)
            {
                return false;
            }

            if (request.DataEgressTier == DataEgressTier.PrivateVpc &&
                modelDef.DataEgressTier == DataEgressTier.PublicCommercialAllowed &&
                route.GatewayKind != ModelProviderKind.LocalOffline)
            {
                return false;
            }

            // 4. Capability & Reasoning Tier Satisfied
            if (request.RequiredReasoningTier == ReasoningTier.HighFrontier &&
                modelDef.CapabilityProfile.ReasoningTier < ReasoningTier.HighFrontier)
            {
                return false;
            }

            // 5. Context Window Satisfied
            if (request.MinimumContextWindow > modelDef.CapabilityProfile.ContextWindowTokens)
            {
                return false;
            }

            // 6. Quota Available or Policy-Permitted
            if (route.QuotaStatus == ProviderQuotaStatus.Exhausted)
            {
                return false;
            }

            // 7. Budget Available
            var estimatedCost = (modelDef.CostPer1kInputTokensUsd + modelDef.CostPer1kOutputTokensUsd) * 2; // rough estimation
            if (estimatedCost > request.MaxAcceptableCostUsd)
            {
                return false;
            }

            var hasBudget = await _budgetGuard.ValidateBudgetAvailabilityAsync(
                request.WorkspaceId,
                request.MissionRunId,
                estimatedCost,
                request.MaxTokens,
                cancellationToken);

            if (!hasBudget)
                return false;

            return true;
        }
    }
}
