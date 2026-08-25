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
                    SelectionMode = ProviderSelectionMode.GatewayManaged,
                    TargetModelStrategy = "anthropic/claude-3-5-sonnet",
                    Temperature = 0.2,
                    MaxTokens = 1500,
                    AllowFallback = true
                },
                AITaskType.VoiceQualification => new AIRoutingPolicy
                {
                    TaskType = taskType,
                    Preference = AIRoutingPreference.UltraLowLatency,
                    SelectionMode = ProviderSelectionMode.GatewayManaged,
                    TargetModelStrategy = "google/gemini-2.0-flash-001",
                    Temperature = 0.1,
                    MaxTokens = 512,
                    StreamingRequired = true,
                    AllowFallback = true
                },
                AITaskType.Classification => new AIRoutingPolicy
                {
                    TaskType = taskType,
                    Preference = AIRoutingPreference.CostControlled,
                    SelectionMode = ProviderSelectionMode.GatewayManaged,
                    TargetModelStrategy = "deepseek/deepseek-chat",
                    Temperature = 0.0,
                    MaxTokens = 256,
                    AllowFallback = true
                },
                AITaskType.LeadQualification or AITaskType.OpportunityAnalysis => new AIRoutingPolicy
                {
                    TaskType = taskType,
                    Preference = AIRoutingPreference.Balanced,
                    SelectionMode = ProviderSelectionMode.GatewayManaged,
                    TargetModelStrategy = "openai/gpt-4o-mini",
                    Temperature = 0.2,
                    MaxTokens = 1024,
                    AllowFallback = true
                },
                _ => new AIRoutingPolicy
                {
                    TaskType = taskType,
                    Preference = preference,
                    SelectionMode = ProviderSelectionMode.GatewayManaged,
                    Temperature = 0.3,
                    MaxTokens = 2048,
                    AllowFallback = true
                }
            };
        }
    }
}
