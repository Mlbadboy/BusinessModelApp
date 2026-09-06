using System;
using System.Collections.Generic;
using BusinessModelApp.Core.Domain.Missions;

namespace BusinessModelApp.Core.Domain.Runtime.Constraints
{
    public record ArbitrationCandidate
    {
        public MissionId MissionId { get; init; }
        public MissionNodeId? NodeId { get; init; }
        public string Title { get; init; } = string.Empty;
        public MissionPriority Priority { get; init; } = MissionPriority.P2_Medium;
        public ResourceClass ResourceClass { get; init; }
        public double RequestedAmount { get; init; }
        public double ExpectedReturnOnInvestment { get; init; }
        public double LiquidityPreservationImpact { get; init; }
        public double StrategicAlignmentScore { get; init; }
        public double RiskScore { get; init; } // 0.0 (low risk) to 1.0 (critical risk)
        public DateTimeOffset RequestedAt { get; init; } = DateTimeOffset.UtcNow;
    }

    public record CandidateArbitrationEvaluation
    {
        public MissionId MissionId { get; init; }
        public MissionNodeId? NodeId { get; init; }
        public bool PassesHardConstraints { get; init; }
        public string? IneligibilityReason { get; init; }
        public double StrategicUtilityScore { get; init; }
        public double RiskAdjustedScore { get; init; }
        public int LexicographicRank { get; init; }
        public IReadOnlyList<ConstraintEvaluationResult> ConstraintResults { get; init; } = Array.Empty<ConstraintEvaluationResult>();
    }

    public record ResourceArbitrationDecision
    {
        public Guid DecisionId { get; init; } = Guid.NewGuid();
        public Guid WorkspaceId { get; init; }
        public ResourceClass ResourceClass { get; init; }
        public double TotalAvailableAmount { get; init; }
        public StrategicRegimeType StrategicRegime { get; init; }
        public IReadOnlyList<CandidateArbitrationEvaluation> CandidateEvaluations { get; init; } = Array.Empty<CandidateArbitrationEvaluation>();
        public MissionId? WinningMissionId { get; init; }
        public MissionNodeId? WinningNodeId { get; init; }
        public double AllocatedAmount { get; init; }
        public ReservationId? ReservationId { get; init; }
        public string SelectionRationale { get; init; } = string.Empty;
        public string TieBreakReason { get; init; } = string.Empty;
        public DateTimeOffset ArbitratedAt { get; init; } = DateTimeOffset.UtcNow;
        public string ConstraintSnapshotHash { get; init; } = string.Empty;
        public string AuditHash { get; init; } = string.Empty;
    }
}
