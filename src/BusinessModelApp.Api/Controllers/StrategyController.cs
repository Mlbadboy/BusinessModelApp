using BusinessModelApp.Core.DTOs.Strategy;
using BusinessModelApp.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/business/strategy")]
    [Authorize]
    public class StrategyController : ControllerBase
    {
        private readonly IStrategyService _strategyService;
        private readonly IAuthorizationService _authorizationService;

        public StrategyController(IStrategyService strategyService, IAuthorizationService authorizationService)
        {
            _strategyService = strategyService;
            _authorizationService = authorizationService;
        }

        // GET: api/business/strategy/list
        [HttpGet("list")]
        [Authorize(Roles = "CEO,COO,CFO,CBO")]
        public async Task<ActionResult<IEnumerable<BusinessStrategyDto>>> GetStrategies()
        {
            var strategies = await _strategyService.GetAllStrategiesAsync();
            return Ok(strategies);
        }

        // GET: api/business/strategy/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "CEO,COO,CFO,CBO")]
        public async Task<ActionResult<BusinessStrategyDto>> GetStrategy(string id)
        {
            var strategy = await _strategyService.GetStrategyByIdAsync(id);
            if (strategy == null)
            {
                return NotFound();
            }
            return Ok(strategy);
        }

        // POST: api/business/strategy
        [HttpPost]
        [Authorize(Roles = "CEO,COO,CFO,CBO")]
        public async Task<ActionResult<BusinessStrategyDto>> CreateStrategy(BusinessStrategyDto strategy)
        {
            var result = await _strategyService.CreateStrategyAsync(strategy);
            return CreatedAtAction(nameof(GetStrategy), new { id = result.Id }, result);
        }

        // PUT: api/business/strategy/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "CEO,COO,CFO,CBO")]
        public async Task<IActionResult> UpdateStrategy(string id, BusinessStrategyDto strategy)
        {
            if (id != strategy.Id)
            {
                return BadRequest();
            }

            await _strategyService.UpdateStrategyAsync(strategy);
            return NoContent();
        }

        // DELETE: api/business/strategy/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "CEO,COO,CFO,CBO")]
        public async Task<IActionResult> DeleteStrategy(string id)
        {
            await _strategyService.DeleteStrategyAsync(id);
            return NoContent();
        }

        // GET: api/business/strategy/goals
        [HttpGet("goals")]
        [Authorize(Roles = "CEO,COO,CFO,CBO")]
        public async Task<ActionResult<IEnumerable<StrategyGoalDto>>> GetGoals()
        {
            var goals = await _strategyService.GetGoalsAsync();
            return Ok(goals);
        }

        // GET: api/business/strategy/actions
        [HttpGet("actions")]
        [Authorize(Roles = "CEO,COO,CFO,CBO")]
        public async Task<ActionResult<IEnumerable<StrategyActionDto>>> GetActions()
        {
            var actions = await _strategyService.GetActionsAsync();
            return Ok(actions);
        }

        // GET: api/business/strategy/metrics
        [HttpGet("metrics")]
        [Authorize(Roles = "CEO,COO,CFO,CBO")]
        public async Task<ActionResult<IEnumerable<StrategyMetricDto>>> GetMetrics()
        {
            var metrics = await _strategyService.GetMetricsAsync();
            return Ok(metrics);
        }

        // GET: api/business/strategy/performance
        [HttpGet("performance")]
        [Authorize(Roles = "CEO,COO,CFO,CBO")]
        public async Task<ActionResult<StrategyPerformanceDto>> GetPerformance()
        {
            var performance = await _strategyService.GetPerformanceAsync();
            return Ok(performance);
        }

        // GET: api/business/strategy/analysis
        [HttpGet("analysis")]
        [Authorize(Roles = "CEO,COO,CFO,CBO")]
        public async Task<ActionResult<StrategyAnalysisDto>> GetAnalysis()
        {
            var analysis = await _strategyService.GetAnalysisAsync();
            return Ok(analysis);
        }
    }
}
