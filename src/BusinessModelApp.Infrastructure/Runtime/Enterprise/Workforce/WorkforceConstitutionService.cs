using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Workforce;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Workforce
{
    public class WorkforceConstitutionService : IWorkforceConstitutionService
    {
        private readonly IWorkforceConstitutionStore _store;

        public WorkforceConstitutionService(IWorkforceConstitutionStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<BusinessObjective> CreateObjectiveAsync(string tenantId, BusinessObjective objective)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId is required", nameof(tenantId));
            if (objective == null) throw new ArgumentNullException(nameof(objective));

            objective.TenantId = tenantId;
            if (string.IsNullOrWhiteSpace(objective.ObjectiveId))
            {
                objective.ObjectiveId = Guid.NewGuid().ToString("N");
            }
            objective.Status = ObjectiveStatus.DRAFT;
            objective.CreatedAt = DateTime.UtcNow;

            await _store.SaveObjectiveAsync(tenantId, objective);
            return objective;
        }

        public async Task<RevenueObjective> CreateRevenueObjectiveAsync(string tenantId, RevenueObjective revenueObjective)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId is required", nameof(tenantId));
            if (revenueObjective == null) throw new ArgumentNullException(nameof(revenueObjective));

            revenueObjective.TenantId = tenantId;
            if (string.IsNullOrWhiteSpace(revenueObjective.ObjectiveId))
            {
                revenueObjective.ObjectiveId = Guid.NewGuid().ToString("N");
            }
            revenueObjective.Status = ObjectiveStatus.DRAFT;
            revenueObjective.CreatedAt = DateTime.UtcNow;

            // Ensure revenue metrics exist
            if (!revenueObjective.Metrics.Any(m => m.MetricName == "TargetRevenue"))
            {
                revenueObjective.Metrics.Add(new ObjectiveMetric
                {
                    MetricName = "TargetRevenue",
                    TargetValue = revenueObjective.TargetRevenue,
                    CurrentValue = 0m,
                    Unit = "INR"
                });
            }

            await _store.SaveObjectiveAsync(tenantId, revenueObjective);
            return revenueObjective;
        }

        public Task<BusinessObjective?> GetObjectiveAsync(string tenantId, string objectiveId)
        {
            return _store.GetObjectiveAsync(tenantId, objectiveId);
        }

        public Task<IReadOnlyList<BusinessObjective>> ListObjectivesAsync(string tenantId)
        {
            return _store.ListObjectivesAsync(tenantId);
        }

        public async Task<bool> ApproveObjectiveAsync(string tenantId, string objectiveId, string approvedBy)
        {
            var objective = await _store.GetObjectiveAsync(tenantId, objectiveId);
            if (objective == null) return false;

            // Constitutional Law: Human or governance must approve
            objective.Status = ObjectiveStatus.ACTIVE;
            objective.ApprovedAt = DateTime.UtcNow;
            objective.ApprovedBy = approvedBy;

            await _store.SaveObjectiveAsync(tenantId, objective);
            return true;
        }

        public Task<WorkforceOrganization> GetOrganizationAsync(string tenantId)
        {
            return _store.GetOrCreateOrganizationAsync(tenantId);
        }

        public async Task<WorkforceDepartment> AddDepartmentAsync(string tenantId, WorkforceDepartment department)
        {
            var org = await _store.GetOrCreateOrganizationAsync(tenantId);
            department.TenantId = tenantId;
            if (string.IsNullOrWhiteSpace(department.DepartmentId))
            {
                department.DepartmentId = Guid.NewGuid().ToString("N");
            }
            org.Departments.Add(department);
            await _store.SaveOrganizationAsync(tenantId, org);
            return department;
        }

        public async Task<WorkforceTeam> AddTeamAsync(string tenantId, WorkforceTeam team)
        {
            var org = await _store.GetOrCreateOrganizationAsync(tenantId);
            var dept = org.Departments.FirstOrDefault(d => d.DepartmentId == team.DepartmentId);
            if (dept == null)
            {
                throw new InvalidOperationException($"Department {team.DepartmentId} does not exist in tenant {tenantId}");
            }
            if (string.IsNullOrWhiteSpace(team.TeamId))
            {
                team.TeamId = Guid.NewGuid().ToString("N");
            }
            dept.Teams.Add(team);
            await _store.SaveOrganizationAsync(tenantId, org);
            return team;
        }

        public async Task<WorkforceRole> DefineRoleAsync(string tenantId, WorkforceRole role)
        {
            var org = await _store.GetOrCreateOrganizationAsync(tenantId);
            if (string.IsNullOrWhiteSpace(role.RoleId))
            {
                role.RoleId = Guid.NewGuid().ToString("N");
            }
            org.DefinedRoles.Add(role);
            await _store.SaveOrganizationAsync(tenantId, org);
            return role;
        }

        public async Task<AgentEmploymentContract> HireAgentAsync(string tenantId, AgentEmploymentContract contract)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId is required", nameof(tenantId));
            if (contract == null) throw new ArgumentNullException(nameof(contract));

            contract.TenantId = tenantId;
            if (string.IsNullOrWhiteSpace(contract.AgentId))
            {
                contract.AgentId = Guid.NewGuid().ToString("N");
            }

            // Constitutional Law I39-K: Agent hiring cannot automatically activate an agent.
            // Starts in DISCOVERED or EVALUATING / SANDBOXED state.
            contract.LifecycleStatus = AgentLifecycleStatus.EVALUATING;
            contract.HiredAt = DateTime.UtcNow;

            await _store.SaveContractAsync(tenantId, contract);
            return contract;
        }

        public Task<AgentEmploymentContract?> GetContractAsync(string tenantId, string agentId)
        {
            return _store.GetContractAsync(tenantId, agentId);
        }

        public Task<IReadOnlyList<AgentEmploymentContract>> ListContractsAsync(string tenantId)
        {
            return _store.ListContractsAsync(tenantId);
        }

        public async Task<bool> PromoteAgentAsync(string tenantId, string agentId, AgentLifecycleStatus targetStatus, string promotedBy)
        {
            var contract = await _store.GetContractAsync(tenantId, agentId);
            if (contract == null) return false;

            // Constitutional Law I39-L: Promotion requires governed evaluation. Agent cannot self-promote.
            if (string.Equals(agentId, promotedBy, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Constitutional Violation (I39-L): Agent cannot promote itself.");
            }

            // Cannot jump directly from EVALUATING to ACTIVE without CERTIFIED
            if (targetStatus == AgentLifecycleStatus.ACTIVE && contract.LifecycleStatus == AgentLifecycleStatus.EVALUATING)
            {
                throw new InvalidOperationException("Constitutional Violation (I39-K/L): Agent must pass CERTIFIED stage before becoming ACTIVE.");
            }

            if (targetStatus == AgentLifecycleStatus.ACTIVE || targetStatus == AgentLifecycleStatus.CERTIFIED)
            {
                contract.CertifiedAt = DateTime.UtcNow;
                contract.CertifiedBy = promotedBy;
            }

            contract.LifecycleStatus = targetStatus;
            contract.LastAuditedAt = DateTime.UtcNow;

            await _store.SaveContractAsync(tenantId, contract);
            return true;
        }

        public async Task<bool> QuarantineAgentAsync(string tenantId, string agentId, string reason)
        {
            var contract = await _store.GetContractAsync(tenantId, agentId);
            if (contract == null) return false;

            // Constitutional Law I39-Z: Fail-closed workforce sandboxing. Quarantined agents lose all execution rights.
            contract.LifecycleStatus = AgentLifecycleStatus.QUARANTINED;
            contract.LastAuditedAt = DateTime.UtcNow;

            await _store.SaveContractAsync(tenantId, contract);
            return true;
        }

        public async Task<bool> ValidateAgentActionAsync(string tenantId, string agentId, string actionName, int riskLevel)
        {
            var contract = await _store.GetContractAsync(tenantId, agentId);
            if (contract == null) return false;

            // Check if agent is active
            if (!contract.IsActive)
            {
                return false; // Quarantined, evaluating, or restricted agents cannot act
            }

            // Constitutional Law I39-B: Forbidden actions cannot be executed
            if (contract.ForbiddenActions.Any(fa => string.Equals(fa, actionName, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            // Risk ceiling check (I39-B/D)
            if (riskLevel > contract.RiskCeiling)
            {
                return false; // Exceeds contract risk ceiling
            }

            // Daily budget check (I39-E)
            if (contract.Quota.DailySpent >= contract.Quota.DailyBudgetCap)
            {
                return false; // Budget exhausted
            }

            return true;
        }

        public async Task<bool> DeductAgentBudgetAsync(string tenantId, string agentId, decimal cost)
        {
            var contract = await _store.GetContractAsync(tenantId, agentId);
            if (contract == null) return false;

            if (cost < 0)
            {
                throw new ArgumentException("Cost deduction must be non-negative", nameof(cost));
            }

            // Constitutional Law I39-E / I39-R: Agents cannot exceed budget
            if (contract.Quota.DailySpent + cost > contract.Quota.DailyBudgetCap)
            {
                return false; // Over budget
            }

            contract.Quota.DailySpent += cost;
            await _store.SaveContractAsync(tenantId, contract);
            return true;
        }
    }
}
