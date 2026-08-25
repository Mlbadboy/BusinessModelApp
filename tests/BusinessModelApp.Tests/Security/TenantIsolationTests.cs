using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BusinessModelApp.Api.Services;
using BusinessModelApp.Core.Domain.Commercial;
using BusinessModelApp.Core.Domain.Users;
using BusinessModelApp.Infrastructure.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BusinessModelApp.Tests.Security
{
    public class TenantIsolationTests
    {
        private AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task UserContextService_ShouldRejectCrossTenantWorkspaceAccess()
        {
            // Arrange: 2 Organizations, 2 Workspaces, 2 Users
            using var context = CreateDbContext();

            var orgA = new Organization { Name = "Org A", Slug = "org-a", IsActive = true };
            var orgB = new Organization { Name = "Org B", Slug = "org-b", IsActive = true };
            context.Organizations.AddRange(orgA, orgB);
            await context.SaveChangesAsync();

            var wsA = new Workspace { OrganizationId = orgA.Id, Name = "Workspace A", IsActive = true };
            var wsB = new Workspace { OrganizationId = orgB.Id, Name = "Workspace B", IsActive = true };
            context.Workspaces.AddRange(wsA, wsB);
            await context.SaveChangesAsync();

            var userA = new User
            {
                UserName = "userA@orga.com",
                Email = "userA@orga.com",
                FirstName = "User",
                LastName = "A",
                OrganizationId = orgA.Id,
                DefaultWorkspaceId = wsA.Id,
                IsActive = true
            };
            context.Users.Add(userA);
            await context.SaveChangesAsync();

            // Mock HttpContext with User A credentials
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userA.Id.ToString()),
                new Claim(ClaimTypes.Email, userA.Email!)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext { User = principal };
            mockHttpContextAccessor.Setup(h => h.HttpContext).Returns(httpContext);

            var userContextService = new UserContextService(mockHttpContextAccessor.Object, context);

            // Act & Assert: User A requesting their own workspace succeeds
            var authorizedWs = await userContextService.GetAuthorizedWorkspaceIdAsync(wsA.Id);
            authorizedWs.Should().Be(wsA.Id);

            // Act & Assert: User A attempting to access Workspace B from Org B MUST fail with UnauthorizedAccessException
            Func<Task> actCrossTenant = async () => await userContextService.GetAuthorizedWorkspaceIdAsync(wsB.Id);
            await actCrossTenant.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("*Access denied*");
        }

        [Fact]
        public async Task UserContextService_ShouldRejectInactiveUser()
        {
            // Arrange: Inactive user
            using var context = CreateDbContext();
            var org = new Organization { Name = "Active Org", Slug = "active-org", IsActive = true };
            context.Organizations.Add(org);
            await context.SaveChangesAsync();

            var ws = new Workspace { OrganizationId = org.Id, Name = "Active Workspace", IsActive = true };
            context.Workspaces.Add(ws);
            await context.SaveChangesAsync();

            var inactiveUser = new User
            {
                UserName = "revoked@org.com",
                Email = "revoked@org.com",
                FirstName = "Revoked",
                LastName = "User",
                OrganizationId = org.Id,
                DefaultWorkspaceId = ws.Id,
                IsActive = false // Deactivated
            };
            context.Users.Add(inactiveUser);
            await context.SaveChangesAsync();

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, inactiveUser.Id.ToString()) };
            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            mockHttpContextAccessor.Setup(h => h.HttpContext).Returns(new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")) });

            var userContextService = new UserContextService(mockHttpContextAccessor.Object, context);

            // Act & Assert
            Func<Task> act = async () => await userContextService.GetAuthorizedWorkspaceIdAsync(ws.Id);
            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }
    }
}
