using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Organizational;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Collaboration;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Collaboration
{
    public class MultiAgentCollaborationService : IMultiAgentCollaborationService
    {
        private readonly ITeamCharterStore _store;
        private readonly ITeamFormationEngine _formationEngine;
        private readonly ICollaborationMessageBus _messageBus;
        private readonly IDisputeArbitrator _disputeArbitrator;
        private readonly ITeamLifecycleManager _lifecycleManager;

        public MultiAgentCollaborationService(
            ITeamCharterStore store,
            ITeamFormationEngine formationEngine,
            ICollaborationMessageBus messageBus,
            IDisputeArbitrator disputeArbitrator,
            ITeamLifecycleManager lifecycleManager)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _formationEngine = formationEngine ?? throw new ArgumentNullException(nameof(formationEngine));
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            _disputeArbitrator = disputeArbitrator ?? throw new ArgumentNullException(nameof(disputeArbitrator));
            _lifecycleManager = lifecycleManager ?? throw new ArgumentNullException(nameof(lifecycleManager));
        }

        public Task<(bool Success, TeamCharter? Charter, string? ErrorReason)> FormTeamAsync(
            string tenantId,
            string workId,
            string objective,
            IReadOnlyList<TeamMemberRole> candidateRoles,
            decimal budget = 0m,
            int governanceTier = 1,
            TeamFormationPolicy? policyOverride = null,
            CancellationToken ct = default)
        {
            return _formationEngine.FormTeamAsync(tenantId, workId, objective, candidateRoles, budget, governanceTier, policyOverride, ct);
        }

        public Task<TeamCharter?> GetCharterAsync(string tenantId, string charterId, CancellationToken ct = default)
        {
            return _store.GetCharterAsync(tenantId, charterId, ct);
        }

        public Task<IReadOnlyList<TeamCharter>> ListActiveChartersAsync(string tenantId, CancellationToken ct = default)
        {
            return _store.ListActiveChartersAsync(tenantId, ct);
        }

        public Task<(bool Dispatched, CollaborationMessage? Message, string? ErrorReason)> SendMessageAsync(
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
            return _messageBus.SendMessageAsync(tenantId, charterId, senderAgentId, recipientAgentId, messageType, content, evidenceRefs, epistemicStatus, ct);
        }

        public Task<IReadOnlyList<CollaborationMessage>> GetMessagesAsync(string tenantId, string charterId, CancellationToken ct = default)
        {
            return _store.ListMessagesAsync(tenantId, charterId, ct);
        }

        public Task<DisputeRecord> RaiseDisputeAsync(
            string tenantId,
            string charterId,
            DisputeType disputeType,
            string topic,
            string initiatorAgentId,
            IReadOnlyList<string> contendingAgentIds,
            Dictionary<string, string> claims,
            Dictionary<string, double>? evidenceScores = null,
            CancellationToken ct = default)
        {
            return _disputeArbitrator.ArbitrateDisputeAsync(tenantId, charterId, disputeType, topic, initiatorAgentId, contendingAgentIds, claims, evidenceScores, ct);
        }

        public Task<IReadOnlyList<DisputeRecord>> GetDisputesAsync(string tenantId, string charterId, CancellationToken ct = default)
        {
            return _store.ListDisputesAsync(tenantId, charterId, ct);
        }

        public Task<(bool Dissolved, string? Reason)> DissolveTeamAsync(string tenantId, string charterId, string reason, CancellationToken ct = default)
        {
            return _lifecycleManager.DissolveTeamAsync(tenantId, charterId, reason, ct);
        }
    }
}
