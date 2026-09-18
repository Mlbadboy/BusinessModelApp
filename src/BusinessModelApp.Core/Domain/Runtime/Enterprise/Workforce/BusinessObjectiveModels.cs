using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce
{
    public enum ObjectiveStatus
    {
        DRAFT,
        APPROVED,
        ACTIVE,
        PAUSED,
        COMPLETED,
        CANCELLED,
        ARCHIVED
    }

    public enum ObjectivePriority
    {
        LOW = 1,
        MEDIUM = 2,
        HIGH = 3,
        CRITICAL = 4
    }

    public class ObjectiveConstraint
    {
        public string ConstraintId { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = string.Empty;
        public string ConstraintType { get; set; } = string.Empty; // e.g. "MaxCAC", "MinGrossMargin", "TargetGeography", "TargetIndustry"
        public decimal? NumericLimit { get; set; }
        public string? StringLimit { get; set; }
        public bool IsHardConstraint { get; set; } = true;
    }

    public class ObjectiveMetric
    {
        public string MetricId { get; set; } = Guid.NewGuid().ToString("N");
        public string MetricName { get; set; } = string.Empty; // e.g. "QualifiedPipeline", "WonRevenue", "CollectedCash", "MeetingsBooked"
        public decimal TargetValue { get; set; }
        public decimal CurrentValue { get; set; }
        public string Unit { get; set; } = "INR";
        public DateTime LastEvaluatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ObjectiveStrategyLink
    {
        public string StrategyId { get; set; } = string.Empty;
        public string StrategyName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> TargetResponsibilities { get; set; } = new();
    }

    public class BusinessObjective
    {
        public string ObjectiveId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ObjectivePriority Priority { get; set; } = ObjectivePriority.MEDIUM;
        public ObjectiveStatus Status { get; set; } = ObjectiveStatus.DRAFT;
        public DateTime PeriodStart { get; set; } = DateTime.UtcNow;
        public DateTime PeriodEnd { get; set; } = DateTime.UtcNow.AddMonths(3);
        public List<ObjectiveMetric> Metrics { get; set; } = new();
        public List<ObjectiveConstraint> Constraints { get; set; } = new();
        public List<ObjectiveStrategyLink> StrategicLinks { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovedAt { get; set; }
        public string ApprovedBy { get; set; } = string.Empty;
    }

    public class RevenueObjective : BusinessObjective
    {
        public decimal TargetRevenue { get; set; }
        public decimal TargetMarginPercent { get; set; } = 40.0m;
        public decimal MaxCustomerAcquisitionCost { get; set; } = 50000m;
        public decimal MinimumDealSize { get; set; } = 100000m;
        public string TargetPeriod { get; set; } = "Q4-2026";
        public List<string> TargetIndustries { get; set; } = new();
        public List<string> TargetGeographies { get; set; } = new();
        public int MaximumRiskLevel { get; set; } = 2; // R1 or R2
        public string RequiredEvidenceLevel { get; set; } = "CORROBORATED"; // VERIFIED or CORROBORATED
        public decimal RealizedCollectedRevenue { get; set; } = 0m;
        public decimal RealizedGrossMargin { get; set; } = 0m;
    }

    public class ObjectiveOutcome
    {
        public string OutcomeId { get; set; } = Guid.NewGuid().ToString("N");
        public string ObjectiveId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public decimal FinalCollectedRevenue { get; set; }
        public decimal TotalOperatingCost { get; set; }
        public decimal NetContribution { get; set; }
        public bool IsTargetAchieved { get; set; }
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
        public string AuditNotes { get; set; } = string.Empty;
    }
}
