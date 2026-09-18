using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Workforce;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/revenue-control-plane")]
    public class RevenueControlPlaneController : ControllerBase
    {
        private readonly IRevenueControlPlane _controlPlane;
        private readonly IRevenueMissionFactory _missionFactory;

        public RevenueControlPlaneController(IRevenueControlPlane controlPlane, IRevenueMissionFactory missionFactory)
        {
            _controlPlane = controlPlane ?? throw new ArgumentNullException(nameof(controlPlane));
            _missionFactory = missionFactory ?? throw new ArgumentNullException(nameof(missionFactory));
        }

        private string GetTenantId() =>
            Request.Headers.TryGetValue("X-Tenant-ID", out var t) && !string.IsNullOrWhiteSpace(t) ? t.ToString() : "tenant-default";

        [HttpGet("state")]
        public async Task<IActionResult> GetState()
        {
            var state = await _controlPlane.GetCurrentStateAsync(GetTenantId());
            return Ok(state);
        }

        [HttpPost("metrics")]
        public async Task<IActionResult> UpdateMetrics([FromBody] UpdateMetricsRequest request)
        {
            await _controlPlane.UpdatePipelineMetricsAsync(
                GetTenantId(),
                request.QualifiedPipeline,
                request.WeightedPipeline,
                request.ClosedWon,
                request.Invoiced,
                request.Collected,
                request.Margin);
            return Ok(new { success = true });
        }

        [HttpPost("missions")]
        public async Task<IActionResult> CreateMission([FromBody] CreateRevenueMissionRequest request)
        {
            var spec = await _missionFactory.CreateRevenueMissionAsync(GetTenantId(), request.OpportunityId, request.TemplateType, request.Budget);
            return Ok(spec);
        }

        [HttpGet("missions/{opportunityId}")]
        public async Task<IActionResult> ListMissions(string opportunityId)
        {
            var list = await _missionFactory.ListMissionsForOpportunityAsync(GetTenantId(), opportunityId);
            return Ok(list);
        }
    }

    public class UpdateMetricsRequest
    {
        public decimal QualifiedPipeline { get; set; }
        public decimal WeightedPipeline { get; set; }
        public decimal ClosedWon { get; set; }
        public decimal Invoiced { get; set; }
        public decimal Collected { get; set; }
        public decimal Margin { get; set; }
    }

    public class CreateRevenueMissionRequest
    {
        public string OpportunityId { get; set; } = string.Empty;
        public RevenueMissionTemplateType TemplateType { get; set; } = RevenueMissionTemplateType.QUALIFY_ACCOUNT;
        public decimal Budget { get; set; } = 50m;
    }
}
