using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce;

namespace BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Workforce
{
    public interface IAgentEconomicsEngine
    {
        Task RecordCostAllocationAsync(string tenantId, CostAllocationRecord record);
        Task<AgentCostBreakdown> GetAgentCostBreakdownAsync(string tenantId, string agentId);
        Task<decimal> GetTotalMissionCostAsync(string tenantId, string missionId);

        Task RecordRevenueAttributionAsync(string tenantId, RevenueAttributionRecord record);
        Task<IReadOnlyList<RevenueAttributionRecord>> GetAgentRevenueAttributionsAsync(string tenantId, string agentId);

        Task UpdateAgentMetrologyPerformanceAsync(string tenantId, AgentMetrologyPerformance performance);
        Task<AgentMetrologyPerformance?> GetAgentMetrologyPerformanceAsync(string tenantId, string agentId);
        Task<IReadOnlyList<AgentMetrologyPerformance>> ListBestAgentsForRoutingAsync(string tenantId, string roleTitle);

        Task<EconomicOutcomeRecord> ComputeEconomicOutcomeAsync(string tenantId, string objectiveId);
    }
}
