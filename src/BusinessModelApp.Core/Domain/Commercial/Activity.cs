using System;
using BusinessModelApp.Core.Domain.Common;

namespace BusinessModelApp.Core.Domain.Commercial
{
    public enum ActivityType
    {
        NoteAdded = 0,
        StageChanged = 1,
        OwnerAssigned = 2,
        InteractionLogged = 3,
        ProposalSent = 4,
        HumanApprovalGranted = 5,
        HumanApprovalRejected = 6,
        AIRecommendationGenerated = 7
    }

    public class Activity : Entity
    {
        public Guid OpportunityId { get; set; }
        public ActivityType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid? PerformedByUserId { get; set; }
        public string PerformedByName { get; set; } = "System";

        // Navigation
        public virtual Opportunity Opportunity { get; set; }
    }
}
