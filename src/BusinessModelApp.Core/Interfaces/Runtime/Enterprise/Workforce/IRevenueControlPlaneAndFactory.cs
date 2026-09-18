using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce;

namespace BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Workforce
{
    public interface IRevenueMissionFactory
    {
        Task<RevenueMissionSpecification> CreateRevenueMissionAsync(string tenantId, string opportunityId, RevenueMissionTemplateType templateType, decimal budget = 50m);
        Task<IReadOnlyList<RevenueMissionSpecification>> ListMissionsForOpportunityAsync(string tenantId, string opportunityId);
    }

    public interface IRevenueControlPlane
    {
        Task<RevenueControlPlaneState> GetCurrentStateAsync(string tenantId);
        Task UpdatePipelineMetricsAsync(string tenantId, decimal qualifiedPipeline, decimal weightedPipeline, decimal closedWon, decimal invoiced, decimal collected, decimal margin);
    }
}
