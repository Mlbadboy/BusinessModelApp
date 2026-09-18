using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Organizational;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Memory
{
    public class InMemoryOrganizationalMemoryStore : IOrganizationalMemoryStore
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, OrganizationalPrecedent>> _precedents = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, OrganizationalAntiPattern>> _antiPatterns = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, OrganizationalTrajectory>> _trajectories = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, OrganizationalContextSnapshot>> _snapshots = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, MemoryProvenanceLineage>> _lineages = new();

        public Task SavePrecedentAsync(OrganizationalPrecedent precedent, CancellationToken ct = default)
        {
            if (precedent == null) throw new ArgumentNullException(nameof(precedent));
            var dict = _precedents.GetOrAdd(precedent.TenantId, _ => new ConcurrentDictionary<string, OrganizationalPrecedent>());
            dict[precedent.PrecedentId] = precedent;
            return Task.CompletedTask;
        }

        public Task<OrganizationalPrecedent?> GetPrecedentAsync(string tenantId, string precedentId, CancellationToken ct = default)
        {
            if (_precedents.TryGetValue(tenantId, out var dict) && dict.TryGetValue(precedentId, out var prec))
            {
                return Task.FromResult<OrganizationalPrecedent?>(prec);
            }
            return Task.FromResult<OrganizationalPrecedent?>(null);
        }

        public Task<IReadOnlyList<OrganizationalPrecedent>> ListPrecedentsAsync(string tenantId, CancellationToken ct = default)
        {
            if (_precedents.TryGetValue(tenantId, out var dict))
            {
                return Task.FromResult<IReadOnlyList<OrganizationalPrecedent>>(dict.Values.OrderByDescending(p => p.CreatedUtc).ToList());
            }
            return Task.FromResult<IReadOnlyList<OrganizationalPrecedent>>(Array.Empty<OrganizationalPrecedent>());
        }

        public Task SaveAntiPatternAsync(OrganizationalAntiPattern antiPattern, CancellationToken ct = default)
        {
            if (antiPattern == null) throw new ArgumentNullException(nameof(antiPattern));
            var dict = _antiPatterns.GetOrAdd(antiPattern.TenantId, _ => new ConcurrentDictionary<string, OrganizationalAntiPattern>());
            dict[antiPattern.AntiPatternId] = antiPattern;
            return Task.CompletedTask;
        }

        public Task<OrganizationalAntiPattern?> GetAntiPatternAsync(string tenantId, string antiPatternId, CancellationToken ct = default)
        {
            if (_antiPatterns.TryGetValue(tenantId, out var dict) && dict.TryGetValue(antiPatternId, out var ap))
            {
                return Task.FromResult<OrganizationalAntiPattern?>(ap);
            }
            return Task.FromResult<OrganizationalAntiPattern?>(null);
        }

        public Task<IReadOnlyList<OrganizationalAntiPattern>> ListAntiPatternsAsync(string tenantId, string? domain = null, CancellationToken ct = default)
        {
            if (_antiPatterns.TryGetValue(tenantId, out var dict))
            {
                var query = dict.Values.Where(a => a.IsActive);
                if (!string.IsNullOrWhiteSpace(domain))
                {
                    query = query.Where(a => string.Equals(a.Domain, domain, StringComparison.OrdinalIgnoreCase));
                }
                return Task.FromResult<IReadOnlyList<OrganizationalAntiPattern>>(query.ToList());
            }
            return Task.FromResult<IReadOnlyList<OrganizationalAntiPattern>>(Array.Empty<OrganizationalAntiPattern>());
        }

        public Task SaveTrajectoryAsync(OrganizationalTrajectory trajectory, CancellationToken ct = default)
        {
            if (trajectory == null) throw new ArgumentNullException(nameof(trajectory));
            var dict = _trajectories.GetOrAdd(trajectory.TenantId, _ => new ConcurrentDictionary<string, OrganizationalTrajectory>());
            dict[trajectory.TrajectoryId] = trajectory;
            return Task.CompletedTask;
        }

        public Task<OrganizationalTrajectory?> GetTrajectoryAsync(string tenantId, string trajectoryId, CancellationToken ct = default)
        {
            if (_trajectories.TryGetValue(tenantId, out var dict) && dict.TryGetValue(trajectoryId, out var traj))
            {
                return Task.FromResult<OrganizationalTrajectory?>(traj);
            }
            return Task.FromResult<OrganizationalTrajectory?>(null);
        }

        public Task<OrganizationalTrajectory?> GetTrajectoryForWorkAsync(string tenantId, string workId, CancellationToken ct = default)
        {
            if (_trajectories.TryGetValue(tenantId, out var dict))
            {
                var traj = dict.Values.FirstOrDefault(t => t.WorkId == workId);
                return Task.FromResult(traj);
            }
            return Task.FromResult<OrganizationalTrajectory?>(null);
        }

        public Task<IReadOnlyList<OrganizationalTrajectory>> ListTrajectoriesAsync(string tenantId, CancellationToken ct = default)
        {
            if (_trajectories.TryGetValue(tenantId, out var dict))
            {
                return Task.FromResult<IReadOnlyList<OrganizationalTrajectory>>(dict.Values.OrderByDescending(t => t.StartedUtc).ToList());
            }
            return Task.FromResult<IReadOnlyList<OrganizationalTrajectory>>(Array.Empty<OrganizationalTrajectory>());
        }

        public Task SaveSnapshotAsync(OrganizationalContextSnapshot snapshot, CancellationToken ct = default)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            var dict = _snapshots.GetOrAdd(snapshot.TenantId, _ => new ConcurrentDictionary<string, OrganizationalContextSnapshot>());
            dict[snapshot.SnapshotId] = snapshot;
            return Task.CompletedTask;
        }

        public Task<OrganizationalContextSnapshot?> GetSnapshotAsync(string tenantId, string snapshotId, CancellationToken ct = default)
        {
            if (_snapshots.TryGetValue(tenantId, out var dict) && dict.TryGetValue(snapshotId, out var snap))
            {
                return Task.FromResult<OrganizationalContextSnapshot?>(snap);
            }
            return Task.FromResult<OrganizationalContextSnapshot?>(null);
        }

        public Task SaveProvenanceLineageAsync(MemoryProvenanceLineage lineage, CancellationToken ct = default)
        {
            if (lineage == null) throw new ArgumentNullException(nameof(lineage));
            var dict = _lineages.GetOrAdd(lineage.TenantId, _ => new ConcurrentDictionary<string, MemoryProvenanceLineage>());
            dict[lineage.MemoryId] = lineage;
            return Task.CompletedTask;
        }

        public Task<MemoryProvenanceLineage?> GetProvenanceLineageAsync(string tenantId, string memoryId, CancellationToken ct = default)
        {
            if (_lineages.TryGetValue(tenantId, out var dict) && dict.TryGetValue(memoryId, out var lin))
            {
                return Task.FromResult<MemoryProvenanceLineage?>(lin);
            }
            return Task.FromResult<MemoryProvenanceLineage?>(null);
        }
    }
}
