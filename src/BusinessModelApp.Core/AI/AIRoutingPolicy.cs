namespace BusinessModelApp.Core.AI
{
    public enum ProviderSelectionMode
    {
        GatewayManaged = 1,
        FixedApprovedModel = 2,
        FallbackPool = 3
    }

    public enum AIRoutingPreference
    {
        Balanced = 1,
        HighQuality = 2,
        UltraLowLatency = 3,
        CostControlled = 4
    }

    public class AIRoutingPolicy
    {
        public AITaskType TaskType { get; set; }
        public AIRoutingPreference Preference { get; set; } = AIRoutingPreference.Balanced;
        public ProviderSelectionMode SelectionMode { get; set; } = ProviderSelectionMode.GatewayManaged;
        public string? TargetModelStrategy { get; set; }
        public int MaxLatencyMs { get; set; } = 30000;
        public double Temperature { get; set; } = 0.2;
        public int MaxTokens { get; set; } = 2048;
        public bool AllowFallback { get; set; } = true;
        public bool StreamingRequired { get; set; } = false;
        public bool AllowCaching { get; set; } = true;
    }
}
