using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Interfaces.Runtime;

namespace BusinessModelApp.Infrastructure.Runtime.BrainFabric
{
    public class InferenceAuditLedger : IInferenceAuditLedger
    {
        private readonly ConcurrentBag<BrainInferenceResult> _auditTrail = new();

        public Task RecordInferenceAuditAsync(BrainInferenceResult result, CancellationToken cancellationToken = default)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            _auditTrail.Add(result);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<BrainInferenceResult>> GetInferenceAuditTrailAsync(Guid workspaceId, MissionRunId? missionRunId, CancellationToken cancellationToken = default)
        {
            var query = _auditTrail.Where(r => r.Provenance.WorkspaceId == workspaceId);
            if (missionRunId.HasValue)
            {
                query = query.Where(r => r.Provenance.MissionRunId.HasValue && r.Provenance.MissionRunId.Value.Value == missionRunId.Value.Value);
            }

            return Task.FromResult<IReadOnlyList<BrainInferenceResult>>(query.ToList());
        }
    }
}
