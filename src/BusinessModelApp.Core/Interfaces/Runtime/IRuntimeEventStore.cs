using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime;

namespace BusinessModelApp.Core.Interfaces.Runtime
{
    public interface IRuntimeEventStore
    {
        Task<RuntimeEventEnvelope> AppendEventAsync<TEvent>(
            TEvent domainEvent,
            Guid runId,
            CancellationToken cancellationToken = default) where TEvent : class, IRuntimeEvent;

        Task<IReadOnlyList<RuntimeEventEnvelope>> ReadStreamAsync(
            Guid runId,
            long fromSequence = 1,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<RuntimeEventEnvelope>> ReadCorrelationStreamAsync(
            Guid correlationId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<RuntimeEventEnvelope>> ReadTenantStreamAsync(
            Guid workspaceId,
            long fromSequence = 1,
            int maxCount = 100,
            CancellationToken cancellationToken = default);

        Task<bool> VerifyStreamIntegrityAsync(
            Guid runId,
            CancellationToken cancellationToken = default);
    }
}
