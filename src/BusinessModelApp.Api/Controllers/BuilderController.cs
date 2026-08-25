using System.Threading.Tasks;
using BusinessModelApp.Core.Agents;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BuilderController : ControllerBase
    {
        private readonly AutonomousAgent _agent;

        public BuilderController(AutonomousAgent agent)
        {
            _agent = agent;
        }

        [HttpPost("build")]
        public async Task<IActionResult> Build([FromBody] BuildRequest request)
        {
            var result = await _agent.RunAsync(request.Goal);
            return Ok(new { Result = result });
        }
    }

    public class BuildRequest
    {
        public string Goal { get; set; }
    }
}
