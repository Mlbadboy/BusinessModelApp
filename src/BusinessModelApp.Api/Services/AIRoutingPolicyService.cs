using BusinessModelApp.Core.AI;

namespace BusinessModelApp.Api.Services
{
    public class AIRoutingPolicyService : IAIRoutingPolicyService
    {
        public AIRoutingPolicy ResolvePolicy(AITaskType taskType, AIRoutingPreference preference = AIRoutingPreference.Balanced)
        {
            return taskType switch
            {
                AITaskType.ExecutiveBrief => new AIRoutingPolicy
                {
                    TaskType = taskType,
                    Preference = AIRoutingPreference.HighQuality,
                    Quality = QualityTier.High,
                    Latency = new LatencyTarget { TargetP50Ms = 1200, TargetP95Ms = 4000 },
                    MaxEstimatedCost = 0.50m,
                    SelectionMode = ProviderSelectionMode.GatewayManaged,
                    TargetModelStrategy = "anthropic/claude-3-5-sonnet",
                    Temperature = 0.2,
                    MaxTokens = 1500,
                    AllowFallback = true
                },
                AITaskType.BusinessHealthExplanation => new AIRoutingPolicy
                {
                    TaskType = taskType,
                    Preference = AIRoutingPreference.HighQuality,
                    Quality = QualityTier.High,
                    Latency = new LatencyTarget { TargetP50Ms = 1000, TargetP95Ms = 3000 },
                    MaxEstimatedCost = 0.25m,
                    SelectionMode = ProviderSelectionMode.GatewayManaged,
                    TargetModelStrategy = "anthropic/claude-3-5-sonnet",
                    Temperature = 0.1,
                    MaxTokens = 1024,
                    AllowFallback = true
                },
                AITaskType.OpportunityAnalysis => new AIRoutingPolicy
                {
                    TaskType = taskType,
                    Preference = AIRoutingPreference.Balanced,
                    Quality = QualityTier.Balanced,
                    Latency = new LatencyTarget { TargetP50Ms = 1500, TargetP95Ms = 4000 },
                    MaxEstimatedCost = 0.20m,
                    SelectionMode = ProviderSelectionMode.GatewayManaged,
                    TargetModelStrategy = "openai/gpt-4o-mini",
                    Temperature = 0.2,
                    MaxTokens = 1024,
                    AllowFallback = true
                },
                AITaskType.LeadQualification => new AIRoutingPolicy
                {
                    TaskType = taskType,
                    Preference = AIRoutingPreference.Balanced,
                    Quality = QualityTier.Balanced,
                    Latency = new LatencyTarget { TargetP50Ms = 800, TargetP95Ms = 2500 },
                    MaxEstimatedCost = 0.10m,
                    SelectionMode = ProviderSelectionMode.GatewayManaged,
                    TargetModelStrategy = "openai/gpt-4o-mini",
                    Temperature = 0.1,
                    MaxTokens = 512,
                    AllowFallback = true
                },
                AITaskType.VoiceQualification => new AIRoutingPolicy
                {
                    TaskType = taskType,
                    Preference = AIRoutingPreference.UltraLowLatency,
                    Quality = QualityTier.Balanced,
                    Latency = new LatencyTarget { TargetP50Ms = 350, TargetP95Ms = 800 },
                    MaxEstimatedCost = 0.05m,
                    SelectionMode = ProviderSelectionMode.FallbackPool,
                    TargetModelStrategy = "google/gemini-2.0-flash-001",
                    Temperature = 0.1,
                    MaxTokens = 256,
                    StreamingRequired = true,
                    AllowFallback = true
                },
                AITaskType.Classification => new AIRoutingPolicy
                {
                    TaskType = taskType,
                    Preference = AIRoutingPreference.CostControlled,
                    Quality = QualityTier.Standard,
                    Latency = new LatencyTarget { TargetP50Ms = 500, TargetP95Ms = 1500 },
                    MaxEstimatedCost = 0.01m,
                    SelectionMode = ProviderSelectionMode.GatewayManaged,
                    TargetModelStrategy = "deepseek/deepseek-chat",
                    Temperature = 0.0,
                    MaxTokens = 256,
                    AllowFallback = true
                },
                _ => new AIRoutingPolicy
                {
                    TaskType = taskType,
                    Preference = preference,
                    Quality = QualityTier.Standard,
                    Latency = new LatencyTarget { TargetP50Ms = 1500, TargetP95Ms = 5000 },
                    SelectionMode = ProviderSelectionMode.GatewayManaged,
                    Temperature = 0.3,
                    MaxTokens = 2048,
                    AllowFallback = true
                }
            };
        }
    }
}
