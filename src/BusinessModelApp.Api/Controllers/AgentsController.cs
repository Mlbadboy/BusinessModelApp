using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.DTOs.Agent;
using BusinessModelApp.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AgentsController : ControllerBase
    {
        private readonly IAgentService _agentService;
        private readonly ILogger<AgentsController> _logger;
        private const int CacheMaxAge = 300; // 5 minutes

        public AgentsController(IAgentService agentService, ILogger<AgentsController> logger)
        {
            _agentService = agentService;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<AgentDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<AgentDto>>> GetAllAgents(CancellationToken cancellationToken)
        {
            var agents = await _agentService.GetAllAgentsAsync(cancellationToken);
            Response.Headers.Add("Cache-Control", $"public, max-age={CacheMaxAge}");
            return Ok(agents);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(AgentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AgentDto>> GetAgent(Guid id, CancellationToken cancellationToken)
        {
            var agent = await _agentService.GetAgentAsync(id, cancellationToken);
            if (agent == null)
                return NotFound();

            Response.Headers.Add("Cache-Control", $"public, max-age={CacheMaxAge}");
            return Ok(agent);
        }

        [HttpGet("department/{department}")]
        [ProducesResponseType(typeof(IEnumerable<AgentDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<AgentDto>>> GetAgentsByDepartment(string department, CancellationToken cancellationToken)
        {
            var agents = await _agentService.GetAgentsByDepartmentAsync(department, cancellationToken);
            Response.Headers.Add("Cache-Control", $"public, max-age={CacheMaxAge}");
            return Ok(agents);
        }

        [HttpGet("available")]
        [ProducesResponseType(typeof(IEnumerable<AgentDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<AgentDto>>> GetAvailableAgents(CancellationToken cancellationToken)
        {
            var agents = await _agentService.GetAvailableAgentsAsync(cancellationToken);
            Response.Headers.Add("Cache-Control", "no-cache");
            return Ok(agents);
        }

        [HttpGet("skill")]
        [ProducesResponseType(typeof(IEnumerable<AgentDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<AgentDto>>> GetAgentsBySkill(
            [FromQuery] string skill, 
            [FromQuery] int minimumLevel, 
            CancellationToken cancellationToken)
        {
            var agents = await _agentService.GetAgentsBySkillAsync(skill, minimumLevel, cancellationToken);
            Response.Headers.Add("Cache-Control", $"public, max-age={CacheMaxAge}");
            return Ok(agents);
        }

        [HttpGet("{id:guid}/monitoring")]
        [ProducesResponseType(typeof(AgentMonitoringDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AgentMonitoringDto>> GetAgentMonitoring(Guid id, CancellationToken cancellationToken)
        {
            var monitoring = await _agentService.GetAgentMonitoringDataAsync(id, cancellationToken);
            if (monitoring == null)
                return NotFound();

            Response.Headers.Add("Cache-Control", "no-cache");
            return Ok(monitoring);
        }

        [HttpPost]
        [ProducesResponseType(typeof(AgentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<AgentDto>> CreateAgent([FromBody] CreateAgentDto createDto, CancellationToken cancellationToken)
        {
            var agent = await _agentService.CreateAgentAsync(createDto, cancellationToken);
            return CreatedAtAction(nameof(GetAgent), new { id = agent.Id }, agent);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(AgentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AgentDto>> UpdateAgent(Guid id, [FromBody] AgentUpdateDto updateDto, CancellationToken cancellationToken)
        {
            var agent = await _agentService.UpdateAgentAsync(id, updateDto, cancellationToken);
            if (agent == null)
                return NotFound();

            return Ok(agent);
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAgent(Guid id, CancellationToken cancellationToken)
        {
            var result = await _agentService.DeleteAgentAsync(id, cancellationToken);
            if (!result)
                return NotFound();

            return NoContent();
        }

        [HttpPut("{id:guid}/status")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateAgentStatus(Guid id, [FromBody] string status, CancellationToken cancellationToken)
        {
            var result = await _agentService.UpdateAgentStatusAsync(id, status, cancellationToken);
            if (!result)
                return NotFound();

            return NoContent();
        }

        [HttpPut("{id:guid}/availability")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateAgentAvailability(
            Guid id, 
            [FromBody] AgentAvailabilityDto availability, 
            CancellationToken cancellationToken)
        {
            var result = await _agentService.UpdateAgentAvailabilityAsync(id, availability, cancellationToken);
            if (!result)
                return NotFound();

            return NoContent();
        }

        [HttpPut("{id:guid}/skills")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateAgentSkills(
            Guid id, 
            [FromBody] IEnumerable<AgentSkillDto> skills, 
            CancellationToken cancellationToken)
        {
            var result = await _agentService.UpdateAgentSkillsAsync(id, skills, cancellationToken);
            if (!result)
                return NotFound();

            return NoContent();
        }

        [HttpPost("{id:guid}/monitoring/start")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> StartMonitoring(Guid id, CancellationToken cancellationToken)
        {
            await _agentService.StartMonitoringAsync(id, cancellationToken);
            return NoContent();
        }

        [HttpPost("{id:guid}/monitoring/stop")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> StopMonitoring(Guid id, CancellationToken cancellationToken)
        {
            await _agentService.StopMonitoringAsync(id, cancellationToken);
            return NoContent();
        }

        [HttpGet("activities")]
        [ProducesResponseType(typeof(IEnumerable<AgentActivityDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<AgentActivityDto>>> GetAgentActivities([FromQuery] int limit = 50, CancellationToken cancellationToken = default)
        {
            var activities = await _agentService.GetAgentActivitiesAsync(limit, cancellationToken);
            Response.Headers.Add("Cache-Control", "no-cache");
            return Ok(activities);
        }
    }
}
