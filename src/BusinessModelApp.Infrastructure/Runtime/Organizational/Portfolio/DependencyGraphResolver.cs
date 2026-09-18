using BusinessModelApp.Core.Domain.Runtime.Organizational.Portfolio;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Portfolio;

/// <summary>
/// Acyclic dependency validator and topological sorter.
/// Invariant I33-F: Missing dependency marks item BlockedByDependency; cannot force execution.
/// Invariant I33-V: Planning dependency graph answers readiness before work begins; cannot mutate/execute a Mission Graph.
/// </summary>
public class DependencyGraphResolver : IDependencyGraphResolver
{
    public Task<(DependencyValidationState State, string? Reason, IReadOnlyList<string> OrderedWorkItemIds)> ValidateAndSortDependenciesAsync(
        string tenantId,
        IReadOnlyList<PortfolioWorkItem> items)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));

        if (items == null || items.Count == 0)
        {
            return Task.FromResult<(DependencyValidationState, string?, IReadOnlyList<string>)>(
                (DependencyValidationState.Satisfied, null, Array.Empty<string>()));
        }

        var itemMap = items.ToDictionary(i => i.WorkItemId);
        var inDegree = items.ToDictionary(i => i.WorkItemId, _ => 0);
        var adjacency = items.ToDictionary(i => i.WorkItemId, _ => new List<string>());

        // 1. Check for missing external/internal prerequisites
        foreach (var item in items)
        {
            foreach (var prereqId in item.PrerequisiteWorkItemIds)
            {
                if (!itemMap.TryGetValue(prereqId, out var prereqItem))
                {
                    item.DependencyState = DependencyValidationState.Missing;
                    item.State = PortfolioLifecycleState.Blocked;
                    return Task.FromResult<(DependencyValidationState, string?, IReadOnlyList<string>)>(
                        (DependencyValidationState.Missing, $"Prerequisite work item '{prereqId}' is missing from portfolio candidate set.", Array.Empty<string>()));
                }

                // If prerequisite exists, prereqId must precede item.WorkItemId
                adjacency[prereqId].Add(item.WorkItemId);
                inDegree[item.WorkItemId]++;
            }
        }

        // 2. Kahn's Algorithm for Topological Sort and Cycle Detection
        var queue = new Queue<string>();
        foreach (var (id, deg) in inDegree)
        {
            if (deg == 0) queue.Enqueue(id);
        }

        var ordered = new List<string>();
        while (queue.Count > 0)
        {
            var curr = queue.Dequeue();
            ordered.Add(curr);

            foreach (var neighbor in adjacency[curr])
            {
                inDegree[neighbor]--;
                if (inDegree[neighbor] == 0)
                {
                    queue.Enqueue(neighbor);
                }
            }
        }

        if (ordered.Count < items.Count)
        {
            // Cycle detected
            foreach (var item in items)
            {
                if (inDegree[item.WorkItemId] > 0)
                {
                    item.DependencyState = DependencyValidationState.CycleDetected;
                    item.State = PortfolioLifecycleState.Blocked;
                }
            }

            return Task.FromResult<(DependencyValidationState, string?, IReadOnlyList<string>)>(
                (DependencyValidationState.CycleDetected, "Circular dependency cycle detected among portfolio work items.", Array.Empty<string>()));
        }

        foreach (var item in items)
        {
            item.DependencyState = DependencyValidationState.Satisfied;
            if (item.State == PortfolioLifecycleState.Draft)
            {
                item.State = PortfolioLifecycleState.DependenciesValidated;
            }
        }

        return Task.FromResult<(DependencyValidationState, string?, IReadOnlyList<string>)>(
            (DependencyValidationState.Satisfied, null, ordered));
    }
}
