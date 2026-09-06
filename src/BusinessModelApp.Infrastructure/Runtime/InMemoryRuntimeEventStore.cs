using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Interfaces.Runtime;

namespace BusinessModelApp.Infrastructure.Runtime
{
    public class RuntimeEventChainTamperedException : Exception
    {
        public RuntimeEventChainTamperedException(string message) : base(message) { }
    }

    public class InMemoryRuntimeEventStore : IRuntimeEventStore
    {
        private readonly ConcurrentDictionary<Guid, List<RuntimeEventEnvelope>> _runStreams = new();
        private readonly ConcurrentDictionary<Guid, List<RuntimeEventEnvelope>> _correlationStreams = new();
        private readonly ConcurrentDictionary<Guid, List<RuntimeEventEnvelope>> _tenantStreams = new();
        private readonly object _lock = new();

        public Task<RuntimeEventEnvelope> AppendEventAsync<TEvent>(
            TEvent domainEvent,
            Guid runId,
            CancellationToken cancellationToken = default) where TEvent : class, IRuntimeEvent
        {
            if (domainEvent == null) throw new ArgumentNullException(nameof(domainEvent));
            if (runId == Guid.Empty) throw new ArgumentException("RunId cannot be empty.", nameof(runId));

            lock (_lock)
            {
                var runStream = _runStreams.GetOrAdd(runId, _ => new List<RuntimeEventEnvelope>());
                long nextSequence = runStream.Count + 1;
                string previousHash = runStream.Count == 0 ? "GENESIS" : runStream[^1].EventHash;

                var envelope = RuntimeEventEnvelope.Create(domainEvent, previousHash, nextSequence);

                runStream.Add(envelope);

                // Add to correlation stream
                var corrStream = _correlationStreams.GetOrAdd(envelope.CorrelationId, _ => new List<RuntimeEventEnvelope>());
                corrStream.Add(envelope);

                // Add to tenant stream
                var tenantStream = _tenantStreams.GetOrAdd(envelope.WorkspaceId, _ => new List<RuntimeEventEnvelope>());
                tenantStream.Add(envelope);

                return Task.FromResult(envelope);
            }
        }

        public Task<IReadOnlyList<RuntimeEventEnvelope>> ReadStreamAsync(
            Guid runId,
            long fromSequence = 1,
            CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                if (!_runStreams.TryGetValue(runId, out var stream))
                    return Task.FromResult<IReadOnlyList<RuntimeEventEnvelope>>(Array.Empty<RuntimeEventEnvelope>());

                var result = stream.Where(e => e.SequenceNumber >= fromSequence).ToList();
                return Task.FromResult<IReadOnlyList<RuntimeEventEnvelope>>(result);
            }
        }

        public Task<IReadOnlyList<RuntimeEventEnvelope>> ReadCorrelationStreamAsync(
            Guid correlationId,
            CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                if (!_correlationStreams.TryGetValue(correlationId, out var stream))
                    return Task.FromResult<IReadOnlyList<RuntimeEventEnvelope>>(Array.Empty<RuntimeEventEnvelope>());

                return Task.FromResult<IReadOnlyList<RuntimeEventEnvelope>>(stream.ToList());
            }
        }

        public Task<IReadOnlyList<RuntimeEventEnvelope>> ReadTenantStreamAsync(
            Guid workspaceId,
            long fromSequence = 1,
            int maxCount = 100,
            CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                if (!_tenantStreams.TryGetValue(workspaceId, out var stream))
                    return Task.FromResult<IReadOnlyList<RuntimeEventEnvelope>>(Array.Empty<RuntimeEventEnvelope>());

                var result = stream.Where(e => e.SequenceNumber >= fromSequence).Take(maxCount).ToList();
                return Task.FromResult<IReadOnlyList<RuntimeEventEnvelope>>(result);
            }
        }

        public Task<bool> VerifyStreamIntegrityAsync(
            Guid runId,
            CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                if (!_runStreams.TryGetValue(runId, out var stream))
                    return Task.FromResult(true);

                string expectedPreviousHash = "GENESIS";
                long expectedSequence = 1;

                foreach (var evt in stream)
                {
                    if (evt.SequenceNumber != expectedSequence)
                        return Task.FromResult(false);

                    if (!evt.VerifyHashIntegrity(expectedPreviousHash))
                        return Task.FromResult(false);

                    expectedPreviousHash = evt.EventHash;
                    expectedSequence++;
                }

                return Task.FromResult(true);
            }
        }
    }
}
