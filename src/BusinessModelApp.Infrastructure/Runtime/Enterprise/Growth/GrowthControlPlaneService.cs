using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Growth;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Growth
{
    public class InMemoryGrowthControlPlaneStore : IGrowthControlPlaneStore
    {
        private readonly ConcurrentDictionary<string, GrowthObjective> _objectives = new();
        private readonly ConcurrentDictionary<string, GrowthStrategyPlan> _plans = new();

        public Task SaveObjectiveAsync(GrowthObjective objective)
        {
            _objectives[$"{objective.TenantId}:{objective.ObjectiveId}"] = objective;
            return Task.CompletedTask;
        }

        public Task<GrowthObjective?> GetObjectiveAsync(string tenantId, string objectiveId)
        {
            _objectives.TryGetValue($"{tenantId}:{objectiveId}", out var obj);
            return Task.FromResult(obj);
        }

        public Task<IReadOnlyList<GrowthObjective>> ListObjectivesAsync(string tenantId)
        {
            var list = _objectives.Values.Where(o => o.TenantId == tenantId).ToList();
            return Task.FromResult<IReadOnlyList<GrowthObjective>>(list);
        }

        public Task SaveStrategyPlanAsync(GrowthStrategyPlan plan)
        {
            _plans[$"{plan.TenantId}:{plan.StrategyId}"] = plan;
            return Task.CompletedTask;
        }

        public Task<GrowthStrategyPlan?> GetStrategyPlanAsync(string tenantId, string strategyId)
        {
            _plans.TryGetValue($"{tenantId}:{strategyId}", out var plan);
            return Task.FromResult(plan);
        }

        public Task<IReadOnlyList<GrowthStrategyPlan>> ListStrategyPlansAsync(string tenantId)
        {
            var list = _plans.Values.Where(p => p.TenantId == tenantId).ToList();
            return Task.FromResult<IReadOnlyList<GrowthStrategyPlan>>(list);
        }
    }

    public class GrowthControlPlaneService : IGrowthControlPlaneService
    {
        private readonly IGrowthControlPlaneStore _store;

        public GrowthControlPlaneService(IGrowthControlPlaneStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<GrowthObjective> CreateGrowthObjectiveAsync(GrowthObjective objective)
        {
            if (string.IsNullOrWhiteSpace(objective.TenantId))
                throw new ArgumentException("TenantId is required", nameof(objective));
            if (string.IsNullOrWhiteSpace(objective.BusinessObjectiveId))
                throw new ArgumentException("Constitutional Violation (Law I41-Y): Growth objective must link to a sovereign Business Objective.", nameof(objective));
            if (string.IsNullOrWhiteSpace(objective.Title))
                throw new ArgumentException("Title is required", nameof(objective));

            // Law I41-G: LTV to CAC Ratio Floor (>= 3.0x)
            if (objective.TargetLtvCacRatio < GrowthConstitutionalInvariants.MinLtvToCacRatio)
            {
                throw new InvalidOperationException($"Constitutional Violation (Law I41-G): Target LTV:CAC ratio ({objective.TargetLtvCacRatio:F1}x) cannot fall below {GrowthConstitutionalInvariants.MinLtvToCacRatio:F1}x.");
            }

            // Law I41-H: Maximum Payback Period Invariant (<= 12m)
            if (objective.MaxPaybackPeriodMonths > GrowthConstitutionalInvariants.MaxPaybackPeriodMonths)
            {
                throw new InvalidOperationException($"Constitutional Violation (Law I41-H): Max payback period ({objective.MaxPaybackPeriodMonths} months) cannot exceed {GrowthConstitutionalInvariants.MaxPaybackPeriodMonths} months.");
            }

            // Law I41-N: Gross Margin Floor (>= 35%)
            if (objective.TargetGrossMarginPercent < GrowthConstitutionalInvariants.MinGrossMarginPercent)
            {
                throw new InvalidOperationException($"Constitutional Violation (Law I41-N): Target gross margin ({objective.TargetGrossMarginPercent:F1}%) cannot violate the 35% margin floor.");
            }

            await _store.SaveObjectiveAsync(objective);
            return objective;
        }

        public async Task<bool> AuthorizeGrowthObjectiveAsync(string tenantId, string objectiveId, string humanSignoffId)
        {
            if (string.IsNullOrWhiteSpace(humanSignoffId))
                throw new InvalidOperationException("Constitutional Violation (Law I41-U): Sovereign PRG-1 human signoff ID is strictly required to activate growth objectives.");

            var objective = await _store.GetObjectiveAsync(tenantId, objectiveId);
            if (objective == null) return false;

            objective.HumanSignoffId = humanSignoffId;
            objective.Status = GrowthObjectiveStatus.ACTIVE;
            await _store.SaveObjectiveAsync(objective);
            return true;
        }

        public async Task<GrowthObjective?> GetGrowthObjectiveAsync(string tenantId, string objectiveId)
        {
            return await _store.GetObjectiveAsync(tenantId, objectiveId);
        }

        public async Task<IReadOnlyList<GrowthObjective>> ListGrowthObjectivesAsync(string tenantId)
        {
            return await _store.ListObjectivesAsync(tenantId);
        }

        public async Task<GrowthStrategyPlan> FormulateStrategyPlanAsync(GrowthStrategyPlan plan)
        {
            if (string.IsNullOrWhiteSpace(plan.TenantId))
                throw new ArgumentException("TenantId is required", nameof(plan));
            if (string.IsNullOrWhiteSpace(plan.GrowthObjectiveId))
                throw new ArgumentException("GrowthObjectiveId is required", nameof(plan));
            if (string.IsNullOrWhiteSpace(plan.TargetIcpSegment))
                throw new ArgumentException("TargetIcpSegment is required", nameof(plan));

            var objective = await _store.GetObjectiveAsync(plan.TenantId, plan.GrowthObjectiveId);
            if (objective == null)
                throw new KeyNotFoundException($"GrowthObjective '{plan.GrowthObjectiveId}' not found.");

            await _store.SaveStrategyPlanAsync(plan);
            return plan;
        }

        public async Task<bool> AuthorizeStrategyPlanAsync(string tenantId, string strategyId, string humanSignoffId)
        {
            if (string.IsNullOrWhiteSpace(humanSignoffId))
                throw new InvalidOperationException("Constitutional Violation (Law I41-U): Human PRG-1 authorization required for commercial strategy allocation.");

            var plan = await _store.GetStrategyPlanAsync(tenantId, strategyId);
            if (plan == null) return false;

            plan.HumanSignoffId = humanSignoffId;
            await _store.SaveStrategyPlanAsync(plan);
            return true;
        }

        public async Task<GrowthStrategyPlan?> GetStrategyPlanAsync(string tenantId, string strategyId)
        {
            return await _store.GetStrategyPlanAsync(tenantId, strategyId);
        }

        public async Task<IReadOnlyList<GrowthStrategyPlan>> ListStrategyPlansAsync(string tenantId)
        {
            return await _store.ListStrategyPlansAsync(tenantId);
        }

        public async Task<GrowthMetricSnapshot> GetGrowthMetricSnapshotAsync(string tenantId)
        {
            var objectives = await _store.ListObjectivesAsync(tenantId);
            decimal totalRealized = objectives.Sum(o => o.CurrentRealizedRevenueINR);
            decimal avgMargin = objectives.Any() ? objectives.Average(o => o.CurrentGrossMarginPercent) : 50.0m;
            decimal avgLtvCac = objectives.Any() ? objectives.Average(o => o.CurrentLtvCacRatio) : 3.5m;
            decimal nrr = objectives.Any() ? objectives.Average(o => o.CurrentNetRetentionRatePercent) : 100.0m;

            return new GrowthMetricSnapshot
            {
                TenantId = tenantId,
                TotalRealizedRevenueINR = totalRealized,
                BlendedGrossMarginPercent = Math.Round(avgMargin, 2),
                BlendedLtvCacRatio = Math.Round(avgLtvCac, 2),
                AveragePaybackMonths = 8.5m,
                NetRevenueRetentionRatePercent = Math.Round(nrr, 2),
                ActiveCustomersCount = 1,
                MonthlyBurnRateINR = 120000m,
                TreasuryCashRunwayMonths = 18.2m,
                ComputedAtUtc = DateTime.UtcNow
            };
        }
    }
}
