using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using BusinessModelApp.Core.AI.Governance;
using BusinessModelApp.Infrastructure.Data;
using BusinessModelApp.Infrastructure.Interceptors;
using BusinessModelApp.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BusinessModelApp.Tests.AI
{
    public class AIBudgetConcurrencyTests
    {
        private AppDbContext CreateDbContext()
        {
            var interceptor = new AppendOnlyAuditInterceptor();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .AddInterceptors(interceptor)
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task BudgetReservation_ShouldPreventOverspendUnderConcurrentLoad()
        {
            // Arrange
            using var context = CreateDbContext();
            var orgId = Guid.NewGuid();
            var wsId = Guid.NewGuid();

            // Set tight monthly budget of ₹1.00
            var policy = new AIBudgetPolicy
            {
                OrganizationId = orgId,
                WorkspaceId = wsId,
                MonthlyBudgetCap = 1.00m,
                EnforceHardCap = true
            };
            context.AIBudgetPolicies.Add(policy);
            await context.SaveChangesAsync();

            var budgetService = new BudgetReservationService(context, NullLogger<BudgetReservationService>.Instance);

            // Act: 20 simultaneous parallel requests, each attempting to reserve ₹0.20
            var attempts = 20;
            var estimatedCostPerRequest = 0.20m;
            var results = new ConcurrentBag<BudgetReservationResult>();

            var tasks = Enumerable.Range(0, attempts).Select(async _ =>
            {
                var res = await budgetService.ReserveBudgetAsync(orgId, wsId, estimatedCostPerRequest);
                results.Add(res);
            });

            await Task.WhenAll(tasks);

            // Assert: Exactly 5 requests (5 * 0.20 = 1.00) can succeed; all remaining 15 must be rejected!
            var allowedCount = results.Count(r => r.IsAllowed);
            var rejectedCount = results.Count(r => !r.IsAllowed);

            allowedCount.Should().Be(5);
            rejectedCount.Should().Be(15);
        }
    }
}
