using BusinessModelApp.Core.Domain.Common;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Users
{
    public class Role : IdentityRole<Guid>, IEntity<Guid>
    {
        // public string Name { get; private set; } // Provided by IdentityRole
        public string Description { get; private set; }
        public bool IsSystemRole { get; private set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();
        public ICollection<RolePermission> RolePermissions { get; private set; } = new List<RolePermission>();

        private Role() { } // For EF Core

        public Role(string name, string description, bool isSystemRole = false) : base(name)
        {
            Description = description ?? throw new ArgumentNullException(nameof(description));
            IsSystemRole = isSystemRole;
        }

        public void Update(string name, string description)
        {
            if (IsSystemRole)
                throw new InvalidOperationException("System roles cannot be modified.");
                
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));

        }
    }
}
