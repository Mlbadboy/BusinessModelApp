using BusinessModelApp.Core.AI;
using BusinessModelApp.Core.Domain.Commercial;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BusinessModelApp.Infrastructure.Interceptors
{
    public class AppendOnlyAuditInterceptor : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            ValidateAppendOnly(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            ValidateAppendOnly(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private static void ValidateAppendOnly(DbContext? context)
        {
            if (context == null) return;

            foreach (var entry in context.ChangeTracker.Entries())
            {
                if (entry.Entity is AuditEvent || entry.Entity is Activity || entry.Entity is BusinessActivity || entry.Entity is AICallRecord)
                {
                    if (entry.State == EntityState.Modified)
                    {
                        throw new InvalidOperationException(
                            $"Audit records of type '{entry.Entity.GetType().Name}' are immutable. Modifications are strictly forbidden.");
                    }

                    if (entry.State == EntityState.Deleted)
                    {
                        throw new InvalidOperationException(
                            $"Audit records of type '{entry.Entity.GetType().Name}' are append-only. Deletions are strictly forbidden.");
                    }
                }
            }
        }
    }
}
