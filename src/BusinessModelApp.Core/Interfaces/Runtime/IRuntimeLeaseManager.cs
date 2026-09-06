using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime;

namespace BusinessModelApp.Core.Interfaces.Runtime
{
    public interface IRuntimeLeaseManager
    {
        Task<LeaseAcquisitionResult> AcquireLeaseAsync(
            RuntimeRunId runId,
            ExecutionAttemptId attemptId,
            string workerId,
            Guid workspaceId,
            TimeSpan duration,
            CancellationToken cancellationToken = default);

        Task<LeaseAcquisitionResult> RenewLeaseAsync(
            RuntimeRunId runId,
            LeaseId leaseId,
            FenceToken token,
            TimeSpan duration,
            CancellationToken cancellationToken = default);

        Task ReleaseLeaseAsync(
            RuntimeRunId runId,
            LeaseId leaseId,
            FenceToken token,
            CancellationToken cancellationToken = default);

        Task<bool> ValidateFenceTokenAsync(
            RuntimeRunId runId,
            FenceToken token,
            CancellationToken cancellationToken = default);

        Task<FenceToken> GetCurrentFenceTokenAsync(
            RuntimeRunId runId,
            CancellationToken cancellationToken = default);
    }
}
