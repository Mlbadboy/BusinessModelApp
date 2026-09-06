using System;
using System.Collections.Generic;
using BusinessModelApp.Core.Domain.Runtime;

namespace BusinessModelApp.Core.Domain.Missions
{
    public enum MissionNodeType
    {
        Perception = 1,
        RealityCheck = 2,
        Hypothesis = 3,
        Analysis = 4,
        Simulation = 5,
        Decision = 6,
        Approval = 7,
        Execution = 8,
        Verification = 9,
        Compensation = 10,
        Rollback = 11
    }

    public class MissionEdge
    {
        public string EdgeId { get; init; } = Guid.NewGuid().ToString("N");
        public MissionNodeId SourceNodeId { get; init; }
        public MissionNodeId TargetNodeId { get; init; }
        public string? ConditionExpression { get; init; }

        public MissionEdge(MissionNodeId source, MissionNodeId target, string? condition = null)
        {
            SourceNodeId = source;
            TargetNodeId = target;
            ConditionExpression = condition;
        }
    }

    public class MissionNode
    {
        public MissionNodeId NodeId { get; init; }
        public MissionGraphId GraphId { get; init; }
        public string Title { get; init; }
        public MissionNodeType NodeType { get; init; }
        public MissionNodeState State { get; set; } = MissionNodeState.Pending;
        public CapabilityId? RequiredCapabilityId { get; init; }
        public string? AssignedAgentRole { get; init; }
        public bool RequiresHumanApproval { get; init; }
        public int RiskTier { get; init; } = 0; // R0 read-only by default
        public decimal AllocatedSpendCeiling { get; init; } = 0.00m;
        public int MaxRetries { get; init; } = 2;
        public ExecutionOutcome? Outcome { get; set; }

        public MissionNode(MissionNodeId nodeId, MissionGraphId graphId, string title, MissionNodeType nodeType)
        {
            NodeId = nodeId;
            GraphId = graphId;
            Title = title;
            NodeType = nodeType;
        }
    }

    public class MissionGraph
    {
        public MissionGraphId GraphId { get; init; }
        public Guid WorkspaceId { get; init; }
        public string Title { get; init; }
        public MissionGraphState State { get; set; } = MissionGraphState.Initialized;
        public Dictionary<string, MissionNode> Nodes { get; init; } = new();
        public List<MissionEdge> Edges { get; init; } = new();
        public RunResourceBudget Budget { get; init; } = RunResourceBudget.Default;
        public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;

        public MissionGraph(MissionGraphId graphId, Guid workspaceId, string title)
        {
            if (workspaceId == Guid.Empty) throw new ArgumentException("WorkspaceId cannot be empty.", nameof(workspaceId));
            GraphId = graphId;
            WorkspaceId = workspaceId;
            Title = title;
        }

        public void AddNode(MissionNode node)
        {
            Nodes[node.NodeId.Value] = node;
        }

        public void AddEdge(MissionNodeId from, MissionNodeId to, string? condition = null)
        {
            Edges.Add(new MissionEdge(from, to, condition));
        }
    }
}
