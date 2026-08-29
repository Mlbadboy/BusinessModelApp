using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Commercial;
using BusinessModelApp.Infrastructure.Services;
using Xunit;

namespace BusinessModelApp.Tests.Services
{
    public class CommercialTransactionAndDeliveryTests
    {
        private readonly DeliverySwarmService _deliverySwarm = new();
        private readonly CommercialTransactionEngine _transactionEngine;

        public CommercialTransactionAndDeliveryTests()
        {
            _transactionEngine = new CommercialTransactionEngine(_deliverySwarm);
        }

        [Fact]
        public async Task CommercialFlow_FromQuoteToPaymentToDeliverySwarm_ExecutesEndToEnd()
        {
            var wsId = Guid.NewGuid();
            var oppId = Guid.NewGuid();

            // 1. Create Proposal Quote
            var quote = await _transactionEngine.CreateProposalQuoteAsync(
                wsId, 
                oppId, 
                125000m, 
                "Apex Realty Inbound Engine", 
                new List<string> { "Portal", "WhatsApp Funnel", "CRM Sync" });

            Assert.NotNull(quote);
            Assert.Equal(ProposalQuoteStage.AwaitingExecutiveAuthorization, quote.Stage);
            Assert.Equal(125000m, quote.TotalAmountINR);

            // 2. Executive Authorizes Quote
            var authorized = await _transactionEngine.AuthorizeAndSendQuoteAsync(quote.Id, "mayur@bitbloom.in");
            Assert.Equal(ProposalQuoteStage.SentToClient, authorized.Stage);

            // 3. Request Live Payment Link (Razorpay)
            var paymentReq = await _transactionEngine.RequestPaymentAsync(quote.Id, "Razorpay");
            Assert.Equal(ProposalQuoteStage.PaymentRequested, paymentReq.Stage);
            Assert.StartsWith("https://rzp.io/l/", paymentReq.PaymentUrl);

            // 4. Confirm Payment Webhook Received
            var paid = await _transactionEngine.ConfirmPaymentReceivedAsync(quote.Id, "pay_rzp_test_12345");
            Assert.Equal(ProposalQuoteStage.PaidAndClosed, paid.Stage);
            Assert.NotNull(paid.PaidAt);

            // 5. Verify Delivery Mission was automatically launched
            var activeMissions = await _deliverySwarm.GetActiveMissionsAsync(wsId);
            Assert.NotEmpty(activeMissions);

            var mission = activeMissions[0];
            Assert.Equal(125000m, mission.ProjectValueINR);
            Assert.True(mission.OverallProgressPercentage >= 15);

            // 6. Step through Delivery Swarm execution
            var stepped = await _deliverySwarm.ExecuteDeliveryStepAsync(mission.Id);
            Assert.True(stepped.OverallProgressPercentage > 15);
        }
    }
}
