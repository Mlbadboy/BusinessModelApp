using System;
using System.Collections.Generic;
using BusinessModelApp.Core.Domain.Responsibilities;
using BusinessModelApp.Core.Domain.Runtime;

namespace BusinessModelApp.Core.Domain.Missions
{
    public enum MissionNodeType
    {
        // Batch 3.0 Types
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
        Rollback = 11,

        // Batch 3.2 Types
        Observe = 12,
        Investigate = 13,
        Research = 14,
        Analyze = 15,
        Reason = 16,
        Simulate = 17,
        Decide = 18,
        Prepare = 19,
        Approve = 20,
        Execute = 21,
        Verify = 22,
        Monitor = 23,
        Escalate = 24,
        Wait = 25,
        Join = 26,
        Compensate = 27
    }

    public enum NodeExecutionEffect
    {
        NoEffect = 0,
        EffectSucceeded = 1,
        EffectFailed = 2,
        UnknownEffect = 3
    }

    public record NodeExecutionPolicy
    {
        public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(5);
        public int MaxRetries { get; init; } = 3;
        public int BackoffMs { get; init; } = 1000;
        public CapabilityId? RequiredCapabilityId { get; init; }
        public AutonomyTier RequiredAutonomyTier { get; init; } = AutonomyTier.L1_Advise;
        public long MaxBudgetTokens { get; init; } = 10_000;
        public decimal MaxCostUsd { get; init; } = 0.50m;
        public bool RequiresHumanApproval { get; init; }
        public int MaxIterations { get; init; } = 1;
    }

    public record NodeVerificationCriteria
    {
        public IReadOnlyList<string> RequiredEvidenceTypes { get; init; } = Array.Empty<string>();
        public string? ExpectedOutputSchema { get; init; }
        public IReadOnlyList<string> DeterministicAssertionKeys { get; init; } = Array.Empty<string>();
        public double MinConfidenceScore { get; init; } = 0.80;
    }

    public record NodeVerificationResult
    {
        public bool IsVerified { get; init; }
        public string? EvidenceHash { get; init; }
        public double ConfidenceScore { get; init; }
        public string? FailureReason { get; init; }
        public bool DeterministicAssertionsPassed { get; init; }
        public DateTimeOffset VerifiedAt { get; init; } = DateTimeOffset.UtcNow;

        public static NodeVerificationResult Success(string evidenceHash, double confidence = 1.0) =>
            new()
            {
                IsVerified = true,
                EvidenceHash = evidenceHash,
                ConfidenceScore = confidence,
                DeterministicAssertionsPassed = true
            };

        public static NodeVerificationResult Failed(string reason, double confidence = 0.0) =>
            new()
            {
                IsVerified = false,
                FailureReason = reason,
                ConfidenceScore = confidence,
                DeterministicAssertionsPassed = false
            };
    }

    public record MissionNodeRecord
    {
        public MissionNodeId NodeId { get; init; }
        public MissionGraphId GraphId { get; init; }
        public MissionNodeType NodeType { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public MissionNodeState State { get; set; } = MissionNodeState.Pending;
        public NodeExecutionPolicy ExecutionPolicy { get; init; } = new();
        public NodeVerificationCriteria VerificationCriteria { get; init; } = new();
        public NodeVerificationResult? VerificationResult { get; set; }
        public NodeExecutionEffect LastEffect { get; set; } = NodeExecutionEffect.NoEffect;
        public IReadOnlyList<Guid> ArtifactIds { get; init; } = Array.Empty<Guid>();
        public int CurrentAttempt { get; set; } = 0;
        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? StartedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
        public string? FailureReason { get; set; }

        public MissionNodeRecord() { }

        public MissionNodeRecord(MissionNodeId nodeId, MissionGraphId graphId, string title, MissionNodeType nodeType)
        {
            NodeId = nodeId;
            GraphId = graphId;
            Title = title;
            NodeType = nodeType;
        }
    }

    public record MissionNode : MissionNodeRecord
    {
        public CapabilityId? RequiredCapabilityId
        {
            get => ExecutionPolicy.RequiredCapabilityId;
            init => ExecutionPolicy = ExecutionPolicy with { RequiredCapabilityId = value };
        }

        public MissionNode() { }

        public MissionNode(MissionNodeId nodeId, MissionGraphId graphId, string title, MissionNodeType nodeType)
            : base(nodeId, graphId, title, nodeType)
        {
        }
    }
}
