using BusinessModelApp.Core.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusinessModelApp.Infrastructure.Configurations
{
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.ToTable("Roles");
            
            builder.HasKey(r => r.Id);
            
            builder.Property(r => r.Name)
                .IsRequired()
                .HasMaxLength(100);
                
            builder.HasIndex(r => r.Name)
                .IsUnique();
                
            builder.Property(r => r.Description)
                .HasMaxLength(500);
                
            builder.Property(r => r.IsSystemRole)
                .IsRequired()
                .HasDefaultValue(false);
                
            // Configure relationships
            builder.HasMany(r => r.UserRoles)
                .WithOne(ur => ur.Role)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.HasMany(r => r.RolePermissions)
                .WithOne(rp => rp.Role)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Seed system roles
            builder.HasData(
                new Role("SystemAdmin", "System Administrator with full access", true) { Id = Guid.Parse("11111111-1111-1111-1111-111111111111") },
                new Role("Admin", "Administrator with full business model access", true) { Id = Guid.Parse("22222222-2222-2222-2222-222222222222") },
                new Role("CEO", "Chief Executive Officer", true) { Id = Guid.Parse("33333333-3333-3333-3333-333333333333") },
                new Role("CTO", "Chief Technology Officer", true) { Id = Guid.Parse("44444444-4444-4444-4444-444444444444") },
                new Role("CFO", "Chief Financial Officer", true) { Id = Guid.Parse("55555555-5555-5555-5555-555555555555") },
                new Role("COO", "Chief Operating Officer", true) { Id = Guid.Parse("66666666-6666-6666-6666-666666666666") },
                new Role("CBO", "Chief Business Officer", true) { Id = Guid.Parse("77777777-7777-7777-7777-777777777777") },
                new Role("Manager", "Department Manager", true) { Id = Guid.Parse("88888888-8888-8888-8888-888888888888") },
                new Role("User", "Standard user with limited access", true) { Id = Guid.Parse("99999999-9999-9999-9999-999999999999") }
            );
        }
    }
    
    public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
    {
        public void Configure(EntityTypeBuilder<UserRole> builder)
        {
            builder.ToTable("UserRoles");
            
            builder.HasKey(ur => ur.Id);
            
            builder.Property(ur => ur.AssignedAt)
                .IsRequired();
                
            // Configure relationships
            builder.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Add unique constraint
            builder.HasIndex(ur => new { ur.UserId, ur.RoleId })
                .IsUnique();
        }
    }
    
    public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
    {
        public void Configure(EntityTypeBuilder<RolePermission> builder)
        {
            builder.ToTable("RolePermissions");
            
            builder.HasKey(rp => rp.Id);
            
            builder.Property(rp => rp.Permission)
                .IsRequired()
                .HasMaxLength(100);
                
            builder.Property(rp => rp.GrantedAt)
                .IsRequired();
                
            // Configure relationships
            builder.HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Add unique constraint
            builder.HasIndex(rp => new { rp.RoleId, rp.Permission })
                .IsUnique();
                
            // Seed default permissions for SystemAdmin role
            builder.HasData(
                // SystemAdmin gets all permissions
                new RolePermission(
                    Guid.Parse("11111111-1111-1111-1111-111111111111"), 
                    "user.read", 
                    Guid.Parse("00000000-0000-0000-0000-000000000001")) { Id = Guid.NewGuid() },
                new RolePermission(
                    Guid.Parse("11111111-1111-1111-1111-111111111111"), 
                    "user.create", 
                    Guid.Parse("00000000-0000-0000-0000-000000000001")) { Id = Guid.NewGuid() },
                // Add all other permissions for SystemAdmin...
                
                // Admin role permissions
                new RolePermission(
                    Guid.Parse("22222222-2222-2222-2222-222222222222"), 
                    "user.read", 
                    Guid.Parse("00000000-0000-0000-0000-000000000001")) { Id = Guid.NewGuid() },
                new RolePermission(
                    Guid.Parse("22222222-2222-2222-2222-222222222222"), 
                    "businessmodel.create", 
                    Guid.Parse("00000000-0000-0000-0000-000000000001")) { Id = Guid.NewGuid() },
                // Add other admin permissions...
                
                // CEO role permissions
                new RolePermission(
                    Guid.Parse("33333333-3333-3333-3333-333333333333"), 
                    "dashboard.view", 
                    Guid.Parse("00000000-0000-0000-0000-000000000001")) { Id = Guid.NewGuid() },
                new RolePermission(
                    Guid.Parse("33333333-3333-3333-3333-333333333333"), 
                    "dashboard.analytics", 
                    Guid.Parse("00000000-0000-0000-0000-000000000001")) { Id = Guid.NewGuid() }
                // Add other role permissions as needed
            );
        }
    }
}
