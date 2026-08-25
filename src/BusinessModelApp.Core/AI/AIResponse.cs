using System;

namespace BusinessModelApp.Core.AI
{
    public class AIUsage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens => PromptTokens + CompletionTokens;
    }

    public class AIResponse
    {
        public string Content { get; set; } = string.Empty;
        public string ModelUsed { get; set; } = string.Empty;
        public string ProviderUsed { get; set; } = string.Empty;
        public string RequestId { get; set; } = string.Empty;
        public AIUsage Usage { get; set; } = new AIUsage();
        public long LatencyMs { get; set; }
        public decimal? EstimatedCost { get; set; } // Nullable when OmniRoute does not provide authoritative cost
        public bool CacheHit { get; set; }
        public int FallbackAttempts { get; set; }
        public string FinishReason { get; set; } = "stop";
    }

    public class AIStreamChunk
    {
        public string DeltaContent { get; set; } = string.Empty;
        public string? FinishReason { get; set; }
    }
}
