using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Organizational;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Collaboration;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Collaboration
{
    public class CollaborationMessageBus : ICollaborationMessageBus
    {
        private readonly ITeamCharterStore _store;
        private readonly TeamFormationPolicy _policy;

        public CollaborationMessageBus(
            ITeamCharterStore store,
            TeamFormationPolicy? policy = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _policy = policy ?? new TeamFormationPolicy();
        }

        public async Task<int> GetTurnCountAsync(string tenantId, string charterId, CancellationToken ct = default)
        {
            var messages = await _store.ListMessagesAsync(tenantId, charterId, ct);
            return messages.Count;
        }

        public async Task<(bool Dispatched, CollaborationMessage? Message, string? ErrorReason)> SendMessageAsync(
            string tenantId,
            string charterId,
            string senderAgentId,
            string recipientAgentId,
            CollaborationMessageType messageType,
            string content,
            IReadOnlyList<string>? evidenceRefs = null,
            EpistemicStatus epistemicStatus = EpistemicStatus.Hypothesis,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) return (false, null, "TenantId is required.");
            if (string.IsNullOrWhiteSpace(charterId)) return (false, null, "CharterId is required.");
            if (string.IsNullOrWhiteSpace(senderAgentId)) return (false, null, "SenderAgentId is required.");
            if (string.IsNullOrWhiteSpace(content)) return (false, null, "Message content is required.");

            // 1. Verify charter validity
            var charter = await _store.GetCharterAsync(tenantId, charterId, ct);
            if (charter == null)
            {
                return (false, null, $"Charter '{charterId}' not found for tenant.");
            }

            if (charter.Status == TeamLifecycleStatus.Dissolved || charter.Status == TeamLifecycleStatus.TimedOut)
            {
                return (false, null, $"Team charter '{charterId}' is {charter.Status}; communication is prohibited.");
            }

            if (DateTime.UtcNow > charter.ExpiresUtc)
            {
                return (false, null, $"Team charter '{charterId}' has expired; communication is prohibited.");
            }

            // 2. Invariant I29-C: Verify sender membership (no unvetted peer messaging)
            bool isMember = charter.Members.Any(m => m.AgentInstanceId.Equals(senderAgentId, StringComparison.OrdinalIgnoreCase));
            if (!isMember && !senderAgentId.Equals("System", StringComparison.OrdinalIgnoreCase))
            {
                return (false, null, $"Sender '{senderAgentId}' is not an authorized member of team charter '{charterId}' per Invariant I29-C.");
            }

            // 3. Invariant I29-G: Bounded negotiation turns (<= 10)
            int currentTurns = await GetTurnCountAsync(tenantId, charterId, ct);
            if (currentTurns >= _policy.MaxNegotiationRounds)
            {
                return (false, null, $"Negotiation turn ceiling ({_policy.MaxNegotiationRounds}) exceeded for team charter '{charterId}' per Invariant I29-G.");
            }

            // 4. Invariant I29-D: Work handoff requires evidence references
            var evidenceList = evidenceRefs != null ? evidenceRefs.ToList() : new List<string>();
            if (messageType == CollaborationMessageType.WorkHandoff && _policy.RequireEvidenceForHandoff && evidenceList.Count == 0)
            {
                return (false, null, "Work handoff messages require explicit evidence references per Invariant I29-D.");
            }

            // 5. Invariant I29-I: Consensus != Truth (cannot elevate to VerifiedTruth without evidence references)
            if ((epistemicStatus == EpistemicStatus.VerifiedTruth || epistemicStatus == EpistemicStatus.ObservedFact) && evidenceList.Count == 0)
            {
                epistemicStatus = EpistemicStatus.Hypothesis; // Demote ungrounded assertion
            }

            var msg = new CollaborationMessage
            {
                MessageId = $"MSG-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                TenantId = tenantId,
                CharterId = charterId,
                SenderAgentId = senderAgentId,
                RecipientAgentId = recipientAgentId,
                MessageType = messageType,
                Content = content,
                EvidenceReferences = evidenceList,
                EpistemicClassification = epistemicStatus,
                TurnNumber = currentTurns + 1,
                CorrelationId = charter.WorkId,
                TimestampUtc = DateTime.UtcNow
            };

            msg.ComputeMessageHash();
            await _store.SaveMessageAsync(msg, ct);

            return (true, msg, null);
        }
    }
}
