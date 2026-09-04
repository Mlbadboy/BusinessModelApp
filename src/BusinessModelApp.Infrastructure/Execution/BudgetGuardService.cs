using System;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BusinessModelApp.Core.Domain.Execution;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Infrastructure.Data;

namespace BusinessModelApp.Infrastructure.Execution
{
    public class BudgetGuardService : IBudgetGuardService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<BudgetGuardService> _logger;

        public BudgetGuardService(AppDbContext context, ILogger<BudgetGuardService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<TenantWalletEntity> GetOrCreateWalletAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        {
            var wallet = await _context.TenantWallets.FirstOrDefaultAsync(w => w.WorkspaceId == workspaceId, cancellationToken);
            if (wallet == null)
            {
                wallet = new TenantWalletEntity
                {
                    WorkspaceId = workspaceId,
                    BalanceINR = 1000000m,
                    AllocatedBudgetINR = 500000m,
                    DailyCapINR = 100000m,
                    TodaySpendINR = 0m,
                    PendingCommitmentsINR = 0m,
                    LastResetDateUtc = DateTime.UtcNow.Date,
                    UpdatedAtUtc = DateTime.UtcNow
                };
                _context.TenantWallets.Add(wallet);
                await _context.SaveChangesAsync(cancellationToken);
            }
            else
            {
                wallet.CheckAndResetDailySpend();
            }

            return wallet;
        }

        public async Task<bool> ValidateAndReserveBudgetAsync(
            Guid workspaceId,
            Guid requestId,
            decimal amountINR,
            CancellationToken cancellationToken = default)
        {
            if (amountINR <= 0m) return true; // Zero cost action

            // If already reserved for this request, avoid double-reserving
            var existing = await _context.BudgetReservations
                .FirstOrDefaultAsync(r => r.WorkspaceId == workspaceId && r.RequestId == requestId && !r.IsSettled && !r.IsReleased, cancellationToken);
            if (existing != null)
            {
                return true;
            }

            var wallet = await GetOrCreateWalletAsync(workspaceId, cancellationToken);

            // Check Daily Cap
            if (wallet.TodaySpendINR + wallet.PendingCommitmentsINR + amountINR > wallet.DailyCapINR)
            {
                _logger.LogWarning("Budget reservation DENIED: Daily cap exceeded for workspace {WorkspaceId}. Current: {Today}, Requested: {Amount}, Cap: {Cap}",
                    workspaceId, wallet.TodaySpendINR, amountINR, wallet.DailyCapINR);
                return false;
            }

            // Check Available Balance
            if (wallet.BalanceINR < (wallet.PendingCommitmentsINR + amountINR))
            {
                _logger.LogWarning("Budget reservation DENIED: Insufficient wallet balance for workspace {WorkspaceId}. Balance: {Balance}, Requested: {Amount}",
                    workspaceId, wallet.BalanceINR, amountINR);
                return false;
            }

            // Reserve funds atomically
            wallet.PendingCommitmentsINR += amountINR;
            wallet.UpdatedAtUtc = DateTime.UtcNow;

            var reservation = new BudgetReservationEntity
            {
                WorkspaceId = workspaceId,
                RequestId = requestId,
                AmountINR = amountINR,
                IsSettled = false,
                IsReleased = false,
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(15)
            };

            _context.BudgetReservations.Add(reservation);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Budget reserved: ₹{Amount} for request {RequestId} in workspace {WorkspaceId}", amountINR, requestId, workspaceId);
            return true;
        }

        public async Task<bool> SettleReservationAsync(Guid workspaceId, Guid requestId, CancellationToken cancellationToken = default)
        {
            var reservation = await _context.BudgetReservations
                .FirstOrDefaultAsync(r => r.WorkspaceId == workspaceId && r.RequestId == requestId && !r.IsSettled && !r.IsReleased, cancellationToken);

            if (reservation == null) return false;

            var wallet = await GetOrCreateWalletAsync(workspaceId, cancellationToken);

            wallet.PendingCommitmentsINR = Math.Max(0m, wallet.PendingCommitmentsINR - reservation.AmountINR);
            wallet.BalanceINR = Math.Max(0m, wallet.BalanceINR - reservation.AmountINR);
            wallet.TodaySpendINR += reservation.AmountINR;
            wallet.UpdatedAtUtc = DateTime.UtcNow;

            reservation.IsSettled = true;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Settled budget reservation ₹{Amount} for request {RequestId}", reservation.AmountINR, requestId);
            return true;
        }

        public async Task<bool> ReleaseReservationAsync(Guid workspaceId, Guid requestId, CancellationToken cancellationToken = default)
        {
            var reservation = await _context.BudgetReservations
                .FirstOrDefaultAsync(r => r.WorkspaceId == workspaceId && r.RequestId == requestId && !r.IsSettled && !r.IsReleased, cancellationToken);

            if (reservation == null) return false;

            var wallet = await GetOrCreateWalletAsync(workspaceId, cancellationToken);

            wallet.PendingCommitmentsINR = Math.Max(0m, wallet.PendingCommitmentsINR - reservation.AmountINR);
            wallet.UpdatedAtUtc = DateTime.UtcNow;

            reservation.IsReleased = true;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Released unconsumed budget reservation ₹{Amount} for request {RequestId}", reservation.AmountINR, requestId);
            return true;
        }

        public async Task<TenantWalletEntity> UpdateBudgetLimitsByHumanAsync(
            Guid workspaceId,
            decimal newDailyCapINR,
            decimal newAllocatedBudgetINR,
            Guid humanUserId,
            CancellationToken cancellationToken = default)
        {
            // HARD INVARIANT: Only authentic human user IDs can modify budgets. Agent IDs or empty GUIDs fail-closed.
            if (humanUserId == Guid.Empty)
            {
                throw new SecurityException("[Budget Guard] VIOLATION: Agent self-budget modification or unauthenticated budget increase is mathematically blocked. Fail-Closed.");
            }

            var wallet = await GetOrCreateWalletAsync(workspaceId, cancellationToken);

            wallet.DailyCapINR = newDailyCapINR;
            wallet.AllocatedBudgetINR = newAllocatedBudgetINR;
            wallet.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Budget limits updated by sovereign user {UserId} for workspace {WorkspaceId}: Cap=₹{Cap}, Allocated=₹{Alloc}",
                humanUserId, workspaceId, newDailyCapINR, newAllocatedBudgetINR);

            return wallet;
        }
    }
}
