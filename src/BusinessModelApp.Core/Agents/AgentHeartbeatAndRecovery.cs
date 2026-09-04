using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Missions;
using Microsoft.Extensions.Logging;

namespace BusinessModelApp.Core.Agents
{
    public class HeartbeatPulse
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public int ActiveAgentCount { get; set; }
        public int UnreadMessageCount { get; set; }
        public int PendingTaskCount { get; set; }
    }

    public class AgentHeartbeatService
    {
        private readonly ILogger<AgentHeartbeatService> _logger;

        public AgentHeartbeatService(ILogger<AgentHeartbeatService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public HeartbeatPulse Pulse(BusinessHive hive)
        {
            var agents = hive.GetActiveAgents();
            int unread = agents.Sum(a => a.Mailbox.UnreadCount);
            int pending = hive.Blackboards.Values.Sum(b => b.Tasks.Count(t => t.Status == "Pending"));

            _logger.LogInformation("[AgentHeartbeat] Pulse: {AgentCount} agents active, {UnreadCount} unread messages, {PendingTasks} pending tasks.",
                agents.Count, unread, pending);

            // Wake any agents with unread messages
            foreach (var agent in agents.Where(a => a.Mailbox.UnreadCount > 0))
            {
                agent.Identity.Status = "Active";
            }

            return new HeartbeatPulse
            {
                Timestamp = DateTime.UtcNow,
                ActiveAgentCount = agents.Count,
                UnreadMessageCount = unread,
                PendingTaskCount = pending
            };
        }
    }

    public class AgentRecoveryService
    {
        private readonly ILogger<AgentRecoveryService> _logger;

        public AgentRecoveryService(ILogger<AgentRecoveryService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void RestoreAgentFromCheckpoint(
            AgentContext context,
            DurableMissionCheckpoint checkpoint)
        {
            _logger.LogInformation("[AgentRecovery] Restoring Agent '{Role}' from Checkpoint '{Step}' (Index: {Index})",
                context.Identity.Role, checkpoint.StepName, checkpoint.StepIndex);

            context.Identity.Status = "Restored";
            context.Memory.AddObservation(
                context.Identity.Role,
                $"Restored from checkpoint '{checkpoint.StepName}'",
                checkpoint.ExecutionOutputJson);
        }
    }
}
