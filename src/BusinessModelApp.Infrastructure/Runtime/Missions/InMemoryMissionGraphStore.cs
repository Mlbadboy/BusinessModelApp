using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Interfaces.Missions;

namespace BusinessModelApp.Infrastructure.Runtime.Missions
{
    public class InMemoryMissionGraphStore : IMissionGraphStore
    {
        private readonly ConcurrentDictionary<Guid, MissionRecord> _missions = new();
        private readonly ConcurrentDictionary<string, MissionGraph> _graphs = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<Guid, List<MissionGraph>> _graphHistory = new();

        public Task SaveMissionAsync(MissionRecord mission, CancellationToken ct = default)
        {
            if (mission == null) throw new ArgumentNullException(nameof(mission));
            _missions[mission.Id.Value] = mission;
            return Task.CompletedTask;
        }

        public Task<MissionRecord?> GetMissionAsync(MissionId missionId, CancellationToken ct = default)
        {
            _missions.TryGetValue(missionId.Value, out var mission);
            return Task.FromResult(mission);
        }

        public Task SaveGraphAsync(MissionGraph graph, CancellationToken ct = default)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));

            var key = $"{graph.GraphId.Value}:{graph.Version.Value}";
            _graphs[key] = graph;

            _graphHistory.AddOrUpdate(
                graph.GraphId.Value,
                new List<MissionGraph> { graph },
                (_, list) =>
                {
                    lock (list)
                    {
                        list.RemoveAll(g => g.Version.Value == graph.Version.Value);
                        list.Add(graph);
                        list.Sort((a, b) => a.Version.Value.CompareTo(b.Version.Value));
                    }
                    return list;
                });

            return Task.CompletedTask;
        }

        public Task<MissionGraph?> GetGraphAsync(MissionGraphId graphId, MissionGraphVersion? version = null, CancellationToken ct = default)
        {
            if (version.HasValue)
            {
                var key = $"{graphId.Value}:{version.Value.Value}";
                _graphs.TryGetValue(key, out var graph);
                return Task.FromResult(graph);
            }

            // Get latest version
            if (_graphHistory.TryGetValue(graphId.Value, out var list))
            {
                lock (list)
                {
                    return Task.FromResult(list.LastOrDefault());
                }
            }

            return Task.FromResult<MissionGraph?>(null);
        }

        public Task<IReadOnlyList<MissionGraph>> GetGraphHistoryAsync(MissionGraphId graphId, CancellationToken ct = default)
        {
            if (_graphHistory.TryGetValue(graphId.Value, out var list))
            {
                lock (list)
                {
                    return Task.FromResult<IReadOnlyList<MissionGraph>>(list.ToList());
                }
            }

            return Task.FromResult<IReadOnlyList<MissionGraph>>(Array.Empty<MissionGraph>());
        }
    }
}
