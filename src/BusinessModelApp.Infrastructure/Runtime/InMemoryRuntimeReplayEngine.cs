using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Interfaces.Runtime;

namespace BusinessModelApp.Infrastructure.Runtime
{
    public class InMemoryRuntimeReplayEngine : IRuntimeReplayEngine
    {
        private readonly IRuntimeEventStore _eventStore;

        public InMemoryRuntimeReplayEngine(IRuntimeEventStore eventStore)
        {
            _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        }

        public async Task<ReplayResult> ReplayAsync(RuntimeRunId runId, CancellationToken cancellationToken = default)
        {
            return await ReplayInternalAsync(runId, fromSequence: 1, isSimulation: false, alternateModelId: null, cancellationToken);
        }

        public async Task<ReplayResult> ReplayFromSequenceAsync(RuntimeRunId runId, long fromSequenceNumber, CancellationToken cancellationToken = default)
        {
            return await ReplayInternalAsync(runId, fromSequence: fromSequenceNumber, isSimulation: false, alternateModelId: null, cancellationToken);
        }

        public async Task<ReplayResult> ReplaySimulationAsync(RuntimeRunId runId, string alternateModelId, CancellationToken cancellationToken = default)
        {
            return await ReplayInternalAsync(runId, fromSequence: 1, isSimulation: true, alternateModelId: alternateModelId, cancellationToken);
        }

        private async Task<ReplayResult> ReplayInternalAsync(
            RuntimeRunId runId,
            long fromSequence,
            bool isSimulation,
            string? alternateModelId,
            CancellationToken cancellationToken)
        {
            var stream = await _eventStore.ReadStreamAsync(runId.Value, fromSequence: 1, cancellationToken);
            if (stream == null || stream.Count == 0)
            {
                return ReplayResult.Failed(runId, $"No events found in event store for RunId '{runId}'.");
            }

            // Verify integrity
            bool isIntact = await _eventStore.VerifyStreamIntegrityAsync(runId.Value, cancellationToken);
            if (!isIntact)
            {
                return ReplayResult.Failed(runId, "Cryptographic hash chain verification failed: historical event log was tampered or corrupted.");
            }

            var eventsToReplay = stream.Where(e => e.SequenceNumber >= fromSequence).ToList();
            RunState reconstructedState = RunState.Pending;
            string lastPayload = string.Empty;
            var violations = new List<string>();

            foreach (var evt in eventsToReplay)
            {
                lastPayload = evt.PayloadJson;

                switch (evt.EventType)
                {
                    case "RunCreated":
                        reconstructedState = RunState.Pending;
                        break;
                    case "RuntimeAdmissionGranted":
                        reconstructedState = RunState.Admitted;
                        break;
                    case "RunStarted":
                        reconstructedState = RunState.Running;
                        break;
                    case "RunPaused":
                        reconstructedState = RunState.Suspended;
                        break;
                    case "RunResumed":
                        reconstructedState = RunState.Running;
                        break;
                    case "RunCompleted":
                        reconstructedState = RunState.Completed;
                        break;
                    case "RunFailed":
                        reconstructedState = RunState.Failed;
                        break;
                    case "RunCancelled":
                        reconstructedState = RunState.Cancelled;
                        break;
                    case "RunKilled":
                        reconstructedState = RunState.Killed;
                        break;
                }

                if (isSimulation)
                {
                    // Assert simulation invariant: replay must never invoke external side-effects
                    if (evt.EventType.Contains("ExecutionDispatched") || evt.EventType.Contains("SideEffect"))
                    {
                        violations.Add($"Side effect invocation detected during simulation replay: Event '{evt.EventType}'.");
                    }
                }
            }

            if (violations.Count > 0)
            {
                return ReplayResult.Failed(runId, "Simulation replay failed safety invariants.", violations);
            }

            long replayedSequence = eventsToReplay.Count > 0 ? eventsToReplay[^1].SequenceNumber : 0;
            return ReplayResult.Succeeded(runId, eventsToReplay.Count, replayedSequence, reconstructedState, lastPayload, isSimulation);
        }
    }
}
