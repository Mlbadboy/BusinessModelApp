using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Execution;

namespace BusinessModelApp.Core.Interfaces
{
    public interface IAuthorityDelegationService
    {
        Task<AuthorityDelegationEntity> GrantDelegationAsync(
            Guid workspaceId,
            string department,
            string agentId,
            string agentRole,
            List<string> allowedCapabilities,
            decimal dailyLimitINR,
            decimal perTxLimitINR,
            Guid grantedByUserId,
            DateTime? validUntilUtc = null,
            CancellationToken cancellationToken = default);

        Task<bool> RevokeDelegationAsync(Guid delegationId, Guid revokedByUserId, string reason, CancellationToken cancellationToken = default);

        Task<bool> ValidateDelegationAsync(
            Guid workspaceId,
            string agentId,
            string capabilityId,
            decimal amountINR,
            CancellationToken cancellationToken = default);

        Task<List<AuthorityDelegationEntity>> GetActiveDelegationsAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    }
}
