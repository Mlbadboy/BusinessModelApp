using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.AI.Governance;
using BusinessModelApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BusinessModelApp.Infrastructure.Services
{
    public interface IBudgetReservationService
    {
        Task<BudgetReservationResult> ReserveBudgetAsync(
            Guid orgId,
            Guid wsId,
            decimal estimatedMaxCost,
            CancellationToken ct = default);

        Task ReconcileReservationAsync(
            string reservationId,
            Guid orgId,
            Guid wsId,
            decimal actualCost,
            int inputTokens,
            int outputTokens,
            long latencyMs,
            bool cacheHit,
            bool fallbackOccurred,
            CancellationToken ct = default);

        Task<AIBudgetPolicy> GetOrCreateBudgetPolicyAsync(
            Guid orgId,
            Guid? wsId,
            CancellationToken ct = default);
    }

    public class BudgetReservationService : IBudgetReservationService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<BudgetReservationService> _logger;

        // In-flight reservation tracking: key = workspaceId, value = total in-flight reserved amount
        private static readonly ConcurrentDictionary<Guid, decimal> ActiveWorkspaceReservations = new ConcurrentDictionary<Guid, decimal>();
        private static readonly ConcurrentDictionary<string, (Guid WorkspaceId, decimal ReservedAmount, DateTime ExpiryUtc)> ActiveReservationTokens =
            new ConcurrentDictionary<string, (Guid, decimal, DateTime)>();

        public BudgetReservationService(AppDbContext context, ILogger<BudgetReservationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<AIBudgetPolicy> GetOrCreateBudgetPolicyAsync(Guid orgId, Guid? wsId, CancellationToken ct = default)
        {
            var policy = await _context.AIBudgetPolicies
                .FirstOrDefaultAsync(p => p.OrganizationId == orgId && p.WorkspaceId == wsId, ct);

            if (policy == null)
            {
                policy = new AIBudgetPolicy
                {
                    OrganizationId = orgId,
                    WorkspaceId = wsId,
                    MonthlyBudgetCap = 50000m,
                    DailyBudgetCap = 2500m,
                    MaxCostPerRequest = 50m,
                    WarningThresholdPercent = 80m,
                    EnforceHardCap = true
                };
                _context.AIBudgetPolicies.Add(policy);
                await _context.SaveChangesAsync(ct);
            }

            return policy;
        }

        public async Task<BudgetReservationResult> ReserveBudgetAsync(
            Guid orgId,
            Guid wsId,
            decimal estimatedMaxCost,
            CancellationToken ct = default)
        {
            var policy = await GetOrCreateBudgetPolicyAsync(orgId, wsId, ct);

            // Calculate month-to-date spent cost from fast derived AIUsageDaily ledger
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var firstDayOfMonth = new DateOnly(today.Year, today.Month, 1);

            var mtdSpent = await _context.AIUsageDailies
                .Where(u => u.WorkspaceId == wsId && u.Date >= firstDayOfMonth && u.Date <= today)
                .SumAsync(u => (decimal?)u.EstimatedCost, ct) ?? 0m;

            var inFlightReserved = ActiveWorkspaceReservations.GetOrAdd(wsId, 0m);
            var totalCommitted = mtdSpent + inFlightReserved;
            var remainingMonthly = Math.Max(0m, policy.MonthlyBudgetCap - totalCommitted);

            if (policy.EnforceHardCap && (totalCommitted + estimatedMaxCost) > policy.MonthlyBudgetCap)
            {
                _logger.LogWarning("AI Budget Exceeded for Workspace {WorkspaceId}. Spent: {Spent:C}, InFlight: {InFlight:C}, Cap: {Cap:C}, Attempted: {Attempted:C}",
                    wsId, mtdSpent, inFlightReserved, policy.MonthlyBudgetCap, estimatedMaxCost);

                return new BudgetReservationResult
                {
                    IsAllowed = false,
                    EstimatedCost = estimatedMaxCost,
                    RemainingMonthlyBudget = remainingMonthly,
                    PercentageConsumed = policy.MonthlyBudgetCap > 0 ? (totalCommitted / policy.MonthlyBudgetCap) * 100m : 100m,
                    RejectionReason = $"Monthly AI budget cap of {policy.MonthlyBudgetCap:C0} exceeded for this workspace."
                };
            }

            // Atomically register reservation
            var reservationId = Guid.NewGuid().ToString("N");
            ActiveReservationTokens[reservationId] = (wsId, estimatedMaxCost, DateTime.UtcNow.AddMinutes(5));

            ActiveWorkspaceReservations.AddOrUpdate(
                wsId,
                estimatedMaxCost,
                (key, existing) => existing + estimatedMaxCost);

            return new BudgetReservationResult
            {
                IsAllowed = true,
                ReservationId = reservationId,
                EstimatedCost = estimatedMaxCost,
                RemainingMonthlyBudget = Math.Max(0m, remainingMonthly - estimatedMaxCost),
                PercentageConsumed = policy.MonthlyBudgetCap > 0 ? ((totalCommitted + estimatedMaxCost) / policy.MonthlyBudgetCap) * 100m : 0m
            };
        }

        public async Task ReconcileReservationAsync(
            string reservationId,
            Guid orgId,
            Guid wsId,
            decimal actualCost,
            int inputTokens,
            int outputTokens,
            long latencyMs,
            bool cacheHit,
            bool fallbackOccurred,
            CancellationToken ct = default)
        {
            // 1. Release In-Flight Reservation
            if (ActiveReservationTokens.TryRemove(reservationId, out var reservation))
            {
                ActiveWorkspaceReservations.AddOrUpdate(
                    wsId,
                    0m,
                    (key, existing) => Math.Max(0m, existing - reservation.ReservedAmount));
            }

            // 2. Fast Upsert into AIUsageDaily ledger
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var dailyRecord = await _context.AIUsageDailies
                .FirstOrDefaultAsync(u => u.WorkspaceId == wsId && u.Date == today, ct);

            if (dailyRecord == null)
            {
                dailyRecord = new AIUsageDaily
                {
                    OrganizationId = orgId,
                    WorkspaceId = wsId,
                    Date = today,
                    RequestCount = 1,
                    InputTokens = inputTokens,
                    OutputTokens = outputTokens,
                    EstimatedCost = actualCost,
                    TotalLatencyMs = latencyMs,
                    CacheHits = cacheHit ? 1 : 0,
                    FallbackCount = fallbackOccurred ? 1 : 0
                };
                _context.AIUsageDailies.Add(dailyRecord);
            }
            else
            {
                dailyRecord.RequestCount += 1;
                dailyRecord.InputTokens += inputTokens;
                dailyRecord.OutputTokens += outputTokens;
                dailyRecord.EstimatedCost += actualCost;
                dailyRecord.TotalLatencyMs += latencyMs;
                if (cacheHit) dailyRecord.CacheHits += 1;
                if (fallbackOccurred) dailyRecord.FallbackCount += 1;
                dailyRecord.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(ct);
        }
    }
}
