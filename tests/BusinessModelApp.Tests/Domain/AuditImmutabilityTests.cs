using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Commercial;
using BusinessModelApp.Infrastructure.Data;
using BusinessModelApp.Infrastructure.Interceptors;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public class AuditImmutabilityTests
    {
        private AppDbContext CreateDbContextWithInterceptor()
        {
            var interceptor = new AppendOnlyAuditInterceptor();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .AddInterceptors(interceptor)
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task AuditEvent_ShouldAllowAppendOperation()
        {
            using var context = CreateDbContextWithInterceptor();

            var auditEvent = new AuditEvent
            {
                WorkspaceId = Guid.NewGuid(),
                EntityType = "Opportunity",
                EntityId = Guid.NewGuid(),
                EventType = AuditEventType.StageChanged,
                ActionName = "Stage Advanced",
                Description = "Opportunity progressed to Proposal.",
                PerformedByName = "Test Agent"
            };

            context.AuditEvents.Add(auditEvent);
            Func<Task> act = async () => await context.SaveChangesAsync();

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task AuditEvent_ShouldRejectModifications()
        {
            using var context = CreateDbContextWithInterceptor();

            var auditEvent = new AuditEvent
            {
                WorkspaceId = Guid.NewGuid(),
                EntityType = "Opportunity",
                EntityId = Guid.NewGuid(),
                EventType = AuditEventType.StageChanged,
                ActionName = "Stage Advanced",
                Description = "Initial stage change description."
            };

            context.AuditEvents.Add(auditEvent);
            await context.SaveChangesAsync();

            // Act: Attempt to modify existing audit record
            auditEvent.Description = "Tampered description after the fact.";
            context.AuditEvents.Update(auditEvent);

            Func<Task> actUpdate = async () => await context.SaveChangesAsync();

            await actUpdate.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*immutable*Modifications are strictly forbidden*");
        }

        [Fact]
        public async Task AuditEvent_ShouldRejectDeletions()
        {
            using var context = CreateDbContextWithInterceptor();

            var auditEvent = new AuditEvent
            {
                WorkspaceId = Guid.NewGuid(),
                EntityType = "Opportunity",
                EntityId = Guid.NewGuid(),
                EventType = AuditEventType.DealClosedWon,
                ActionName = "Deal Won",
                Description = "Audited close event."
            };

            context.AuditEvents.Add(auditEvent);
            await context.SaveChangesAsync();

            // Act: Attempt to delete audit record
            context.AuditEvents.Remove(auditEvent);

            Func<Task> actDelete = async () => await context.SaveChangesAsync();

            await actDelete.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*append-only*Deletions are strictly forbidden*");
        }
    }
}
