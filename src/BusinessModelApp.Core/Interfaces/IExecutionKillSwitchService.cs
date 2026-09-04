using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Execution;

namespace BusinessModelApp.Core.Interfaces
{
    public interface IExecutionKillSwitchService
    {
        Task<bool> IsHaltedAsync(
            Guid workspaceId,
            Guid? missionId = null,
            string? agentId = null,
            string? capabilityId = null,
            CancellationToken cancellationToken = default);

        Task<ExecutionKillSwitchEntity> TriggerKillSwitchAsync(
            ExecutionKillSwitchTier tier,
            Guid? workspaceId,
            string? targetIdentifier,
            string reason,
            Guid triggeredByUserId,
            CancellationToken cancellationToken = default);

        Task<bool> DeactivateKillSwitchAsync(Guid switchId, Guid deactivatedByUserId, CancellationToken cancellationToken = default);

        Task<List<ExecutionKillSwitchEntity>> GetActiveKillSwitchesAsync(Guid? workspaceId = null, CancellationToken cancellationToken = default);
    }
}
