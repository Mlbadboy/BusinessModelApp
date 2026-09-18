using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime.Organizational;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational
{
    public class InMemoryWorkManagerRunStore : IWorkManagerRunStore
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, WorkManagerRun>> _runs = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, GovernanceQueueItem>> _queue = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, MissionGraphProposal>> _proposals = new();

        public Task SaveRunAsync(WorkManagerRun run, CancellationToken ct = default)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));
            var dict = _runs.GetOrAdd(run.TenantId, _ => new ConcurrentDictionary<string, WorkManagerRun>());
            dict[run.ManagerRunId] = run;
            return Task.CompletedTask;
        }

        public Task<WorkManagerRun?> GetRunAsync(string tenantId, string runId, CancellationToken ct = default)
        {
            if (_runs.TryGetValue(tenantId, out var dict) && dict.TryGetValue(runId, out var run))
            {
                return Task.FromResult<WorkManagerRun?>(run);
            }
            return Task.FromResult<WorkManagerRun?>(null);
        }

        public Task<IReadOnlyList<WorkManagerRun>> ListRunsAsync(string tenantId, CancellationToken ct = default)
        {
            if (_runs.TryGetValue(tenantId, out var dict))
            {
                return Task.FromResult<IReadOnlyList<WorkManagerRun>>(dict.Values.OrderByDescending(r => r.StartedUtc).ToList());
            }
            return Task.FromResult<IReadOnlyList<WorkManagerRun>>(Array.Empty<WorkManagerRun>());
        }

        public Task SaveGovernanceQueueItemAsync(GovernanceQueueItem item, CancellationToken ct = default)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            var dict = _queue.GetOrAdd(item.TenantId, _ => new ConcurrentDictionary<string, GovernanceQueueItem>());
            dict[item.QueueItemId] = item;
            return Task.CompletedTask;
        }

        public Task<GovernanceQueueItem?> GetGovernanceQueueItemAsync(string tenantId, string queueItemId, CancellationToken ct = default)
        {
            if (_queue.TryGetValue(tenantId, out var dict) && dict.TryGetValue(queueItemId, out var item))
            {
                return Task.FromResult<GovernanceQueueItem?>(item);
            }
            return Task.FromResult<GovernanceQueueItem?>(null);
        }

        public Task<IReadOnlyList<GovernanceQueueItem>> ListGovernanceQueueAsync(string tenantId, GovernanceQueueStatus? status = null, CancellationToken ct = default)
        {
            if (_queue.TryGetValue(tenantId, out var dict))
            {
                var query = dict.Values.AsEnumerable();
                if (status.HasValue)
                {
                    query = query.Where(q => q.Status == status.Value);
                }
                return Task.FromResult<IReadOnlyList<GovernanceQueueItem>>(query.OrderByDescending(q => q.PriorityScore).ToList());
            }
            return Task.FromResult<IReadOnlyList<GovernanceQueueItem>>(Array.Empty<GovernanceQueueItem>());
        }

        public Task SaveMissionProposalLinkAsync(string tenantId, string workId, MissionGraphProposal proposal, CancellationToken ct = default)
        {
            if (proposal == null) throw new ArgumentNullException(nameof(proposal));
            var dict = _proposals.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, MissionGraphProposal>());
            dict[workId] = proposal;
            return Task.CompletedTask;
        }

        public Task<MissionGraphProposal?> GetMissionProposalLinkAsync(string tenantId, string workId, CancellationToken ct = default)
        {
            if (_proposals.TryGetValue(tenantId, out var dict) && dict.TryGetValue(workId, out var prop))
            {
                return Task.FromResult<MissionGraphProposal?>(prop);
            }
            return Task.FromResult<MissionGraphProposal?>(null);
        }
    }
}
