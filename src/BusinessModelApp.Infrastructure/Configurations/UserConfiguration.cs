using BusinessModelApp.Core.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusinessModelApp.Infrastructure.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");
            
            builder.HasKey(u => u.Id);
            
            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(256);
                
            builder.HasIndex(u => u.Email)
                .IsUnique();
                
            builder.Property(u => u.FirstName)
                .IsRequired()
                .HasMaxLength(100);
                
            builder.Property(u => u.LastName)
                .IsRequired()
                .HasMaxLength(100);
                
            builder.Property(u => u.HashedPassword)
                .IsRequired()
                .HasMaxLength(256);
                
            builder.Property(u => u.Salt)
                .IsRequired()
                .HasMaxLength(128);
                
            builder.Property(u => u.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
                
            builder.Property(u => u.LastLogin)
                .IsRequired(false);
                
            // Configure soft delete filter
            builder.HasQueryFilter(u => !u.IsDeleted);
            
            // Configure relationships
            builder.HasMany(u => u.UserRoles)
                .WithOne(ur => ur.User)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.HasMany(u => u.BusinessModels)
                .WithOne(bm => bm.CreatedBy)
                .HasForeignKey(bm => bm.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
