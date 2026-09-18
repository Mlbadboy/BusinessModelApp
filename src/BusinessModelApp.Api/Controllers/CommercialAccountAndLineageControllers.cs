using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Workforce;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/governed-crm")]
    public class GovernedCrmController : ControllerBase
    {
        private readonly IGovernedCrmService _crmService;

        public GovernedCrmController(IGovernedCrmService crmService)
        {
            _crmService = crmService ?? throw new ArgumentNullException(nameof(crmService));
        }

        private string GetTenantId() =>
            Request.Headers.TryGetValue("X-Tenant-ID", out var t) && !string.IsNullOrWhiteSpace(t) ? t.ToString() : "tenant-default";

        [HttpPost("mutations")]
        public async Task<IActionResult> SubmitMutation([FromBody] GovernedCrmMutation mutation)
        {
            try
            {
                var success = await _crmService.SubmitGovernedMutationAsync(GetTenantId(), mutation);
                return Ok(new { success });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("mutations")]
        public async Task<IActionResult> ListMutations()
        {
            var list = await _crmService.ListMutationsAsync(GetTenantId());
            return Ok(list);
        }
    }

    [ApiController]
    [Route("api/account-graphs")]
    public class AccountGraphController : ControllerBase
    {
        private readonly IAccountGraphService _graphService;

        public AccountGraphController(IAccountGraphService graphService)
        {
            _graphService = graphService ?? throw new ArgumentNullException(nameof(graphService));
        }

        private string GetTenantId() =>
            Request.Headers.TryGetValue("X-Tenant-ID", out var t) && !string.IsNullOrWhiteSpace(t) ? t.ToString() : "tenant-default";

        [HttpPost]
        public async Task<IActionResult> SaveGraph([FromBody] AccountGraph graph)
        {
            var saved = await _graphService.SaveAccountGraphAsync(GetTenantId(), graph);
            return Ok(saved);
        }

        [HttpGet("{accountId}")]
        public async Task<IActionResult> GetGraph(string accountId)
        {
            var graph = await _graphService.GetAccountGraphAsync(GetTenantId(), accountId);
            if (graph == null) return NotFound();
            return Ok(graph);
        }

        [HttpGet]
        public async Task<IActionResult> ListGraphs()
        {
            var list = await _graphService.ListAccountGraphsAsync(GetTenantId());
            return Ok(list);
        }
    }

    [ApiController]
    [Route("api/commercial-lineage")]
    public class CommercialLineageController : ControllerBase
    {
        private readonly ICommercialLineageService _lineageService;

        public CommercialLineageController(ICommercialLineageService lineageService)
        {
            _lineageService = lineageService ?? throw new ArgumentNullException(nameof(lineageService));
        }

        private string GetTenantId() =>
            Request.Headers.TryGetValue("X-Tenant-ID", out var t) && !string.IsNullOrWhiteSpace(t) ? t.ToString() : "tenant-default";

        [HttpPost("nodes")]
        public async Task<IActionResult> AppendNode([FromBody] CommercialLineageNode node)
        {
            var appended = await _lineageService.AppendLineageNodeAsync(GetTenantId(), node);
            return Ok(appended);
        }

        [HttpGet("{opportunityId}")]
        public async Task<IActionResult> GetChain(string opportunityId)
        {
            var chain = await _lineageService.GetLineageChainAsync(GetTenantId(), opportunityId);
            return Ok(chain);
        }

        [HttpGet("{opportunityId}/verify")]
        public async Task<IActionResult> VerifyIntegrity(string opportunityId)
        {
            var valid = await _lineageService.VerifyLineageIntegrityAsync(GetTenantId(), opportunityId);
            return Ok(new { isValid = valid });
        }

        [HttpPost("effects")]
        public async Task<IActionResult> RecordEffect([FromBody] ImmutableExternalEffect effect)
        {
            var recorded = await _lineageService.RecordExternalEffectAsync(GetTenantId(), effect);
            return Ok(recorded);
        }
    }
}
