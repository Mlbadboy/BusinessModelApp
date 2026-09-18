using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Organizational;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Collaboration;

namespace BusinessModelApp.Core.Interfaces.Runtime.Organizational
{
    public interface ITeamCharterStore
    {
        Task SaveCharterAsync(TeamCharter charter, CancellationToken ct = default);
        Task<TeamCharter?> GetCharterAsync(string tenantId, string charterId, CancellationToken ct = default);
        Task<IReadOnlyList<TeamCharter>> ListChartersForWorkAsync(string tenantId, string workId, CancellationToken ct = default);
        Task<IReadOnlyList<TeamCharter>> ListActiveChartersAsync(string tenantId, CancellationToken ct = default);
        Task SaveMessageAsync(CollaborationMessage message, CancellationToken ct = default);
        Task<IReadOnlyList<CollaborationMessage>> ListMessagesAsync(string tenantId, string charterId, CancellationToken ct = default);
        Task SaveDisputeAsync(DisputeRecord dispute, CancellationToken ct = default);
        Task<IReadOnlyList<DisputeRecord>> ListDisputesAsync(string tenantId, string charterId, CancellationToken ct = default);
        Task SavePerformanceRecordAsync(TeamPerformanceRecord record, CancellationToken ct = default);
        Task<TeamPerformanceRecord?> GetPerformanceRecordAsync(string tenantId, string charterId, CancellationToken ct = default);
    }

    public interface ITeamFormationEngine
    {
        Task<(bool Success, TeamCharter? Charter, string? ErrorReason)> FormTeamAsync(
            string tenantId,
            string workId,
            string objective,
            IReadOnlyList<TeamMemberRole> candidateRoles,
            decimal budget = 0m,
            int governanceTier = 1,
            TeamFormationPolicy? policyOverride = null,
            CancellationToken ct = default);

        Task<(bool Valid, string? Reason)> ValidateTeamCompositionAsync(
            string tenantId,
            IReadOnlyList<TeamMemberRole> roles,
            TeamFormationPolicy policy,
            CancellationToken ct = default);
    }

    public interface ICollaborationMessageBus
    {
        Task<(bool Dispatched, CollaborationMessage? Message, string? ErrorReason)> SendMessageAsync(
            string tenantId,
            string charterId,
            string senderAgentId,
            string recipientAgentId,
            CollaborationMessageType messageType,
            string content,
            IReadOnlyList<string>? evidenceRefs = null,
            EpistemicStatus epistemicStatus = EpistemicStatus.Hypothesis,
            CancellationToken ct = default);

        Task<int> GetTurnCountAsync(string tenantId, string charterId, CancellationToken ct = default);
    }

    public interface IDisputeArbitrator
    {
        Task<DisputeRecord> ArbitrateDisputeAsync(
            string tenantId,
            string charterId,
            DisputeType disputeType,
            string topic,
            string initiatorAgentId,
            IReadOnlyList<string> contendingAgentIds,
            Dictionary<string, string> claims,
            Dictionary<string, double>? evidenceScores = null,
            CancellationToken ct = default);
    }

    public interface ITeamLifecycleManager
    {
        Task<(bool Dissolved, string? Reason)> DissolveTeamAsync(
            string tenantId,
            string charterId,
            string reason,
            CancellationToken ct = default);

        Task<int> ProcessExpiredChartersAsync(
            string tenantId,
            CancellationToken ct = default);
    }

    public interface IMultiAgentCollaborationService
    {
        Task<(bool Success, TeamCharter? Charter, string? ErrorReason)> FormTeamAsync(
            string tenantId,
            string workId,
            string objective,
            IReadOnlyList<TeamMemberRole> candidateRoles,
            decimal budget = 0m,
            int governanceTier = 1,
            TeamFormationPolicy? policyOverride = null,
            CancellationToken ct = default);

        Task<TeamCharter?> GetCharterAsync(string tenantId, string charterId, CancellationToken ct = default);
        Task<IReadOnlyList<TeamCharter>> ListActiveChartersAsync(string tenantId, CancellationToken ct = default);

        Task<(bool Dispatched, CollaborationMessage? Message, string? ErrorReason)> SendMessageAsync(
            string tenantId,
            string charterId,
            string senderAgentId,
            string recipientAgentId,
            CollaborationMessageType messageType,
            string content,
            IReadOnlyList<string>? evidenceRefs = null,
            EpistemicStatus epistemicStatus = EpistemicStatus.Hypothesis,
            CancellationToken ct = default);

        Task<IReadOnlyList<CollaborationMessage>> GetMessagesAsync(string tenantId, string charterId, CancellationToken ct = default);

        Task<DisputeRecord> RaiseDisputeAsync(
            string tenantId,
            string charterId,
            DisputeType disputeType,
            string topic,
            string initiatorAgentId,
            IReadOnlyList<string> contendingAgentIds,
            Dictionary<string, string> claims,
            Dictionary<string, double>? evidenceScores = null,
            CancellationToken ct = default);

        Task<IReadOnlyList<DisputeRecord>> GetDisputesAsync(string tenantId, string charterId, CancellationToken ct = default);

        Task<(bool Dissolved, string? Reason)> DissolveTeamAsync(string tenantId, string charterId, string reason, CancellationToken ct = default);
    }
}
