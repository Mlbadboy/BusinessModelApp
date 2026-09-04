using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.ExternalReality;

namespace BusinessModelApp.Core.Interfaces
{
    public interface IOpportunityIntelligenceService
    {
        Task<MarketOpportunity> CreateOpportunityAsync(MarketOpportunity opportunity, CancellationToken ct = default);
        Task<MarketOpportunity?> GetOpportunityAsync(Guid opportunityId, Guid workspaceId, CancellationToken ct = default);
        Task<IReadOnlyList<MarketOpportunity>> ListOpportunitiesAsync(Guid workspaceId, OpportunityStatus? status = null, CancellationToken ct = default);
        Task<CommercialOpportunityScore> CalculateCommercialScoreAsync(Guid opportunityId, Guid workspaceId, CancellationToken ct = default);
        Task<IReadOnlyList<ScenarioOutcome>> GenerateCounterfactualScenariosAsync(Guid opportunityId, Guid workspaceId, CancellationToken ct = default);
        Task<StrategicRecommendation> GenerateStrategicRecommendationAsync(Guid opportunityId, Guid workspaceId, CancellationToken ct = default);
        Task<StrategicRecommendation?> GetRecommendationAsync(Guid recommendationId, Guid workspaceId, CancellationToken ct = default);
        Task<IReadOnlyList<StrategicRecommendation>> ListRecommendationsAsync(Guid workspaceId, CancellationToken ct = default);
    }

    public interface IThreatIntelligenceService
    {
        Task<MarketThreat> RecordThreatAsync(MarketThreat threat, CancellationToken ct = default);
        Task<MarketThreat?> GetThreatAsync(Guid threatId, Guid workspaceId, CancellationToken ct = default);
        Task<IReadOnlyList<MarketThreat>> ListThreatsAsync(Guid workspaceId, CancellationToken ct = default);
    }
}
