using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Commercial;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AnalyticsController : ControllerBase
    {
        private readonly ICommercialRepository _repository;
        private readonly IUserContextService _userContext;
        private readonly IBusinessHealthEngine _healthEngine;
        private readonly IExecutiveBriefService _briefService;

        public AnalyticsController(
            ICommercialRepository repository,
            IUserContextService userContext,
            IBusinessHealthEngine healthEngine,
            IExecutiveBriefService briefService)
        {
            _repository = repository;
            _userContext = userContext;
            _healthEngine = healthEngine;
            _briefService = briefService;
        }

        [HttpGet("business-health")]
        public async Task<ActionResult<BusinessHealthResult>> GetBusinessHealth([FromQuery] Guid? workspaceId, [FromQuery] decimal? quarterlyTarget)
        {
            var targetWorkspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(workspaceId);
            var leads = await _repository.GetLeadsByWorkspaceIdAsync(targetWorkspaceId);
            var opps = await _repository.GetOpportunitiesByWorkspaceIdAsync(targetWorkspaceId);

            var target = quarterlyTarget ?? 5000000m;
            var health = _healthEngine.CalculateHealth(leads, opps, target);

            return Ok(health);
        }

        [HttpGet("executive-brief")]
        public async Task<ActionResult<ExecutiveBriefContext>> GetExecutiveBrief([FromQuery] Guid? workspaceId, [FromQuery] decimal? quarterlyTarget)
        {
            var targetWorkspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(workspaceId);
            var leads = await _repository.GetLeadsByWorkspaceIdAsync(targetWorkspaceId);
            var opps = await _repository.GetOpportunitiesByWorkspaceIdAsync(targetWorkspaceId);

            var target = quarterlyTarget ?? 5000000m;
            var health = _healthEngine.CalculateHealth(leads, opps, target);
            var brief = await _briefService.GenerateExecutiveBriefAsync(health);

            return Ok(brief);
        }
    }
}
