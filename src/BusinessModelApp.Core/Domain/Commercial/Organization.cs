using System;
using System.Collections.Generic;
using BusinessModelApp.Core.Domain.Common;

namespace BusinessModelApp.Core.Domain.Commercial
{
    public class Organization : Entity, ISoftDeletable
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Plan { get; set; } = "Free"; // Free, Pro, Enterprise
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; private set; }

        // Navigation
        public virtual ICollection<Workspace> Workspaces { get; set; } = new List<Workspace>();

        public void MarkAsDeleted()
        {
            IsDeleted = true;
            UpdateTimestamps();
        }
    }
}
