using System;

namespace BusinessModelApp.Core.DTOs.Agent
{
    public class AgentActivityDto
    {
        public Guid Id { get; set; }
        public Guid AgentId { get; set; }
        public string AgentName { get; set; }
        public string ActivityType { get; set; } // e.g., "TaskStarted", "TaskCompleted", "StatusChange"
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
        public string Details { get; set; }
    }
}
