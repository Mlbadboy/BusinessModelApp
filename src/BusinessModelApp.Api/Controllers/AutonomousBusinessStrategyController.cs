using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Strategy;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Strategy;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/enterprise/strategy")]
    public sealed class AutonomousBusinessStrategyController : ControllerBase
    {
        private readonly IAutonomousBusinessStrategyService _service;
        private readonly IAutonomousBusinessStrategyStore _store;

        public AutonomousBusinessStrategyController(
            IAutonomousBusinessStrategyService service,
            IAutonomousBusinessStrategyStore store)
        {
            _service = service;
            _store = store;
        }

        public record CreateObjectiveRequest(string TenantId, string Title, StrategicTimeHorizon Horizon, decimal TargetRevenueINR, decimal AllocatedCapitalINR, decimal MaxRiskToleranceScore = 0.30m);
        public record ProposeInitiativeRequest(string TenantId, string ObjectiveId, string Title, string Description, decimal StrategicValueScore, decimal RequiredCapitalINR);
        public record ApproveInitiativeRequest(string PRG1SignoffSha256);

        [HttpPost("objectives")]
        public async Task<ActionResult<StrategicObjective>> CreateObjective([FromBody] CreateObjectiveRequest request, CancellationToken cancellationToken)
        {
            var obj = await _service.CreateStrategicObjectiveAsync(request.TenantId, request.Title, request.Horizon, request.TargetRevenueINR, request.AllocatedCapitalINR, request.MaxRiskToleranceScore, cancellationToken);
            return Ok(obj);
        }

        [HttpGet("objectives")]
        public async Task<ActionResult<IReadOnlyList<StrategicObjective>>> ListObjectives([FromQuery] string tenantId, CancellationToken cancellationToken)
        {
            var list = await _store.ListObjectivesForTenantAsync(tenantId, cancellationToken);
            return Ok(list);
        }

        [HttpPost("initiatives")]
        public async Task<ActionResult<StrategicInitiative>> ProposeInitiative([FromBody] ProposeInitiativeRequest request, CancellationToken cancellationToken)
        {
            var init = await _service.ProposeInitiativeAsync(request.TenantId, request.ObjectiveId, request.Title, request.Description, request.StrategicValueScore, request.RequiredCapitalINR, cancellationToken);
            return Ok(init);
        }

        [HttpPost("initiatives/{initiativeId}/approve")]
        public async Task<ActionResult<StrategicInitiative>> ApproveInitiative(string initiativeId, [FromBody] ApproveInitiativeRequest request, CancellationToken cancellationToken)
        {
            var init = await _service.ApproveInitiativeWithPRG1Async(initiativeId, request.PRG1SignoffSha256, cancellationToken);
            return Ok(init);
        }

        [HttpPost("initiatives/{initiativeId}/launch")]
        public async Task<ActionResult<StrategicInitiative>> LaunchInitiative(string initiativeId, CancellationToken cancellationToken)
        {
            var init = await _service.LaunchInitiativeExecutionAsync(initiativeId, cancellationToken);
            return Ok(init);
        }
    }
}
