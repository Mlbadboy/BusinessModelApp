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
    public class InMemoryRuntimeCheckpointStore : IRuntimeCheckpointStore
    {
        private readonly ConcurrentDictionary<Guid, List<RuntimeCheckpoint>> _checkpoints = new();
        private readonly object _lock = new();

        public Task SaveCheckpointAsync(RuntimeCheckpoint checkpoint, CancellationToken cancellationToken = default)
        {
            if (checkpoint == null) throw new ArgumentNullException(nameof(checkpoint));

            lock (_lock)
            {
                var list = _checkpoints.GetOrAdd(checkpoint.RunId.Value, _ => new List<RuntimeCheckpoint>());
                list.Add(checkpoint);
            }

            return Task.CompletedTask;
        }

        public Task<RuntimeCheckpoint?> GetLatestCheckpointAsync(RuntimeRunId runId, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                if (!_checkpoints.TryGetValue(runId.Value, out var list) || list.Count == 0)
                    return Task.FromResult<RuntimeCheckpoint?>(null);

                var latest = list.OrderByDescending(c => c.SequenceNumber).FirstOrDefault();
                return Task.FromResult<RuntimeCheckpoint?>(latest);
            }
        }

        public Task<IReadOnlyList<RuntimeCheckpoint>> GetCheckpointsAsync(RuntimeRunId runId, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                if (!_checkpoints.TryGetValue(runId.Value, out var list))
                    return Task.FromResult<IReadOnlyList<RuntimeCheckpoint>>(Array.Empty<RuntimeCheckpoint>());

                return Task.FromResult<IReadOnlyList<RuntimeCheckpoint>>(list.OrderBy(c => c.SequenceNumber).ToList());
            }
        }
    }
}
