using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Workforce;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/governed-fabric")]
    public class GovernedToolAndSkillController : ControllerBase
    {
        private readonly IGovernedToolFabric _toolFabric;
        private readonly IGovernedSkillFabric _skillFabric;

        public GovernedToolAndSkillController(IGovernedToolFabric toolFabric, IGovernedSkillFabric skillFabric)
        {
            _toolFabric = toolFabric ?? throw new ArgumentNullException(nameof(toolFabric));
            _skillFabric = skillFabric ?? throw new ArgumentNullException(nameof(skillFabric));
        }

        private string GetTenantId()
        {
            if (Request.Headers.TryGetValue("X-Tenant-ID", out var tenantId) && !string.IsNullOrWhiteSpace(tenantId))
            {
                return tenantId.ToString();
            }
            return "tenant-default";
        }

        [HttpPost("tools")]
        public async Task<IActionResult> RegisterTool([FromBody] GovernedTool tool)
        {
            var created = await _toolFabric.RegisterToolAsync(GetTenantId(), tool);
            return Ok(created);
        }

        [HttpGet("tools")]
        public async Task<IActionResult> ListTools()
        {
            var list = await _toolFabric.ListToolsAsync(GetTenantId());
            return Ok(list);
        }

        [HttpPost("tools/resolve")]
        public async Task<IActionResult> ResolveTool([FromBody] ToolExecutionResolutionRequest request)
        {
            request.TenantId = GetTenantId();
            var result = await _toolFabric.ResolveToolExecutionAsync(request);
            return Ok(result);
        }

        [HttpPost("skills")]
        public async Task<IActionResult> RegisterSkill([FromBody] GovernedSkill skill)
        {
            var created = await _skillFabric.RegisterSkillAsync(GetTenantId(), skill);
            return Ok(created);
        }

        [HttpGet("skills")]
        public async Task<IActionResult> ListSkills()
        {
            var list = await _skillFabric.ListSkillsAsync(GetTenantId());
            return Ok(list);
        }

        [HttpPost("skills/{skillId}/promote")]
        public async Task<IActionResult> PromoteSkill(string skillId, [FromBody] PromoteSkillRequest request)
        {
            try
            {
                var success = await _skillFabric.PromoteSkillStateAsync(GetTenantId(), skillId, request.TargetState, request.PromotedBy);
                if (!success) return NotFound();
                return Ok(new { success = true });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    public class PromoteSkillRequest
    {
        public SkillLifecycleState TargetState { get; set; }
        public string PromotedBy { get; set; } = string.Empty;
    }
}
