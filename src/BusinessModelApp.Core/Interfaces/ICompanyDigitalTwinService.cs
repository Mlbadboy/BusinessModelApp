using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.DigitalTwin;
using BusinessModelApp.Core.Domain.Reality;

namespace BusinessModelApp.Core.Interfaces
{
    /// <summary>
    /// Governed Service Contract for Charlie's Company Digital Twin.
    /// Provides deterministic, multi-tenant state projections, dimension inspection,
    /// field-level provenance, conflict tracking, reproducible snapshotting, and snapshot diffs.
    /// </summary>
    public interface ICompanyDigitalTwinService
    {
        Task<DigitalTwinState> GetCurrentStateAsync(Guid workspaceId, CancellationToken ct = default);
        Task<DigitalTwinDimensionState> GetDimensionAsync(Guid workspaceId, DigitalTwinDimension dimension, CancellationToken ct = default);
        Task<DigitalTwinFieldState?> GetFieldAsync(Guid workspaceId, string fieldPath, CancellationToken ct = default);
        Task<IReadOnlyList<DigitalTwinSnapshotSummary>> GetHistoryAsync(Guid workspaceId, CancellationToken ct = default);
        Task<IReadOnlyList<DigitalTwinConflictRecord>> GetConflictsAsync(Guid workspaceId, CancellationToken ct = default);
        Task<IReadOnlyList<DigitalTwinFieldState>> GetStaleDataAsync(Guid workspaceId, CancellationToken ct = default);
        Task<IReadOnlyList<DigitalTwinFieldState>> GetUnknownsAsync(Guid workspaceId, CancellationToken ct = default);
        Task<IReadOnlyList<EvidenceRecord>> GetEvidenceForFieldAsync(Guid workspaceId, string fieldPath, CancellationToken ct = default);
        Task<DigitalTwinSnapshot> CreateSnapshotAsync(Guid workspaceId, CancellationToken ct = default);
        Task<DigitalTwinDiff> CompareSnapshotsAsync(Guid workspaceId, Guid snapshotAId, Guid snapshotBId, CancellationToken ct = default);
        Task<DigitalTwinHealthReport> GetHealthReportAsync(Guid workspaceId, CancellationToken ct = default);
    }
}
