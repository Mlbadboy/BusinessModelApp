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

            // 4. Seed Initial Leads & Opportunities (Vertical Slice demonstration)
            if (!context.Leads.Any())
            {
                var lead1 = new Lead
                {
                    WorkspaceId = workspace.Id,
                    ContactName = "Rajesh Gupta",
                    CompanyName = "Apex Retail Dynamics",
                    Email = "rajesh@apexretail.in",
                    Phone = "+91 98230 11223",
                    Source = LeadSource.VoiceAI,
                    Status = LeadStatus.Qualified,
                    QualityScore = 92.5,
                    Notes = "Voice AI qualified inbound inquiry for 500 branch POS automation."
                };
                context.Leads.Add(lead1);
                await context.SaveChangesAsync();

                var opp1 = new Opportunity
                {
                    WorkspaceId = workspace.Id,
                    LeadId = lead1.Id,
                    Title = "Apex Retail - Enterprise AI Operations Rollout",
                    EstimatedValue = 1850000m, // ₹18.5 Lakhs
                    Currency = "INR",
                    Stage = OpportunityStage.Proposal,
                    Probability = 0.5,
                    ExpectedCloseDate = DateTime.UtcNow.AddDays(30),
                    PrimaryConcern = "Deployment SLA and local compliance.",
                    NextStep = "Deliver tailored commercial proposal and security architecture brief."
                };
                context.Opportunities.Add(opp1);
                await context.SaveChangesAsync();

                // Seed Activities
                context.Activities.Add(new Activity
                {
                    OpportunityId = opp1.Id,
                    Type = ActivityType.InteractionLogged,
                    Title = "Inbound Voice Call Completed",
                    Description = "Voice agent qualified lead with 92.5% confidence score.",
                    PerformedByName = "Growth Voice Agent"
                });
                context.Activities.Add(new Activity
                {
                    OpportunityId = opp1.Id,
                    Type = ActivityType.StageChanged,
                    Title = "Stage advanced to Proposal",
                    Description = "Lead successfully converted to Opportunity.",
                    PerformedByName = "System Orchestrator"
                });
                await context.SaveChangesAsync();
                logger.LogInformation("Vertical slice sample leads & opportunities seeded.");
            }

            logger.LogInformation("Database seeding complete.");
        }
    }
}
