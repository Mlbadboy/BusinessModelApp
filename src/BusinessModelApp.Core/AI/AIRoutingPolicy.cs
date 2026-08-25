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

    public enum QualityTier
    {
        Standard = 1,
        Balanced = 2,
        High = 3,
        MaximumReasoning = 4
    }

    public class LatencyTarget
    {
        public int TargetP50Ms { get; set; } = 1500;
        public int TargetP95Ms { get; set; } = 5000;
    }

    public class AIRoutingPolicy
    {
        public AITaskType TaskType { get; set; }
        public AIRoutingPreference Preference { get; set; } = AIRoutingPreference.Balanced;
        public QualityTier Quality { get; set; } = QualityTier.Balanced;
        public LatencyTarget Latency { get; set; } = new LatencyTarget();
        public decimal? MaxEstimatedCost { get; set; } // Upper bound estimated target cost per request
        public ProviderSelectionMode SelectionMode { get; set; } = ProviderSelectionMode.GatewayManaged;
        public string? TargetModelStrategy { get; set; }
        public double Temperature { get; set; } = 0.2;
        public int MaxTokens { get; set; } = 2048;
        public bool AllowFallback { get; set; } = true;
        public bool StreamingRequired { get; set; } = false;
        public bool AllowCaching { get; set; } = true;
    }
}
