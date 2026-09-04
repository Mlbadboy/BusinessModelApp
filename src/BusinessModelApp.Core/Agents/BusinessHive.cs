using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Missions;

namespace BusinessModelApp.Core.Agents
{
    public class HiveEvent
    {
        public Guid EventId { get; set; } = Guid.NewGuid();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string EventType { get; set; } = string.Empty;
        public string SourceAgent { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class BusinessHive
    {
        public Guid HiveId { get; set; } = Guid.NewGuid();
        public Guid WorkspaceId { get; set; }

        public ConcurrentDictionary<Guid, AgentContext> AgentRegistry { get; } = new();
        public ConcurrentDictionary<Guid, MissionBlackboard> Blackboards { get; } = new();
        public ConcurrentDictionary<Guid, DurableMission> MissionLedger { get; } = new();
        public IAgentMessageBus MessageBus { get; }
        public List<HiveEvent> EventLog { get; } = new();
        public List<BlackboardTaskItem> TaskQueue { get; } = new();

        private readonly object _lock = new();

        public BusinessHive(Guid workspaceId, IAgentMessageBus? messageBus = null)
        {
            WorkspaceId = workspaceId;
            MessageBus = messageBus ?? new InMemoryAgentMessageBus();
        }

        public AgentContext RegisterAgent(AgentRole role, Guid? missionId = null)
        {
            var identity = AgentIdentity.Create(role);
            identity.MissionId = missionId;

            var memory = new AgentMemory
            {
                AgentId = identity.AgentId,
                MissionId = missionId ?? Guid.Empty
            };

            var mailbox = new AgentMailbox(identity.AgentId);
            MessageBus.RegisterMailbox(mailbox);

            var context = new AgentContext(identity, memory, mailbox);
            AgentRegistry[identity.AgentId] = context;

            RecordEvent("AGENT_REGISTERED", identity.Name, $"Agent '{identity.Name}' registered in Business Hive.");
            return context;
        }

        public MissionBlackboard GetOrCreateBlackboard(Guid missionId, Guid objectiveId, string objectiveTitle, decimal targetRevenueINR)
        {
            return Blackboards.GetOrAdd(missionId, id =>
            {
                var bb = new MissionBlackboard
                {
                    MissionId = id,
                    ObjectiveId = objectiveId,
                    ObjectiveTitle = objectiveTitle,
                    TargetRevenueINR = targetRevenueINR
                };
                RecordEvent("BLACKBOARD_CREATED", "Supervisor", $"Blackboard created for mission '{objectiveTitle}'.");
                return bb;
            });
        }

        public void RecordEvent(string eventType, string sourceAgent, string description)
        {
            lock (_lock)
            {
                EventLog.Add(new HiveEvent
                {
                    EventType = eventType,
                    SourceAgent = sourceAgent,
                    Description = description
                });
            }
        }

        public IReadOnlyList<AgentContext> GetActiveAgents()
        {
            return AgentRegistry.Values.ToList();
        }
    }
}
