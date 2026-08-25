using BusinessModelApp.Core.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

            // Apply any pending migrations
            // await context.Database.MigrateAsync();

            logger.LogInformation("Seeding database...");

            // Seed Roles
            if (!roleManager.Roles.Any())
            {
                logger.LogInformation("No roles found, seeding roles.");
                var roles = new[] { "CEO", "CBO", "CFO", "CHRO", "Admin", "Manager", "Agent" };
                foreach (var roleName in roles)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        await roleManager.CreateAsync(new Role { Name = roleName, NormalizedName = roleName.ToUpper() });
                        logger.LogInformation($"Role '{roleName}' created.");
                    }
                }
            }
            else
            {
                logger.LogInformation("Roles already exist, skipping role seeding.");
            }

            // Seed Users
            if (!userManager.Users.Any())
            {
                logger.LogInformation("No users found, seeding users.");

                var executiveUsers = new[]
                {
                    new { Email = "ceo@example.com", FirstName = "Alex", LastName = "Chen", Role = "CEO", Password = "Exec123!" },
                    new { Email = "cbo@example.com", FirstName = "Brenda", LastName = "Mendoza", Role = "CBO", Password = "Exec123!" },
                    new { Email = "cfo@example.com", FirstName = "Charles", LastName = "Finley", Role = "CFO", Password = "Exec123!" },
                    new { Email = "chro@example.com", FirstName = "Diana", LastName = "Huang", Role = "CHRO", Password = "Exec123!" }
                };

                foreach (var userData in executiveUsers)
                {
                    var user = new User
                    {
                        UserName = userData.Email,
                        Email = userData.Email,
                        FirstName = userData.FirstName,
                        LastName = userData.LastName,
                        EmailConfirmed = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    var result = await userManager.CreateAsync(user, userData.Password);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, userData.Role);
                        logger.LogInformation($"User '{user.Email}' created and assigned to role '{userData.Role}'.");
                    }
                    else
                    {
                        logger.LogError($"Failed to create user '{user.Email}'. Errors: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
            }
            else
            {
                logger.LogInformation("Users already exist, skipping user seeding.");
            }

            logger.LogInformation("Database seeding complete.");
        }
    }
}
