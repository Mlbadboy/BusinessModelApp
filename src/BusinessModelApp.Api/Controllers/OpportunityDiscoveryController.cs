using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OpportunityDiscoveryController : ControllerBase
    {
        private readonly IBusinessOpportunityEngine _opportunityEngine;

        public OpportunityDiscoveryController(IBusinessOpportunityEngine opportunityEngine)
        {
            _opportunityEngine = opportunityEngine;
        }

        public class SearchRequest
        {
            public Guid? WorkspaceId { get; set; }
            public string City { get; set; } = "Pune";
            public string Industry { get; set; } = "Real Estate Developer";
            public int TargetCount { get; set; } = 10;
        }

        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] SearchRequest request)
        {
            var wsId = request.WorkspaceId ?? Guid.Empty;
            var filter = new OpportunityDiscoveryFilter
            {
                City = request.City,
                IndustryOrCategory = request.Industry,
                TargetCount = request.TargetCount
            };

            var packages = await _opportunityEngine.DiscoverOpportunitiesAsync(wsId, filter);
            return Ok(new { success = true, count = packages.Count, results = packages });
        }

        public class AuditRequest
        {
            public string WebsiteUrl { get; set; } = string.Empty;
            public string BusinessName { get; set; } = string.Empty;
        }

        [HttpPost("audit")]
        public async Task<IActionResult> Audit([FromBody] AuditRequest request)
        {
            var audit = await _opportunityEngine.AuditBusinessPresenceAsync(request.WebsiteUrl, request.BusinessName);
            return Ok(new { success = true, audit });
        }
    }
}
