using System;

namespace BusinessModelApp.Core.Domain.Commercial
{
    public enum AuditEventType
    {
        LeadCreated = 1,
        LeadQualified = 2,
        OpportunityCreated = 3,
        StageChanged = 4,
        ValueChanged = 5,
        OwnerChanged = 6,
        ApprovalGranted = 7,
        DealClosedWon = 8,
        DealClosedLost = 9
    }

    public class AuditEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid WorkspaceId { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public AuditEventType EventType { get; set; }
        public string ActionName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? OldStateJson { get; set; }
        public string? NewStateJson { get; set; }
        public Guid? PerformedByUserId { get; set; }
        public string PerformedByName { get; set; } = "System";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
