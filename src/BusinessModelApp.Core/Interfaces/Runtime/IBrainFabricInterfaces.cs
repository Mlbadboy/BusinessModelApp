using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime;

namespace BusinessModelApp.Core.Interfaces.Runtime
{
    public interface IInferenceGateway
    {
        ModelProviderKind GatewayKind { get; }
        Task<BrainInferenceResult> DispatchInferenceAsync(BrainInferenceRequest request, ModelRoute route, CancellationToken cancellationToken = default);
    }

    public interface IOmniRouteGateway : IInferenceGateway
    {
        Task<List<ModelRoute>> DiscoverQuotaRoutesAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    }

    public interface ILocalInferenceGateway : IInferenceGateway
    {
    }

    public interface IOpenRouterGateway : IInferenceGateway
    {
    }

    public interface IDirectApiGateway : IInferenceGateway
    {
    }

    public interface IModelEvaluationGate
    {
        Task<bool> IsModelApprovedAsync(ModelId modelId, CancellationToken cancellationToken = default);
        Task<ModelDefinition?> GetModelDefinitionAsync(ModelId modelId, CancellationToken cancellationToken = default);
        Task RegisterModelDefinitionAsync(ModelDefinition definition, CancellationToken cancellationToken = default);
    }

    public interface IInferenceBudgetGuard
    {
        Task<bool> ValidateBudgetAvailabilityAsync(Guid workspaceId, MissionRunId? missionRunId, decimal estimatedCostUsd, long estimatedTokens, CancellationToken cancellationToken = default);
        Task RecordUsageAsync(Guid workspaceId, MissionRunId? missionRunId, decimal actualCostUsd, long actualTokens, CancellationToken cancellationToken = default);
        Task<InferenceBudgetTracker> GetBudgetTrackerAsync(Guid workspaceId, MissionRunId? missionRunId, CancellationToken cancellationToken = default);
    }

    public interface IProviderCircuitBreaker
    {
        bool IsCircuitOpen(ProviderId providerId);
        void RecordSuccess(ProviderId providerId);
        void RecordFailure(ProviderId providerId, string reason);
        ProviderHealthScore GetHealthScore(ProviderId providerId);
    }

    public interface IDataEgressClassifier
    {
        DataEgressTier ClassifyTaskDataSensitivity(string prompt, string systemPrompt);
    }

    public interface IContextOptimizer
    {
        Task<ContextCompressionResult> OptimizeContextAsync(string prompt, long maxTargetTokens, CancellationToken cancellationToken = default);
    }

    public interface IInferenceAuditLedger
    {
        Task RecordInferenceAuditAsync(BrainInferenceResult result, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<BrainInferenceResult>> GetInferenceAuditTrailAsync(Guid workspaceId, MissionRunId? missionRunId, CancellationToken cancellationToken = default);
    }

    public interface IModelRouter
    {
        Task<ModelRoute?> SelectEligibleRouteAsync(BrainInferenceRequest request, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ModelRoute>> GetRegisteredRoutesAsync(CancellationToken cancellationToken = default);
        Task RegisterRouteAsync(ModelRoute route, CancellationToken cancellationToken = default);
    }

    public interface IBrainDirector
    {
        Task<BrainInferenceResult> RequestInferenceAsync(BrainInferenceRequest request, CancellationToken cancellationToken = default);
    }
}
