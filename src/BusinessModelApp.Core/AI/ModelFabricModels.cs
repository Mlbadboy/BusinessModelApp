using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.AI
{
    public enum ModelTier
    {
        FastAndCheap = 1,
        ReasoningAndStrategy = 2,
        VisionAndMultimodal = 3,
        CodingAndDelivery = 4
    }

    public enum PrivacyClassification
    {
        Public = 1,
        InternalEnterprise = 2,
        ConfidentialFinancial = 3,
        RestrictedPII = 4
    }

    public class ModelDescriptor
    {
        public string ModelId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public ModelTier Tier { get; set; } = ModelTier.FastAndCheap;
        public int ContextWindow { get; set; } = 128000;
        public decimal CostPer1kTokensINR { get; set; } = 0.05m;
        public int TypicalLatencyMs { get; set; } = 800;
        public PrivacyClassification PrivacyClass { get; set; } = PrivacyClassification.InternalEnterprise;
        public List<string> Capabilities { get; set; } = new();
        public double ReliabilityScore { get; set; } = 0.95; // 0.0 to 1.0
        public bool IsActive { get; set; } = true;
    }

    public class ModelRoutingRequest
    {
        public AITaskType TaskType { get; set; } = AITaskType.GeneralAssistant;
        public string Prompt { get; set; } = string.Empty;
        public int EstimatedTokens { get; set; } = 1000;
        public decimal MaxBudgetINR { get; set; } = 10.0m;
        public int MaxLatencyMs { get; set; } = 5000;
        public PrivacyClassification MinimumPrivacy { get; set; } = PrivacyClassification.InternalEnterprise;
        public ModelTier PreferredTier { get; set; } = ModelTier.FastAndCheap;
        public bool RequiresDeterministicOutput { get; set; } = false;
    }

    public class ModelRoutingDecision
    {
        public string SelectedModelId { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public ModelTier Tier { get; set; }
        public decimal EstimatedCostINR { get; set; }
        public List<string> FallbackModelIds { get; set; } = new();
        public string RoutingReason { get; set; } = string.Empty;
        public DateTime DecidedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public interface IModelRegistry
    {
        void RegisterModel(ModelDescriptor descriptor);
        ModelDescriptor? GetModel(string modelId);
        IReadOnlyList<ModelDescriptor> GetModelsByTier(ModelTier tier);
        IReadOnlyList<ModelDescriptor> GetAllModels();
    }

    public interface IModelRouter
    {
        ModelRoutingDecision Route(ModelRoutingRequest request);
    }
}
