using System;
using System.Text.Json.Serialization;
using BusinessModelApp.Core.Domain.Runtime;

namespace BusinessModelApp.Core.Domain.Missions
{
    public readonly record struct MissionId
    {
        public Guid Value { get; }

        [JsonConstructor]
        public MissionId(Guid value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("MissionId cannot be Guid.Empty.", nameof(value));
            Value = value;
        }

        public static MissionId New() => new(Guid.NewGuid());
        public static MissionId From(Guid value) => new(value);
        public static MissionId From(string value) => new(Guid.Parse(value));
        public override string ToString() => Value.ToString();
    }

    public enum MissionPriority
    {
        P0_Critical = 0,
        P1_High = 1,
        P2_Medium = 2,
        P3_Low = 3
    }

    public enum MissionStatus
    {
        Created = 1,
        Compiling = 2,
        Active = 3,
        WaitingHuman = 4,
        WaitingExternalEvent = 5,
        WaitingSla = 6,
        Paused = 7,
        Suspended = 8,
        WaitingVerification = 9,
        Completed = 10,
        Failed = 11,
        Cancelled = 12,
        Killed = 13
    }

    public record MissionBudget
    {
        public long TotalTokenBudget { get; init; }
        public decimal TotalCostUsdBudget { get; init; }
        public long ReservedTokens { get; set; }
        public decimal ReservedCostUsd { get; set; }
        public long ConsumedTokens { get; set; }
        public decimal ConsumedCostUsd { get; set; }

        public long RemainingTokens => Math.Max(0, TotalTokenBudget - ReservedTokens - ConsumedTokens);
        public decimal RemainingCostUsd => Math.Max(0m, TotalCostUsdBudget - ReservedCostUsd - ConsumedCostUsd);

        public bool CanReserve(long tokens, decimal costUsd) =>
            (ReservedTokens + ConsumedTokens + tokens <= TotalTokenBudget) &&
            (ReservedCostUsd + ConsumedCostUsd + costUsd <= TotalCostUsdBudget);

        public bool TryReserve(long tokens, decimal costUsd)
        {
            if (!CanReserve(tokens, costUsd))
                return false;

            ReservedTokens += tokens;
            ReservedCostUsd += costUsd;
            return true;
        }

        public void Consume(long tokens, decimal costUsd)
        {
            ReservedTokens = Math.Max(0, ReservedTokens - tokens);
            ReservedCostUsd = Math.Max(0m, ReservedCostUsd - costUsd);
            ConsumedTokens += tokens;
            ConsumedCostUsd += costUsd;
        }

        public void ReleaseReservation(long tokens, decimal costUsd)
        {
            ReservedTokens = Math.Max(0, ReservedTokens - tokens);
            ReservedCostUsd = Math.Max(0m, ReservedCostUsd - costUsd);
        }
    }

    public record MissionRecord
    {
        public MissionId Id { get; init; }
        public Guid WorkspaceId { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public Guid? TriggeringResponsibilityId { get; init; }
        public string? OriginatingSignalId { get; init; }
        public MissionPriority Priority { get; set; } = MissionPriority.P2_Medium;
        public MissionStatus Status { get; set; } = MissionStatus.Created;
        public MissionBudget Budget { get; init; } = new();
        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? StartedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
        public DateTimeOffset? TargetCompletionTime { get; init; }
        public string? FailureReason { get; set; }
    }
}
