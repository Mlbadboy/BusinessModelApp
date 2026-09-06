using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BusinessModelApp.Core.Domain.Runtime
{
    // ========================================================================
    // 1. LIFECYCLE & GOVERNANCE ENUMS
    // ========================================================================

    public enum InferenceLifecycleState
    {
        Requested,
        Admitted,
        RouteSelected,
        Dispatched,
        Receiving,
        Validating,
        Completed,
        Failed,
        Timeout,
        Cancelled,
        QuotaExhausted,
        ProviderUnavailable,
        PolicyDenied,
        BudgetDenied,
        SchemaInvalid,
        UnknownEffect
    }

    public enum InferenceCapacityState
    {
        FreeQuotaAvailable,
        FreeQuotaLow,
        FreeQuotaExhausted,
        PaidAvailable,
        LocalAvailable,
        ProviderDegraded,
        NoEligibleRoute
    }

    public enum ProviderQuotaStatus
    {
        Available,
        LowQuota,
        Exhausted,
        UnknownRateLimited,
        UnmeteredLocal
    }

    public enum ModelApprovalState
    {
        Discovered,
        Evaluating,
        Approved,
        ConditionallyApproved,
        Quarantined,
        Rejected,
        Deprecated
    }

    public enum DataEgressTier
    {
        LocalOnly,
        PrivateVpc,
        PublicCommercialAllowed
    }

    public enum ModelProviderKind
    {
        LocalOffline,
        OmniRouteGateway,
        OpenRouterGateway,
        DirectVendorApi
    }

    public enum ReasoningTier
    {
        StandardFast,
        Moderate,
        HighFrontier
    }

    // ========================================================================
    // 2. CAPABILITY & HEALTH METRICS
    // ========================================================================

    public record ModelCapabilityProfile
    {
        public long ContextWindowTokens { get; init; } = 8192;
        public bool SupportsStructuredOutput { get; init; } = true;
        public bool SupportsVision { get; init; } = false;
        public ReasoningTier ReasoningTier { get; init; } = ReasoningTier.StandardFast;
        public bool SupportsStreaming { get; init; } = false;
        public bool SupportsFunctionCalling { get; init; } = false;
    }

    public record ProviderHealthScore
    {
        public int ConsecutiveFailures { get; init; }
        public int SuccessCount { get; init; }
        public int FailureCount { get; init; }
        public bool IsCircuitOpen { get; init; }
        public DateTime? CircuitOpenedAtUtc { get; init; }
        public DateTime? LastFailureUtc { get; init; }

        public double SuccessRatePercent =>
            (SuccessCount + FailureCount) == 0 ? 100.0 : (SuccessCount * 100.0 / (SuccessCount + FailureCount));
    }

    // ========================================================================
    // 3. INFERENCE REQUEST, OUTCOME & PROVENANCE
    // ========================================================================

    public record BrainInferenceRequest
    {
        public BrainRequestId RequestId { get; init; } = BrainRequestId.New();
        public Guid WorkspaceId { get; init; }
        public MissionRunId? MissionRunId { get; init; }
        public MissionNodeId? MissionNodeId { get; init; }
        public AgentInstanceId? AgentInstanceId { get; init; }
        public string Prompt { get; init; } = string.Empty;
        public string SystemPrompt { get; init; } = string.Empty;
        public string? StructuredOutputSchemaJson { get; init; }
        public List<string> RequiredCapabilities { get; init; } = new();
        public long MinimumContextWindow { get; init; } = 4096;
        public int MaxTokens { get; init; } = 2048;
        public double Temperature { get; init; } = 0.2;
        public ReasoningTier RequiredReasoningTier { get; init; } = ReasoningTier.StandardFast;
        public decimal MaxAcceptableCostUsd { get; init; } = 0.05m;
        public DataEgressTier DataEgressTier { get; init; } = DataEgressTier.PublicCommercialAllowed;
        public ModelRouteId? PreferredRouteId { get; init; }
        public bool IsReplay { get; init; } = false;
        public DateTime RequestedAtUtc { get; init; } = DateTime.UtcNow;
    }

    public record ModelProvenance
    {
        public ProviderId ProviderId { get; init; }
        public ModelId ModelId { get; init; }
        public ModelVersionId ModelVersionId { get; init; }
        public string InputPromptHash { get; init; } = string.Empty;
        public string OutputHash { get; init; } = string.Empty;
        public string SchemaVersion { get; init; } = "1.0";
        public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
        public Guid WorkspaceId { get; init; }
        public MissionRunId? MissionRunId { get; init; }
    }

    public record BrainInferenceResult
    {
        public InferenceRequestId InferenceId { get; init; } = InferenceRequestId.New();
        public BrainRequestId RequestId { get; init; }
        public ProviderId ProviderId { get; init; }
        public ModelId ModelId { get; init; }
        public ModelVersionId ModelVersionId { get; init; }
        public string RawOutput { get; init; } = string.Empty;
        public string? ParsedStructuredJson { get; init; }
        public int InputTokens { get; init; }
        public int OutputTokens { get; init; }
        public long LatencyMs { get; init; }
        public decimal CostUsd { get; init; }
        public ModelProvenance Provenance { get; init; } = new();
        public InferenceLifecycleState Status { get; init; } = InferenceLifecycleState.Completed;
        public string? ErrorMessage { get; init; }
    }

    public record ModelDefinition
    {
        public ModelId ModelId { get; init; }
        public ProviderId ProviderId { get; init; }
        public ModelVersionId ModelVersionId { get; init; }
        public string DisplayName { get; init; } = string.Empty;
        public ModelCapabilityProfile CapabilityProfile { get; init; } = new();
        public ModelApprovalState ApprovalState { get; init; } = ModelApprovalState.Approved;
        public decimal CostPer1kInputTokensUsd { get; init; } = 0m;
        public decimal CostPer1kOutputTokensUsd { get; init; } = 0m;
        public bool IsLegitimatelyFreeRoute { get; init; } = false;
        public DataEgressTier DataEgressTier { get; init; } = DataEgressTier.PublicCommercialAllowed;
    }

    public record ModelRoute
    {
        public ModelRouteId RouteId { get; init; }
        public ProviderId ProviderId { get; init; }
        public ModelId ModelId { get; init; }
        public ModelProviderKind GatewayKind { get; init; }
        public int Priority { get; init; } = 100;
        public bool IsActive { get; init; } = true;
        public ProviderQuotaStatus QuotaStatus { get; init; } = ProviderQuotaStatus.Available;
    }

    public record InferenceBudgetTracker
    {
        public InferenceBudgetId BudgetId { get; init; } = InferenceBudgetId.New();
        public Guid WorkspaceId { get; init; }
        public MissionRunId? MissionRunId { get; init; }
        public decimal AllocatedBudgetUsd { get; init; } = 10.0m;
        public decimal ConsumedBudgetUsd { get; set; } = 0m;
        public long AllocatedTokens { get; init; } = 1_000_000;
        public long ConsumedTokens { get; set; } = 0;

        public decimal RemainingBudgetUsd => Math.Max(0m, AllocatedBudgetUsd - ConsumedBudgetUsd);
        public long RemainingTokens => Math.Max(0, AllocatedTokens - ConsumedTokens);
    }

    public record ContextCompressionResult
    {
        public int OriginalTokenCount { get; init; }
        public int CompressedTokenCount { get; init; }
        public string CompressedPrompt { get; init; } = string.Empty;
        public bool SourceRecordsPreserved { get; init; } = true;
    }

    // ========================================================================
    // 4. BRAIN RUNTIME EVENTS
    // ========================================================================

    public record BrainRequestCreatedEvent(
        BrainRequestId RequestId,
        MissionRunId? MissionRunId,
        string TaskCategory) : RuntimeEventBase
    {
        public override string EventType => nameof(BrainRequestCreatedEvent);
    }

    public record InferenceAdmittedEvent(
        BrainRequestId RequestId,
        ModelRouteId SelectedRouteId) : RuntimeEventBase
    {
        public override string EventType => nameof(InferenceAdmittedEvent);
    }

    public record ModelRouteSelectedEvent(
        BrainRequestId RequestId,
        ModelRouteId RouteId,
        ProviderId ProviderId,
        ModelId ModelId) : RuntimeEventBase
    {
        public override string EventType => nameof(ModelRouteSelectedEvent);
    }

    public record InferenceCompletedEvent(
        BrainRequestId RequestId,
        InferenceRequestId InferenceId,
        ProviderId ProviderId,
        ModelId ModelId,
        int TotalTokens,
        decimal CostUsd,
        string OutputHash) : RuntimeEventBase
    {
        public override string EventType => nameof(InferenceCompletedEvent);
    }

    public record InferenceFailedEvent(
        BrainRequestId RequestId,
        ProviderId ProviderId,
        string Reason) : RuntimeEventBase
    {
        public override string EventType => nameof(InferenceFailedEvent);
    }

    public record InferenceFallbackTriggeredEvent(
        BrainRequestId RequestId,
        ModelRouteId PrimaryRouteId,
        ModelRouteId FallbackRouteId,
        string Reason) : RuntimeEventBase
    {
        public override string EventType => nameof(InferenceFallbackTriggeredEvent);
    }

    public record InferenceQuotaObservedEvent(
        ProviderId ProviderId,
        ProviderQuotaStatus Status) : RuntimeEventBase
    {
        public override string EventType => nameof(InferenceQuotaObservedEvent);
    }

    public record InferenceBudgetConsumedEvent(
        BrainRequestId RequestId,
        decimal ConsumedCostUsd,
        long ConsumedTokens,
        decimal RemainingBudgetUsd) : RuntimeEventBase
    {
        public override string EventType => nameof(InferenceBudgetConsumedEvent);
    }

    public record InferenceSchemaRejectedEvent(
        BrainRequestId RequestId,
        string SchemaViolations) : RuntimeEventBase
    {
        public override string EventType => nameof(InferenceSchemaRejectedEvent);
    }

    public record InferenceContextCompressedEvent(
        BrainRequestId RequestId,
        int OriginalTokens,
        int CompressedTokens) : RuntimeEventBase
    {
        public override string EventType => nameof(InferenceContextCompressedEvent);
    }

    public record CircuitBreakerTrippedEvent(
        ProviderId ProviderId,
        string Reason,
        DateTime TrippedAtUtc) : RuntimeEventBase
    {
        public override string EventType => nameof(CircuitBreakerTrippedEvent);
    }
}
