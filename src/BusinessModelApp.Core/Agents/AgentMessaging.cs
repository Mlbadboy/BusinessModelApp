using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace BusinessModelApp.Core.Agents
{
    public enum AgentMessageType
    {
        DirectTask = 0,
        InformationShare = 1,
        HypothesisNotification = 2,
        EvidenceDiscovered = 3,
        ApprovalRequest = 4,
        Escalation = 5,
        MissionStatusUpdate = 6
    }

    public class AgentMessage
    {
        public Guid MessageId { get; set; } = Guid.NewGuid();
        public Guid SenderAgentId { get; set; }
        public AgentRole SenderRole { get; set; }
        public Guid RecipientAgentId { get; set; }
        public AgentRole RecipientRole { get; set; }
        public Guid MissionId { get; set; }
        public AgentMessageType Type { get; set; } = AgentMessageType.InformationShare;
        public string Subject { get; set; } = string.Empty;
        public string BodyPayloadJson { get; set; } = "{}";
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public bool IsProcessed { get; set; } = false;
        public DateTime? ProcessedAt { get; set; }
    }

    public class AgentMailbox
    {
        public Guid AgentId { get; set; }
        private readonly List<AgentMessage> _inbox = new();
        private readonly List<AgentMessage> _outbox = new();
        private readonly object _lock = new();

        public AgentMailbox(Guid agentId)
        {
            AgentId = agentId;
        }

        public void EnqueueIncoming(AgentMessage message)
        {
            lock (_lock)
            {
                _inbox.Add(message);
            }
        }

        public IReadOnlyList<AgentMessage> ReadPendingMessages()
        {
            lock (_lock)
            {
                return _inbox.Where(m => !m.IsProcessed).ToList();
            }
        }

        public void MarkAsProcessed(Guid messageId)
        {
            lock (_lock)
            {
                var msg = _inbox.FirstOrDefault(m => m.MessageId == messageId);
                if (msg != null)
                {
                    msg.IsProcessed = true;
                    msg.ProcessedAt = DateTime.UtcNow;
                }
            }
        }

        public void RecordSent(AgentMessage message)
        {
            lock (_lock)
            {
                _outbox.Add(message);
            }
        }

        public int UnreadCount
        {
            get
            {
                lock (_lock)
                {
                    return _inbox.Count(m => !m.IsProcessed);
                }
            }
        }
    }

    public interface IAgentMessageBus
    {
        void RegisterMailbox(AgentMailbox mailbox);
        void PostMessage(AgentMessage message);
        IReadOnlyList<AgentMessage> GetMissionMessages(Guid missionId);
    }

    public class InMemoryAgentMessageBus : IAgentMessageBus
    {
        private readonly ConcurrentDictionary<Guid, AgentMailbox> _mailboxes = new();
        private readonly List<AgentMessage> _messageLedger = new();
        private readonly object _lock = new();

        public void RegisterMailbox(AgentMailbox mailbox)
        {
            _mailboxes[mailbox.AgentId] = mailbox;
        }

        public void PostMessage(AgentMessage message)
        {
            lock (_lock)
            {
                _messageLedger.Add(message);
            }

            if (_mailboxes.TryGetValue(message.RecipientAgentId, out var mailbox))
            {
                mailbox.EnqueueIncoming(message);
            }

            if (_mailboxes.TryGetValue(message.SenderAgentId, out var senderMailbox))
            {
                senderMailbox.RecordSent(message);
            }
        }

        public IReadOnlyList<AgentMessage> GetMissionMessages(Guid missionId)
        {
            lock (_lock)
            {
                return _messageLedger.Where(m => m.MissionId == missionId).OrderBy(m => m.SentAt).ToList();
            }
        }
    }
}
