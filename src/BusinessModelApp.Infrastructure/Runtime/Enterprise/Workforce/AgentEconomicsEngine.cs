using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Workforce;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Workforce
{
    public interface IAgentEconomicsStore
    {
        Task SaveCostRecordAsync(string tenantId, CostAllocationRecord record);
        Task<IReadOnlyList<CostAllocationRecord>> GetCostRecordsAsync(string tenantId, string? agentId = null, string? missionId = null);

        Task SaveAttributionRecordAsync(string tenantId, RevenueAttributionRecord record);
        Task<IReadOnlyList<RevenueAttributionRecord>> GetAttributionRecordsAsync(string tenantId, string? agentId = null);

        Task SavePerformanceAsync(string tenantId, AgentMetrologyPerformance performance);
        Task<AgentMetrologyPerformance?> GetPerformanceAsync(string tenantId, string agentId);
        Task<IReadOnlyList<AgentMetrologyPerformance>> ListPerformancesAsync(string tenantId);
    }

    public class InMemoryAgentEconomicsStore : IAgentEconomicsStore
    {
        private readonly ConcurrentDictionary<string, List<CostAllocationRecord>> _costsByTenant = new();
        private readonly ConcurrentDictionary<string, List<RevenueAttributionRecord>> _attributionsByTenant = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, AgentMetrologyPerformance>> _perfByTenant = new();

        public Task SaveCostRecordAsync(string tenantId, CostAllocationRecord record)
        {
            var list = _costsByTenant.GetOrAdd(tenantId, _ => new List<CostAllocationRecord>());
            lock (list)
            {
                list.Add(record);
            }
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<CostAllocationRecord>> GetCostRecordsAsync(string tenantId, string? agentId = null, string? missionId = null)
        {
            if (_costsByTenant.TryGetValue(tenantId, out var list))
            {
                lock (list)
                {
                    var q = list.AsEnumerable();
                    if (!string.IsNullOrWhiteSpace(agentId)) q = q.Where(c => c.AgentId == agentId);
                    if (!string.IsNullOrWhiteSpace(missionId)) q = q.Where(c => c.MissionId == missionId);
                    return Task.FromResult<IReadOnlyList<CostAllocationRecord>>(q.ToList());
                }
            }
            return Task.FromResult<IReadOnlyList<CostAllocationRecord>>(Array.Empty<CostAllocationRecord>());
        }

        public Task SaveAttributionRecordAsync(string tenantId, RevenueAttributionRecord record)
        {
            var list = _attributionsByTenant.GetOrAdd(tenantId, _ => new List<RevenueAttributionRecord>());
            lock (list)
            {
                list.Add(record);
            }
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<RevenueAttributionRecord>> GetAttributionRecordsAsync(string tenantId, string? agentId = null)
        {
            if (_attributionsByTenant.TryGetValue(tenantId, out var list))
            {
                lock (list)
                {
                    var q = list.AsEnumerable();
                    if (!string.IsNullOrWhiteSpace(agentId)) q = q.Where(a => a.AgentId == agentId);
                    return Task.FromResult<IReadOnlyList<RevenueAttributionRecord>>(q.ToList());
                }
            }
            return Task.FromResult<IReadOnlyList<RevenueAttributionRecord>>(Array.Empty<RevenueAttributionRecord>());
        }

        public Task SavePerformanceAsync(string tenantId, AgentMetrologyPerformance performance)
        {
            var map = _perfByTenant.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, AgentMetrologyPerformance>());
            map[performance.AgentId] = performance;
            return Task.CompletedTask;
        }

        public Task<AgentMetrologyPerformance?> GetPerformanceAsync(string tenantId, string agentId)
        {
            if (_perfByTenant.TryGetValue(tenantId, out var map) && map.TryGetValue(agentId, out var perf))
            {
                return Task.FromResult<AgentMetrologyPerformance?>(perf);
            }
            return Task.FromResult<AgentMetrologyPerformance?>(null);
        }

        public Task<IReadOnlyList<AgentMetrologyPerformance>> ListPerformancesAsync(string tenantId)
        {
            if (_perfByTenant.TryGetValue(tenantId, out var map))
            {
                return Task.FromResult<IReadOnlyList<AgentMetrologyPerformance>>(map.Values.ToList());
            }
            return Task.FromResult<IReadOnlyList<AgentMetrologyPerformance>>(Array.Empty<AgentMetrologyPerformance>());
        }
    }

    public class AgentEconomicsEngine : IAgentEconomicsEngine
    {
        private readonly IAgentEconomicsStore _store;

        public AgentEconomicsEngine(IAgentEconomicsStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task RecordCostAllocationAsync(string tenantId, CostAllocationRecord record)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId required", nameof(tenantId));
            if (record == null) throw new ArgumentNullException(nameof(record));

            record.TenantId = tenantId;
            record.IncurredAt = DateTime.UtcNow;
            await _store.SaveCostRecordAsync(tenantId, record);
        }

        public async Task<AgentCostBreakdown> GetAgentCostBreakdownAsync(string tenantId, string agentId)
        {
            var records = await _store.GetCostRecordsAsync(tenantId, agentId: agentId);
            var breakdown = new AgentCostBreakdown();

            foreach (var r in records)
            {
                switch (r.CostType)
                {
                    case "ModelInference":
                        breakdown.ModelInferenceCost += r.Amount;
                        break;
                    case "ToolUsage":
                        breakdown.ToolUsageCost += r.Amount;
                        break;
                    case "BrowserCompute":
                        breakdown.BrowserComputeCost += r.Amount;
                        break;
                    case "ApiCost":
                        breakdown.ApiCost += r.Amount;
                        break;
                    case "Infrastructure":
                        breakdown.InfrastructureCost += r.Amount;
                        break;
                    case "Storage":
                        breakdown.StorageCost += r.Amount;
                        break;
                    case "HumanAttention":
                        breakdown.HumanAttentionCost += r.Amount;
                        break;
                    default:
                        breakdown.ExternalServicesCost += r.Amount;
                        break;
                }
            }

            return breakdown;
        }

        public async Task<decimal> GetTotalMissionCostAsync(string tenantId, string missionId)
        {
            var records = await _store.GetCostRecordsAsync(tenantId, missionId: missionId);
            return records.Sum(r => r.Amount);
        }

        public async Task RecordRevenueAttributionAsync(string tenantId, RevenueAttributionRecord record)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId required", nameof(tenantId));
            if (record == null) throw new ArgumentNullException(nameof(record));

            record.TenantId = tenantId;
            record.RecordedAt = DateTime.UtcNow;
            await _store.SaveAttributionRecordAsync(tenantId, record);
        }

        public Task<IReadOnlyList<RevenueAttributionRecord>> GetAgentRevenueAttributionsAsync(string tenantId, string agentId)
        {
            return _store.GetAttributionRecordsAsync(tenantId, agentId);
        }

        public async Task UpdateAgentMetrologyPerformanceAsync(string tenantId, AgentMetrologyPerformance performance)
        {
            performance.TenantId = tenantId;
            performance.LastEvaluatedAt = DateTime.UtcNow;
            await _store.SavePerformanceAsync(tenantId, performance);
        }

        public Task<AgentMetrologyPerformance?> GetAgentMetrologyPerformanceAsync(string tenantId, string agentId)
        {
            return _store.GetPerformanceAsync(tenantId, agentId);
        }

        public async Task<IReadOnlyList<AgentMetrologyPerformance>> ListBestAgentsForRoutingAsync(string tenantId, string roleTitle)
        {
            var all = await _store.ListPerformancesAsync(tenantId);
            return all.OrderByDescending(p => p.RoutingScore).ToList();
        }

        public async Task<EconomicOutcomeRecord> ComputeEconomicOutcomeAsync(string tenantId, string objectiveId)
        {
            var allCosts = await _store.GetCostRecordsAsync(tenantId);
            var allAttributions = await _store.GetAttributionRecordsAsync(tenantId);

            var totalCollected = allAttributions.Sum(a => a.AttributedCollectedRevenue);
            var totalDelivery = allAttributions.Sum(a => a.AttributedDeliveryCost);
            var totalWorkforce = allCosts.Sum(c => c.Amount);
            var totalCosts = totalDelivery + totalWorkforce;
            var netMargin = totalCollected - totalCosts;
            var roi = totalCosts > 0 ? (netMargin / totalCosts) : (totalCollected > 0 ? 1.0m : 0.0m);

            return new EconomicOutcomeRecord
            {
                TenantId = tenantId,
                ObjectiveId = objectiveId,
                TotalCollectedRevenue = totalCollected,
                TotalDeliveryCost = totalDelivery,
                TotalWorkforceCost = totalWorkforce,
                NetGrossMargin = netMargin,
                RealizedROI = roi,
                CalculatedAt = DateTime.UtcNow
            };
        }
    }
}
