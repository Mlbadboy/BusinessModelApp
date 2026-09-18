using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Workforce
{
    public interface IWorkforceConstitutionStore
    {
        Task SaveObjectiveAsync(string tenantId, BusinessObjective objective);
        Task<BusinessObjective?> GetObjectiveAsync(string tenantId, string objectiveId);
        Task<IReadOnlyList<BusinessObjective>> ListObjectivesAsync(string tenantId);

        Task<WorkforceOrganization> GetOrCreateOrganizationAsync(string tenantId);
        Task SaveOrganizationAsync(string tenantId, WorkforceOrganization org);

        Task SaveContractAsync(string tenantId, AgentEmploymentContract contract);
        Task<AgentEmploymentContract?> GetContractAsync(string tenantId, string agentId);
        Task<IReadOnlyList<AgentEmploymentContract>> ListContractsAsync(string tenantId);
    }

    public class InMemoryWorkforceConstitutionStore : IWorkforceConstitutionStore
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, BusinessObjective>> _objectivesByTenant = new();
        private readonly ConcurrentDictionary<string, WorkforceOrganization> _orgsByTenant = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, AgentEmploymentContract>> _contractsByTenant = new();

        public Task SaveObjectiveAsync(string tenantId, BusinessObjective objective)
        {
            var tenantMap = _objectivesByTenant.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, BusinessObjective>());
            tenantMap[objective.ObjectiveId] = objective;
            return Task.CompletedTask;
        }

        public Task<BusinessObjective?> GetObjectiveAsync(string tenantId, string objectiveId)
        {
            if (_objectivesByTenant.TryGetValue(tenantId, out var tenantMap) &&
                tenantMap.TryGetValue(objectiveId, out var objective))
            {
                return Task.FromResult<BusinessObjective?>(objective);
            }
            return Task.FromResult<BusinessObjective?>(null);
        }

        public Task<IReadOnlyList<BusinessObjective>> ListObjectivesAsync(string tenantId)
        {
            if (_objectivesByTenant.TryGetValue(tenantId, out var tenantMap))
            {
                return Task.FromResult<IReadOnlyList<BusinessObjective>>(tenantMap.Values.ToList());
            }
            return Task.FromResult<IReadOnlyList<BusinessObjective>>(Array.Empty<BusinessObjective>());
        }

        public Task<WorkforceOrganization> GetOrCreateOrganizationAsync(string tenantId)
        {
            var org = _orgsByTenant.GetOrAdd(tenantId, t => new WorkforceOrganization
            {
                TenantId = t,
                Name = $"Charlie Organization ({t})"
            });
            return Task.FromResult(org);
        }

        public Task SaveOrganizationAsync(string tenantId, WorkforceOrganization org)
        {
            _orgsByTenant[tenantId] = org;
            return Task.CompletedTask;
        }

        public Task SaveContractAsync(string tenantId, AgentEmploymentContract contract)
        {
            var tenantMap = _contractsByTenant.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, AgentEmploymentContract>());
            tenantMap[contract.AgentId] = contract;
            return Task.CompletedTask;
        }

        public Task<AgentEmploymentContract?> GetContractAsync(string tenantId, string agentId)
        {
            if (_contractsByTenant.TryGetValue(tenantId, out var tenantMap) &&
                tenantMap.TryGetValue(agentId, out var contract))
            {
                return Task.FromResult<AgentEmploymentContract?>(contract);
            }
            return Task.FromResult<AgentEmploymentContract?>(null);
        }

        public Task<IReadOnlyList<AgentEmploymentContract>> ListContractsAsync(string tenantId)
        {
            if (_contractsByTenant.TryGetValue(tenantId, out var tenantMap))
            {
                return Task.FromResult<IReadOnlyList<AgentEmploymentContract>>(tenantMap.Values.ToList());
            }
            return Task.FromResult<IReadOnlyList<AgentEmploymentContract>>(Array.Empty<AgentEmploymentContract>());
        }
    }
}
