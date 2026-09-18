using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Commercial;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Commercial;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/commercial/operations")]
    [Authorize]
    public class CommercialOperationsController : ControllerBase
    {
        private readonly IOpportunityDiscoveryService _oppService;
        private readonly IOpportunityDiscoveryStore _oppStore;
        private readonly IProposalAndDealService _dealService;
        private readonly IProposalAndDealStore _dealStore;
        private readonly IDeliveryAndInvoiceService _deliveryService;
        private readonly IDeliveryAndInvoiceStore _deliveryStore;
        private readonly ICommercialWatchtowerService _watchtowerService;

        public CommercialOperationsController(
            IOpportunityDiscoveryService oppService,
            IOpportunityDiscoveryStore oppStore,
            IProposalAndDealService dealService,
            IProposalAndDealStore dealStore,
            IDeliveryAndInvoiceService deliveryService,
            IDeliveryAndInvoiceStore deliveryStore,
            ICommercialWatchtowerService watchtowerService)
        {
            _oppService = oppService ?? throw new ArgumentNullException(nameof(oppService));
            _oppStore = oppStore ?? throw new ArgumentNullException(nameof(oppStore));
            _dealService = dealService ?? throw new ArgumentNullException(nameof(dealService));
            _dealStore = dealStore ?? throw new ArgumentNullException(nameof(dealStore));
            _deliveryService = deliveryService ?? throw new ArgumentNullException(nameof(deliveryService));
            _deliveryStore = deliveryStore ?? throw new ArgumentNullException(nameof(deliveryStore));
            _watchtowerService = watchtowerService ?? throw new ArgumentNullException(nameof(watchtowerService));
        }

        private string GetTenantId() => User.FindFirst("tenant_id")?.Value ?? "default-tenant";

        [HttpGet("metrics")]
        public async Task<IActionResult> GetPipelineMetrics()
        {
            var metrics = await _watchtowerService.CalculatePipelineMetricsAsync(GetTenantId());
            return Ok(metrics);
        }

        [HttpGet("opportunities")]
        public async Task<IActionResult> ListOpportunities()
        {
            var opps = await _oppStore.ListOpportunitiesAsync(GetTenantId());
            return Ok(opps);
        }

        [HttpPost("opportunities")]
        public async Task<IActionResult> IngestOpportunity([FromBody] GroundedOpportunity opp)
        {
            opp.TenantId = GetTenantId();
            await _oppStore.SaveOpportunityAsync(opp);
            return Ok(opp);
        }

        [HttpGet("proposals")]
        public async Task<IActionResult> ListProposals()
        {
            var proposals = await _dealStore.ListProposalsAsync(GetTenantId());
            return Ok(proposals);
        }

        [HttpPost("proposals")]
        public async Task<IActionResult> CreateProposal([FromBody] CommercialProposal proposal)
        {
            proposal.TenantId = GetTenantId();
            var created = await _dealService.CreateProposalAsync(proposal);
            return Ok(created);
        }

        [HttpPost("proposals/{proposalId}/approve")]
        public async Task<IActionResult> ApproveProposal(string proposalId, [FromBody] ProposalApprovalRequest req)
        {
            var approved = await _dealService.ApproveProposalForSubmissionAsync(GetTenantId(), proposalId, req.HumanSignoffId);
            return Ok(new { success = approved });
        }

        [HttpGet("contracts")]
        public async Task<IActionResult> ListContracts()
        {
            var contracts = await _dealStore.ListContractsAsync(GetTenantId());
            return Ok(contracts);
        }

        [HttpPost("contracts")]
        public async Task<IActionResult> RecordContract([FromBody] AuthoritativeDealContract contract)
        {
            contract.TenantId = GetTenantId();
            var recorded = await _dealService.RecordAuthoritativeContractAsync(contract);
            return Ok(recorded);
        }

        [HttpGet("invoices")]
        public async Task<IActionResult> ListInvoices()
        {
            var invoices = await _deliveryStore.ListInvoicesAsync(GetTenantId());
            return Ok(invoices);
        }

        [HttpPost("invoices/{invoiceId}/issue")]
        public async Task<IActionResult> IssueInvoice(string invoiceId, [FromBody] InvoiceIssueRequest req)
        {
            var issued = await _deliveryService.IssueInvoiceWithBatch6PermitAsync(GetTenantId(), invoiceId, req.Batch6PermitId);
            return Ok(issued);
        }

        [HttpGet("receipts")]
        public async Task<IActionResult> ListReceipts()
        {
            var receipts = await _deliveryStore.ListReceiptsAsync(GetTenantId());
            return Ok(receipts);
        }

        [HttpPost("receipts")]
        public async Task<IActionResult> ProcessCashCollection([FromBody] CashCollectionRequest req)
        {
            var receipt = await _deliveryService.ProcessCashCollectionAsync(
                GetTenantId(),
                req.InvoiceId,
                req.CollectedAmountINR,
                req.BankReferenceNumber,
                req.GatewayOrRailId,
                req.BankConfirmationDigestSha256);

            return Ok(receipt);
        }

        [HttpGet("lineage/{receiptId}")]
        public async Task<IActionResult> TraceRevenueLineage(string receiptId)
        {
            var audit = await _watchtowerService.TraceRevenueLineageAsync(GetTenantId(), receiptId);
            return Ok(audit);
        }
    }

    public class ProposalApprovalRequest
    {
        public string HumanSignoffId { get; set; } = string.Empty;
    }

    public class InvoiceIssueRequest
    {
        public string Batch6PermitId { get; set; } = string.Empty;
    }

    public class CashCollectionRequest
    {
        public string InvoiceId { get; set; } = string.Empty;
        public decimal CollectedAmountINR { get; set; } = 0m;
        public string BankReferenceNumber { get; set; } = string.Empty;
        public string GatewayOrRailId { get; set; } = string.Empty;
        public string BankConfirmationDigestSha256 { get; set; } = string.Empty;
    }
}
