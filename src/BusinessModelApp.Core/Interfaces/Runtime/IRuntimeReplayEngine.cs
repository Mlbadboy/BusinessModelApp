using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime;

namespace BusinessModelApp.Core.Interfaces.Runtime
{
    public record ReplayResult
    {
        public bool IsSuccessful { get; init; }
        public RuntimeRunId RunId { get; init; }
        public long EventsReplayedCount { get; init; }
        public long ReplayedToSequence { get; init; }
        public RunState ReconstructedState { get; init; }
        public string? ReconstructedPayload { get; init; }
        public string? FailureReason { get; init; }
        public bool IsSimulationOnly { get; init; }
        public IReadOnlyList<string> InvariantViolations { get; init; } = Array.Empty<string>();

        public static ReplayResult Succeeded(RuntimeRunId runId, long count, long sequence, RunState state, string payload, bool isSimulation = false) => new()
        {
            IsSuccessful = true,
            RunId = runId,
            EventsReplayedCount = count,
            ReplayedToSequence = sequence,
            ReconstructedState = state,
            ReconstructedPayload = payload,
            IsSimulationOnly = isSimulation
        };

        public static ReplayResult Failed(RuntimeRunId runId, string reason, IReadOnlyList<string>? violations = null) => new()
        {
            IsSuccessful = false,
            RunId = runId,
            FailureReason = reason,
            InvariantViolations = violations ?? Array.Empty<string>()
        };
    }

    public interface IRuntimeReplayEngine
    {
        Task<ReplayResult> ReplayAsync(
            RuntimeRunId runId,
            CancellationToken cancellationToken = default);

        Task<ReplayResult> ReplayFromSequenceAsync(
            RuntimeRunId runId,
            long fromSequenceNumber,
            CancellationToken cancellationToken = default);

        Task<ReplayResult> ReplaySimulationAsync(
            RuntimeRunId runId,
            string alternateModelId,
            CancellationToken cancellationToken = default);
    }
}
