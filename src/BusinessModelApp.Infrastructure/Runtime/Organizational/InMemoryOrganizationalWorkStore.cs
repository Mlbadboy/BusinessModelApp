using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Organizational;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational
{
    public class InMemoryOrganizationalWorkStore : IOrganizationalWorkRepository
    {
        // TenantId -> Key -> Entity
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, OrganizationalResponsibility>> _responsibilities = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, WorkProposal>> _proposals = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, WorkItem>> _workItems = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, WorkPlan>> _workPlans = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, List<WorkDependency>>> _dependencies = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, WorkMissionLineageRecord>> _lineage = new();

        public Task<OrganizationalResponsibility?> GetResponsibilityAsync(string tenantId, string responsibilityId, CancellationToken ct = default)
        {
            if (_responsibilities.TryGetValue(tenantId, out var dict) && dict.TryGetValue(responsibilityId, out var resp))
            {
                return Task.FromResult<OrganizationalResponsibility?>(resp);
            }
            return Task.FromResult<OrganizationalResponsibility?>(null);
        }

        public Task<IReadOnlyList<OrganizationalResponsibility>> ListResponsibilitiesAsync(string tenantId, CancellationToken ct = default)
        {
            if (_responsibilities.TryGetValue(tenantId, out var dict))
            {
                return Task.FromResult<IReadOnlyList<OrganizationalResponsibility>>(dict.Values.ToList());
            }
            return Task.FromResult<IReadOnlyList<OrganizationalResponsibility>>(Array.Empty<OrganizationalResponsibility>());
        }

        public Task SaveResponsibilityAsync(OrganizationalResponsibility responsibility, CancellationToken ct = default)
        {
            if (responsibility == null) throw new ArgumentNullException(nameof(responsibility));
            var dict = _responsibilities.GetOrAdd(responsibility.TenantId, _ => new ConcurrentDictionary<string, OrganizationalResponsibility>());
            dict[responsibility.ResponsibilityId] = responsibility;
            return Task.CompletedTask;
        }

        public Task<WorkProposal?> GetProposalAsync(string tenantId, string proposalId, CancellationToken ct = default)
        {
            if (_proposals.TryGetValue(tenantId, out var dict) && dict.TryGetValue(proposalId, out var prop))
            {
                return Task.FromResult<WorkProposal?>(prop);
            }
            return Task.FromResult<WorkProposal?>(null);
        }

        public Task<IReadOnlyList<WorkProposal>> ListProposalsAsync(string tenantId, CancellationToken ct = default)
        {
            if (_proposals.TryGetValue(tenantId, out var dict))
            {
                return Task.FromResult<IReadOnlyList<WorkProposal>>(dict.Values.ToList());
            }
            return Task.FromResult<IReadOnlyList<WorkProposal>>(Array.Empty<WorkProposal>());
        }

        public Task SaveProposalAsync(WorkProposal proposal, CancellationToken ct = default)
        {
            if (proposal == null) throw new ArgumentNullException(nameof(proposal));
            if (string.IsNullOrWhiteSpace(proposal.ProvenanceHash))
            {
                proposal.ComputeProvenanceHash();
            }
            var dict = _proposals.GetOrAdd(proposal.TenantId, _ => new ConcurrentDictionary<string, WorkProposal>());
            dict[proposal.ProposalId] = proposal;
            return Task.CompletedTask;
        }

        public Task<WorkItem?> GetWorkItemAsync(string tenantId, string workId, CancellationToken ct = default)
        {
            if (_workItems.TryGetValue(tenantId, out var dict) && dict.TryGetValue(workId, out var item))
            {
                return Task.FromResult<WorkItem?>(item);
            }
            return Task.FromResult<WorkItem?>(null);
        }

        public Task<IReadOnlyList<WorkItem>> ListWorkItemsAsync(string tenantId, WorkState? stateFilter = null, CancellationToken ct = default)
        {
            if (_workItems.TryGetValue(tenantId, out var dict))
            {
                var query = dict.Values.AsEnumerable();
                if (stateFilter.HasValue)
                {
                    query = query.Where(w => w.State == stateFilter.Value);
                }
                return Task.FromResult<IReadOnlyList<WorkItem>>(query.ToList());
            }
            return Task.FromResult<IReadOnlyList<WorkItem>>(Array.Empty<WorkItem>());
        }

        public Task SaveWorkItemAsync(WorkItem item, CancellationToken ct = default)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            item.ComputeProvenanceHash();
            var dict = _workItems.GetOrAdd(item.TenantId, _ => new ConcurrentDictionary<string, WorkItem>());
            dict[item.WorkId] = item;
            return Task.CompletedTask;
        }

        public Task<WorkPlan?> GetWorkPlanAsync(string tenantId, string planId, CancellationToken ct = default)
        {
            if (_workPlans.TryGetValue(tenantId, out var dict) && dict.TryGetValue(planId, out var plan))
            {
                return Task.FromResult<WorkPlan?>(plan);
            }
            return Task.FromResult<WorkPlan?>(null);
        }

        public Task SaveWorkPlanAsync(string tenantId, WorkPlan plan, CancellationToken ct = default)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            var dict = _workPlans.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, WorkPlan>());
            dict[plan.PlanId] = plan;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<WorkDependency>> GetDependenciesForWorkAsync(string tenantId, string workId, CancellationToken ct = default)
        {
            if (_dependencies.TryGetValue(tenantId, out var dict) && dict.TryGetValue(workId, out var list))
            {
                lock (list)
                {
                    return Task.FromResult<IReadOnlyList<WorkDependency>>(list.ToList());
                }
            }
            return Task.FromResult<IReadOnlyList<WorkDependency>>(Array.Empty<WorkDependency>());
        }

        public Task SaveDependencyAsync(string tenantId, WorkDependency dependency, CancellationToken ct = default)
        {
            if (dependency == null) throw new ArgumentNullException(nameof(dependency));
            var dict = _dependencies.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, List<WorkDependency>>());
            var list = dict.GetOrAdd(dependency.DependentWorkId, _ => new List<WorkDependency>());
            lock (list)
            {
                list.RemoveAll(d => d.DependencyId == dependency.DependencyId);
                list.Add(dependency);
            }
            return Task.CompletedTask;
        }

        public Task SaveLineageRecordAsync(string tenantId, WorkMissionLineageRecord record, CancellationToken ct = default)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            record.ComputeChainHash();
            var dict = _lineage.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, WorkMissionLineageRecord>());
            dict[record.WorkId] = record;
            return Task.CompletedTask;
        }

        public Task<WorkMissionLineageRecord?> GetLineageRecordAsync(string tenantId, string workId, CancellationToken ct = default)
        {
            if (_lineage.TryGetValue(tenantId, out var dict) && dict.TryGetValue(workId, out var record))
            {
                return Task.FromResult<WorkMissionLineageRecord?>(record);
            }
            return Task.FromResult<WorkMissionLineageRecord?>(null);
        }
    }
}
