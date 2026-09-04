using BusinessModelApp.Core.Domain.Commercial;
using BusinessModelApp.Core.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessModelApp.Infrastructure.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider, ILogger logger)
        {
            var context = serviceProvider.GetRequiredService<AppDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<Role>>();

            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            logger.LogInformation("Seeding multi-tenant business data...");

            // 1. Seed Organization & Workspace
            var org = await context.Organizations.FirstOrDefaultAsync(o => o.Slug == "bitbloom-ai");
            if (org == null)
            {
                org = new Organization
                {
                    Name = "Bitbloom Services Enterprise",
                    Slug = "bitbloom-ai",
                    Plan = "Enterprise",
                    IsActive = true
                };
                context.Organizations.Add(org);
                await context.SaveChangesAsync();
                logger.LogInformation("Default organization created.");
            }

            var workspace = await context.Workspaces.FirstOrDefaultAsync(w => w.OrganizationId == org.Id);
            if (workspace == null)
            {
                workspace = new Workspace
                {
                    OrganizationId = org.Id,
                    Name = "Commercial Operations & Growth",
                    Description = "Primary operating workspace for leads, opportunities, and revenue.",
                    Currency = "INR",
                    IsActive = true
                };
                context.Workspaces.Add(workspace);
                await context.SaveChangesAsync();
                logger.LogInformation("Default workspace created.");
            }

            // 2. Seed Roles
            var roles = new[] { "CEO", "CBO", "CFO", "CHRO", "Admin", "Manager", "Agent" };
            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new Role(roleName, $"{roleName} role", false));
                    logger.LogInformation($"Role '{roleName}' created.");
                }
            }

            // 3. Seed Users
            if (!userManager.Users.Any())
            {
                var executiveUsers = new[]
                {
                    new { Email = "mayur@bitbloom.in", FirstName = "Mayur", LastName = "Prabhune", Role = "CEO", Password = "Password123!" },
                    new { Email = "growth@bitbloom.in", FirstName = "Aarav", LastName = "Sharma", Role = "Manager", Password = "Password123!" },
                    new { Email = "cfo@bitbloom.in", FirstName = "Charles", LastName = "Finley", Role = "CFO", Password = "Password123!" }
                };

                foreach (var userData in executiveUsers)
                {
                    var user = new User
                    {
                        UserName = userData.Email,
                        Email = userData.Email,
                        FirstName = userData.FirstName,
                        LastName = userData.LastName,
                        OrganizationId = org.Id,
                        DefaultWorkspaceId = workspace.Id,
                        EmailConfirmed = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    var result = await userManager.CreateAsync(user, userData.Password);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, userData.Role);
                        logger.LogInformation($"User '{user.Email}' created.");
                    }
                }
            }

            // 4. Commercial truth: No synthetic leads or opportunities are seeded.
            // All leads and opportunities must originate from real human creation or governed agent missions.
            logger.LogInformation("Database identity and workspace initialized (Commercial zero-state enforced).");
            logger.LogInformation("Database seeding complete.");
        }
    }
}
