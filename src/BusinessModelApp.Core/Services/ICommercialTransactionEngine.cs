using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Commercial;

namespace BusinessModelApp.Core.Services
{
    public interface ICommercialTransactionEngine
    {
        Task<CommercialProposalQuote> CreateProposalQuoteAsync(Guid workspaceId, Guid opportunityHypothesisId, decimal amountINR, string title, List<string> deliverables);
        Task<CommercialProposalQuote> AuthorizeAndSendQuoteAsync(Guid quoteId, string approverEmail);
        Task<CommercialProposalQuote> RequestPaymentAsync(Guid quoteId, string paymentProvider);
        Task<CommercialProposalQuote> ConfirmPaymentReceivedAsync(Guid quoteId, string transactionReference);
        Task<List<CommercialProposalQuote>> GetQuotesAsync(Guid workspaceId);
    }

    public interface IDeliverySwarmService
    {
        Task<DeliveryMission> InitializeDeliveryMissionAsync(Guid workspaceId, Guid proposalQuoteId);
        Task<DeliveryMission> ExecuteDeliveryStepAsync(Guid deliveryMissionId);
        Task<DeliveryMission> GetDeliveryMissionAsync(Guid deliveryMissionId);
        Task<List<DeliveryMission>> GetActiveMissionsAsync(Guid workspaceId);
    }
}
