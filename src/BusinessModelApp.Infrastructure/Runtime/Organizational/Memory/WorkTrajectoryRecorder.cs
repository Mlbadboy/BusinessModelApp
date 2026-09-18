using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Organizational;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Memory
{
    public class WorkTrajectoryRecorder : IWorkTrajectoryRecorder
    {
        private readonly IOrganizationalMemoryStore _store;

        public WorkTrajectoryRecorder(IOrganizationalMemoryStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<OrganizationalTrajectory> StartTrajectoryAsync(
            string tenantId,
            string responsibilityId,
            string proposalId,
            string workId,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId is required.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(workId)) throw new ArgumentException("WorkId is required.", nameof(workId));

            var trajectory = new OrganizationalTrajectory
            {
                TrajectoryId = $"TRAJ-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                TenantId = tenantId,
                ResponsibilityId = responsibilityId,
                WorkProposalId = proposalId,
                WorkId = workId,
                StartedUtc = DateTime.UtcNow
            };

            trajectory.ComputeProvenance();
            await _store.SaveTrajectoryAsync(trajectory, ct);
            return trajectory;
        }

        public async Task RecordMilestoneAsync(
            string tenantId,
            string workId,
            Action<OrganizationalTrajectory> updateAction,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId is required.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(workId)) throw new ArgumentException("WorkId is required.", nameof(workId));
            if (updateAction == null) throw new ArgumentNullException(nameof(updateAction));

            var trajectory = await _store.GetTrajectoryForWorkAsync(tenantId, workId, ct);
            if (trajectory == null)
            {
                // Auto-create if not yet started
                trajectory = await StartTrajectoryAsync(tenantId, "UNKNOWN", "UNKNOWN", workId, ct);
            }

            updateAction(trajectory);
            trajectory.ComputeProvenance();
            await _store.SaveTrajectoryAsync(trajectory, ct);
        }
    }
}
