using Microsoft.EntityFrameworkCore;
using BusinessModelApp.Infrastructure.Data.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using CoreUser = BusinessModelApp.Core.Domain.Users.User;
using CoreRole = BusinessModelApp.Core.Domain.Users.Role;

namespace BusinessModelApp.Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<CoreUser, CoreRole, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // DbSets
        public DbSet<BusinessModel> BusinessModels { get; set; }
        public DbSet<RevenueSource> RevenueSources { get; set; }
        public DbSet<Expense> Expenses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<BusinessModel>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
            });

            modelBuilder.Entity<RevenueSource>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Currency).HasMaxLength(50);
                entity.Property(e => e.BusinessModelId).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();

                entity.HasOne(e => e.BusinessModel)
                      .WithMany(b => b.RevenueSources)
                      .HasForeignKey(e => e.BusinessModelId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Expense>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Currency).HasMaxLength(50);
                entity.Property(e => e.Category).HasMaxLength(100);
                entity.Property(e => e.BusinessModelId).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();

                entity.HasOne(e => e.BusinessModel)
                      .WithMany(b => b.Expenses)
                      .HasForeignKey(e => e.BusinessModelId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CoreUser>(entity =>
            {
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            });

            modelBuilder.Entity<CoreRole>(entity =>
            {
                entity.Property(e => e.Description).HasMaxLength(500);
            });
        }
    }
}
