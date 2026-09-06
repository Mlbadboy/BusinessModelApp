using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Interfaces.Missions;

namespace BusinessModelApp.Infrastructure.Runtime.Missions
{
    public class GraphExpansionEngine : IGraphExpansionEngine
    {
        private readonly IGraphValidator _graphValidator;
        private readonly IMissionGraphAuditLedger _auditLedger;

        public GraphExpansionEngine(IGraphValidator graphValidator, IMissionGraphAuditLedger auditLedger)
        {
            _graphValidator = graphValidator ?? throw new ArgumentNullException(nameof(graphValidator));
            _auditLedger = auditLedger ?? throw new ArgumentNullException(nameof(auditLedger));
        }

        public async Task<MissionGraph> ApplyExpansionAsync(
            MissionGraph currentGraph,
            DynamicExpansionProposal expansion,
            TenantMissionPolicyContext tenantPolicy,
            CancellationToken ct = default)
        {
            var validation = await _graphValidator.ValidateExpansionAsync(currentGraph, expansion, tenantPolicy, ct);
            if (!validation.IsValid)
            {
                await _auditLedger.RecordEventAsync(new MissionGraphAuditEntry
                {
                    GraphId = currentGraph.GraphId,
                    Version = currentGraph.Version,
                    EventType = "GraphExpansionRejected",
                    Details = $"Expansion rejected: {string.Join("; ", validation.Errors)}",
                    Sha256Hash = currentGraph.VersionHash
                }, ct);

                throw new InvalidOperationException($"Cannot apply graph expansion. Validation failed: {string.Join("; ", validation.Errors)}");
            }

            var nextVersion = currentGraph.Version.Next();

            // Clone existing nodes
            var combinedNodes = currentGraph.Nodes.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value with { },
                StringComparer.Ordinal);

            // Add new nodes
            foreach (var pNode in expansion.NewNodes)
            {
                var nodeRecord = new MissionNodeRecord
                {
                    NodeId = MissionNodeId.From(pNode.NodeId),
                    GraphId = currentGraph.GraphId,
                    NodeType = pNode.NodeType,
                    Title = pNode.Title,
                    Description = pNode.Description,
                    State = MissionNodeState.Ready, // Ready to be picked up by scheduler
                    ExecutionPolicy = new NodeExecutionPolicy
                    {
                        Timeout = pNode.Timeout,
                        MaxRetries = pNode.MaxRetries,
                        RequiredCapabilityId = !string.IsNullOrWhiteSpace(pNode.RequiredCapabilityId)
                            ? (pNode.RequiredCapabilityId.Contains(':') ? CapabilityId.Parse(pNode.RequiredCapabilityId) : new CapabilityId(pNode.RequiredCapabilityId, "v1"))
                            : null,
                        RequiredAutonomyTier = (pNode.RequiredAutonomyTier <= currentGraph.MaxAllowedAutonomyTier) ? pNode.RequiredAutonomyTier : currentGraph.MaxAllowedAutonomyTier,
                        MaxBudgetTokens = pNode.MaxBudgetTokens,
                        MaxCostUsd = pNode.MaxCostUsd,
                        RequiresHumanApproval = pNode.RequiresHumanApproval
                    },
                    VerificationCriteria = pNode.VerificationCriteria ?? new NodeVerificationCriteria(),
                    CreatedAt = DateTimeOffset.UtcNow
                };

                combinedNodes[pNode.NodeId] = nodeRecord;
            }

            // Rewire edges: remove removed edges and add new edges
            var removedEdgeSet = expansion.RemovedEdges
                .Select(e => (e.SourceNodeId, e.TargetNodeId))
                .ToHashSet();

            var combinedEdges = currentGraph.Edges
                .Where(e => !removedEdgeSet.Contains((e.SourceNodeId.Value, e.TargetNodeId.Value)))
                .ToList();

            foreach (var pEdge in expansion.NewEdges)
            {
                combinedEdges.Add(new MissionEdge
                {
                    SourceNodeId = MissionNodeId.From(pEdge.SourceNodeId),
                    TargetNodeId = MissionNodeId.From(pEdge.TargetNodeId),
                    Type = pEdge.Type,
                    Predicate = pEdge.Predicate,
                    JoinPolicy = pEdge.JoinPolicy
                });
            }

            // Recalculate dependencies
            var dependencies = combinedEdges.Select(e => new MissionDependency
            {
                DependentNodeId = e.TargetNodeId,
                RequiredNodeId = e.SourceNodeId,
                IsStrict = e.Type != EdgeType.Conditional
            }).ToList();

            // Reserve additional budget
            currentGraph.Budget.TryReserve(expansion.AdditionalBudgetTokens, expansion.AdditionalCostUsd);

            var expandedGraph = new MissionGraph
            {
                GraphId = currentGraph.GraphId,
                MissionId = currentGraph.MissionId,
                WorkspaceId = currentGraph.WorkspaceId,
                Version = nextVersion,
                ParentVersionHash = currentGraph.VersionHash,
                Nodes = combinedNodes,
                Edges = combinedEdges,
                Dependencies = dependencies,
                State = currentGraph.State,
                MaxAllowedAutonomyTier = currentGraph.MaxAllowedAutonomyTier,
                Budget = currentGraph.Budget,
                CreatedAt = currentGraph.CreatedAt,
                CompiledAt = DateTimeOffset.UtcNow
            };

            expandedGraph.VersionHash = expandedGraph.ComputeVersionHash();

            await _auditLedger.RecordEventAsync(new MissionGraphAuditEntry
            {
                GraphId = expandedGraph.GraphId,
                Version = expandedGraph.Version,
                EventType = "GraphExpansionApproved",
                Details = $"Expanded graph {expandedGraph.GraphId} to version {expandedGraph.Version} (Nodes: {expandedGraph.Nodes.Count}, Edges: {expandedGraph.Edges.Count}). VersionHash: {expandedGraph.VersionHash}",
                Sha256Hash = expandedGraph.VersionHash
            }, ct);

            return expandedGraph;
        }
    }
}
