using System;

namespace BusinessModelApp.Core.AI.Governance
{
    public enum AITrafficStatus
    {
        Enabled = 1,
        Degraded = 2,
        EmergencyDisabled = 3
    }

    public class AITrafficControlPolicy
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrganizationId { get; set; }
        public Guid? WorkspaceId { get; set; }
        public AITrafficStatus Status { get; set; } = AITrafficStatus.Enabled;
        public string? DisabledReason { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string UpdatedByName { get; set; } = "System";
    }

    public class AIKillSwitchActiveException : Exception
    {
        public Guid OrganizationId { get; }
        public string Reason { get; }

        public AIKillSwitchActiveException(Guid orgId, string reason)
            : base($"AI Traffic is currently disabled by administrator kill-switch for Organization {orgId}. Reason: {reason}")
        {
            OrganizationId = orgId;
            Reason = reason;
        }
    }
}
