using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Commercial;
using BusinessModelApp.Core.Services;

namespace BusinessModelApp.Infrastructure.Services
{
    public class CommercialTransactionEngine : ICommercialTransactionEngine
    {
        private static readonly ConcurrentDictionary<Guid, CommercialProposalQuote> _quotes = new();
        private readonly IDeliverySwarmService _deliverySwarm;

        public CommercialTransactionEngine(IDeliverySwarmService deliverySwarm)
        {
            _deliverySwarm = deliverySwarm;
            
            // Seed a sample paid and active quote
            if (_quotes.IsEmpty)
            {
                var sampleId = Guid.Parse("11111111-2222-3333-4444-555555555555");
                _quotes[sampleId] = new CommercialProposalQuote
                {
                    Id = sampleId,
                    WorkspaceId = Guid.Empty,
                    ClientName = "Apex Realty & Infrastructure Ltd",
                    ClientEmail = "director@apexrealtyinfra.in",
                    Title = "Modern PropTech Inbound Engine & WhatsApp CRM Automation",
                    ScopeOfWork = "Design, build, and deploy a headless real-estate portal with integrated lead CRM and WhatsApp automation.",
                    DeliverableItems = new List<string>
                    {
                        "Custom Headless Showcase Portal (Next.js)",
                        "Instant WhatsApp Lead Routing & Chatbot",
                        "Automated Real-Estate Opportunity Pipeline Sync",
                        "Speed Optimization (Lighthouse 95+)",
                        "Production Cloud Hosting & SSL Setup"
                    },
                    TotalAmountINR = 125000m,
                    Stage = ProposalQuoteStage.PaidAndClosed,
                    PaymentProvider = "Razorpay",
                    PaymentLinkId = "plink_Q918239102",
                    PaymentUrl = "https://rzp.io/l/charlie_apex_125k",
                    PaymentRequestedAt = DateTime.UtcNow.AddDays(-2),
                    PaidAt = DateTime.UtcNow.AddHours(-12),
                    EvidenceKey = "EVD-QUOTE-8172"
                };
            }
        }

        public Task<CommercialProposalQuote> CreateProposalQuoteAsync(Guid workspaceId, Guid opportunityHypothesisId, decimal amountINR, string title, List<string> deliverables)
        {
            var quote = new CommercialProposalQuote
            {
                WorkspaceId = workspaceId,
                OpportunityHypothesisId = opportunityHypothesisId,
                Title = title,
                ClientName = "Apex Realty Dynamics",
                ClientEmail = "management@apexrealty.in",
                ScopeOfWork = "Turnkey digital transformation package with guaranteed lead capture.",
                DeliverableItems = deliverables ?? new List<string> { "Modern Portal", "WhatsApp Funnel", "CRM Integration" },
                TotalAmountINR = amountINR > 0 ? amountINR : 125000m,
                Stage = ProposalQuoteStage.AwaitingExecutiveAuthorization,
                PaymentProvider = "Razorpay",
                EvidenceKey = $"EVD-QUOTE-{new Random().Next(1000, 9999)}"
            };

            _quotes[quote.Id] = quote;
            return Task.FromResult(quote);
        }

        public Task<CommercialProposalQuote> AuthorizeAndSendQuoteAsync(Guid quoteId, string approverEmail)
        {
            if (!_quotes.TryGetValue(quoteId, out var quote))
            {
                quote = new CommercialProposalQuote { Id = quoteId, TotalAmountINR = 125000m };
                _quotes[quoteId] = quote;
            }

            quote.Stage = ProposalQuoteStage.SentToClient;
            return Task.FromResult(quote);
        }

        public Task<CommercialProposalQuote> RequestPaymentAsync(Guid quoteId, string paymentProvider)
        {
            if (!_quotes.TryGetValue(quoteId, out var quote))
            {
                quote = new CommercialProposalQuote { Id = quoteId, TotalAmountINR = 125000m };
                _quotes[quoteId] = quote;
            }

            quote.Stage = ProposalQuoteStage.PaymentRequested;
            quote.PaymentProvider = paymentProvider ?? "Razorpay";
            quote.PaymentLinkId = $"plink_{Guid.NewGuid().ToString("N").Substring(0, 10)}";
            quote.PaymentUrl = $"https://rzp.io/l/charlie_{quote.PaymentLinkId}";
            quote.PaymentRequestedAt = DateTime.UtcNow;

            return Task.FromResult(quote);
        }

        public async Task<CommercialProposalQuote> ConfirmPaymentReceivedAsync(Guid quoteId, string transactionReference)
        {
            if (!_quotes.TryGetValue(quoteId, out var quote))
            {
                quote = new CommercialProposalQuote { Id = quoteId, TotalAmountINR = 125000m, ClientName = "Apex Realty Dynamics", Title = "PropTech Accelerator" };
                _quotes[quoteId] = quote;
            }

            quote.Stage = ProposalQuoteStage.PaidAndClosed;
            quote.PaidAt = DateTime.UtcNow;

            // Automatically launch Delivery Swarm mission upon confirmed payment!
            await _deliverySwarm.InitializeDeliveryMissionAsync(quote.WorkspaceId, quote.Id);

            return quote;
        }

        public Task<List<CommercialProposalQuote>> GetQuotesAsync(Guid workspaceId)
        {
            var list = _quotes.Values.Where(q => q.WorkspaceId == workspaceId || q.WorkspaceId == Guid.Empty).ToList();
            return Task.FromResult(list);
        }
    }
}
