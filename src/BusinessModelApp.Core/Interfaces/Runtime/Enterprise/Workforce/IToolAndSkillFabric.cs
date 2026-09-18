using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce;

namespace BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Workforce
{
    public interface IGovernedToolFabric
    {
        Task<GovernedTool> RegisterToolAsync(string tenantId, GovernedTool tool);
        Task<GovernedTool?> GetToolAsync(string tenantId, string toolId);
        Task<IReadOnlyList<GovernedTool>> ListToolsAsync(string tenantId);
        Task<ToolExecutionResolutionResult> ResolveToolExecutionAsync(ToolExecutionResolutionRequest request);
    }

    public interface IGovernedSkillFabric
    {
        Task<GovernedSkill> RegisterSkillAsync(string tenantId, GovernedSkill skill);
        Task<GovernedSkill?> GetSkillAsync(string tenantId, string skillId);
        Task<IReadOnlyList<GovernedSkill>> ListSkillsAsync(string tenantId);
        Task<bool> PromoteSkillStateAsync(string tenantId, string skillId, SkillLifecycleState targetState, string promotedBy);
    }
}
