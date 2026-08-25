using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using BusinessModelApp.Core.Domain.Commercial;
using CoreUser = BusinessModelApp.Core.Domain.Users.User;
using CoreRole = BusinessModelApp.Core.Domain.Users.Role;

namespace BusinessModelApp.Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<CoreUser, CoreRole, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Multi-Tenant & Commercial DbSets
        public DbSet<Organization> Organizations { get; set; }
        public DbSet<Workspace> Workspaces { get; set; }
        public DbSet<Lead> Leads { get; set; }
        public DbSet<Opportunity> Opportunities { get; set; }
        public DbSet<Interaction> Interactions { get; set; }
        public DbSet<Activity> Activities { get; set; }
        public DbSet<AuditEvent> AuditEvents { get; set; }
        public DbSet<BusinessActivity> BusinessActivities { get; set; }
        public DbSet<BusinessModelApp.Core.AI.AICallRecord> AICallRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Organization
            modelBuilder.Entity<Organization>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Slug).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Slug).IsUnique();
            });

            // Workspace
            modelBuilder.Entity<Workspace>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Currency).HasMaxLength(10).HasDefaultValue("INR");
                
                entity.HasOne(e => e.Organization)
                      .WithMany(o => o.Workspaces)
                      .HasForeignKey(e => e.OrganizationId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Lead
            modelBuilder.Entity<Lead>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ContactName).IsRequired().HasMaxLength(150);
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.Property(e => e.Phone).HasMaxLength(50);
                entity.Property(e => e.CompanyName).HasMaxLength(200);

                entity.HasOne(e => e.Workspace)
                      .WithMany(w => w.Leads)
                      .HasForeignKey(e => e.WorkspaceId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Opportunity
            modelBuilder.Entity<Opportunity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(250);
                entity.Property(e => e.EstimatedValue).HasPrecision(18, 2);
                entity.Property(e => e.Currency).HasMaxLength(10).HasDefaultValue("INR");

                entity.HasOne(e => e.Workspace)
                      .WithMany(w => w.Opportunities)
                      .HasForeignKey(e => e.WorkspaceId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Lead)
                      .WithOne(l => l.Opportunity)
                      .HasForeignKey<Opportunity>(e => e.LeadId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Interaction
            modelBuilder.Entity<Interaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Channel).HasMaxLength(50);

                entity.HasOne(e => e.Lead)
                      .WithMany(l => l.Interactions)
                      .HasForeignKey(e => e.LeadId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Activity (Legacy & General activities)
            modelBuilder.Entity<Activity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.PerformedByName).HasMaxLength(100);

                entity.HasOne(e => e.Opportunity)
                      .WithMany(o => o.Activities)
                      .HasForeignKey(e => e.OpportunityId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // AuditEvent (Security & compliance audit log)
            modelBuilder.Entity<AuditEvent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ActionName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PerformedByName).HasMaxLength(100);
                entity.HasIndex(e => new { e.WorkspaceId, e.Timestamp });
                entity.HasIndex(e => new { e.EntityType, e.EntityId });
            });

            // BusinessActivity (Commercial touchpoints)
            modelBuilder.Entity<BusinessActivity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.PerformedByName).HasMaxLength(100);

                entity.HasOne(e => e.Opportunity)
                      .WithMany()
                      .HasForeignKey(e => e.OpportunityId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Lead)
                      .WithMany()
                      .HasForeignKey(e => e.LeadId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.WorkspaceId, e.CreatedAt });
            });

            // Core User Organization Scoping
            modelBuilder.Entity<CoreUser>(entity =>
            {
                entity.HasOne<Organization>()
                      .WithMany()
                      .HasForeignKey(u => u.OrganizationId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne<Workspace>()
                      .WithMany()
                      .HasForeignKey(u => u.DefaultWorkspaceId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // AICallRecord (Append-only AI telemetry)
            modelBuilder.Entity<BusinessModelApp.Core.AI.AICallRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Provider).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Model).IsRequired().HasMaxLength(150);
                entity.Property(e => e.EstimatedCost).HasPrecision(18, 6);
                entity.Property(e => e.RequestCorrelationId).HasMaxLength(100);
                entity.Property(e => e.OmniRouteRequestId).HasMaxLength(100);

                entity.HasIndex(e => new { e.OrganizationId, e.WorkspaceId, e.CreatedAt });
                entity.HasIndex(e => e.RequestCorrelationId);
            });
        }
    }
}
