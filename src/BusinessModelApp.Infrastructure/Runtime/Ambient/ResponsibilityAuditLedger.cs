using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Responsibilities;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Interfaces.Ambient;

namespace BusinessModelApp.Infrastructure.Runtime.Ambient
{
    public class ResponsibilityAuditLedger : IResponsibilityAuditLedger
    {
        private readonly ConcurrentBag<(DateTime TimestampUtc, ResponsibilityRecord Record, AmbientBusinessEvent Event)> _detections = new();
        private readonly ConcurrentBag<(DateTime TimestampUtc, ResponsibilityId Id, string Decision, string Reason)> _debounceLog = new();
        private readonly ConcurrentBag<ResponsibilityMissionProposal> _proposals = new();

        public Task RecordDetectionAsync(ResponsibilityRecord record, AmbientBusinessEvent businessEvent, CancellationToken cancellationToken = default)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            _detections.Add((DateTime.UtcNow, record, businessEvent));
            return Task.CompletedTask;
        }

        public Task RecordDebounceDecisionAsync(ResponsibilityId responsibilityId, string decision, string reason, CancellationToken cancellationToken = default)
        {
            _debounceLog.Add((DateTime.UtcNow, responsibilityId, decision, reason));
            return Task.CompletedTask;
        }

        public Task RecordProposalAsync(ResponsibilityMissionProposal proposal, CancellationToken cancellationToken = default)
        {
            if (proposal == null) throw new ArgumentNullException(nameof(proposal));
            _proposals.Add(proposal);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ResponsibilityMissionProposal>> GetProposalsAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        {
            var results = _proposals.Where(p => p.WorkspaceId == workspaceId).ToList();
            return Task.FromResult<IReadOnlyList<ResponsibilityMissionProposal>>(results);
        }
    }
}
