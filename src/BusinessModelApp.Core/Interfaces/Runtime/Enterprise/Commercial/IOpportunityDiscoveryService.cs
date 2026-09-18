using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Commercial;

namespace BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Commercial
{
    public interface IOpportunityDiscoveryStore
    {
        Task SaveSignalAsync(MarketSignalItem signal);
        Task<IReadOnlyList<MarketSignalItem>> ListSignalsAsync(string tenantId);
        Task SaveIcpProfileAsync(ICPProfile profile);
        Task<ICPProfile?> GetIcpProfileAsync(string tenantId, string profileId);
        Task<IReadOnlyList<ICPProfile>> ListIcpProfilesAsync(string tenantId);
        Task SaveOpportunityAsync(GroundedOpportunity opportunity);
        Task<GroundedOpportunity?> GetOpportunityAsync(string tenantId, string opportunityId);
        Task<IReadOnlyList<GroundedOpportunity>> ListOpportunitiesAsync(string tenantId);
    }

    public interface IOpportunityDiscoveryService
    {
        Task<MarketSignalItem> IngestMarketSignalAsync(MarketSignalItem signal);
        Task<ICPProfile> CreateIcpProfileAsync(ICPProfile profile);
        Task<GroundedOpportunity> EvaluateSignalAgainstIcpAsync(string tenantId, string signalId, string profileId);
        Task<IReadOnlyList<GroundedOpportunity>> GetGroundedOpportunitiesAsync(string tenantId);
    }
}
