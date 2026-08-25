using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Agents;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    public class CreateMissionRequest
    {
        public string Title { get; set; } = "Autonomous Enterprise Pipeline Generation";
        public string Objective { get; set; } = "Generate ₹25L qualified pipeline in BFSI";
        public string TargetIndustry { get; set; } = "Enterprise BFSI";
        public int TargetProspectCount { get; set; } = 25;
        public decimal TargetValueINR { get; set; } = 2500000m;
        public MissionMode Mode { get; set; } = MissionMode.Simulation;
        public AutonomyLevel AutonomyLevel { get; set; } = AutonomyLevel.Level3_ControlledAutonomy;
        public decimal WalletBudgetINR { get; set; } = 5000m;
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AgentMissionsController : ControllerBase
    {
        private readonly IAgentOrchestratorService _orchestrator;
        private readonly IUserContextService _userContext;

        public AgentMissionsController(IAgentOrchestratorService orchestrator, IUserContextService userContext)
        {
            _orchestrator = orchestrator;
            _userContext = userContext;
        }

        [HttpPost]
        public async Task<ActionResult<AgentMission>> CreateAndLaunchMission([FromBody] CreateMissionRequest request, [FromQuery] Guid? workspaceId)
        {
            var targetWorkspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(workspaceId);
            var orgId = await _userContext.GetCurrentOrganizationIdAsync();

            var mission = _orchestrator.CreateMission(
                targetWorkspaceId,
                orgId,
                request.Title,
                request.Objective,
                request.TargetIndustry,
                request.TargetProspectCount,
                request.TargetValueINR,
                request.Mode,
                request.AutonomyLevel,
                request.WalletBudgetINR);

            var executed = await _orchestrator.LaunchMissionAsync(mission.Id);
            return Ok(executed);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AgentMission>>> GetMissions([FromQuery] Guid? workspaceId)
        {
            var targetWorkspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(workspaceId);
            var missions = _orchestrator.GetMissionsByWorkspace(targetWorkspaceId);
            return Ok(missions);
        }

        [HttpGet("{id}")]
        public ActionResult<AgentMission> GetMissionById(Guid id)
        {
            var mission = _orchestrator.GetMission(id);
            if (mission == null) return NotFound();
            return Ok(mission);
        }

        [HttpPost("{id}/approve-task/{taskId}")]
        public async Task<ActionResult> ApproveGatedTask(Guid id, Guid taskId)
        {
            var approverName = User.Identity?.Name ?? "Executive Leader";
            var success = await _orchestrator.ApproveGatedTaskAsync(id, taskId, approverName);
            if (!success) return BadRequest(new { message = "Unable to approve task. Task may not exist or is not blocked on approval." });

            var mission = _orchestrator.GetMission(id);
            return Ok(mission);
        }
    }
}
