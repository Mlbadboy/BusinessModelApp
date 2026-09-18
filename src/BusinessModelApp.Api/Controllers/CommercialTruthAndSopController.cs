using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Workforce;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/commercial-truth")]
    public class CommercialTruthAndSopController : ControllerBase
    {
        private readonly ICommercialTruthEngine _truthEngine;
        private readonly IBusinessSopCompiler _sopCompiler;

        public CommercialTruthAndSopController(ICommercialTruthEngine truthEngine, IBusinessSopCompiler sopCompiler)
        {
            _truthEngine = truthEngine ?? throw new ArgumentNullException(nameof(truthEngine));
            _sopCompiler = sopCompiler ?? throw new ArgumentNullException(nameof(sopCompiler));
        }

        private string GetTenantId()
        {
            if (Request.Headers.TryGetValue("X-Tenant-ID", out var tenantId) && !string.IsNullOrWhiteSpace(tenantId))
            {
                return tenantId.ToString();
            }
            return "tenant-default";
        }

        [HttpPost("claims")]
        public async Task<IActionResult> RecordClaim([FromBody] RecordClaimRequest request)
        {
            var fact = await _truthEngine.RecordCommercialClaimAsync(GetTenantId(), request.OpportunityId, request.ClaimedState, request.Description ?? string.Empty);
            return Ok(fact);
        }

        [HttpPost("corroborate")]
        public async Task<IActionResult> CorroborateFact([FromBody] CorroborateFactRequest request)
        {
            var fact = await _truthEngine.CorroborateCommercialFactAsync(GetTenantId(), request.OpportunityId, request.Evidence);
            return Ok(fact);
        }

        [HttpGet("facts/{opportunityId}")]
        public async Task<IActionResult> GetFact(string opportunityId)
        {
            var fact = await _truthEngine.GetCommercialFactAsync(GetTenantId(), opportunityId);
            if (fact == null) return NotFound();
            return Ok(fact);
        }

        [HttpGet("sops")]
        public async Task<IActionResult> ListSops()
        {
            var list = await _sopCompiler.ListSopDefinitionsAsync(GetTenantId());
            return Ok(list);
        }

        [HttpPost("sops/compile")]
        public async Task<IActionResult> CompileSop([FromBody] CompileSopRequest request)
        {
            try
            {
                var proposal = await _sopCompiler.CompileSopToWorkProposalAsync(GetTenantId(), request.SopId, request.OpportunityId, request.Title);
                return Ok(proposal);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    public class RecordClaimRequest
    {
        public string OpportunityId { get; set; } = string.Empty;
        public CommercialTruthState ClaimedState { get; set; }
        public string? Description { get; set; }
    }

    public class CorroborateFactRequest
    {
        public string OpportunityId { get; set; } = string.Empty;
        public CommercialEvidenceItem Evidence { get; set; } = new();
    }

    public class CompileSopRequest
    {
        public string SopId { get; set; } = string.Empty;
        public string OpportunityId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }
}
