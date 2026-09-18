using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Strategy
{
    public enum StrategicTimeHorizon
    {
        CurrentQuarter = 0,
        Annual = 1,
        MultiYear = 2
    }

    public enum StrategicInitiativeStatus
    {
        Proposed = 0,
        ApprovedPRG1 = 1,
        InExecution = 2,
        Achieved = 3,
        Abandoned = 4
    }

    public sealed class StrategicObjective
    {
        public string ObjectiveId { get; init; } = Guid.NewGuid().ToString("N");
        public required string TenantId { get; init; }
        public required string Title { get; init; }
        public StrategicTimeHorizon Horizon { get; init; }
        public decimal TargetRevenueINR { get; init; }
        public decimal AllocatedCapitalINR { get; init; }
        public decimal MaxRiskToleranceScore { get; init; } = 0.30m; // 30% max risk tolerance
        public List<StrategicInitiative> Initiatives { get; init; } = new();
    }

    public sealed class StrategicInitiative
    {
        public string InitiativeId { get; init; } = Guid.NewGuid().ToString("N");
        public required string ObjectiveId { get; init; }
        public required string TenantId { get; init; }
        public required string Title { get; init; }
        public required string Description { get; init; }
        public decimal StrategicValueScore { get; init; } // 1 to 100
        public decimal RequiredCapitalINR { get; init; }
        public StrategicInitiativeStatus Status { get; private set; } = StrategicInitiativeStatus.Proposed;
        public string PRG1SignoffSha256 { get; private set; } = string.Empty;
        public DateTime ProposedAtUtc { get; init; } = DateTime.UtcNow;
        public DateTime? ApprovedAtUtc { get; private set; }

        public void ApprovePRG1(string prg1SignoffSha256)
        {
            if (string.IsNullOrWhiteSpace(prg1SignoffSha256) || prg1SignoffSha256.Length != 64)
                throw new ArgumentException("PRG-1 human strategic approval requires valid 64-character SHA-256 signature hash.", nameof(prg1SignoffSha256));

            PRG1SignoffSha256 = prg1SignoffSha256.Trim().ToLowerInvariant();
            Status = StrategicInitiativeStatus.ApprovedPRG1;
            ApprovedAtUtc = DateTime.UtcNow;
        }

        public void StartExecution()
        {
            if (Status != StrategicInitiativeStatus.ApprovedPRG1)
                throw new InvalidOperationException("Initiative must have PRG-1 human strategic approval before execution (Law I39 & I42).");
            Status = StrategicInitiativeStatus.InExecution;
        }

        public void Complete()
        {
            Status = StrategicInitiativeStatus.Achieved;
        }
    }
}
