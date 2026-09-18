using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Lifecycle;

namespace BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Lifecycle
{
    public interface ICustomerLifecycleRuntimeStore
    {
        Task SaveAccountAsync(CustomerAccount account, CancellationToken cancellationToken = default);
        Task<CustomerAccount?> GetAccountAsync(string customerId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CustomerAccount>> ListAccountsForTenantAsync(string tenantId, CancellationToken cancellationToken = default);

        Task SaveAssessmentAsync(ChurnRiskAssessment assessment, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ChurnRiskAssessment>> ListAssessmentsForCustomerAsync(string customerId, CancellationToken cancellationToken = default);

        Task SaveExpansionAsync(ExpansionOpportunity expansion, CancellationToken cancellationToken = default);
        Task<ExpansionOpportunity?> GetExpansionAsync(string expansionId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ExpansionOpportunity>> ListExpansionsForCustomerAsync(string customerId, CancellationToken cancellationToken = default);
    }

    public interface ICustomerLifecycleRuntimeService
    {
        Task<CustomerAccount> RegisterAccountAsync(
            string tenantId,
            string companyName,
            decimal initialArrINR,
            CancellationToken cancellationToken = default);

        Task<ChurnRiskAssessment> AssessChurnRiskAsync(
            string tenantId,
            string customerId,
            decimal churnProbability,
            string primaryRiskFactor,
            string rootCause,
            List<string> interventions,
            CancellationToken cancellationToken = default);

        Task<ExpansionOpportunity> IdentifyExpansionAsync(
            string tenantId,
            string customerId,
            string targetModule,
            decimal additionalArrINR,
            decimal confidenceScore,
            CancellationToken cancellationToken = default);

        Task<ExpansionOpportunity> CloseWonExpansionAsync(
            string expansionId,
            string signedContractSha256,
            CancellationToken cancellationToken = default);

        Task<NetRetentionCalculation> CalculateRetentionMetricsAsync(
            string tenantId,
            decimal startingArrINR,
            decimal contractionArrINR,
            decimal churnArrINR,
            CancellationToken cancellationToken = default);
    }
}
