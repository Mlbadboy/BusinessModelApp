using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CommercialTransactionsController : ControllerBase
    {
        private readonly ICommercialTransactionEngine _transactionEngine;

        public CommercialTransactionsController(ICommercialTransactionEngine transactionEngine)
        {
            _transactionEngine = transactionEngine;
        }

        [HttpGet("quotes")]
        public async Task<IActionResult> GetQuotes([FromQuery] Guid? workspaceId)
        {
            var wsId = workspaceId ?? Guid.Empty;
            var quotes = await _transactionEngine.GetQuotesAsync(wsId);
            return Ok(new { success = true, quotes });
        }

        public class CreateQuoteRequest
        {
            public Guid? WorkspaceId { get; set; }
            public Guid OpportunityHypothesisId { get; set; }
            public decimal AmountINR { get; set; } = 125000m;
            public string Title { get; set; } = "PropTech Growth Engine";
            public List<string> Deliverables { get; set; } = new();
        }

        [HttpPost("quotes")]
        public async Task<IActionResult> CreateQuote([FromBody] CreateQuoteRequest request)
        {
            var wsId = request.WorkspaceId ?? Guid.Empty;
            var quote = await _transactionEngine.CreateProposalQuoteAsync(wsId, request.OpportunityHypothesisId, request.AmountINR, request.Title, request.Deliverables);
            return Ok(new { success = true, quote });
        }

        [HttpPost("quotes/{id}/authorize")]
        public async Task<IActionResult> AuthorizeQuote([FromRoute] Guid id)
        {
            var userEmail = User.Identity?.Name ?? "mayur@bitbloom.in";
            var quote = await _transactionEngine.AuthorizeAndSendQuoteAsync(id, userEmail);
            return Ok(new { success = true, quote });
        }

        public class PaymentRequest
        {
            public string Provider { get; set; } = "Razorpay";
        }

        [HttpPost("quotes/{id}/request-payment")]
        public async Task<IActionResult> RequestPayment([FromRoute] Guid id, [FromBody] PaymentRequest request)
        {
            var quote = await _transactionEngine.RequestPaymentAsync(id, request.Provider);
            return Ok(new { success = true, quote, paymentUrl = quote.PaymentUrl });
        }

        public class ConfirmPaymentRequest
        {
            public string TransactionReference { get; set; } = "pay_rzp_91823901";
        }

        [HttpPost("quotes/{id}/confirm-payment")]
        public async Task<IActionResult> ConfirmPayment([FromRoute] Guid id, [FromBody] ConfirmPaymentRequest request)
        {
            var quote = await _transactionEngine.ConfirmPaymentReceivedAsync(id, request.TransactionReference);
            return Ok(new { success = true, quote, status = "PaidAndClosed", message = "Payment confirmed. Delivery Swarm mission automatically launched!" });
        }
    }
}
