using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Workforce;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Workforce
{
    public interface IToolAndSkillStore
    {
        Task SaveToolAsync(string tenantId, GovernedTool tool);
        Task<GovernedTool?> GetToolAsync(string tenantId, string toolId);
        Task<IReadOnlyList<GovernedTool>> ListToolsAsync(string tenantId);

        Task SaveSkillAsync(string tenantId, GovernedSkill skill);
        Task<GovernedSkill?> GetSkillAsync(string tenantId, string skillId);
        Task<IReadOnlyList<GovernedSkill>> ListSkillsAsync(string tenantId);
    }

    public class InMemoryToolAndSkillStore : IToolAndSkillStore
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, GovernedTool>> _toolsByTenant = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, GovernedSkill>> _skillsByTenant = new();

        public Task SaveToolAsync(string tenantId, GovernedTool tool)
        {
            var map = _toolsByTenant.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, GovernedTool>());
            map[tool.ToolId] = tool;
            return Task.CompletedTask;
        }

        public Task<GovernedTool?> GetToolAsync(string tenantId, string toolId)
        {
            if (_toolsByTenant.TryGetValue(tenantId, out var map) && map.TryGetValue(toolId, out var tool))
            {
                return Task.FromResult<GovernedTool?>(tool);
            }
            return Task.FromResult<GovernedTool?>(null);
        }

        public Task<IReadOnlyList<GovernedTool>> ListToolsAsync(string tenantId)
        {
            if (_toolsByTenant.TryGetValue(tenantId, out var map))
            {
                return Task.FromResult<IReadOnlyList<GovernedTool>>(map.Values.ToList());
            }
            return Task.FromResult<IReadOnlyList<GovernedTool>>(Array.Empty<GovernedTool>());
        }

        public Task SaveSkillAsync(string tenantId, GovernedSkill skill)
        {
            var map = _skillsByTenant.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, GovernedSkill>());
            map[skill.SkillId] = skill;
            return Task.CompletedTask;
        }

        public Task<GovernedSkill?> GetSkillAsync(string tenantId, string skillId)
        {
            if (_skillsByTenant.TryGetValue(tenantId, out var map) && map.TryGetValue(skillId, out var skill))
            {
                return Task.FromResult<GovernedSkill?>(skill);
            }
            return Task.FromResult<GovernedSkill?>(null);
        }

        public Task<IReadOnlyList<GovernedSkill>> ListSkillsAsync(string tenantId)
        {
            if (_skillsByTenant.TryGetValue(tenantId, out var map))
            {
                return Task.FromResult<IReadOnlyList<GovernedSkill>>(map.Values.ToList());
            }
            return Task.FromResult<IReadOnlyList<GovernedSkill>>(Array.Empty<GovernedSkill>());
        }
    }

    public class ToolAndSkillFabricService : IGovernedToolFabric, IGovernedSkillFabric
    {
        private readonly IToolAndSkillStore _store;
        private readonly IWorkforceConstitutionService _constitutionService;

        public ToolAndSkillFabricService(IToolAndSkillStore store, IWorkforceConstitutionService constitutionService)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _constitutionService = constitutionService ?? throw new ArgumentNullException(nameof(constitutionService));
        }

        // --- Tool Fabric ---

        public async Task<GovernedTool> RegisterToolAsync(string tenantId, GovernedTool tool)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId required", nameof(tenantId));
            if (tool == null) throw new ArgumentNullException(nameof(tool));

            await _store.SaveToolAsync(tenantId, tool);
            return tool;
        }

        public Task<GovernedTool?> GetToolAsync(string tenantId, string toolId)
        {
            return _store.GetToolAsync(tenantId, toolId);
        }

        public Task<IReadOnlyList<GovernedTool>> ListToolsAsync(string tenantId)
        {
            return _store.ListToolsAsync(tenantId);
        }

        public async Task<ToolExecutionResolutionResult> ResolveToolExecutionAsync(ToolExecutionResolutionRequest request)
        {
            var tool = await _store.GetToolAsync(request.TenantId, request.ToolId);
            if (tool == null || !tool.IsActive)
            {
                return new ToolExecutionResolutionResult
                {
                    IsAuthorized = false,
                    RejectionReason = $"Tool '{request.ToolId}' is not registered or inactive."
                };
            }

            // Check agent employment contract via constitution service (Law I39-D: Tool availability != authorization)
            var contract = await _constitutionService.GetContractAsync(request.TenantId, request.AgentId);
            if (contract == null || !contract.IsActive)
            {
                return new ToolExecutionResolutionResult
                {
                    IsAuthorized = false,
                    RejectionReason = $"Agent '{request.AgentId}' is not active or lacks an employment contract."
                };
            }

            // Check if tool is permitted in contract
            if (contract.AllowedToolIds.Count > 0 && !contract.AllowedToolIds.Contains(tool.ToolId))
            {
                return new ToolExecutionResolutionResult
                {
                    IsAuthorized = false,
                    RejectionReason = $"Tool '{tool.ToolId}' is not permitted by agent contract."
                };
            }

            // Check Risk Level
            if (request.RiskLevel > contract.RiskCeiling)
            {
                return new ToolExecutionResolutionResult
                {
                    IsAuthorized = false,
                    RequiresHumanApproval = true,
                    RejectionReason = $"Execution risk level {request.RiskLevel} exceeds agent contract ceiling {contract.RiskCeiling} (Escalated to PRG-1)."
                };
            }

            // Budget deduction check
            var budgetOk = await _constitutionService.DeductAgentBudgetAsync(request.TenantId, request.AgentId, tool.BudgetCostPerCall);
            if (!budgetOk)
            {
                return new ToolExecutionResolutionResult
                {
                    IsAuthorized = false,
                    RejectionReason = $"Agent budget exhausted for tool cost ({tool.BudgetCostPerCall})."
                };
            }

            return new ToolExecutionResolutionResult
            {
                IsAuthorized = true,
                CapabilityId = tool.CapabilityId,
                EstimatedCost = tool.BudgetCostPerCall,
                RequiresHumanApproval = tool.RiskTier >= 3
            };
        }

        // --- Skill Fabric ---

        public async Task<GovernedSkill> RegisterSkillAsync(string tenantId, GovernedSkill skill)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId required", nameof(tenantId));
            if (skill == null) throw new ArgumentNullException(nameof(skill));

            skill.LifecycleState = SkillLifecycleState.DISCOVER;
            skill.CreatedAt = DateTime.UtcNow;

            await _store.SaveSkillAsync(tenantId, skill);
            return skill;
        }

        public Task<GovernedSkill?> GetSkillAsync(string tenantId, string skillId)
        {
            return _store.GetSkillAsync(tenantId, skillId);
        }

        public Task<IReadOnlyList<GovernedSkill>> ListSkillsAsync(string tenantId)
        {
            return _store.ListSkillsAsync(tenantId);
        }

        public async Task<bool> PromoteSkillStateAsync(string tenantId, string skillId, SkillLifecycleState targetState, string promotedBy)
        {
            var skill = await _store.GetSkillAsync(tenantId, skillId);
            if (skill == null) return false;

            // Constitutional Law I39-M: Generated skills cannot self-certify
            if (string.Equals(skill.Owner, promotedBy, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Constitutional Violation (I39-M): Skill owner/generator cannot self-certify the skill.");
            }

            // If promoting to CERTIFIED or ACTIVE, verify it went through TEST and RED_TEAM
            if (targetState == SkillLifecycleState.ACTIVE && skill.LifecycleState == SkillLifecycleState.DISCOVER)
            {
                throw new InvalidOperationException("Constitutional Violation (I39-M): Skill must pass TEST, RED_TEAM, and CERTIFIED stages before becoming ACTIVE.");
            }

            if (targetState == SkillLifecycleState.CERTIFIED)
            {
                skill.CertifiedAt = DateTime.UtcNow;
                skill.CertifiedBy = promotedBy;
            }

            skill.LifecycleState = targetState;
            await _store.SaveSkillAsync(tenantId, skill);
            return true;
        }
    }
}
