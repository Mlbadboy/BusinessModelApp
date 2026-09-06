using System;
using System.Collections.Generic;
using BusinessModelApp.Core.Domain.Runtime;

namespace BusinessModelApp.Core.Domain.Responsibilities
{
    public enum ResponsibilityLifecycleState
    {
        Detected,
        Evaluating,
        Confirmed,
        Active,
        Suppressed,
        Escalated,
        Resolved,
        Expired,
        Rejected,
        Unknown
    }

    public enum ResponsibilityDomain
    {
        Revenue,
        Sales,
        Customers,
        Marketing,
        Operations,
        Finance,
        Inventory,
        People,
        Technology,
        Security,
        Compliance,
        Strategy
    }

    public enum ResponsibilityPriority
    {
        P0_Critical,
        P1_High,
        P2_Medium,
        P3_Low
    }

    public enum AutonomyTier
    {
        L0_Observe = 0,
        L1_Advise = 1,
        L2_Simulate = 2,
        L3_Prepare = 3,
        L4_ExecuteWithApproval = 4,
        L5_ExecuteBounded = 5
    }

    public record ResponsibilitySuppressionDetails
    {
        public bool IsSuppressed { get; init; }
        public string? Reason { get; init; }
        public string? SuppressedByActor { get; init; }
        public DateTime? SuppressedAtUtc { get; init; }
        public DateTime? ExpiresAtUtc { get; init; }
        public decimal? SuppressedMetricThreshold { get; init; }
    }

    public record ResponsibilityRecord
    {
        public ResponsibilityId Id { get; init; } = ResponsibilityId.New();
        public Guid WorkspaceId { get; init; }
        public string DefinitionId { get; init; } = string.Empty;
        public string EntityScope { get; init; } = "global";
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public ResponsibilityDomain Domain { get; init; } = ResponsibilityDomain.Operations;
        public string OwnerRole { get; init; } = "Operator";
        public ResponsibilityPriority Priority { get; set; } = ResponsibilityPriority.P2_Medium;
        public ResponsibilityLifecycleState State { get; set; } = ResponsibilityLifecycleState.Detected;
        public AutonomyTier AllowedAutonomyTier { get; init; } = AutonomyTier.L1_Advise;
        public decimal SeverityScore { get; set; } = 1.0m;
        public TimeSpan CooldownWindow { get; init; } = TimeSpan.FromMinutes(30);
        public TimeSpan PersistenceWindow { get; init; } = TimeSpan.FromMinutes(5);
        public TimeSpan RecoveryPersistenceWindow { get; init; } = TimeSpan.FromMinutes(10);
        public DateTime? LastTriggeredUtc { get; set; }
        public DateTime? FirstTriggeredUtc { get; set; }
        public DateTime? RecoveryStartedUtc { get; set; }
        public DateTime? ResolvedAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
        public decimal RiskCeiling { get; init; } = 1000m;
        public ResponsibilitySuppressionDetails Suppression { get; set; } = new();
        public List<string> RequiredEvidenceTypes { get; init; } = new();
        public List<Guid> CorroboratedEvidenceIds { get; init; } = new();
        public int ConsecutiveBreaches { get; set; }
        public int ConsecutiveRecoveries { get; set; }
    }
}
