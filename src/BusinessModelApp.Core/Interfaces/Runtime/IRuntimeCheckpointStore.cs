using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime;

namespace BusinessModelApp.Core.Interfaces.Runtime
{
    public interface IRuntimeCheckpointStore
    {
        Task SaveCheckpointAsync(
            RuntimeCheckpoint checkpoint,
            CancellationToken cancellationToken = default);

        Task<RuntimeCheckpoint?> GetLatestCheckpointAsync(
            RuntimeRunId runId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<RuntimeCheckpoint>> GetCheckpointsAsync(
            RuntimeRunId runId,
            CancellationToken cancellationToken = default);
    }
}
