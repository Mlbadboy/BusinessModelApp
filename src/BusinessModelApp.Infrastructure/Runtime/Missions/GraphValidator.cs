using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Responsibilities;
using BusinessModelApp.Core.Interfaces.Missions;

namespace BusinessModelApp.Infrastructure.Runtime.Missions
{
    public class GraphValidator : IGraphValidator
    {
        private readonly ICycleDetector _cycleDetector;

        private static readonly Regex PoisonPattern = new(
            @"(ignore\s+(previous|all)\s+instructions|system\s+prompt|drop\s+database|<script|javascript:|bash\s+-c|rm\s+-rf|transfer\s+money|send\s+wire|execute\s+payment)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly HashSet<string> AllowedOperators = new(StringComparer.Ordinal)
        {
            "==", "!=", "<", "<=", ">", ">="
        };

        public GraphValidator(ICycleDetector cycleDetector)
        {
            _cycleDetector = cycleDetector ?? throw new ArgumentNullException(nameof(cycleDetector));
        }

        public Task<GraphValidationResult> ValidateProposalAsync(
            MissionGraphProposal proposal,
            TenantMissionPolicyContext tenantPolicy,
            CancellationToken ct = default)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            if (proposal == null)
            {
                return Task.FromResult(GraphValidationResult.Failed(new[] { "Proposal cannot be null." }));
            }

            // 1. Multi-tenant isolation
            if (proposal.WorkspaceId == Guid.Empty || proposal.WorkspaceId != tenantPolicy.WorkspaceId)
            {
                errors.Add($"Workspace ID mismatch: Proposal workspace {proposal.WorkspaceId} does not match tenant policy {tenantPolicy.WorkspaceId}.");
            }

            // 2. Resource limits (Max nodes)
            if (proposal.ProposedNodes.Count == 0)
            {
                errors.Add("Mission graph proposal must contain at least one node.");
            }
            if (proposal.ProposedNodes.Count > tenantPolicy.MaxNodesPerGraph)
            {
                errors.Add($"Proposal node count ({proposal.ProposedNodes.Count}) exceeds tenant limit ({tenantPolicy.MaxNodesPerGraph}).");
            }

            // 3. Unique node IDs and poison checking
            var nodeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var node in proposal.ProposedNodes)
            {
                if (string.IsNullOrWhiteSpace(node.NodeId))
                {
                    errors.Add("Node ID cannot be empty or whitespace.");
                    continue;
                }

                if (!nodeIds.Add(node.NodeId))
                {
                    errors.Add($"Duplicate node ID detected: '{node.NodeId}'.");
                }

                // Disallowed node types
                if (tenantPolicy.DisallowedNodeTypes.Contains(node.NodeType))
                {
                    errors.Add($"Node '{node.NodeId}' uses disallowed NodeType '{node.NodeType}'.");
                }

                // Poison / prompt injection defense
                if (PoisonPattern.IsMatch(node.Title) || PoisonPattern.IsMatch(node.Description))
                {
                    errors.Add($"Node '{node.NodeId}' contains potentially malicious or poisoned content in title/description.");
                }

                // Consequential execution node validation
                if (node.NodeType == MissionNodeType.Execute)
                {
                    if (PoisonPattern.IsMatch(node.Title) || PoisonPattern.IsMatch(node.Description))
                    {
                        errors.Add($"Unauthorized execution node '{node.NodeId}' rejected: destructive action detected.");
                    }

                    // Execution nodes require at least L4 autonomy or explicit human approval
                    if (node.RequiredAutonomyTier < AutonomyTier.L4_ExecuteWithApproval && !node.RequiresHumanApproval)
                    {
                        warnings.Add($"Execution node '{node.NodeId}' upgraded to require human approval under zero-trust governance.");
                    }
                }

                // Capability validation (compile-time)
                if (!string.IsNullOrWhiteSpace(node.RequiredCapabilityId))
                {
                    if (!tenantPolicy.RegisteredCapabilityIds.Contains(node.RequiredCapabilityId))
                    {
                        errors.Add($"Node '{node.NodeId}' requires unregistered capability '{node.RequiredCapabilityId}'.");
                    }
                }
            }

            // 4. Edges validation & Predicate safety
            var edgeTuples = new List<(string Source, string Target)>();
            foreach (var edge in proposal.ProposedEdges)
            {
                if (!nodeIds.Contains(edge.SourceNodeId))
                {
                    errors.Add($"Edge references unknown source node '{edge.SourceNodeId}'.");
                }
                if (!nodeIds.Contains(edge.TargetNodeId))
                {
                    errors.Add($"Edge references unknown target node '{edge.TargetNodeId}'.");
                }

                // Closed typed predicate safety
                if (edge.Predicate != null && edge.Predicate.Type == PredicateType.MetricComparison)
                {
                    if (!AllowedOperators.Contains(edge.Predicate.Operator))
                    {
                        errors.Add($"Edge ({edge.SourceNodeId} -> {edge.TargetNodeId}) uses disallowed operator '{edge.Predicate.Operator}'.");
                    }
                }

                edgeTuples.Add((edge.SourceNodeId, edge.TargetNodeId));
            }

            // 5. Acyclicity check (Tarjan / Kahn via ICycleDetector)
            var cycleResult = _cycleDetector.DetectCycles(nodeIds, edgeTuples);
            if (cycleResult.HasCycle)
            {
                errors.Add($"Cycle detected in mission graph: {string.Join(" -> ", cycleResult.CyclePath)}.");
                return Task.FromResult(GraphValidationResult.Failed(errors, cycleDetected: true, cyclePath: cycleResult.CyclePath));
            }

            // 6. Graph depth & branch ceiling
            var depth = CalculateMaxDepth(nodeIds, edgeTuples);
            if (depth > tenantPolicy.MaxGraphDepth)
            {
                errors.Add($"Graph depth ({depth}) exceeds maximum tenant limit ({tenantPolicy.MaxGraphDepth}).");
            }

            // 7. Budget bounds validation
            if (proposal.EstimatedBudgetTokens > tenantPolicy.MaxTotalBudgetTokens)
            {
                errors.Add($"Estimated tokens ({proposal.EstimatedBudgetTokens}) exceeds tenant ceiling ({tenantPolicy.MaxTotalBudgetTokens}).");
            }
            if (proposal.EstimatedCostUsd > tenantPolicy.MaxTotalCostUsd)
            {
                errors.Add($"Estimated cost (${proposal.EstimatedCostUsd}) exceeds tenant ceiling (${tenantPolicy.MaxTotalCostUsd}).");
            }

            // 8. Autonomy ceiling clamp
            var clampedTier = (AutonomyTier)Math.Min((int)proposal.ProposedAutonomyTier, (int)tenantPolicy.MaxAllowedAutonomyTier);
            if (proposal.ProposedAutonomyTier > tenantPolicy.MaxAllowedAutonomyTier)
            {
                warnings.Add($"Proposal autonomy tier '{proposal.ProposedAutonomyTier}' clamped to tenant ceiling '{clampedTier}'.");
            }

            if (errors.Count > 0)
            {
                return Task.FromResult(GraphValidationResult.Failed(errors));
            }

            return Task.FromResult(GraphValidationResult.Success(clampedTier, warnings));
        }

        public Task<GraphValidationResult> ValidateExpansionAsync(
            MissionGraph currentGraph,
            DynamicExpansionProposal expansion,
            TenantMissionPolicyContext tenantPolicy,
            CancellationToken ct = default)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            if (currentGraph == null)
            {
                return Task.FromResult(GraphValidationResult.Failed(new[] { "Current graph cannot be null." }));
            }
            if (expansion == null)
            {
                return Task.FromResult(GraphValidationResult.Failed(new[] { "Expansion proposal cannot be null." }));
            }

            // Check if tenant policy permits expansion
            if (!tenantPolicy.AllowDynamicExpansion)
            {
                return Task.FromResult(GraphValidationResult.Failed(new[] { "Dynamic graph expansion is disabled by tenant policy." }));
            }

            // 1. Optimistic Concurrency check
            if (expansion.ParentGraphId != currentGraph.GraphId)
            {
                errors.Add($"Expansion ParentGraphId '{expansion.ParentGraphId}' does not match active graph '{currentGraph.GraphId}'.");
            }
            if (expansion.ParentVersion != currentGraph.Version)
            {
                errors.Add($"Stale expansion: proposal parent version '{expansion.ParentVersion}' does not match current graph version '{currentGraph.Version}'.");
            }
            if (!string.IsNullOrWhiteSpace(expansion.ExpectedGraphHash) &&
                !string.Equals(expansion.ExpectedGraphHash, currentGraph.VersionHash, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Concurrency conflict: expected graph hash '{expansion.ExpectedGraphHash}' does not match active graph hash '{currentGraph.VersionHash}'.");
            }

            // 2. Resource limits on expansion
            if (expansion.NewNodes.Count == 0)
            {
                errors.Add("Expansion proposal must introduce at least one new node.");
            }
            if (expansion.NewNodes.Count > tenantPolicy.MaxNodesPerExpansion)
            {
                errors.Add($"Expansion node count ({expansion.NewNodes.Count}) exceeds limit per expansion ({tenantPolicy.MaxNodesPerExpansion}).");
            }
            if (currentGraph.Nodes.Count + expansion.NewNodes.Count > tenantPolicy.MaxNodesPerGraph)
            {
                errors.Add($"Combined node count ({currentGraph.Nodes.Count + expansion.NewNodes.Count}) exceeds max graph limit ({tenantPolicy.MaxNodesPerGraph}).");
            }

            // 3. Check triggering node exists
            if (!currentGraph.Nodes.ContainsKey(expansion.TriggeringNodeId.Value))
            {
                errors.Add($"Triggering node '{expansion.TriggeringNodeId.Value}' does not exist in graph.");
            }

            // 4. Validate new nodes (no duplicate with existing, no poison)
            var combinedNodeIds = currentGraph.Nodes.Keys.ToHashSet(StringComparer.Ordinal);
            foreach (var node in expansion.NewNodes)
            {
                if (string.IsNullOrWhiteSpace(node.NodeId))
                {
                    errors.Add("Expansion node ID cannot be empty.");
                    continue;
                }

                if (!combinedNodeIds.Add(node.NodeId))
                {
                    errors.Add($"Expansion introduces duplicate node ID '{node.NodeId}'.");
                }

                if (tenantPolicy.DisallowedNodeTypes.Contains(node.NodeType))
                {
                    errors.Add($"Expansion node '{node.NodeId}' uses disallowed NodeType '{node.NodeType}'.");
                }

                if (PoisonPattern.IsMatch(node.Title) || PoisonPattern.IsMatch(node.Description))
                {
                    errors.Add($"Expansion node '{node.NodeId}' contains potentially malicious content.");
                }

                if (!string.IsNullOrWhiteSpace(node.RequiredCapabilityId) &&
                    !tenantPolicy.RegisteredCapabilityIds.Contains(node.RequiredCapabilityId))
                {
                    errors.Add($"Expansion node '{node.NodeId}' requires unregistered capability '{node.RequiredCapabilityId}'.");
                }
            }

            // 5. Combine edges and verify acyclicity
            var combinedEdges = currentGraph.Edges
                .Select(e => (e.SourceNodeId.Value, e.TargetNodeId.Value))
                .ToList();

            // Exclude removed edges
            var removedSet = expansion.RemovedEdges
                .Select(e => (e.SourceNodeId, e.TargetNodeId))
                .ToHashSet();
            combinedEdges.RemoveAll(e => removedSet.Contains(e));

            // Add new edges
            foreach (var newEdge in expansion.NewEdges)
            {
                if (!combinedNodeIds.Contains(newEdge.SourceNodeId))
                {
                    errors.Add($"Expansion edge references unknown source '{newEdge.SourceNodeId}'.");
                }
                if (!combinedNodeIds.Contains(newEdge.TargetNodeId))
                {
                    errors.Add($"Expansion edge references unknown target '{newEdge.TargetNodeId}'.");
                }
                combinedEdges.Add((newEdge.SourceNodeId, newEdge.TargetNodeId));
            }

            var cycleResult = _cycleDetector.DetectCycles(combinedNodeIds, combinedEdges);
            if (cycleResult.HasCycle)
            {
                errors.Add($"Expansion introduces cycle: {string.Join(" -> ", cycleResult.CyclePath)}.");
                return Task.FromResult(GraphValidationResult.Failed(errors, cycleDetected: true, cyclePath: cycleResult.CyclePath));
            }

            // 6. Cumulative budget check
            if (!currentGraph.Budget.CanReserve(expansion.AdditionalBudgetTokens, expansion.AdditionalCostUsd))
            {
                errors.Add($"Expansion budget requirements ({expansion.AdditionalBudgetTokens} tokens, ${expansion.AdditionalCostUsd}) exceed remaining graph budget ({currentGraph.Budget.RemainingTokens} tokens, ${currentGraph.Budget.RemainingCostUsd}).");
            }

            if (errors.Count > 0)
            {
                return Task.FromResult(GraphValidationResult.Failed(errors));
            }

            return Task.FromResult(GraphValidationResult.Success(currentGraph.MaxAllowedAutonomyTier, warnings));
        }

        private static int CalculateMaxDepth(HashSet<string> nodes, List<(string Source, string Target)> edges)
        {
            var adj = nodes.ToDictionary(n => n, _ => new List<string>(), StringComparer.Ordinal);
            var inDegree = nodes.ToDictionary(n => n, _ => 0, StringComparer.Ordinal);

            foreach (var (src, dst) in edges)
            {
                if (adj.ContainsKey(src) && inDegree.ContainsKey(dst))
                {
                    adj[src].Add(dst);
                    inDegree[dst]++;
                }
            }

            var depths = nodes.ToDictionary(n => n, _ => 1, StringComparer.Ordinal);
            var queue = new Queue<string>(nodes.Where(n => inDegree[n] == 0));

            while (queue.Count > 0)
            {
                var curr = queue.Dequeue();
                var currentDepth = depths[curr];

                foreach (var neighbor in adj[curr])
                {
                    if (depths[neighbor] < currentDepth + 1)
                    {
                        depths[neighbor] = currentDepth + 1;
                    }
                    inDegree[neighbor]--;
                    if (inDegree[neighbor] == 0)
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return depths.Values.DefaultIfEmpty(0).Max();
        }
    }
}
