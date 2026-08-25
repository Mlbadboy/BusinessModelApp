using BusinessModelApp.Core.DTOs;
using BusinessModelApp.Core.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecommendationController : ControllerBase
    {
        private readonly IRecommendationService _recommendationService;

        public RecommendationController(IRecommendationService recommendationService)
        {
            _recommendationService = recommendationService;
        }

        [HttpGet("agent/{agentId}")]
        public async Task<ActionResult<IEnumerable<Recommendation>>> GetAgentRecommendations(string agentId)
        {
            var recommendations = await _recommendationService.GetAgentRecommendationsAsync(agentId);
            return Ok(recommendations);
        }

        [HttpGet("executive/{executiveId}")]
        public async Task<ActionResult<IEnumerable<Recommendation>>> GetExecutiveRecommendations(string executiveId)
        {
            var recommendations = await _recommendationService.GetExecutiveRecommendationsAsync(executiveId);
            return Ok(recommendations);
        }
    }
}
