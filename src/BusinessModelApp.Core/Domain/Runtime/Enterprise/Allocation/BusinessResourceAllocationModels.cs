using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Allocation
{
    public enum ResourcePoolType
    {
        ComputeTokens = 0,
        AgentMissionSlots = 1,
        HumanReviewHours = 2,
        MarketingSpendINR = 3,
        ExternalApiQuota = 4
    }

    public enum AllocationPriority
    {
        P3_Background = 0,
        P2_Normal = 1,
        P1_High = 2,
        P0_Critical = 3
    }

    public enum AllocationDecisionStatus
    {
        Pending = 0,
        Approved = 1,
        RejectedExceedsBudget = 2,
        RejectedRunwayRisk = 3,
        RejectedReserveExhaustion = 4,
        Released = 5
    }

    public sealed class TenantResourceBudget
    {
        public required string TenantId { get; init; }
        public ResourcePoolType PoolType { get; init; }
        public decimal TotalCapacity { get; init; }
        public decimal ConsumedCapacity { get; private set; }
        public decimal ReserveFloorPct { get; init; } = 20.0m; // 20% reserved for P0/P1
        public decimal AvailableCapacity => Math.Max(0m, TotalCapacity - ConsumedCapacity);
        public decimal UsableCapacityForNormalPriority => Math.Max(0m, TotalCapacity * (1m - (ReserveFloorPct / 100m)) - ConsumedCapacity);

        public bool TryAllocate(decimal amount, AllocationPriority priority, out string failureReason)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Allocation amount must be positive.");

            if (priority >= AllocationPriority.P1_High)
            {
                if (ConsumedCapacity + amount > TotalCapacity)
                {
                    failureReason = $"Total capacity exceeded for {PoolType} (Requested: {amount}, Available: {AvailableCapacity}).";
                    return false;
                }
            }
            else
            {
                if (ConsumedCapacity + amount > TotalCapacity * (1m - (ReserveFloorPct / 100m)))
                {
                    failureReason = $"Standard allocation pool exhausted for {PoolType} (Reserve floor of {ReserveFloorPct}% protected for P0/P1).";
                    return false;
                }
            }

            ConsumedCapacity += amount;
            failureReason = string.Empty;
            return true;
        }

        public void Release(decimal amount)
        {
            ConsumedCapacity = Math.Max(0m, ConsumedCapacity - amount);
        }
    }

    public sealed class ResourceAllocationRequest
    {
        public string RequestId { get; init; } = Guid.NewGuid().ToString("N");
        public required string TenantId { get; init; }
        public required string MissionId { get; init; }
        public ResourcePoolType PoolType { get; init; }
        public decimal RequestedAmount { get; init; }
        public AllocationPriority Priority { get; init; }
        public AllocationDecisionStatus Status { get; private set; } = AllocationDecisionStatus.Pending;
        public string DecisionReason { get; private set; } = string.Empty;
        public DateTime RequestedAtUtc { get; init; } = DateTime.UtcNow;
        public DateTime? DecidedAtUtc { get; private set; }

        public void Approve()
        {
            Status = AllocationDecisionStatus.Approved;
            DecisionReason = "Allocated within policy constraints.";
            DecidedAtUtc = DateTime.UtcNow;
        }

        public void Reject(AllocationDecisionStatus reasonStatus, string reason)
        {
            Status = reasonStatus;
            DecisionReason = reason;
            DecidedAtUtc = DateTime.UtcNow;
        }

        public void MarkReleased()
        {
            Status = AllocationDecisionStatus.Released;
        }
    }
}
