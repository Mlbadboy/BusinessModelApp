using System.Collections.Generic;

namespace BusinessModelApp.Infrastructure.AI.OmniRoute
{
    public class OmniRouteOptions
    {
        public const string SectionName = "AI:Gateway";

        public string Provider { get; set; } = "OmniRoute";
        public string BaseUrl { get; set; } = "http://localhost:8000";
        public string ApiKey { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 60;
        public bool EnableCaching { get; set; } = true;
        public Dictionary<string, string> DefaultModelMapping { get; set; } = new Dictionary<string, string>
        {
            { "HighQuality", "anthropic/claude-3-5-sonnet" },
            { "UltraLowLatency", "google/gemini-2.0-flash-001" },
            { "CostControlled", "deepseek/deepseek-chat" },
            { "Balanced", "openai/gpt-4o-mini" }
        };
    }
}
