using BusinessModelApp.Core.DTOs.Revenue;
using BusinessModelApp.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/business/revenue")]
    // [Authorize]
    public class RevenueController : ControllerBase
    {
        private readonly IRevenueService _revenueService;
        public RevenueController(IRevenueService revenueService)
        {
            _revenueService = revenueService;
        }

        // GET: api/business/revenue/sources
        [HttpGet("sources")]
        // [Authorize(Roles = "CEO,COO,CFO,CBO")]
        public async Task<ActionResult<IEnumerable<RevenueSourceDto>>> GetRevenueSources()
        {
            var sources = await _revenueService.GetAllRevenueSourcesAsync();
            return Ok(sources);
        }

        // GET: api/business/revenue/sources/{id}
        [HttpGet("sources/{id}")]
        // [Authorize(Roles = "CEO,COO,CFO,CBO")]
        public async Task<ActionResult<RevenueSourceDto>> GetRevenueSource(string id)
        {
            var source = await _revenueService.GetRevenueSourceByIdAsync(id);
            if (source == null)
            {
                return NotFound();
            }
            return Ok(source);
        }

        // POST: api/business/revenue/sources
        [HttpPost("sources")]
        // [Authorize(Roles = "CEO,COO,CFO,CBO")]
        public async Task<ActionResult<RevenueSourceDto>> CreateRevenueSource(RevenueSourceDto source)
        {
            var result = await _revenueService.CreateRevenueSourceAsync(source);
            return CreatedAtAction(nameof(GetRevenueSource), new { id = result.Id }, result);
        }

        // PUT: api/business/revenue/sources/{id}
        [HttpPut("sources/{id}")]
        // [Authorize(Roles = "CEO,COO,CFO,CBO")]
        public async Task<IActionResult> UpdateRevenueSource(string id, RevenueSourceDto source)
        {
            if (id != source.Id)
            {
                return BadRequest();
            }

            await _revenueService.UpdateRevenueSourceAsync(source);
            return NoContent();
        }

        // DELETE: api/business/revenue/sources/{id}
        [HttpDelete("sources/{id}")]
        // [Authorize(Roles = "CEO,COO,CFO,CBO")]
        public async Task<IActionResult> DeleteRevenueSource(string id)
        {
            await _revenueService.DeleteRevenueSourceAsync(id);
            return NoContent();
        }

        // GET: api/business/revenue/metrics
        [HttpGet("metrics")]
        // [Authorize(Roles = "CEO,COO,CFO,CBO")]
        public async Task<ActionResult<IEnumerable<RevenueMetricDto>>> GetRevenueMetrics()
        {
            try
            {
                var metrics = await _revenueService.GetRevenueMetricsAsync();
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RevenueController] Error: {ex}");
                return StatusCode(500, ex.Message);
            }
        }

        // GET: api/business/revenue/performance
        [HttpGet("performance")]
        // [Authorize(Roles = "CEO,COO,CFO,CBO")]
        public async Task<ActionResult<RevenuePerformanceDto>> GetRevenuePerformance()
        {
            var performance = await _revenueService.GetRevenuePerformanceAsync();
            return Ok(performance);
        }

        // GET: api/business/revenue/analysis
        [HttpGet("analysis")]
        // [Authorize(Roles = "CEO,COO,CFO,CBO")]
        public async Task<ActionResult<RevenueAnalysisDto>> GetRevenueAnalysis()
        {
            var analysis = await _revenueService.GetRevenueAnalysisAsync();
            return Ok(analysis);
        }
    }
}
