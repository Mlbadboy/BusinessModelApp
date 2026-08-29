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

        [HttpGet("{id}/trajectory")]
        public ActionResult<MissionTrajectoryReport> GetMissionTrajectory(Guid id)
        {
            try
            {
                var report = _orchestrator.GetTrajectoryReport(id);
                return Ok(report);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPost("{id}/replan")]
        public async Task<ActionResult<AgentMission>> ReplanMission(Guid id)
        {
            try
            {
                var mission = await _orchestrator.ReplanMissionAsync(id);
                return Ok(mission);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPost("{id}/approve-task/{taskId}")]
        public async Task<ActionResult<AgentMission>> ApproveTask(Guid id, Guid taskId)
        {
            var approver = User.Identity?.Name ?? "Executive Approver";
            var success = await _orchestrator.ApproveGatedTaskAsync(id, taskId, approver);
            if (!success) return BadRequest(new { message = "Failed to approve task or task is not blocked on approval." });

            var mission = _orchestrator.GetMission(id);
            return Ok(mission);
        }
    }
}

