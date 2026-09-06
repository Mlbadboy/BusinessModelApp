using System;
using System.Collections.Generic;
using System.Linq;

namespace BusinessModelApp.Core.Domain.Missions
{
    public class InvalidMissionGraphException : Exception
    {
        public IReadOnlyList<string> Errors { get; }

        public InvalidMissionGraphException(string message, IEnumerable<string> errors)
            : base($"{message} Details: {string.Join("; ", errors)}")
        {
            Errors = errors.ToList();
        }
    }

    public static class MissionGraphValidator
    {
        public const int MaxAllowedNodes = 100;
        public const int MaxAllowedEdges = 250;

        public static void AssertValid(MissionGraph graph)
        {
            var errors = new List<string>();

            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            if (graph.Nodes.Count == 0)
            {
                errors.Add("MissionGraph must contain at least one node.");
            }

            if (graph.Nodes.Count > MaxAllowedNodes)
            {
                errors.Add($"MissionGraph node count ({graph.Nodes.Count}) exceeds maximum allowed ({MaxAllowedNodes}).");
            }

            if (graph.Edges.Count > MaxAllowedEdges)
            {
                errors.Add($"MissionGraph edge count ({graph.Edges.Count}) exceeds maximum allowed ({MaxAllowedEdges}).");
            }

            // Verify all edge references exist
            foreach (var edge in graph.Edges)
            {
                if (!graph.Nodes.ContainsKey(edge.SourceNodeId.Value))
                    errors.Add($"Edge source node '{edge.SourceNodeId}' does not exist in graph.");
                if (!graph.Nodes.ContainsKey(edge.TargetNodeId.Value))
                    errors.Add($"Edge target node '{edge.TargetNodeId}' does not exist in graph.");
            }

            // Execution nodes must define capability
            foreach (var node in graph.Nodes.Values)
            {
                if ((node.NodeType == MissionNodeType.Execution || node.NodeType == MissionNodeType.Execute) &&
                    node.ExecutionPolicy.RequiredCapabilityId == null)
                {
                    errors.Add($"Execution node '{node.NodeId}' must declare a required CapabilityId.");
                }
            }

            // Detect cycles using DFS
            if (HasCycle(graph))
            {
                errors.Add("MissionGraph contains cycles. Directed Acyclic Graph (DAG) invariant violated.");
            }

            if (errors.Count > 0)
            {
                throw new InvalidMissionGraphException("MissionGraph failed structural invariant validation.", errors);
            }
        }

        private static bool HasCycle(MissionGraph graph)
        {
            var adj = new Dictionary<string, List<string>>();
            foreach (var node in graph.Nodes.Keys)
                adj[node] = new List<string>();

            foreach (var edge in graph.Edges)
            {
                if (adj.ContainsKey(edge.SourceNodeId.Value))
                    adj[edge.SourceNodeId.Value].Add(edge.TargetNodeId.Value);
            }

            var visited = new HashSet<string>();
            var recStack = new HashSet<string>();

            foreach (var node in graph.Nodes.Keys)
            {
                if (CheckCycleDfs(node, adj, visited, recStack))
                    return true;
            }

            return false;
        }

        private static bool CheckCycleDfs(string node, Dictionary<string, List<string>> adj, HashSet<string> visited, HashSet<string> recStack)
        {
            if (recStack.Contains(node))
                return true;
            if (visited.Contains(node))
                return false;

            visited.Add(node);
            recStack.Add(node);

            if (adj.TryGetValue(node, out var neighbors))
            {
                foreach (var neighbor in neighbors)
                {
                    if (CheckCycleDfs(neighbor, adj, visited, recStack))
                        return true;
                }
            }

            recStack.Remove(node);
            return false;
        }
    }
}
