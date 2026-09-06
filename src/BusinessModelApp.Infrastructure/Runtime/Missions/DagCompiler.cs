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
    public class DagCompiler : IDagCompiler
    {
        private readonly IGraphValidator _graphValidator;
        private readonly IMissionGraphAuditLedger _auditLedger;

        public DagCompiler(IGraphValidator graphValidator, IMissionGraphAuditLedger auditLedger)
        {
            _graphValidator = graphValidator ?? throw new ArgumentNullException(nameof(graphValidator));
            _auditLedger = auditLedger ?? throw new ArgumentNullException(nameof(auditLedger));
        }

        public async Task<MissionGraph> CompileAsync(
            MissionGraphProposal proposal,
            TenantMissionPolicyContext tenantPolicy,
            CancellationToken ct = default)
        {
            var validation = await _graphValidator.ValidateProposalAsync(proposal, tenantPolicy, ct);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException($"Cannot compile mission graph. Validation failed: {string.Join("; ", validation.Errors)}");
            }

            var graphId = MissionGraphId.New();

            // Determine in-degrees to identify root nodes
            var inDegrees = proposal.ProposedNodes.ToDictionary(n => n.NodeId, _ => 0, StringComparer.Ordinal);
            foreach (var edge in proposal.ProposedEdges)
            {
                if (inDegrees.ContainsKey(edge.TargetNodeId))
                {
                    inDegrees[edge.TargetNodeId]++;
                }
            }

            // Build node records
            var nodeDict = new Dictionary<string, MissionNodeRecord>(StringComparer.Ordinal);
            foreach (var pNode in proposal.ProposedNodes)
            {
                var isRoot = inDegrees[pNode.NodeId] == 0;
                var initialState = isRoot ? MissionNodeState.Ready : MissionNodeState.Pending;

                var nodeRecord = new MissionNodeRecord
                {
                    NodeId = MissionNodeId.From(pNode.NodeId),
                    GraphId = graphId,
                    NodeType = pNode.NodeType,
                    Title = pNode.Title,
                    Description = pNode.Description,
                    State = initialState,
                    ExecutionPolicy = new NodeExecutionPolicy
                    {
                        Timeout = pNode.Timeout,
                        MaxRetries = pNode.MaxRetries,
                        RequiredCapabilityId = !string.IsNullOrWhiteSpace(pNode.RequiredCapabilityId)
                            ? (pNode.RequiredCapabilityId.Contains(':') ? CapabilityId.Parse(pNode.RequiredCapabilityId) : new CapabilityId(pNode.RequiredCapabilityId, "v1"))
                            : null,
                        RequiredAutonomyTier = (pNode.RequiredAutonomyTier <= validation.ClampedAutonomyTier) ? pNode.RequiredAutonomyTier : validation.ClampedAutonomyTier,
                        MaxBudgetTokens = pNode.MaxBudgetTokens,
                        MaxCostUsd = pNode.MaxCostUsd,
                        RequiresHumanApproval = pNode.RequiresHumanApproval
                    },
                    VerificationCriteria = pNode.VerificationCriteria ?? new NodeVerificationCriteria(),
                    CreatedAt = DateTimeOffset.UtcNow
                };

                nodeDict[pNode.NodeId] = nodeRecord;
            }

            // Build edges and dependencies
            var edges = new List<MissionEdge>();
            var dependencies = new List<MissionDependency>();

            foreach (var pEdge in proposal.ProposedEdges)
            {
                var edge = new MissionEdge
                {
                    SourceNodeId = MissionNodeId.From(pEdge.SourceNodeId),
                    TargetNodeId = MissionNodeId.From(pEdge.TargetNodeId),
                    Type = pEdge.Type,
                    Predicate = pEdge.Predicate,
                    JoinPolicy = pEdge.JoinPolicy
                };
                edges.Add(edge);

                dependencies.Add(new MissionDependency
                {
                    DependentNodeId = edge.TargetNodeId,
                    RequiredNodeId = edge.SourceNodeId,
                    IsStrict = edge.Type != EdgeType.Conditional
                });
            }

            var budget = new MissionBudget
            {
                TotalTokenBudget = proposal.EstimatedBudgetTokens,
                TotalCostUsdBudget = proposal.EstimatedCostUsd
            };

            var graph = new MissionGraph
            {
                GraphId = graphId,
                MissionId = proposal.MissionId,
                WorkspaceId = proposal.WorkspaceId,
                Version = MissionGraphVersion.Initial,
                ParentVersionHash = null,
                Nodes = nodeDict,
                Edges = edges,
                Dependencies = dependencies,
                State = MissionGraphState.Active,
                MaxAllowedAutonomyTier = validation.ClampedAutonomyTier,
                Budget = budget,
                CreatedAt = DateTimeOffset.UtcNow,
                CompiledAt = DateTimeOffset.UtcNow
            };

            graph.VersionHash = graph.ComputeVersionHash();

            await _auditLedger.RecordEventAsync(new MissionGraphAuditEntry
            {
                GraphId = graph.GraphId,
                Version = graph.Version,
                EventType = "GraphCompiled",
                Details = $"Compiled graph {graph.GraphId} with {graph.Nodes.Count} nodes, {graph.Edges.Count} edges. VersionHash: {graph.VersionHash}",
                Sha256Hash = graph.VersionHash
            }, ct);

            return graph;
        }
    }
}
