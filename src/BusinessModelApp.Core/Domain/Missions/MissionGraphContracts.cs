using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BusinessModelApp.Core.Domain.Responsibilities;
using BusinessModelApp.Core.Domain.Runtime;

namespace BusinessModelApp.Core.Domain.Missions
{
    public readonly record struct MissionGraphVersion
    {
        public int Value { get; }

        [JsonConstructor]
        public MissionGraphVersion(int value)
        {
            if (value < 1)
                throw new ArgumentException("MissionGraphVersion must be >= 1.", nameof(value));
            Value = value;
        }

        public static MissionGraphVersion Initial => new(1);
        public MissionGraphVersion Next() => new(Value + 1);
        public static implicit operator int(MissionGraphVersion v) => v.Value;
        public override string ToString() => $"v{Value}";
    }

    public enum EdgeType
    {
        Sequential = 1,
        Conditional = 2,
        Branch = 3,
        Join = 4,
        Compensate = 5
    }

    public enum JoinType
    {
        AllCompleted = 1,
        AnyCompleted = 2,
        ThresholdJoin = 3
    }

    public enum PredicateType
    {
        MetricComparison = 1,
        ArtifactPresence = 2,
        NodeStateCheck = 3,
        BooleanConstant = 4
    }

    public record TypedPredicate
    {
        public PredicateType Type { get; init; } = PredicateType.BooleanConstant;
        public string? MetricName { get; init; }
        public string Operator { get; init; } = "=="; // "==", "!=", "<", "<=", ">", ">="
        public string? TargetValue { get; init; }
        public string? ArtifactName { get; init; }
        public MissionNodeId? CheckedNodeId { get; init; }
        public MissionNodeState? ExpectedNodeState { get; init; }
        public bool ConstantValue { get; init; } = true;

        public static TypedPredicate AlwaysTrue => new() { Type = PredicateType.BooleanConstant, ConstantValue = true };
        public static TypedPredicate AlwaysFalse => new() { Type = PredicateType.BooleanConstant, ConstantValue = false };

        public static TypedPredicate Metric(string metricName, string op, string targetValue) =>
            new()
            {
                Type = PredicateType.MetricComparison,
                MetricName = metricName,
                Operator = op,
                TargetValue = targetValue
            };

        public static TypedPredicate Artifact(string artifactName) =>
            new()
            {
                Type = PredicateType.ArtifactPresence,
                ArtifactName = artifactName
            };

        public static TypedPredicate NodeState(MissionNodeId nodeId, MissionNodeState state) =>
            new()
            {
                Type = PredicateType.NodeStateCheck,
                CheckedNodeId = nodeId,
                ExpectedNodeState = state
            };
    }

    public record JoinPolicy
    {
        public JoinType Type { get; init; } = JoinType.AllCompleted;
        public int Threshold { get; init; } = 1;
    }

    public record MissionEdge
    {
        public string EdgeId { get; init; } = Guid.NewGuid().ToString("N");
        public MissionNodeId SourceNodeId { get; init; }
        public MissionNodeId TargetNodeId { get; init; }
        public EdgeType Type { get; init; } = EdgeType.Sequential;
        public TypedPredicate? Predicate { get; init; }
        public JoinPolicy? JoinPolicy { get; init; }
        public string? ConditionExpression { get; init; }

        public MissionEdge() { }

        public MissionEdge(MissionNodeId source, MissionNodeId target, string? condition = null)
        {
            SourceNodeId = source;
            TargetNodeId = target;
            ConditionExpression = condition;
            Predicate = !string.IsNullOrWhiteSpace(condition) ? TypedPredicate.AlwaysTrue : null;
        }
    }

    public record MissionDependency
    {
        public MissionNodeId DependentNodeId { get; init; }
        public MissionNodeId RequiredNodeId { get; init; }
        public bool IsStrict { get; init; } = true;
    }

    public record MissionArtifact
    {
        public Guid ArtifactId { get; init; } = Guid.NewGuid();
        public MissionNodeId NodeId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string ContentType { get; init; } = "application/json";
        public string Sha256Hash { get; init; } = string.Empty;
        public string? StorageUri { get; init; }
        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    }

    public record MissionGraph
    {
        public MissionGraphId GraphId { get; init; }
        public MissionId MissionId { get; init; }
        public Guid WorkspaceId { get; init; }
        public string Title { get; init; } = string.Empty;
        public MissionGraphVersion Version { get; init; } = MissionGraphVersion.Initial;
        public string? ParentVersionHash { get; init; }
        public string VersionHash { get; set; } = string.Empty;
        public Dictionary<string, MissionNodeRecord> Nodes { get; init; } = new(StringComparer.Ordinal);
        public List<MissionEdge> Edges { get; init; } = new();
        public List<MissionDependency> Dependencies { get; init; } = new();
        public MissionGraphState State { get; set; } = MissionGraphState.Initialized;
        public AutonomyTier MaxAllowedAutonomyTier { get; init; } = AutonomyTier.L1_Advise;
        public MissionBudget Budget { get; init; } = new();
        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? CompiledAt { get; set; }

        public MissionGraph() { }

        public MissionGraph(MissionGraphId graphId, Guid workspaceId, string title)
        {
            if (workspaceId == Guid.Empty) throw new ArgumentException("WorkspaceId cannot be empty.", nameof(workspaceId));
            GraphId = graphId;
            MissionId = MissionId.New();
            WorkspaceId = workspaceId;
            Title = title;
        }

        public void AddNode(MissionNodeRecord node)
        {
            Nodes[node.NodeId.Value] = node;
        }

        public void AddEdge(MissionNodeId from, MissionNodeId to, string? condition = null)
        {
            Edges.Add(new MissionEdge(from, to, condition));
            Dependencies.Add(new MissionDependency
            {
                DependentNodeId = to,
                RequiredNodeId = from,
                IsStrict = true
            });
        }

        public string ComputeVersionHash()
        {
            var sb = new StringBuilder();
            sb.Append(ParentVersionHash ?? "ROOT").Append(':');
            sb.Append(Version.Value).Append(':');
            sb.Append(GraphId.ToString()).Append(':');
            sb.Append(MissionId.ToString()).Append(':');
            sb.Append(WorkspaceId.ToString()).Append(':');
            sb.Append((int)MaxAllowedAutonomyTier).Append(':');

            // Sort nodes deterministically
            var sortedNodeKeys = new List<string>(Nodes.Keys);
            sortedNodeKeys.Sort(StringComparer.Ordinal);
            foreach (var key in sortedNodeKeys)
            {
                var n = Nodes[key];
                sb.Append(n.NodeId.Value).Append('-')
                  .Append((int)n.NodeType).Append('-')
                  .Append((int)n.ExecutionPolicy.RequiredAutonomyTier).Append('-')
                  .Append(n.ExecutionPolicy.RequiredCapabilityId?.ToString() ?? "NONE").Append(';');
            }

            // Sort edges deterministically
            sb.Append("|EDGES:");
            foreach (var edge in Edges)
            {
                sb.Append(edge.SourceNodeId.Value).Append("->").Append(edge.TargetNodeId.Value)
                  .Append('(').Append((int)edge.Type).Append(')').Append(';');
            }

            using var sha = SHA256.Create();
            var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }
}
