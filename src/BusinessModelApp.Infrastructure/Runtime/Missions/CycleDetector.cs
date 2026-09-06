using System;
using System.Collections.Generic;
using System.Linq;
using BusinessModelApp.Core.Interfaces.Missions;

namespace BusinessModelApp.Infrastructure.Runtime.Missions
{
    public class CycleDetector : ICycleDetector
    {
        public CycleDetectionResult DetectCycles(IEnumerable<string> nodeIds, IEnumerable<(string Source, string Target)> edges)
        {
            var nodes = new HashSet<string>(nodeIds, StringComparer.Ordinal);
            var edgeList = edges.ToList();

            // Check self loops first
            foreach (var (src, dst) in edgeList)
            {
                if (string.Equals(src, dst, StringComparison.Ordinal))
                {
                    return CycleDetectionResult.Cyclic(new[] { src, dst });
                }
                nodes.Add(src);
                nodes.Add(dst);
            }

            var adjacency = nodes.ToDictionary(n => n, _ => new List<string>(), StringComparer.Ordinal);
            var inDegree = nodes.ToDictionary(n => n, _ => 0, StringComparer.Ordinal);

            foreach (var (src, dst) in edgeList)
            {
                if (adjacency.ContainsKey(src) && adjacency.ContainsKey(dst))
                {
                    adjacency[src].Add(dst);
                    inDegree[dst]++;
                }
            }

            // Kahn's algorithm for topological sorting
            var queue = new Queue<string>(nodes.Where(n => inDegree[n] == 0));
            var topoOrder = new List<string>();

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                topoOrder.Add(current);

                foreach (var neighbor in adjacency[current])
                {
                    inDegree[neighbor]--;
                    if (inDegree[neighbor] == 0)
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }

            if (topoOrder.Count == nodes.Count)
            {
                return CycleDetectionResult.Acyclic(topoOrder);
            }

            // Cycle detected: extract a cycle path using DFS on remaining nodes
            var cycleNodes = nodes.Where(n => inDegree[n] > 0).ToHashSet(StringComparer.Ordinal);
            var visited = new Dictionary<string, int>(StringComparer.Ordinal); // 0=unvisited, 1=visiting, 2=visited
            var parentMap = new Dictionary<string, string>(StringComparer.Ordinal);
            var cyclePath = new List<string>();

            foreach (var node in cycleNodes)
            {
                if (!visited.ContainsKey(node))
                {
                    if (DfsFindCycle(node, adjacency, cycleNodes, visited, parentMap, cyclePath))
                        break;
                }
            }

            if (cyclePath.Count == 0)
            {
                // Fallback to any nodes in cycle set
                cyclePath = cycleNodes.Take(3).ToList();
                if (cyclePath.Count > 0) cyclePath.Add(cyclePath[0]);
            }

            return CycleDetectionResult.Cyclic(cyclePath);
        }

        private static bool DfsFindCycle(
            string current,
            Dictionary<string, List<string>> adj,
            HashSet<string> cycleNodes,
            Dictionary<string, int> visited,
            Dictionary<string, string> parent,
            List<string> cyclePath)
        {
            visited[current] = 1; // visiting

            if (adj.TryGetValue(current, out var neighbors))
            {
                foreach (var neighbor in neighbors)
                {
                    if (!cycleNodes.Contains(neighbor))
                        continue;

                    if (!visited.TryGetValue(neighbor, out var status) || status == 0)
                    {
                        parent[neighbor] = current;
                        if (DfsFindCycle(neighbor, adj, cycleNodes, visited, parent, cyclePath))
                            return true;
                    }
                    else if (status == 1) // cycle detected!
                    {
                        cyclePath.Add(neighbor);
                        var curr = current;
                        while (curr != null && curr != neighbor)
                        {
                            cyclePath.Add(curr);
                            parent.TryGetValue(curr, out curr!);
                        }
                        cyclePath.Add(neighbor);
                        cyclePath.Reverse();
                        return true;
                    }
                }
            }

            visited[current] = 2; // visited
            return false;
        }
    }
}
