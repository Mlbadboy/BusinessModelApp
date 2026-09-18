using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Multimodal;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Multimodal;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Multimodal
{
    public class ComputerEnvironmentModel : IEnvironmentModel
    {
        private readonly IComputerPerceptionService _perceptionService;
        private readonly ConcurrentDictionary<string, ComputerEnvironmentSnapshot> _snapshots = new();

        public ComputerEnvironmentModel(IComputerPerceptionService perceptionService)
        {
            _perceptionService = perceptionService ?? throw new ArgumentNullException(nameof(perceptionService));
        }

        public async Task<ComputerEnvironmentSnapshot> CreateSnapshotAsync(string tenantId, string sessionId, string application, string window, string? url = null)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId is required", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(sessionId)) throw new ArgumentException("SessionId is required", nameof(sessionId));

            var snapshot = await _perceptionService.ObserveEnvironmentAsync(tenantId, sessionId, application, window, url);
            _snapshots[snapshot.EnvironmentId] = snapshot;
            return snapshot;
        }

        public Task<ComputerEnvironmentSnapshot?> GetSnapshotAsync(string tenantId, string snapshotId)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId is required", nameof(tenantId));

            if (_snapshots.TryGetValue(snapshotId, out var snapshot))
            {
                if (snapshot.TenantId != tenantId)
                {
                    throw new UnauthorizedAccessException($"Tenant penetration defense: snapshot {snapshotId} does not belong to tenant {tenantId}");
                }
                return Task.FromResult<ComputerEnvironmentSnapshot?>(snapshot);
            }

            return Task.FromResult<ComputerEnvironmentSnapshot?>(null);
        }

        public bool VerifyEnvironmentIntegrity(ComputerEnvironmentSnapshot snapshot)
        {
            if (snapshot == null) return false;
            var expected = snapshot.ComputeIntegrityHash();
            return string.Equals(snapshot.IntegrityHash, expected, StringComparison.OrdinalIgnoreCase);
        }
    }
}
