using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using BusinessModelApp.Core.Domain.ExternalReality;
using BusinessModelApp.Core.Domain.Missions;

namespace BusinessModelApp.Core.Domain.Runtime.Constraints
{
    public enum ConstraintType
    {
        Liquidity,
        Solvency,
        UnitEconomics,
        Revenue,
        Operational,
        Capacity,
        Governance,
        Regulatory,
        Strategic,
        Resource
    }

    public enum ConstraintEnforcementMode
    {
        HardFailClosed,
        SoftOptimizing,
        AdvisoryPreference
    }

    public enum ConstraintComparisonOperator
    {
        LessThanOrEqual,
        GreaterThanOrEqual,
        Equal,
        NotEqual,
        Between,
        OneOf
    }

    public enum ConstraintEvaluationState
    {
        Satisfied,
        Warning,
        Constrained,
        Blocked,
        Unknown,
        Stale,
        Conflicted
    }

    public enum ViolationSeverity
    {
        None,
        Low,
        Medium,
        High,
        Critical
    }

    public readonly record struct ConstraintId
    {
        public Guid Value { get; }

        public ConstraintId(Guid value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("ConstraintId cannot be empty.", nameof(value));
            Value = value;
        }

        public static ConstraintId New() => new(Guid.NewGuid());
        public static ConstraintId From(Guid value) => new(value);
        public static ConstraintId From(string value) => new(Guid.Parse(value));

        public override string ToString() => Value.ToString();
    }

    public record ConstraintVersion
    {
        public int Value { get; init; } = 1;

        public ConstraintVersion Next() => new() { Value = Value + 1 };
        public static ConstraintVersion Initial => new() { Value = 1 };

        public override string ToString() => $"v{Value}";
    }

    public record BusinessConstraintDefinition
    {
        public ConstraintId ConstraintId { get; init; } = ConstraintId.New();
        public Guid WorkspaceId { get; init; }
        public ConstraintVersion Version { get; set; } = ConstraintVersion.Initial;
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public ConstraintType Type { get; init; }
        public ConstraintEnforcementMode EnforcementMode { get; init; } = ConstraintEnforcementMode.HardFailClosed;
        public string MetricName { get; init; } = string.Empty;
        public ConstraintComparisonOperator ComparisonOperator { get; init; } = ConstraintComparisonOperator.GreaterThanOrEqual;
        public double ThresholdValue { get; init; }
        public double? SecondaryThresholdValue { get; init; } // for Between operator
        public string Unit { get; init; } = "INR";
        public TimeSpan FreshnessRequirement { get; init; } = TimeSpan.FromMinutes(30);
        public string RequiredEvidenceType { get; init; } = "FinancialTelemetry";
        public ViolationSeverity Severity { get; init; } = ViolationSeverity.High;
        public DateTimeOffset EffectiveFrom { get; init; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? ExpiresAt { get; init; }
        public string Authority { get; init; } = "ExecutiveBoard";
        public string? ParentVersionHash { get; set; }
        public string VersionHash { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public string ComputeHash()
        {
            var raw = $"{WorkspaceId}:{ConstraintId.Value}:{Version.Value}:{MetricName}:{ComparisonOperator}:{ThresholdValue}:{EnforcementMode}:{FreshnessRequirement.TotalSeconds}:{IsActive}:{ParentVersionHash}";
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }
    }

    public record ConstraintEvaluationResult
    {
        public ConstraintId ConstraintId { get; init; }
        public string Title { get; init; } = string.Empty;
        public ConstraintType Type { get; init; }
        public ConstraintEnforcementMode EnforcementMode { get; init; }
        public ConstraintEvaluationState State { get; init; }
        public double ObservedValue { get; init; }
        public double ThresholdValue { get; init; }
        public string Unit { get; init; } = "INR";
        public IReadOnlyList<string> EvidenceReferences { get; init; } = Array.Empty<string>();
        public TimeSpan EvidenceAge { get; init; }
        public bool IsStale { get; init; }
        public ViolationSeverity Severity { get; init; }
        public double? ProjectedValue { get; init; }
        public string Rationale { get; init; } = string.Empty;
        public DateTimeOffset EvaluatedAt { get; init; } = DateTimeOffset.UtcNow;
        public ConstraintVersion ConstraintVersion { get; init; } = ConstraintVersion.Initial;
        public string AuditHash { get; init; } = string.Empty;

        public bool IsHardViolation => EnforcementMode == ConstraintEnforcementMode.HardFailClosed &&
                                      (State == ConstraintEvaluationState.Blocked ||
                                       State == ConstraintEvaluationState.Unknown ||
                                       State == ConstraintEvaluationState.Stale ||
                                       State == ConstraintEvaluationState.Conflicted);
    }
}
