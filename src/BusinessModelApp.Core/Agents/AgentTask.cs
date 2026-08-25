using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Agents
{
    public enum AgentTaskStatus
    {
        Pending = 0,
        Running = 1,
        Completed = 2,
        BlockedOnApproval = 3,
        Failed = 4
    }

    public class AgentTask
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid MissionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public AgentRole AssignedRole { get; set; }
        public AgentActionType ActionType { get; set; }
        public AgentTaskStatus Status { get; set; } = AgentTaskStatus.Pending;

        public List<Guid> DependencyTaskIds { get; set; } = new List<Guid>();
        public string InputParametersJson { get; set; } = "{}";
        public string OutputResultJson { get; set; } = "{}";
        public string ThoughtStream { get; set; } = string.Empty;
        public string? EvidenceReferenceId { get; set; }
        public Guid? ApprovalRequestId { get; set; }

        public decimal EstimatedCostINR { get; set; } = 0.50m;
        public decimal ActualCostINR { get; set; } = 0.0m;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
