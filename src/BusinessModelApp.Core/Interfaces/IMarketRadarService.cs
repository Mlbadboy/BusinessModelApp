using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.ExternalReality;

namespace BusinessModelApp.Core.Interfaces
{
    public interface IMarketRadarService
    {
        Task<ExternalSignal> DetectSignalAsync(ExternalSignal signal, CancellationToken ct = default);
        Task<IReadOnlyList<ExternalSignal>> GetActiveSignalsAsync(Guid workspaceId, ExternalSignalPriority? minPriority = null, CancellationToken ct = default);
        Task<ExternalSignal?> GetSignalAsync(Guid signalId, Guid workspaceId, CancellationToken ct = default);
        Task<CompetitorProfile> UpdateCompetitorProfileAsync(CompetitorProfile profile, CancellationToken ct = default);
        Task<IReadOnlyList<CompetitorProfile>> ListCompetitorsAsync(Guid workspaceId, CancellationToken ct = default);
        Task<CompetitorProfile?> GetCompetitorAsync(Guid competitorId, Guid workspaceId, CancellationToken ct = default);
        Task<MarketRegimeAssessment> AssessMarketRegimeAsync(Guid workspaceId, CancellationToken ct = default);
    }
}
