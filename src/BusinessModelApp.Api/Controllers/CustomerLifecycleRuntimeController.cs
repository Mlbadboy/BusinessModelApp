using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Lifecycle;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Lifecycle;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/enterprise/lifecycle")]
    public sealed class CustomerLifecycleRuntimeController : ControllerBase
    {
        private readonly ICustomerLifecycleRuntimeService _service;
        private readonly ICustomerLifecycleRuntimeStore _store;

        public CustomerLifecycleRuntimeController(
            ICustomerLifecycleRuntimeService service,
            ICustomerLifecycleRuntimeStore store)
        {
            _service = service;
            _store = store;
        }

        public record RegisterAccountRequest(string TenantId, string CompanyName, decimal InitialArrINR);
        public record AssessChurnRequest(string TenantId, string CustomerId, decimal ChurnProbability, string PrimaryRiskFactor, string RootCause, List<string> Interventions);
        public record ExpansionRequest(string TenantId, string CustomerId, string TargetModule, decimal AdditionalArrINR, decimal ConfidenceScore);
        public record CloseExpansionRequest(string SignedContractSha256);

        [HttpPost("accounts")]
        public async Task<ActionResult<CustomerAccount>> RegisterAccount([FromBody] RegisterAccountRequest request, CancellationToken cancellationToken)
        {
            var acc = await _service.RegisterAccountAsync(request.TenantId, request.CompanyName, request.InitialArrINR, cancellationToken);
            return Ok(acc);
        }

        [HttpGet("accounts/{customerId}")]
        public async Task<ActionResult<CustomerAccount>> GetAccount(string customerId, CancellationToken cancellationToken)
        {
            var acc = await _store.GetAccountAsync(customerId, cancellationToken);
            if (acc == null) return NotFound();
            return Ok(acc);
        }

        [HttpPost("churn-assessments")]
        public async Task<ActionResult<ChurnRiskAssessment>> AssessChurn([FromBody] AssessChurnRequest request, CancellationToken cancellationToken)
        {
            var assessment = await _service.AssessChurnRiskAsync(
                request.TenantId,
                request.CustomerId,
                request.ChurnProbability,
                request.PrimaryRiskFactor,
                request.RootCause,
                request.Interventions,
                cancellationToken);

            return Ok(assessment);
        }

        [HttpPost("expansions")]
        public async Task<ActionResult<ExpansionOpportunity>> IdentifyExpansion([FromBody] ExpansionRequest request, CancellationToken cancellationToken)
        {
            var exp = await _service.IdentifyExpansionAsync(
                request.TenantId,
                request.CustomerId,
                request.TargetModule,
                request.AdditionalArrINR,
                request.ConfidenceScore,
                cancellationToken);

            return Ok(exp);
        }

        [HttpPost("expansions/{expansionId}/won")]
        public async Task<ActionResult<ExpansionOpportunity>> CloseWonExpansion(string expansionId, [FromBody] CloseExpansionRequest request, CancellationToken cancellationToken)
        {
            var exp = await _service.CloseWonExpansionAsync(expansionId, request.SignedContractSha256, cancellationToken);
            return Ok(exp);
        }

        [HttpGet("retention-metrics")]
        public async Task<ActionResult<NetRetentionCalculation>> GetRetentionMetrics(
            [FromQuery] string tenantId,
            [FromQuery] decimal startingArrINR,
            [FromQuery] decimal contractionArrINR,
            [FromQuery] decimal churnArrINR,
            CancellationToken cancellationToken)
        {
            var metrics = await _service.CalculateRetentionMetricsAsync(tenantId, startingArrINR, contractionArrINR, churnArrINR, cancellationToken);
            return Ok(metrics);
        }
    }
}
