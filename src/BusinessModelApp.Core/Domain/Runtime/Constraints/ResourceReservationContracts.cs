using System;
using System.Collections.Generic;
using BusinessModelApp.Core.Domain.Missions;

namespace BusinessModelApp.Core.Domain.Runtime.Constraints
{
    public enum ResourceClass
    {
        Cash,
        Budget,
        HumanApproval,
        Inventory,
        Compute,
        Tokens,
        Concurrency,
        VendorQuota,
        APIQuota
    }

    public enum ReservationState
    {
        Requested,
        Validating,
        Reserved,
        Committed,
        Consumed,
        Released,
        Expired,
        Cancelled,
        Rejected,
        ReconciliationRequired
    }

    public readonly record struct ReservationId
    {
        public Guid Value { get; }

        public ReservationId(Guid value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("ReservationId cannot be empty.", nameof(value));
            Value = value;
        }

        public static ReservationId New() => new(Guid.NewGuid());
        public static ReservationId From(Guid value) => new(value);
        public static ReservationId From(string value) => new(Guid.Parse(value));

        public override string ToString() => Value.ToString();
    }

    public record ResourceReservation
    {
        public ReservationId ReservationId { get; init; } = ReservationId.New();
        public Guid WorkspaceId { get; init; }
        public MissionId MissionId { get; init; }
        public MissionNodeId? NodeId { get; init; }
        public ResourceClass ResourceClass { get; init; }
        public double RequestedAmount { get; init; }
        public double ReservedAmount { get; set; }
        public double ConsumedAmount { get; set; }
        public ReservationState State { get; set; } = ReservationState.Requested;
        public string Unit { get; init; } = "INR";
        public string IdempotencyKey { get; init; } = string.Empty;
        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
        public DateTimeOffset ExpiresAt { get; set; } = DateTimeOffset.UtcNow.AddMinutes(15);
        public DateTimeOffset? CommittedAt { get; set; }
        public DateTimeOffset? ReleasedAt { get; set; }
        public string ConstraintSnapshotHash { get; init; } = string.Empty;
        public int PolicyVersion { get; init; } = 1;
        public long ReservationVersion { get; set; } = 1; // Optimistic concurrency version
    }

    public record ReservationResult
    {
        public bool IsGranted { get; init; }
        public ResourceReservation? Reservation { get; init; }
        public string? FailureReason { get; init; }
        public double AvailableAmountBefore { get; init; }
        public double AvailableAmountAfter { get; init; }

        public static ReservationResult Granted(ResourceReservation reservation, double before, double after) =>
            new() { IsGranted = true, Reservation = reservation, AvailableAmountBefore = before, AvailableAmountAfter = after };

        public static ReservationResult Denied(string reason, double available) =>
            new() { IsGranted = false, FailureReason = reason, AvailableAmountBefore = available, AvailableAmountAfter = available };
    }
}
