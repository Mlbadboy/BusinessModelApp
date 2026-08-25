using BusinessModelApp.Core.Domain.BusinessModels;
using BusinessModelApp.Core.Domain.Common;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Users
{
    public class User : IdentityUser<Guid>, IEntity<Guid>, ISoftDeletable
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Role { get; set; } // Added for simple role handling in mock mode
        // public string Email { get; set; } // Provided by IdentityUser
        // public string HashedPassword { get; set; } // Provided by IdentityUser (PasswordHash)
        // public string Salt { get; set; } // Handled by Identity's password hasher
        public bool IsActive { get; set; }
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
