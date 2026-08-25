using System;

namespace BusinessModelApp.Core.AI
{
    public class AICallRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrganizationId { get; set; }
        public Guid WorkspaceId { get; set; }
        public Guid UserId { get; set; }
        public string? AgentId { get; set; }
        public AITaskType TaskType { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens => PromptTokens + CompletionTokens;
        public decimal? EstimatedCost { get; set; }
        public long LatencyMs { get; set; }
        public bool CacheHit { get; set; }
        public int FallbackAttempts { get; set; }
        public string RequestCorrelationId { get; set; } = string.Empty;
        public string? OmniRouteRequestId { get; set; }
        
        // Commercial Journey Attribution
        public Guid? LeadId { get; set; }
        public Guid? OpportunityId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
