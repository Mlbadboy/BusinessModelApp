using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace BusinessModelApp.Core.Domain.Missions
{
    public interface IMissionEvent
    {
        Guid EventId { get; }
        Guid MissionId { get; }
        long SequenceNumber { get; }
        string EventType { get; }
        DateTime OccurredAtUtc { get; }
        string PayloadJson { get; }
    }

    public abstract class MissionEventBase : IMissionEvent
    {
        public Guid EventId { get; set; } = Guid.NewGuid();
        public Guid MissionId { get; set; }
        public long SequenceNumber { get; set; }
        public abstract string EventType { get; }
        public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
        public virtual string PayloadJson => JsonSerializer.Serialize((object)this);
    }

    public class MissionCreatedEvent : MissionEventBase
    {
        public override string EventType => nameof(MissionCreatedEvent);
        public string Objective { get; set; } = string.Empty;
        public decimal TargetRevenueINR { get; set; }
        public decimal MaxBudgetINR { get; set; }
    }

    public class WorldModelBoundEvent : MissionEventBase
    {
        public override string EventType => nameof(WorldModelBoundEvent);
        public Guid SnapshotId { get; set; }
        public int GroundedEvidenceCount { get; set; }
    }

    public class RevenueBaselineCalculatedEvent : MissionEventBase
    {
        public override string EventType => nameof(RevenueBaselineCalculatedEvent);
        public decimal ContractedRevenueINR { get; set; }
        public decimal WeightedPipelineINR { get; set; }
        public decimal AutonomousGapINR { get; set; }
    }

    public class StrategySimulatedEvent : MissionEventBase
    {
        public override string EventType => nameof(StrategySimulatedEvent);
        public string StrategyName { get; set; } = string.Empty;
        public decimal SimulatedRevenueINR { get; set; }
        public double Probability { get; set; }
    }

    public class PolicyApprovedEvent : MissionEventBase
    {
        public override string EventType => nameof(PolicyApprovedEvent);
        public string PolicyEngineVersion { get; set; } = "2.0";
        public int EvaluatedRulesCount { get; set; }
    }

    public class AgentDispatchedEvent : MissionEventBase
    {
        public override string EventType => nameof(AgentDispatchedEvent);
        public string AgentId { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public decimal AllocatedBudgetINR { get; set; }
    }

    public class MissionStateTransitionedEvent : MissionEventBase
    {
        public override string EventType => nameof(MissionStateTransitionedEvent);
        public DurableMissionState PreviousState { get; set; }
        public DurableMissionState NewState { get; set; }
    }

    public class MissionStateProjection
    {
        public Guid MissionId { get; set; }
        public DurableMissionState CurrentState { get; set; } = DurableMissionState.Draft;
        public string Objective { get; set; } = string.Empty;
        public decimal TargetRevenueINR { get; set; }
        public decimal TotalAllocatedBudgetINR { get; set; }
        public decimal AutonomousGapINR { get; set; }
        public int EventCount { get; set; }
        public long LastSequenceNumber { get; set; }
        public List<string> DispatchedAgentIds { get; set; } = new();
    }

    public interface IMissionEventStore
    {
        Task<bool> AppendEventAsync(IMissionEvent evt);
        Task<IReadOnlyList<IMissionEvent>> GetEventStreamAsync(Guid missionId);
        Task<MissionStateProjection> ReplayMissionStateAsync(Guid missionId);
    }
}
