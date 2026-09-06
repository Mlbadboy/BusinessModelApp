using System;
using System.Text.Json.Serialization;

namespace BusinessModelApp.Core.Domain.Runtime
{
    /// <summary>
    /// Monotonically increasing fencing token to prevent stale workers from mutating runtime state.
    /// </summary>
    public readonly record struct FenceToken : IComparable<FenceToken>
    {
        public long Value { get; }

        [JsonConstructor]
        public FenceToken(long value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "FenceToken must be non-negative.");
            Value = value;
        }

        public static FenceToken Initial => new(1);
        public FenceToken Next() => new(Value + 1);

        public int CompareTo(FenceToken other) => Value.CompareTo(other.Value);
        public static bool operator <(FenceToken left, FenceToken right) => left.Value < right.Value;
        public static bool operator >(FenceToken left, FenceToken right) => left.Value > right.Value;
        public static bool operator <=(FenceToken left, FenceToken right) => left.Value <= right.Value;
        public static bool operator >=(FenceToken left, FenceToken right) => left.Value >= right.Value;

        public override string ToString() => Value.ToString();
    }

    public readonly record struct ResponsibilityId
    {
        public Guid Value { get; }

        [JsonConstructor]
        public ResponsibilityId(Guid value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("ResponsibilityId cannot be Guid.Empty.", nameof(value));
            Value = value;
        }

        public static ResponsibilityId New() => new(Guid.NewGuid());
        public static ResponsibilityId From(Guid value) => new(value);
        public static ResponsibilityId From(string value) => new(Guid.Parse(value));
        public override string ToString() => Value.ToString();
    }

    public readonly record struct MissionGraphId
    {
        public Guid Value { get; }

        [JsonConstructor]
        public MissionGraphId(Guid value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("MissionGraphId cannot be Guid.Empty.", nameof(value));
            Value = value;
        }

        public static MissionGraphId New() => new(Guid.NewGuid());
        public static MissionGraphId From(Guid value) => new(value);
        public static MissionGraphId From(string value) => new(Guid.Parse(value));
        public override string ToString() => Value.ToString();
    }

    public readonly record struct MissionRunId
    {
        public Guid Value { get; }

        [JsonConstructor]
        public MissionRunId(Guid value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("MissionRunId cannot be Guid.Empty.", nameof(value));
            Value = value;
        }

        public static MissionRunId New() => new(Guid.NewGuid());
        public static MissionRunId From(Guid value) => new(value);
        public static MissionRunId From(string value) => new(Guid.Parse(value));
        public override string ToString() => Value.ToString();
    }

    public readonly record struct RuntimeRunId
    {
        public Guid Value { get; }

        [JsonConstructor]
        public RuntimeRunId(Guid value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("RuntimeRunId cannot be Guid.Empty.", nameof(value));
            Value = value;
        }

        public static RuntimeRunId New() => new(Guid.NewGuid());
        public static RuntimeRunId From(Guid value) => new(value);
        public static RuntimeRunId From(string value) => new(Guid.Parse(value));
        public override string ToString() => Value.ToString();
    }

    public readonly record struct MissionNodeId
    {
        public string Value { get; }

        [JsonConstructor]
        public MissionNodeId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("MissionNodeId cannot be null or whitespace.", nameof(value));
            Value = value.Trim();
        }

        public static MissionNodeId From(string value) => new(value);
        public override string ToString() => Value;
    }

    public readonly record struct AgentDefinitionId
    {
        public string Value { get; }

        [JsonConstructor]
        public AgentDefinitionId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("AgentDefinitionId cannot be null or whitespace.", nameof(value));
            Value = value.Trim().ToLowerInvariant();
        }

        public static AgentDefinitionId From(string value) => new(value);
        public override string ToString() => Value;
    }

    public readonly record struct AgentInstanceId
    {
        public Guid Value { get; }

        [JsonConstructor]
        public AgentInstanceId(Guid value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("AgentInstanceId cannot be Guid.Empty.", nameof(value));
            Value = value;
        }

        public static AgentInstanceId New() => new(Guid.NewGuid());
        public static AgentInstanceId From(Guid value) => new(value);
        public static AgentInstanceId From(string value) => new(Guid.Parse(value));
        public override string ToString() => Value.ToString();
    }

    public readonly record struct CapabilityId
    {
        public string Name { get; }
        public string Version { get; }

        [JsonConstructor]
        public CapabilityId(string name, string version)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Capability name cannot be null or whitespace.", nameof(name));
            if (string.IsNullOrWhiteSpace(version))
                throw new ArgumentException("Capability version cannot be null or whitespace.", nameof(version));

            Name = name.Trim().ToLowerInvariant();
            Version = version.Trim().ToLowerInvariant();
        }

        public static CapabilityId Parse(string canonical)
        {
            if (string.IsNullOrWhiteSpace(canonical))
                throw new ArgumentException("Canonical capability string cannot be null or whitespace.", nameof(canonical));

            var parts = canonical.Split(':');
            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
                throw new ArgumentException($"Invalid canonical capability format '{canonical}'. Expected 'name:version'.", nameof(canonical));

            return new CapabilityId(parts[0], parts[1]);
        }

        public override string ToString() => $"{Name}:{Version}";
    }

    public readonly record struct ExecutionIntentId
    {
        public Guid Value { get; }

        [JsonConstructor]
        public ExecutionIntentId(Guid value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("ExecutionIntentId cannot be Guid.Empty.", nameof(value));
            Value = value;
        }

        public static ExecutionIntentId New() => new(Guid.NewGuid());
        public static ExecutionIntentId From(Guid value) => new(value);
        public static ExecutionIntentId From(string value) => new(Guid.Parse(value));
        public override string ToString() => Value.ToString();
    }

    public readonly record struct ExecutionAttemptId
    {
        public Guid Value { get; }

        [JsonConstructor]
        public ExecutionAttemptId(Guid value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("ExecutionAttemptId cannot be Guid.Empty.", nameof(value));
            Value = value;
        }

        public static ExecutionAttemptId New() => new(Guid.NewGuid());
        public static ExecutionAttemptId From(Guid value) => new(value);
        public static ExecutionAttemptId From(string value) => new(Guid.Parse(value));
        public override string ToString() => Value.ToString();
    }

    public readonly record struct LeaseId
    {
        public Guid Value { get; }

        [JsonConstructor]
        public LeaseId(Guid value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("LeaseId cannot be Guid.Empty.", nameof(value));
            Value = value;
        }

        public static LeaseId New() => new(Guid.NewGuid());
        public static LeaseId From(Guid value) => new(value);
        public static LeaseId From(string value) => new(Guid.Parse(value));
        public override string ToString() => Value.ToString();
    }

    public readonly record struct CheckpointId
    {
        public Guid Value { get; }

        [JsonConstructor]
        public CheckpointId(Guid value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("CheckpointId cannot be Guid.Empty.", nameof(value));
            Value = value;
        }

        public static CheckpointId New() => new(Guid.NewGuid());
        public static CheckpointId From(Guid value) => new(value);
        public static CheckpointId From(string value) => new(Guid.Parse(value));
        public override string ToString() => Value.ToString();
    }

    public readonly record struct RuntimeEventId
    {
        public Guid Value { get; }

        [JsonConstructor]
        public RuntimeEventId(Guid value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("RuntimeEventId cannot be Guid.Empty.", nameof(value));
            Value = value;
        }

        public static RuntimeEventId New() => new(Guid.NewGuid());
        public static RuntimeEventId From(Guid value) => new(value);
        public static RuntimeEventId From(string value) => new(Guid.Parse(value));
        public override string ToString() => Value.ToString();
    }
}
