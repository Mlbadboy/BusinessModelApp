using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Lifecycle;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Lifecycle;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Lifecycle
{
    public sealed class InMemoryCustomerLifecycleRuntimeStore : ICustomerLifecycleRuntimeStore
    {
        private readonly ConcurrentDictionary<string, CustomerAccount> _accounts = new();
        private readonly ConcurrentDictionary<string, ChurnRiskAssessment> _assessments = new();
        private readonly ConcurrentDictionary<string, ExpansionOpportunity> _expansions = new();

        public Task SaveAccountAsync(CustomerAccount account, CancellationToken cancellationToken = default)
        {
            _accounts[account.CustomerId] = account;
            return Task.CompletedTask;
        }

        public Task<CustomerAccount?> GetAccountAsync(string customerId, CancellationToken cancellationToken = default)
        {
            _accounts.TryGetValue(customerId, out var acc);
            return Task.FromResult(acc);
        }

        public Task<IReadOnlyList<CustomerAccount>> ListAccountsForTenantAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            var list = _accounts.Values.Where(a => a.TenantId == tenantId).ToList();
            return Task.FromResult<IReadOnlyList<CustomerAccount>>(list);
        }

        public Task SaveAssessmentAsync(ChurnRiskAssessment assessment, CancellationToken cancellationToken = default)
        {
            _assessments[assessment.AssessmentId] = assessment;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ChurnRiskAssessment>> ListAssessmentsForCustomerAsync(string customerId, CancellationToken cancellationToken = default)
        {
            var list = _assessments.Values.Where(a => a.CustomerId == customerId).ToList();
            return Task.FromResult<IReadOnlyList<ChurnRiskAssessment>>(list);
        }

        public Task SaveExpansionAsync(ExpansionOpportunity expansion, CancellationToken cancellationToken = default)
        {
            _expansions[expansion.ExpansionId] = expansion;
            return Task.CompletedTask;
        }

        public Task<ExpansionOpportunity?> GetExpansionAsync(string expansionId, CancellationToken cancellationToken = default)
        {
            _expansions.TryGetValue(expansionId, out var exp);
            return Task.FromResult(exp);
        }

        public Task<IReadOnlyList<ExpansionOpportunity>> ListExpansionsForCustomerAsync(string customerId, CancellationToken cancellationToken = default)
        {
            var list = _expansions.Values.Where(e => e.CustomerId == customerId).ToList();
            return Task.FromResult<IReadOnlyList<ExpansionOpportunity>>(list);
        }
    }

    public sealed class CustomerLifecycleRuntimeService : ICustomerLifecycleRuntimeService
    {
        private readonly ICustomerLifecycleRuntimeStore _store;

        public CustomerLifecycleRuntimeService(ICustomerLifecycleRuntimeStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<CustomerAccount> RegisterAccountAsync(
            string tenantId,
            string companyName,
            decimal initialArrINR,
            CancellationToken cancellationToken = default)
        {
            var account = new CustomerAccount
            {
                TenantId = tenantId,
                CompanyName = companyName
            };
            account.SetInitialARR(initialArrINR);

            await _store.SaveAccountAsync(account, cancellationToken);
            return account;
        }

        public async Task<ChurnRiskAssessment> AssessChurnRiskAsync(
            string tenantId,
            string customerId,
            decimal churnProbability,
            string primaryRiskFactor,
            string rootCause,
            List<string> interventions,
            CancellationToken cancellationToken = default)
        {
            var account = await _store.GetAccountAsync(customerId, cancellationToken);
            if (account == null) throw new KeyNotFoundException($"Customer '{customerId}' not found.");

            var assessment = new ChurnRiskAssessment
            {
                TenantId = tenantId,
                CustomerId = customerId,
                ChurnProbability = Math.Clamp(churnProbability, 0m, 1m),
                PrimaryRiskFactor = primaryRiskFactor,
                RootCauseAnalysis = rootCause,
                ProposedInterventions = interventions ?? new List<string>()
            };

            // If probability > 0.50, update account status to AtRisk
            if (assessment.ChurnProbability > 0.50m)
            {
                account.UpdateHealth(1.0m - assessment.ChurnProbability, CustomerAccountStatus.AtRisk);
                assessment.TriggerIntervention();
            }
            else
            {
                account.UpdateHealth(1.0m - assessment.ChurnProbability, CustomerAccountStatus.Healthy);
            }

            await _store.SaveAssessmentAsync(assessment, cancellationToken);
            await _store.SaveAccountAsync(account, cancellationToken);
            return assessment;
        }

        public async Task<ExpansionOpportunity> IdentifyExpansionAsync(
            string tenantId,
            string customerId,
            string targetModule,
            decimal additionalArrINR,
            decimal confidenceScore,
            CancellationToken cancellationToken = default)
        {
            var account = await _store.GetAccountAsync(customerId, cancellationToken);
            if (account == null) throw new KeyNotFoundException($"Customer '{customerId}' not found.");

            var opportunity = new ExpansionOpportunity
            {
                TenantId = tenantId,
                CustomerId = customerId,
                TargetModule = targetModule,
                AdditionalARR_INR = additionalArrINR,
                ConfidenceScore = Math.Clamp(confidenceScore, 0m, 1m)
            };

            await _store.SaveExpansionAsync(opportunity, cancellationToken);
            return opportunity;
        }

        public async Task<ExpansionOpportunity> CloseWonExpansionAsync(
            string expansionId,
            string signedContractSha256,
            CancellationToken cancellationToken = default)
        {
            var opportunity = await _store.GetExpansionAsync(expansionId, cancellationToken);
            if (opportunity == null) throw new KeyNotFoundException($"Expansion '{expansionId}' not found.");

            opportunity.AdvanceStatus(ExpansionStatus.Won, signedContractSha256);

            var account = await _store.GetAccountAsync(opportunity.CustomerId, cancellationToken);
            if (account != null)
            {
                account.ApplyExpansion(opportunity.AdditionalARR_INR);
                await _store.SaveAccountAsync(account, cancellationToken);
            }

            await _store.SaveExpansionAsync(opportunity, cancellationToken);
            return opportunity;
        }

        public async Task<NetRetentionCalculation> CalculateRetentionMetricsAsync(
            string tenantId,
            decimal startingArrINR,
            decimal contractionArrINR,
            decimal churnArrINR,
            CancellationToken cancellationToken = default)
        {
            var accounts = await _store.ListAccountsForTenantAsync(tenantId, cancellationToken);
            var wonExpansions = new List<ExpansionOpportunity>();
            foreach (var acc in accounts)
            {
                var exps = await _store.ListExpansionsForCustomerAsync(acc.CustomerId, cancellationToken);
                wonExpansions.AddRange(exps.Where(e => e.Status == ExpansionStatus.Won));
            }

            var totalExpansionArr = wonExpansions.Sum(e => e.AdditionalARR_INR);

            return new NetRetentionCalculation
            {
                TenantId = tenantId,
                StartingARR_INR = startingArrINR,
                ExpansionARR_INR = totalExpansionArr,
                ContractionARR_INR = contractionArrINR,
                ChurnARR_INR = churnArrINR
            };
        }
    }
}
