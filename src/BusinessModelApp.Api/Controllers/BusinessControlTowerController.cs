using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.ControlTower;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.ControlTower;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/business-control")]
    public sealed class BusinessControlTowerController : ControllerBase
    {
        private readonly IBusinessControlTowerService _service;

        public BusinessControlTowerController(IBusinessControlTowerService service)
        {
            _service = service;
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<ControlTowerExecutiveDashboard>> GetDashboard(
            [FromQuery] string tenantId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) return BadRequest("TenantId is required.");
            var dashboard = await _service.GetExecutiveDashboardAsync(tenantId, cancellationToken);
            return Ok(dashboard);
        }
    }
}
