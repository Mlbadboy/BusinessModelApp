using System;
using System.Collections.Generic;
using BusinessModelApp.Core.Domain.Common;

namespace BusinessModelApp.Core.Domain.Commercial
{
    public class Workspace : Entity, ISoftDeletable
    {
        public Guid OrganizationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Currency { get; set; } = "INR"; // Default INR
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; private set; }

        // Navigation
        public virtual Organization Organization { get; set; }
        public virtual ICollection<Lead> Leads { get; set; } = new List<Lead>();
        public virtual ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();

        public void MarkAsDeleted()
        {
            IsDeleted = true;
            UpdateTimestamps();
        }
    }
}
