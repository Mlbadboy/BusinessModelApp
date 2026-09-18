using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Organizational;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational
{
    public class WorkDependencyResolver : IWorkDependencyResolver
    {
        private readonly IOrganizationalWorkRepository _repository;

        public WorkDependencyResolver(IOrganizationalWorkRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public bool HasCircularDependency(string tenantId, string dependentWorkId, string requiredWorkId)
        {
            if (string.Equals(dependentWorkId, requiredWorkId, StringComparison.OrdinalIgnoreCase))
                return true;

            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var queue = new Queue<string>();
            queue.Enqueue(requiredWorkId);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!visited.Add(current))
                    continue;

                // If traversing downstream from requiredWorkId reaches dependentWorkId, that forms a cycle
                var dependencies = _repository.GetDependenciesForWorkAsync(tenantId, current).GetAwaiter().GetResult();
                foreach (var dep in dependencies)
                {
                    if (string.Equals(dep.RequiredWorkId, dependentWorkId, StringComparison.OrdinalIgnoreCase))
                        return true;

                    queue.Enqueue(dep.RequiredWorkId);
                }
            }

            return false;
        }

        public async Task<bool> IsWorkBlockedAsync(string tenantId, string workId, CancellationToken ct = default)
        {
            var blocking = await GetBlockingPrerequisitesAsync(tenantId, workId, ct);
            return blocking.Count > 0;
        }

        public async Task<IReadOnlyList<string>> GetBlockingPrerequisitesAsync(string tenantId, string workId, CancellationToken ct = default)
        {
            var dependencies = await _repository.GetDependenciesForWorkAsync(tenantId, workId, ct);
            var blocking = new List<string>();

            foreach (var dep in dependencies)
            {
                if (dep.DependencyType != WorkDependencyType.HardBlock)
                    continue;

                if (dep.IsSatisfied)
                    continue;

                // Check required work item state
                var requiredItem = await _repository.GetWorkItemAsync(tenantId, dep.RequiredWorkId, ct);
                if (requiredItem == null)
                {
                    // Missing prerequisite is a hard block
                    blocking.Add(dep.RequiredWorkId);
                    continue;
                }

                // If not in a finished state, it blocks
                if (requiredItem.State != WorkState.Completed &&
                    requiredItem.State != WorkState.Measured &&
                    requiredItem.State != WorkState.Closed)
                {
                    blocking.Add(dep.RequiredWorkId);
                }
            }

            return blocking;
        }
    }
}
