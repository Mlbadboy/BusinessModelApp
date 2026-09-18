using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Missions;
using BusinessModelApp.Core.Interfaces.Runtime.Missions;

namespace BusinessModelApp.Infrastructure.Runtime.Missions.Coordination
{
    public class CrossMissionDependencyResolver : ICrossMissionDependencyResolver
    {
        private readonly IMissionCoordinationStore _store;

        public CrossMissionDependencyResolver(IMissionCoordinationStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<CrossMissionDependency> RegisterDependencyAsync(
            CrossMissionDependency dep,
            CancellationToken ct = default)
        {
            if (dep == null) throw new ArgumentNullException(nameof(dep));
            if (string.IsNullOrWhiteSpace(dep.DependencyId))
            {
                dep.DependencyId = $"DEP-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
            }
            await _store.SaveDependencyAsync(dep, ct);
            return dep;
        }

        public async Task<bool> AreDependenciesSatisfiedAsync(
            string tenantId,
            string consumerMissionId,
            string consumerNodeId,
            CancellationToken ct = default)
        {
            var deps = await _store.ListDependenciesAsync(tenantId, consumerMissionId, ct);
            var nodeDeps = deps.Where(d => d.ConsumerNodeId == consumerNodeId).ToList();
            if (nodeDeps.Count == 0) return true; // No dependencies

            return nodeDeps.All(d => d.IsSatisfied);
        }

        public async Task MarkArtifactProducedAsync(
            string tenantId,
            string producerMissionId,
            string artifactType,
            string artifactId,
            CancellationToken ct = default)
        {
            // Scan dependencies across tenant that wait for this artifact
            var allTickets = await _store.ListTicketsAsync(tenantId, ct);
            foreach (var ticket in allTickets)
            {
                var deps = await _store.ListDependenciesAsync(tenantId, ticket.MissionId, ct);
                foreach (var dep in deps)
                {
                    if (dep.PrerequisiteMissionId == producerMissionId &&
                        string.Equals(dep.PrerequisiteArtifactType, artifactType, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(dep.PrerequisiteArtifactId, artifactId, StringComparison.OrdinalIgnoreCase))
                    {
                        dep.IsSatisfied = true;
                        dep.SatisfiedAtUtc = DateTime.UtcNow;
                        await _store.SaveDependencyAsync(dep, ct);
                    }
                }
            }
        }
    }
}
