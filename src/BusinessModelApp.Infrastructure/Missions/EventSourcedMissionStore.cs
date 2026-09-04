using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Missions;

namespace BusinessModelApp.Infrastructure.Missions
{
    public class EventSourcedMissionStore : IMissionEventStore
    {
        private readonly ConcurrentDictionary<Guid, List<IMissionEvent>> _streams = new();
        private readonly ConcurrentDictionary<Guid, HashSet<long>> _sequenceTracker = new();
        private readonly ConcurrentDictionary<Guid, bool> _seenEventIds = new();
        private readonly object _lock = new();

        public Task<bool> AppendEventAsync(IMissionEvent evt)
        {
            if (evt == null) throw new ArgumentNullException(nameof(evt));

            lock (_lock)
            {
                // Idempotency: duplicate event ID check
                if (_seenEventIds.ContainsKey(evt.EventId))
                {
                    return Task.FromResult(true);
                }

                var stream = _streams.GetOrAdd(evt.MissionId, _ => new List<IMissionEvent>());
                var sequences = _sequenceTracker.GetOrAdd(evt.MissionId, _ => new HashSet<long>());

                // Sequence duplicate check
                if (sequences.Contains(evt.SequenceNumber))
                {
                    return Task.FromResult(true); // Idempotent success
                }

                stream.Add(evt);
                sequences.Add(evt.SequenceNumber);
                _seenEventIds[evt.EventId] = true;

                return Task.FromResult(true);
            }
        }

        public Task<IReadOnlyList<IMissionEvent>> GetEventStreamAsync(Guid missionId)
        {
            if (_streams.TryGetValue(missionId, out var stream))
            {
                var sorted = stream.OrderBy(e => e.SequenceNumber).ToList();
                return Task.FromResult<IReadOnlyList<IMissionEvent>>(sorted);
            }

            return Task.FromResult<IReadOnlyList<IMissionEvent>>(Array.Empty<IMissionEvent>());
        }

        public async Task<MissionStateProjection> ReplayMissionStateAsync(Guid missionId)
        {
            var stream = await GetEventStreamAsync(missionId);

            var projection = new MissionStateProjection
            {
                MissionId = missionId,
                CurrentState = DurableMissionState.Draft,
                EventCount = stream.Count
            };

            foreach (var evt in stream)
            {
                projection.LastSequenceNumber = evt.SequenceNumber;

                switch (evt)
                {
                    case MissionCreatedEvent created:
                        projection.Objective = created.Objective;
                        projection.TargetRevenueINR = created.TargetRevenueINR;
                        projection.TotalAllocatedBudgetINR = created.MaxBudgetINR;
                        projection.CurrentState = DurableMissionState.Draft;
                        break;

                    case RevenueBaselineCalculatedEvent baseline:
                        projection.AutonomousGapINR = baseline.AutonomousGapINR;
                        projection.CurrentState = DurableMissionState.RevenueBaselineCalculated;
                        break;

                    case AgentDispatchedEvent dispatched:
                        if (!projection.DispatchedAgentIds.Contains(dispatched.AgentId))
                        {
                            projection.DispatchedAgentIds.Add(dispatched.AgentId);
                        }
                        break;

                    case MissionStateTransitionedEvent transitioned:
                        projection.CurrentState = transitioned.NewState;
                        break;
                }
            }

            return projection;
        }
    }
}
