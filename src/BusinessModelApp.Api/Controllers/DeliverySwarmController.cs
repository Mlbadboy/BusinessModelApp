using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DeliverySwarmController : ControllerBase
    {
        private readonly IDeliverySwarmService _deliverySwarm;

        public DeliverySwarmController(IDeliverySwarmService deliverySwarm)
        {
            _deliverySwarm = deliverySwarm;
        }

        [HttpGet("missions")]
        public async Task<IActionResult> GetMissions([FromQuery] Guid? workspaceId)
        {
            var wsId = workspaceId ?? Guid.Empty;
            var missions = await _deliverySwarm.GetActiveMissionsAsync(wsId);
            return Ok(new { success = true, missions });
        }

        [HttpGet("missions/{id}")]
        public async Task<IActionResult> GetMissionById([FromRoute] Guid id)
        {
            var mission = await _deliverySwarm.GetDeliveryMissionAsync(id);
            return Ok(new { success = true, mission });
        }

        [HttpPost("missions/{id}/step")]
        public async Task<IActionResult> ExecuteStep([FromRoute] Guid id)
        {
            var mission = await _deliverySwarm.ExecuteDeliveryStepAsync(id);
            return Ok(new { success = true, mission });
        }
    }
}
