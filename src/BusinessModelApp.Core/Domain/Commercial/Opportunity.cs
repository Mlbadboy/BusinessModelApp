using System;
using System.Collections.Generic;
using BusinessModelApp.Core.Domain.Common;

namespace BusinessModelApp.Core.Domain.Commercial
{
    public enum OpportunityStage
    {
        Discovery = 0,
        Proposal = 1,
        Negotiation = 2,
        ClosedWon = 3,
        ClosedLost = 4
    }

    public class Opportunity : Entity, ISoftDeletable
    {
        public Guid WorkspaceId { get; set; }
        public Guid LeadId { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal EstimatedValue { get; set; } = 0m;
        public string Currency { get; set; } = "INR";
        public OpportunityStage Stage { get; set; } = OpportunityStage.Discovery;
        public double Probability { get; set; } = 0.2; // 0.0 - 1.0
        public DateTime? ExpectedCloseDate { get; set; }
        public Guid? OwnerUserId { get; set; }
        public string PrimaryConcern { get; set; } = string.Empty;
        public string NextStep { get; set; } = string.Empty;
        public bool IsDeleted { get; private set; }

        // Navigation
        public virtual Workspace Workspace { get; set; }
        public virtual Lead Lead { get; set; }
        public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();

        public void AdvanceStage(OpportunityStage newStage)
        {
            Stage = newStage;
            switch (newStage)
            {
                case OpportunityStage.Discovery:
                    Probability = 0.2;
                    break;
                case OpportunityStage.Proposal:
                    Probability = 0.5;
                    break;
                case OpportunityStage.Negotiation:
                    Probability = 0.8;
                    break;
                case OpportunityStage.ClosedWon:
                    Probability = 1.0;
                    break;
                case OpportunityStage.ClosedLost:
                    Probability = 0.0;
                    break;
            }
            UpdateTimestamps();
        }

        public void MarkAsDeleted()
        {
            IsDeleted = true;
            UpdateTimestamps();
        }
    }
}
