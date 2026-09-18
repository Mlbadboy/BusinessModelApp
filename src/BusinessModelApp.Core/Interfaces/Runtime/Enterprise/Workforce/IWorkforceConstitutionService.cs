using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce;

namespace BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Workforce
{
    public interface IWorkforceConstitutionService
    {
        // Business Objectives
        Task<BusinessObjective> CreateObjectiveAsync(string tenantId, BusinessObjective objective);
        Task<RevenueObjective> CreateRevenueObjectiveAsync(string tenantId, RevenueObjective revenueObjective);
        Task<BusinessObjective?> GetObjectiveAsync(string tenantId, string objectiveId);
        Task<IReadOnlyList<BusinessObjective>> ListObjectivesAsync(string tenantId);
        Task<bool> ApproveObjectiveAsync(string tenantId, string objectiveId, string approvedBy);

        // Organization Hierarchy
        Task<WorkforceOrganization> GetOrganizationAsync(string tenantId);
        Task<WorkforceDepartment> AddDepartmentAsync(string tenantId, WorkforceDepartment department);
        Task<WorkforceTeam> AddTeamAsync(string tenantId, WorkforceTeam team);
        Task<WorkforceRole> DefineRoleAsync(string tenantId, WorkforceRole role);

        // Agent Employment Contracts
        Task<AgentEmploymentContract> HireAgentAsync(string tenantId, AgentEmploymentContract contract);
        Task<AgentEmploymentContract?> GetContractAsync(string tenantId, string agentId);
        Task<IReadOnlyList<AgentEmploymentContract>> ListContractsAsync(string tenantId);
        Task<bool> PromoteAgentAsync(string tenantId, string agentId, AgentLifecycleStatus targetStatus, string promotedBy);
        Task<bool> QuarantineAgentAsync(string tenantId, string agentId, string reason);
        Task<bool> ValidateAgentActionAsync(string tenantId, string agentId, string actionName, int riskLevel);
        Task<bool> DeductAgentBudgetAsync(string tenantId, string agentId, decimal cost);
    }
}
