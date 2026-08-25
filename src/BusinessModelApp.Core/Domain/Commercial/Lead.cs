using System;
using System.Collections.Generic;
using BusinessModelApp.Core.Domain.Common;

namespace BusinessModelApp.Core.Domain.Commercial
{
    public enum LeadStatus
    {
        New = 0,
        Contacted = 1,
        Qualified = 2,
        Unqualified = 3,
        Converted = 4
    }

    public enum LeadSource
    {
        InboundWeb = 0,
        VoiceAI = 1,
        WhatsApp = 2,
        Email = 3,
        Referral = 4,
        Manual = 5
    }

    public class Lead : Entity, ISoftDeletable
    {
        public Guid WorkspaceId { get; set; }
        public string ContactName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public LeadStatus Status { get; set; } = LeadStatus.New;
        public LeadSource Source { get; set; } = LeadSource.Manual;
        public double QualityScore { get; set; } = 0.0; // AI Lead Quality Score 0-100
        public string Notes { get; set; } = string.Empty;
        public Guid? AssignedToUserId { get; set; }
        public bool IsDeleted { get; private set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        // Navigation
        public virtual Workspace Workspace { get; set; }
        public virtual ICollection<Interaction> Interactions { get; set; } = new List<Interaction>();
        public virtual Opportunity Opportunity { get; set; }

        public void MarkAsDeleted()
        {
            IsDeleted = true;
            UpdateTimestamps();
        }
    }
}
