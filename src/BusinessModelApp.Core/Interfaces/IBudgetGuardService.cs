using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Execution;

namespace BusinessModelApp.Core.Interfaces
{
    public interface IBudgetGuardService
    {
        Task<TenantWalletEntity> GetOrCreateWalletAsync(Guid workspaceId, CancellationToken cancellationToken = default);
        Task<bool> ValidateAndReserveBudgetAsync(Guid workspaceId, Guid requestId, decimal amountINR, CancellationToken cancellationToken = default);
        Task<bool> SettleReservationAsync(Guid workspaceId, Guid requestId, CancellationToken cancellationToken = default);
        Task<bool> ReleaseReservationAsync(Guid workspaceId, Guid requestId, CancellationToken cancellationToken = default);
        
        // Hard invariant: Agent cannot modify its own budget. Only human users with sovereign authority can modify caps.
        Task<TenantWalletEntity> UpdateBudgetLimitsByHumanAsync(
            Guid workspaceId,
            decimal newDailyCapINR,
            decimal newAllocatedBudgetINR,
            Guid humanUserId,
            CancellationToken cancellationToken = default);
    }
}
