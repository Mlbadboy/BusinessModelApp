using BusinessModelApp.Core.Domain.Common;
using System;

namespace BusinessModelApp.Core.Domain.Users
{
    public class RolePermission : Entity
    {
        public Guid RoleId { get; private set; }
        public string Permission { get; private set; }
        public DateTime GrantedAt { get; private set; }
        public Guid? GrantedBy { get; private set; }
        
        // Navigation properties
        public Role Role { get; private set; }

        private RolePermission() { } // For EF Core

        public RolePermission(Guid roleId, string permission, Guid? grantedBy = null)
        {
            if (roleId == Guid.Empty) throw new ArgumentException("Role ID cannot be empty", nameof(roleId));
            if (string.IsNullOrWhiteSpace(permission)) 
                throw new ArgumentException("Permission cannot be empty", nameof(permission));
            
            RoleId = roleId;
            Permission = permission.Trim().ToLowerInvariant();
            GrantedAt = DateTime.UtcNow;
            GrantedBy = grantedBy;
        }
    }
}
