using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Agents;

namespace BusinessModelApp.Core.Connectors
{
    public interface IWebSearchConnector
    {
        Task<List<string>> SearchMarketSignalsAsync(string query, int maxResults = 5, CancellationToken ct = default);
    }

    public interface ICompanyIntelligenceConnector
    {
        Task<AccountGraph> BuildAccountGraphAsync(string companyDomain, CancellationToken ct = default);
    }

    public interface IProspectDiscoveryConnector
    {
        Task<List<DecisionMakerProfile>> DiscoverDecisionMakersAsync(string companyDomain, ExecutivePersona[] targetPersonas, CancellationToken ct = default);
    }

    public interface IEmailCommunicationConnector
    {
        Task<bool> SendGovernedEmailAsync(Guid tenantId, string recipientEmail, string subject, string body, string evidenceGroundingId, CancellationToken ct = default);
        Task<List<string>> CheckIncomingResponsesAsync(Guid tenantId, string threadId, CancellationToken ct = default);
    }

    public interface ICalendarSchedulingConnector
    {
        Task<List<DateTime>> GetAvailableSlotsAsync(Guid tenantId, int daysAhead = 5, CancellationToken ct = default);
        Task<MeetingBrief> BookMeetingAsync(Guid tenantId, string companyName, string executiveEmail, DateTime slot, List<string> evidenceTokens, CancellationToken ct = default);
    }

    public interface IProposalEngineConnector
    {
        Task<string> DraftContractProposalAsync(Guid tenantId, string companyName, decimal amountINR, string scopeOfWork, CancellationToken ct = default);
    }
}
