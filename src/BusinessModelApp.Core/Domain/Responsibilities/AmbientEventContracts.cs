using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Responsibilities
{
    public enum ComparisonOperator
    {
        LessThan,
        GreaterThan,
        Equals,
        PercentageDrop,
        PercentageIncrease
    }

    public enum EvidenceStatus
    {
        Strong,
        Weak,
        Contradicted,
        Poisoned,
        Unknown
    }

    public record AmbientBusinessEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public Guid WorkspaceId { get; init; }
        public string Source { get; init; } = string.Empty;
        public string EventType { get; init; } = string.Empty;
        public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
        public DateTime ReceivedAtUtc { get; init; } = DateTime.UtcNow;
        public string EntityType { get; init; } = string.Empty;
        public string EntityId { get; init; } = string.Empty;
        public decimal MetricValue { get; init; }
        public string Unit { get; init; } = string.Empty;
        public Dictionary<string, object> Payload { get; init; } = new();
        public string IdempotencyKey { get; init; } = string.Empty;
        public double SourceTrustLevel { get; init; } = 1.0;
        public bool IsContradictorySignal { get; init; } = false;
        public bool IsPoisoned { get; init; } = false;
    }

    public record AmbientTriggerCondition
    {
        public string TriggerId { get; init; } = string.Empty;
        public string MetricName { get; init; } = string.Empty;
        public ComparisonOperator Operator { get; init; } = ComparisonOperator.LessThan;
        public decimal ThresholdValue { get; init; }
        public decimal RecoveryThresholdValue { get; init; }
        public TimeSpan ObservationWindow { get; init; } = TimeSpan.FromMinutes(15);
        public int RequiredConsecutiveBreaches { get; init; } = 1;
        public int RequiredConsecutiveRecoveries { get; init; } = 2;
    }

    public record SignalEvidenceAssessment
    {
        public Guid SignalEventId { get; init; }
        public Guid WorkspaceId { get; init; }
        public bool IsCorroborated { get; init; }
        public double TrustScore { get; init; } = 1.0;
        public double FreshnessScore { get; init; } = 1.0;
        public bool HasContradiction { get; init; }
        public bool IsPoisonedSignal { get; init; }
        public EvidenceStatus Status { get; init; } = EvidenceStatus.Strong;
        public DateTime EvaluatedAtUtc { get; init; } = DateTime.UtcNow;
        public string Explanation { get; init; } = string.Empty;
    }

    public record TriggerEvaluationResult
    {
        public bool IsTriggered { get; init; }
        public bool IsRecovered { get; init; }
        public decimal CurrentValue { get; init; }
        public decimal ThresholdValue { get; init; }
        public decimal RecoveryThresholdValue { get; init; }
        public string Explanation { get; init; } = string.Empty;
    }
}
