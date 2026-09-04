using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.ExternalReality;

namespace BusinessModelApp.Core.Interfaces
{
    public interface IExternalSourceRegistry
    {
        Task<ExternalSourceRegistryEntry> RegisterSourceAsync(ExternalSourceRegistryEntry entry, CancellationToken ct = default);
        Task<ExternalSourceRegistryEntry?> GetSourceAsync(Guid sourceId, Guid workspaceId, CancellationToken ct = default);
        Task<IReadOnlyList<ExternalSourceRegistryEntry>> ListSourcesAsync(Guid workspaceId, CancellationToken ct = default);
        Task<SourceTrustProfile> CalculateTrustProfileAsync(Guid sourceId, Guid workspaceId, CancellationToken ct = default);
        Task RecordRetrievalOutcomeAsync(Guid sourceId, bool success, string? failureDetails = null, CancellationToken ct = default);
        Task PenalizeSourceOnAnomalyAsync(Guid sourceId, Guid workspaceId, string reason, CancellationToken ct = default);
        Task RecordVerifiedCleanObservationAsync(Guid sourceId, Guid workspaceId, CancellationToken ct = default);

        Task<ExternalEvidenceRecord> IngestExternalEvidenceAsync(ExternalEvidenceRecord evidence, CancellationToken ct = default);
        Task<ExternalEvidenceRecord?> GetEvidenceAsync(Guid evidenceId, Guid workspaceId, CancellationToken ct = default);
        Task<IReadOnlyList<ExternalEvidenceRecord>> ListEvidenceAsync(Guid workspaceId, int limit = 50, CancellationToken ct = default);
        Task<IReadOnlyList<SignalCluster>> ClusterEvidenceAsync(Guid workspaceId, CancellationToken ct = default);
    }
}
