using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Domain.Runtime.Fleet;
using BusinessModelApp.Core.Interfaces.Runtime.Fleet;

namespace BusinessModelApp.Infrastructure.Runtime.Fleet
{
    public class AgentDispatcher : IAgentDispatcher
    {
        public Task<AgentOutcomeProposal> ExecuteAttemptAsync(
            FencingEnvelope envelope,
            MissionNodeRecord node,
            AgentInstanceRecord agent,
            CancellationToken ct = default)
        {
            if (envelope == null) throw new ArgumentNullException(nameof(envelope));
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (agent == null) throw new ArgumentNullException(nameof(agent));

            // Build mock cognitive payload based on criteria
            var outputObj = new
            {
                node_id = node.NodeId.Value,
                agent_id = agent.InstanceId.ToString(),
                attempt_id = envelope.AttemptId.ToString(),
                findings = $"Execution findings for {node.Title}",
                confidence = 1.0,
                completed_at = DateTimeOffset.UtcNow
            };

            var proposal = new AgentOutcomeProposal
            {
                ProposalId = Guid.NewGuid(),
                Envelope = envelope,
                ReportedStatus = ExecutionOutcomeStatus.Succeeded,
                OutputPayloadJson = JsonSerializer.Serialize(outputObj),
                TokensConsumed = 500,
                CostUsdConsumed = 0.02m,
                ProposedAt = DateTimeOffset.UtcNow
            };

            return Task.FromResult(proposal);
        }
    }
}
