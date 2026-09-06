using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BusinessModelApp.Core.Domain.Runtime
{
    public interface IRuntimeEvent
    {
        RuntimeEventId EventId { get; }
        string EventType { get; }
        DateTime OccurredAtUtc { get; }
        DateTime RecordedAtUtc { get; }
        Guid CorrelationId { get; }
        Guid CausationId { get; }
        Guid? ParentEventId { get; }
        Guid WorkspaceId { get; }
        string ActorId { get; }
        string SchemaVersion { get; }
        string ProducerVersion { get; }
        long SequenceNumber { get; }
    }

    public abstract record RuntimeEventBase : IRuntimeEvent
    {
        public RuntimeEventId EventId { get; init; } = RuntimeEventId.New();
        public abstract string EventType { get; }
        public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
        public DateTime RecordedAtUtc { get; init; } = DateTime.UtcNow;
        public Guid CorrelationId { get; init; }
        public Guid CausationId { get; init; }
        public Guid? ParentEventId { get; init; }
        public Guid WorkspaceId { get; init; }
        public string ActorId { get; init; } = "system";
        public string SchemaVersion { get; init; } = "1.0.0";
        public string ProducerVersion { get; init; } = "3.0.0";
        public long SequenceNumber { get; init; }
    }

    public class RuntimeEventEnvelope
    {
        public RuntimeEventId EventId { get; init; }
        public string EventType { get; init; } = string.Empty;
        public DateTime OccurredAtUtc { get; init; }
        public DateTime RecordedAtUtc { get; init; }
        public Guid CorrelationId { get; init; }
        public Guid CausationId { get; init; }
        public Guid? ParentEventId { get; init; }
        public Guid WorkspaceId { get; init; }
        public string ActorId { get; init; } = string.Empty;
        public string SchemaVersion { get; init; } = "1.0.0";
        public string ProducerVersion { get; init; } = "3.0.0";
        public long SequenceNumber { get; init; }

        public string PayloadJson { get; init; } = string.Empty;
        public string PayloadHash { get; init; } = string.Empty;
        public string PreviousEventHash { get; init; } = string.Empty;
        public string EventHash { get; init; } = string.Empty;

        public static string ComputeSha256(string content)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(content ?? string.Empty));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        public static string ComputeEventHash(long sequenceNumber, string previousHash, string payloadHash, Guid workspaceId, DateTime recordedAtUtc)
        {
            var raw = $"{sequenceNumber}:{previousHash}:{payloadHash}:{workspaceId}:{recordedAtUtc:O}";
            return ComputeSha256(raw);
        }

        public static RuntimeEventEnvelope Create<TEvent>(TEvent domainEvent, string previousEventHash, long sequenceNumber)
            where TEvent : class, IRuntimeEvent
        {
            if (domainEvent == null) throw new ArgumentNullException(nameof(domainEvent));
            if (domainEvent.WorkspaceId == Guid.Empty)
                throw new ArgumentException("Event WorkspaceId cannot be empty.", nameof(domainEvent));

            var payloadJson = JsonSerializer.Serialize(domainEvent);
            var payloadHash = ComputeSha256(payloadJson);
            var recordedAtUtc = DateTime.UtcNow;
            var eventHash = ComputeEventHash(sequenceNumber, previousEventHash ?? string.Empty, payloadHash, domainEvent.WorkspaceId, recordedAtUtc);

            return new RuntimeEventEnvelope
            {
                EventId = domainEvent.EventId,
                EventType = domainEvent.EventType,
                OccurredAtUtc = domainEvent.OccurredAtUtc,
                RecordedAtUtc = recordedAtUtc,
                CorrelationId = domainEvent.CorrelationId == Guid.Empty ? Guid.NewGuid() : domainEvent.CorrelationId,
                CausationId = domainEvent.CausationId == Guid.Empty ? domainEvent.EventId.Value : domainEvent.CausationId,
                ParentEventId = domainEvent.ParentEventId,
                WorkspaceId = domainEvent.WorkspaceId,
                ActorId = domainEvent.ActorId,
                SchemaVersion = domainEvent.SchemaVersion,
                ProducerVersion = domainEvent.ProducerVersion,
                SequenceNumber = sequenceNumber,
                PayloadJson = payloadJson,
                PayloadHash = payloadHash,
                PreviousEventHash = previousEventHash ?? string.Empty,
                EventHash = eventHash
            };
        }

        public bool VerifyHashIntegrity(string expectedPreviousHash)
        {
            if (!string.Equals(PreviousEventHash, expectedPreviousHash ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                return false;

            var recomputedPayloadHash = ComputeSha256(PayloadJson);
            if (!string.Equals(PayloadHash, recomputedPayloadHash, StringComparison.OrdinalIgnoreCase))
                return false;

            var recomputedEventHash = ComputeEventHash(SequenceNumber, PreviousEventHash, PayloadHash, WorkspaceId, RecordedAtUtc);
            return string.Equals(EventHash, recomputedEventHash, StringComparison.OrdinalIgnoreCase);
        }
    }

    public class RuntimeEventEnvelope<TEvent> : RuntimeEventEnvelope where TEvent : class, IRuntimeEvent
    {
        [JsonIgnore]
        public TEvent? TypedPayload => string.IsNullOrEmpty(PayloadJson) ? null : JsonSerializer.Deserialize<TEvent>(PayloadJson);
    }

    // ==========================================
    // CORE RUNTIME DOMAIN EVENT CONTRACTS
    // ==========================================

    public record ResponsibilityCreatedEvent(ResponsibilityId ResponsibilityId, string Title, string BusinessDomain) : RuntimeEventBase
    {
        public override string EventType => "ResponsibilityCreated";
    }

    public record ResponsibilityTriggeredEvent(ResponsibilityId ResponsibilityId, string Reason, decimal MetricValue) : RuntimeEventBase
    {
        public override string EventType => "ResponsibilityTriggered";
    }

    public record ResponsibilityStateChangedEvent(ResponsibilityId ResponsibilityId, ResponsibilityState OldState, ResponsibilityState NewState) : RuntimeEventBase
    {
        public override string EventType => "ResponsibilityStateChanged";
    }

    public record MissionGraphCreatedEvent(MissionGraphId GraphId, string Title) : RuntimeEventBase
    {
        public override string EventType => "MissionGraphCreated";
    }

    public record MissionGraphStateChangedEvent(MissionGraphId GraphId, MissionGraphState OldState, MissionGraphState NewState) : RuntimeEventBase
    {
        public override string EventType => "MissionGraphStateChanged";
    }

    public record MissionNodeCreatedEvent(MissionGraphId GraphId, MissionNodeId NodeId, string NodeType) : RuntimeEventBase
    {
        public override string EventType => "MissionNodeCreated";
    }

    public record MissionNodeStateChangedEvent(MissionGraphId GraphId, MissionNodeId NodeId, MissionNodeState OldState, MissionNodeState NewState) : RuntimeEventBase
    {
        public override string EventType => "MissionNodeStateChanged";
    }

    public record RunCreatedEvent(RuntimeRunId RunId, MissionGraphId GraphId) : RuntimeEventBase
    {
        public override string EventType => "RunCreated";
    }

    public record RunStartedEvent(RuntimeRunId RunId, string WorkerId) : RuntimeEventBase
    {
        public override string EventType => "RunStarted";
    }

    public record RunPausedEvent(RuntimeRunId RunId, string Reason) : RuntimeEventBase
    {
        public override string EventType => "RunPaused";
    }

    public record RunResumedEvent(RuntimeRunId RunId) : RuntimeEventBase
    {
        public override string EventType => "RunResumed";
    }

    public record RunCompletedEvent(RuntimeRunId RunId, string Summary) : RuntimeEventBase
    {
        public override string EventType => "RunCompleted";
    }

    public record RunFailedEvent(RuntimeRunId RunId, string ErrorMessage) : RuntimeEventBase
    {
        public override string EventType => "RunFailed";
    }

    public record RunCancelledEvent(RuntimeRunId RunId, string Reason) : RuntimeEventBase
    {
        public override string EventType => "RunCancelled";
    }

    public record RunKilledEvent(RuntimeRunId RunId, string Reason) : RuntimeEventBase
    {
        public override string EventType => "RunKilled";
    }

    public record AgentSpawnedEvent(AgentInstanceId InstanceId, AgentDefinitionId DefinitionId, string Role) : RuntimeEventBase
    {
        public override string EventType => "AgentSpawned";
    }

    public record AgentStateChangedEvent(AgentInstanceId InstanceId, AgentInstanceState OldState, AgentInstanceState NewState) : RuntimeEventBase
    {
        public override string EventType => "AgentStateChanged";
    }

    public record AgentHeartbeatEvent(AgentInstanceId InstanceId, AgentHealthStatus Status, long WorkloadCount) : RuntimeEventBase
    {
        public override string EventType => "AgentHeartbeat";
    }

    public record AgentTerminatedEvent(AgentInstanceId InstanceId, string Reason) : RuntimeEventBase
    {
        public override string EventType => "AgentTerminated";
    }

    public record CapabilityRegisteredEvent(CapabilityId CapabilityId, string Description) : RuntimeEventBase
    {
        public override string EventType => "CapabilityRegistered";
    }

    public record CapabilityStateChangedEvent(CapabilityId CapabilityId, CapabilityState OldState, CapabilityState NewState) : RuntimeEventBase
    {
        public override string EventType => "CapabilityStateChanged";
    }

    public record LeaseAcquiredEvent(LeaseId LeaseId, RuntimeRunId RunId, string WorkerId, FenceToken Token, DateTime ExpiresAtUtc) : RuntimeEventBase
    {
        public override string EventType => "LeaseAcquired";
    }

    public record LeaseRenewedEvent(LeaseId LeaseId, RuntimeRunId RunId, FenceToken Token, DateTime ExpiresAtUtc) : RuntimeEventBase
    {
        public override string EventType => "LeaseRenewed";
    }

    public record LeaseExpiredEvent(LeaseId LeaseId, RuntimeRunId RunId) : RuntimeEventBase
    {
        public override string EventType => "LeaseExpired";
    }

    public record LeaseFencedEvent(LeaseId LeaseId, RuntimeRunId RunId, FenceToken StaleToken, FenceToken CurrentToken) : RuntimeEventBase
    {
        public override string EventType => "LeaseFenced";
    }

    public record CheckpointCreatedEvent(CheckpointId CheckpointId, RuntimeRunId RunId, long CheckpointSequenceNumber) : RuntimeEventBase
    {
        public override string EventType => "CheckpointCreated";
    }

    public record RuntimeAdmissionGrantedEvent(RuntimeRunId RunId, Guid TargetWorkspaceId, int Priority) : RuntimeEventBase
    {
        public override string EventType => "RuntimeAdmissionGranted";
    }

    public record RuntimeAdmissionDeniedEvent(RuntimeRunId RunId, Guid TargetWorkspaceId, string RejectionReason) : RuntimeEventBase
    {
        public override string EventType => "RuntimeAdmissionDenied";
    }
}
