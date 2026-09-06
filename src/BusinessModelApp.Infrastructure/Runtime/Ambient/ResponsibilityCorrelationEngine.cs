using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Responsibilities;
using BusinessModelApp.Core.Interfaces.Ambient;

namespace BusinessModelApp.Infrastructure.Runtime.Ambient
{
    public class ResponsibilityCorrelationEngine : IResponsibilityCorrelationEngine
    {
        private readonly ConcurrentBag<AmbientBusinessEvent> _eventLog = new();

        public Task RecordEventForCorrelationAsync(AmbientBusinessEvent businessEvent, CancellationToken cancellationToken = default)
        {
            if (businessEvent == null) throw new ArgumentNullException(nameof(businessEvent));
            _eventLog.Add(businessEvent);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AmbientBusinessEvent>> CorrelateEventsAsync(
            Guid workspaceId,
            string entityScope,
            TimeSpan correlationWindow,
            CancellationToken cancellationToken = default)
        {
            var cutoff = DateTime.UtcNow - correlationWindow;
            var correlated = _eventLog
                .Where(e => e.WorkspaceId == workspaceId &&
                            e.OccurredAtUtc >= cutoff &&
                            (string.Equals(e.EntityType, entityScope, StringComparison.OrdinalIgnoreCase) ||
                             entityScope == "global"))
                .OrderByDescending(e => e.OccurredAtUtc)
                .ToList();

            return Task.FromResult<IReadOnlyList<AmbientBusinessEvent>>(correlated);
        }
    }
}
