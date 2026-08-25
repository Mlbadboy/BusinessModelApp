using System;

namespace BusinessModelApp.Core.Domain.Commercial
{
    public enum BusinessActivityType
    {
        Call = 1,
        Meeting = 2,
        Email = 3,
        Proposal = 4,
        FollowUp = 5,
        Note = 6
    }

    public class BusinessActivity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid WorkspaceId { get; set; }
        public Guid? OpportunityId { get; set; }
        public Guid? LeadId { get; set; }
        public BusinessActivityType Type { get; set; } = BusinessActivityType.Note;
        public string Title { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string PerformedByName { get; set; } = "User";
        public int DurationMinutes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Opportunity? Opportunity { get; set; }
        public Lead? Lead { get; set; }
    }
}
