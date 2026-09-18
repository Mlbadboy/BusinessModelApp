using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Workforce;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/agent-economics")]
    public class AgentEconomicsController : ControllerBase
    {
        private readonly IAgentEconomicsEngine _economicsEngine;

        public AgentEconomicsController(IAgentEconomicsEngine economicsEngine)
        {
            _economicsEngine = economicsEngine ?? throw new ArgumentNullException(nameof(economicsEngine));
        }

        private string GetTenantId()
        {
            if (Request.Headers.TryGetValue("X-Tenant-ID", out var tenantId) && !string.IsNullOrWhiteSpace(tenantId))
            {
                return tenantId.ToString();
            }
            return "tenant-default";
        }

        [HttpPost("costs")]
        public async Task<IActionResult> RecordCost([FromBody] CostAllocationRecord record)
        {
            await _economicsEngine.RecordCostAllocationAsync(GetTenantId(), record);
            return Ok(new { success = true });
        }

        [HttpGet("agents/{agentId}/breakdown")]
        public async Task<IActionResult> GetCostBreakdown(string agentId)
        {
            var breakdown = await _economicsEngine.GetAgentCostBreakdownAsync(GetTenantId(), agentId);
            return Ok(breakdown);
        }

        [HttpPost("attributions")]
        public async Task<IActionResult> RecordAttribution([FromBody] RevenueAttributionRecord record)
        {
            await _economicsEngine.RecordRevenueAttributionAsync(GetTenantId(), record);
            return Ok(new { success = true });
        }

        [HttpGet("agents/{agentId}/attributions")]
        public async Task<IActionResult> GetAgentAttributions(string agentId)
        {
            var list = await _economicsEngine.GetAgentRevenueAttributionsAsync(GetTenantId(), agentId);
            return Ok(list);
        }

        [HttpGet("outcomes/{objectiveId}")]
        public async Task<IActionResult> ComputeOutcome(string objectiveId)
        {
            var outcome = await _economicsEngine.ComputeEconomicOutcomeAsync(GetTenantId(), objectiveId);
            return Ok(outcome);
        }

        [HttpPost("performance")]
        public async Task<IActionResult> UpdatePerformance([FromBody] AgentMetrologyPerformance perf)
        {
            await _economicsEngine.UpdateAgentMetrologyPerformanceAsync(GetTenantId(), perf);
            return Ok(new { success = true });
        }

        [HttpGet("routing/{roleTitle}")]
        public async Task<IActionResult> ListBestAgentsForRouting(string roleTitle)
        {
            var agents = await _economicsEngine.ListBestAgentsForRoutingAsync(GetTenantId(), roleTitle);
            return Ok(agents);
        }
    }
}
