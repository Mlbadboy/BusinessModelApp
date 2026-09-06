using System;
using System.Collections.Generic;
using BusinessModelApp.Core.Domain.Responsibilities;
using BusinessModelApp.Core.Domain.Runtime;

namespace BusinessModelApp.Core.Domain.Missions
{
    public record ProposedNode
    {
        public string NodeId { get; init; } = string.Empty;
        public MissionNodeType NodeType { get; init; } = MissionNodeType.Analyze;
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string? RequiredCapabilityId { get; init; }
        public AutonomyTier RequiredAutonomyTier { get; init; } = AutonomyTier.L1_Advise;
        public long MaxBudgetTokens { get; init; } = 5_000;
        public decimal MaxCostUsd { get; init; } = 0.25m;
        public NodeVerificationCriteria VerificationCriteria { get; init; } = new();
        public bool RequiresHumanApproval { get; init; }
        public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(5);
        public int MaxRetries { get; init; } = 2;
    }

    public record ProposedEdge
    {
        public string SourceNodeId { get; init; } = string.Empty;
        public string TargetNodeId { get; init; } = string.Empty;
        public EdgeType Type { get; init; } = EdgeType.Sequential;
        public TypedPredicate? Predicate { get; init; }
        public JoinPolicy? JoinPolicy { get; init; }
    }

    public record MissionGraphProposal
    {
        public Guid ProposalId { get; init; } = Guid.NewGuid();
        public Guid WorkspaceId { get; init; }
        public MissionId MissionId { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public IReadOnlyList<ProposedNode> ProposedNodes { get; init; } = Array.Empty<ProposedNode>();
        public IReadOnlyList<ProposedEdge> ProposedEdges { get; init; } = Array.Empty<ProposedEdge>();
        public AutonomyTier ProposedAutonomyTier { get; init; } = AutonomyTier.L1_Advise;
        public string HypothesisSummary { get; init; } = string.Empty;
        public long EstimatedBudgetTokens { get; init; } = 20_000;
        public decimal EstimatedCostUsd { get; init; } = 1.00m;
        public DateTimeOffset ProposedAt { get; init; } = DateTimeOffset.UtcNow;
    }

    public record DynamicExpansionProposal
    {
        public Guid ExpansionProposalId { get; init; } = Guid.NewGuid();
        public MissionGraphId ParentGraphId { get; init; }
        public MissionGraphVersion ParentVersion { get; init; } = MissionGraphVersion.Initial;
        public string ExpectedGraphHash { get; init; } = string.Empty;
        public MissionNodeId TriggeringNodeId { get; init; }
        public string ExpansionReason { get; init; } = string.Empty;
        public IReadOnlyList<ProposedNode> NewNodes { get; init; } = Array.Empty<ProposedNode>();
        public IReadOnlyList<ProposedEdge> NewEdges { get; init; } = Array.Empty<ProposedEdge>();
        public IReadOnlyList<ProposedEdge> RemovedEdges { get; init; } = Array.Empty<ProposedEdge>();
        public long AdditionalBudgetTokens { get; init; } = 0;
        public decimal AdditionalCostUsd { get; init; } = 0m;
        public DateTimeOffset ProposedAt { get; init; } = DateTimeOffset.UtcNow;
    }
}
