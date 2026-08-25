using BusinessModelApp.Core.Domain.BusinessModels;
using BusinessModelApp.Core.Domain.Common;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Users
{
    public class User : IdentityUser<Guid>, IEntity<Guid>, ISoftDeletable
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = "Member"; // Added for simple role handling
        public Guid? OrganizationId { get; set; }
        public Guid? DefaultWorkspaceId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? LastLogin { get; set; }
        public bool IsDeleted { get; private set; }

        // Navigation properties
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public virtual ICollection<BusinessModel> BusinessModels { get; set; } = new List<BusinessModel>();

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public void MarkAsDeleted()
        {
            IsDeleted = true;
        }
    }
}
