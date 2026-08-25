using BusinessModelApp.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusinessModelApp.Infrastructure.Data.Configurations
{
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.HasKey(r => r.Id);
            
            builder.Property(r => r.RoleName)
                .IsRequired()
                .HasMaxLength(255);
            
            builder.Property(r => r.Description)
                .HasMaxLength(1000);
            
            builder.Property(r => r.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            // Seed default roles with detailed permissions
            builder.HasData(
                new Role
                {
                    Id = 1,
                    RoleName = "CEO",
                    Description = "Chief Executive Officer",
                    CreatedAt = DateTime.UtcNow,
                    Permissions = new List<string>
                    {
                        "business.model.create",
                        "business.model.update",
                        "business.model.delete",
                        "business.metrics.view",
                        "user.manage",
                        "role.assign",
                        "audit.log.view",
                        "system.settings.modify",
                        "data.export",
                        "data.import"
                    },
                    AccessLevels = new List<string> { "Admin", "Write", "Read" },
                    AllowedModules = new List<string> { "All" },
                    SpecialPrivileges = new List<string> { "SystemAdmin", "SuperUser" }
                },
                new Role
                {
                    Id = 2,
                    RoleName = "COO",
                    Description = "Chief Operating Officer",
                    CreatedAt = DateTime.UtcNow,
                    Permissions = new List<string>
                    {
                        "business.model.view",
                        "business.model.update",
                        "business.metrics.view",
                        "user.view",
                        "role.view",
                        "audit.log.view"
                    },
                    AccessLevels = new List<string> { "Write", "Read" },
                    AllowedModules = new List<string> { "Operations", "Analytics" },
                    SpecialPrivileges = new List<string> { "OperationsAdmin" }
                },
                new Role
                {
                    Id = 3,
                    RoleName = "CFO",
                    Description = "Chief Financial Officer",
                    CreatedAt = DateTime.UtcNow,
                    Permissions = new List<string>
                    {
                        "business.model.view",
                        "business.model.update",
                        "business.metrics.view",
                        "financial.reports.view",
                        "financial.reports.generate",
                        "audit.log.view"
                    },
                    AccessLevels = new List<string> { "Write", "Read" },
                    AllowedModules = new List<string> { "Finance", "Analytics" },
                    SpecialPrivileges = new List<string> { "FinanceAdmin" }
                },
                new Role
                {
                    Id = 4,
                    RoleName = "CTO",
                    Description = "Chief Technology Officer",
                    CreatedAt = DateTime.UtcNow,
                    Permissions = new List<string>
                    {
                        "business.model.view",
                        "business.model.update",
                        "technical.metrics.view",
                        "technical.metrics.update",
                        "system.settings.view",
                        "audit.log.view"
                    },
                    AccessLevels = new List<string> { "Write", "Read" },
                    AllowedModules = new List<string> { "Technology", "Development", "Infrastructure" },
                    SpecialPrivileges = new List<string> { "TechAdmin" }
                },
                new Role
                {
                    Id = 5,
                    RoleName = "CBO",
                    Description = "Chief Business Officer",
                    CreatedAt = DateTime.UtcNow,
                    Permissions = new List<string>
                    {
                        "business.model.view",
                        "business.model.update",
                        "market.metrics.view",
                        "market.metrics.update",
                        "customer.metrics.view",
                        "audit.log.view"
                    },
                    AccessLevels = new List<string> { "Write", "Read" },
                    AllowedModules = new List<string> { "Business", "Marketing", "Sales" },
                    SpecialPrivileges = new List<string> { "BusinessAdmin" }
                },
                new Role
                {
                    Id = 6,
                    RoleName = "Developer",
                    Description = "System Developer",
                    CreatedAt = DateTime.UtcNow,
                    Permissions = new List<string>
                    {
                        "code.write",
                        "code.review",
                        "system.settings.view",
                        "audit.log.view"
                    },
                    AccessLevels = new List<string> { "Write", "Read" },
                    AllowedModules = new List<string> { "Development", "Infrastructure" },
                    SpecialPrivileges = new List<string> { "Developer" }
                },
                new Role
                {
                    Id = 7,
                    RoleName = "Analyst",
                    Description = "Business Analyst",
                    CreatedAt = DateTime.UtcNow,
                    Permissions = new List<string>
                    {
                        "business.model.view",
                        "business.metrics.view",
                        "report.view"
                    },
                    AccessLevels = new List<string> { "Read" },
                    AllowedModules = new List<string> { "Analytics" },
                    SpecialPrivileges = new List<string> { "Analyst" }
                }
            );
        }
    }
}
