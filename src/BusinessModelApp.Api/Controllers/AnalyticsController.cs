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

        [HttpGet("explain-metric/{evidenceId}")]
        public async Task<ActionResult<object>> ExplainMetric(
            string evidenceId,
            [FromServices] BusinessModelApp.Core.AI.IAIInferenceGateway aiGateway,
            [FromQuery] Guid? workspaceId,
            [FromQuery] decimal? quarterlyTarget)
        {
            var targetWorkspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(workspaceId);
            var leads = await _repository.GetLeadsByWorkspaceIdAsync(targetWorkspaceId);
            var opps = await _repository.GetOpportunitiesByWorkspaceIdAsync(targetWorkspaceId);

            var target = quarterlyTarget ?? 5000000m;
            var health = _healthEngine.CalculateHealth(leads, opps, target);
            var evidence = health.EvidenceRecords.Find(e => e.EvidenceId.Equals(evidenceId, StringComparison.OrdinalIgnoreCase));

            if (evidence == null)
            {
                return NotFound(new { message = $"Evidence record '{evidenceId}' not found in current health model." });
            }

            var prompt = $"Explain the business metric '{evidence.DisplayName}' (Value: {evidence.FormattedValue}) calculated with formula: {evidence.Formula}. Provide a 2-sentence executive summary and 2 specific operational recommendations for improving this metric.";
            var aiRequest = new BusinessModelApp.Core.AI.AIRequest
            {
                TaskType = BusinessModelApp.Core.AI.AITaskType.BusinessHealthExplanation,
                WorkspaceId = targetWorkspaceId,
                Messages = { BusinessModelApp.Core.AI.AIMessage.User(prompt) }
            };

            var aiResponse = await aiGateway.ExecuteAsync(aiRequest);

            return Ok(new
            {
                evidenceId = evidence.EvidenceId,
                displayName = evidence.DisplayName,
                formattedValue = evidence.FormattedValue,
                formula = evidence.Formula,
                explanation = aiResponse.Content,
                modelUsed = aiResponse.ModelUsed,
                providerUsed = aiResponse.ProviderUsed
            });
        }
    }
}
