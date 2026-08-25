using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.AI
{
    public class AIRequest
    {
        public AITaskType TaskType { get; set; } = AITaskType.GeneralAssistant;
        public List<AIMessage> Messages { get; set; } = new List<AIMessage>();
        public AIRoutingPreference Preference { get; set; } = AIRoutingPreference.Balanced;

        // Contextual tenant identifiers (Overwritten and validated server-side by IUserContextService)
        public Guid? OrganizationId { get; set; }
        public Guid? WorkspaceId { get; set; }
        public Guid? UserId { get; set; }
        public string? AgentId { get; set; }
        public string RequestCorrelationId { get; set; } = Guid.NewGuid().ToString("N");

        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
}
