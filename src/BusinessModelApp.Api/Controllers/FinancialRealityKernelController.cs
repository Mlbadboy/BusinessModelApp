using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Finance;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Finance;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/enterprise/finance")]
    public sealed class FinancialRealityKernelController : ControllerBase
    {
        private readonly IFinancialRealityKernelService _service;
        private readonly IFinancialRealityStore _store;

        public FinancialRealityKernelController(
            IFinancialRealityKernelService service,
            IFinancialRealityStore store)
        {
            _service = service;
            _store = store;
        }

        public record DraftInvoiceRequest(string TenantId, string CustomerId, string ContractId, decimal SubtotalINR, DateTime DueDateUtc);
        public record IssueInvoiceRequest(string Batch6PermitId);
        public record ReconcilePaymentRequest(decimal AmountINR, string WireReference, string StatementDigestSha256);
        public record RecordCostRequest(string TenantId, string CustomerId, DeliveryCostCategory Category, decimal AmountINR, string Description);

        [HttpPost("invoices")]
        public async Task<ActionResult<CommercialInvoice>> DraftInvoice([FromBody] DraftInvoiceRequest request, CancellationToken cancellationToken)
        {
            var inv = await _service.DraftInvoiceAsync(request.TenantId, request.CustomerId, request.ContractId, request.SubtotalINR, request.DueDateUtc, cancellationToken);
            return Ok(inv);
        }

        [HttpPost("invoices/{invoiceId}/issue")]
        public async Task<ActionResult<CommercialInvoice>> IssueInvoice(string invoiceId, [FromBody] IssueInvoiceRequest request, CancellationToken cancellationToken)
        {
            var inv = await _service.ApproveAndIssueInvoiceAsync(invoiceId, request.Batch6PermitId, cancellationToken);
            return Ok(inv);
        }

        [HttpPost("invoices/{invoiceId}/reconcile")]
        public async Task<ActionResult<BankCashReceipt>> ReconcilePayment(string invoiceId, [FromBody] ReconcilePaymentRequest request, CancellationToken cancellationToken)
        {
            var receipt = await _service.ReconcileBankPaymentAsync(invoiceId, request.AmountINR, request.WireReference, request.StatementDigestSha256, cancellationToken);
            return Ok(receipt);
        }

        [HttpPost("costs")]
        public async Task<ActionResult<DeliveryCostEntry>> RecordCost([FromBody] RecordCostRequest request, CancellationToken cancellationToken)
        {
            var cost = await _service.RecordDeliveryCostAsync(request.TenantId, request.CustomerId, request.Category, request.AmountINR, request.Description, cancellationToken);
            return Ok(cost);
        }

        [HttpGet("customers/{customerId}/profitability")]
        public async Task<ActionResult<CustomerProfitabilityReport>> GetProfitability([FromQuery] string tenantId, string customerId, CancellationToken cancellationToken)
        {
            var rep = await _service.GetCustomerProfitabilityAsync(tenantId, customerId, cancellationToken);
            return Ok(rep);
        }

        [HttpGet("treasury")]
        public async Task<ActionResult<TreasuryCashPosition>> GetTreasury(
            [FromQuery] string tenantId,
            [FromQuery] decimal totalCashINR,
            [FromQuery] decimal monthlyOutflowINR,
            CancellationToken cancellationToken)
        {
            var pos = await _service.GetTreasuryPositionAsync(tenantId, totalCashINR, monthlyOutflowINR, cancellationToken);
            return Ok(pos);
        }
    }
}
