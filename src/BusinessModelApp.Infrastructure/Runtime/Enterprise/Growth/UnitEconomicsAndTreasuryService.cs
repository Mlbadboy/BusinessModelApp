using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Growth;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Growth
{
    public sealed class InMemoryUnitEconomicsAndTreasuryStore : IUnitEconomicsAndTreasuryStore
    {
        private readonly ConcurrentDictionary<string, UnitEconomicsPolicy> _policies = new();
        private readonly ConcurrentDictionary<string, CustomerUnitEconomicsRecord> _customerPnls = new();
        private readonly ConcurrentDictionary<string, CohortLtvCacAnalysisRecord> _cohorts = new();
        private readonly List<TreasuryHealthSnapshot> _treasurySnapshots = new();
        private readonly ConcurrentDictionary<string, ExternalRevenueEvidenceAttestation> _attestations = new();
        private readonly object _lock = new();

        public Task SavePolicyAsync(UnitEconomicsPolicy policy, CancellationToken cancellationToken = default)
        {
            string key = $"{policy.TenantId}:{policy.BusinessObjectiveId}";
            _policies[key] = policy;
            return Task.CompletedTask;
        }

        public Task<UnitEconomicsPolicy?> GetPolicyAsync(string tenantId, string businessObjectiveId, CancellationToken cancellationToken = default)
        {
            string key = $"{tenantId}:{businessObjectiveId}";
            _policies.TryGetValue(key, out var policy);
            return Task.FromResult(policy);
        }

        public Task SaveCustomerUnitPnlAsync(CustomerUnitEconomicsRecord record, CancellationToken cancellationToken = default)
        {
            _customerPnls[record.CustomerId] = record;
            return Task.CompletedTask;
        }

        public Task<CustomerUnitEconomicsRecord?> GetCustomerUnitPnlAsync(string customerId, CancellationToken cancellationToken = default)
        {
            _customerPnls.TryGetValue(customerId, out var record);
            return Task.FromResult(record);
        }

        public Task<IReadOnlyList<CustomerUnitEconomicsRecord>> ListCustomerUnitPnlsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<CustomerUnitEconomicsRecord>>(_customerPnls.Values.ToList());
        }

        public Task SaveCohortAnalysisAsync(CohortLtvCacAnalysisRecord record, CancellationToken cancellationToken = default)
        {
            _cohorts[record.CohortId] = record;
            return Task.CompletedTask;
        }

        public Task<CohortLtvCacAnalysisRecord?> GetCohortAnalysisAsync(string cohortId, CancellationToken cancellationToken = default)
        {
            _cohorts.TryGetValue(cohortId, out var record);
            return Task.FromResult(record);
        }

        public Task<IReadOnlyList<CohortLtvCacAnalysisRecord>> ListCohortAnalysesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<CohortLtvCacAnalysisRecord>>(_cohorts.Values.ToList());
        }

        public Task SaveTreasurySnapshotAsync(TreasuryHealthSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                _treasurySnapshots.Add(snapshot);
            }
            return Task.CompletedTask;
        }

        public Task<TreasuryHealthSnapshot?> GetLatestTreasurySnapshotAsync(CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                var latest = _treasurySnapshots.OrderByDescending(s => s.TimestampUtc).FirstOrDefault();
                return Task.FromResult(latest);
            }
        }

        public Task SaveExternalAttestationAsync(ExternalRevenueEvidenceAttestation attestation, CancellationToken cancellationToken = default)
        {
            _attestations[attestation.AttestationId] = attestation;
            return Task.CompletedTask;
        }

        public Task<ExternalRevenueEvidenceAttestation?> GetExternalAttestationAsync(string attestationId, CancellationToken cancellationToken = default)
        {
            _attestations.TryGetValue(attestationId, out var attestation);
            return Task.FromResult(attestation);
        }

        public Task<IReadOnlyList<ExternalRevenueEvidenceAttestation>> ListExternalAttestationsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ExternalRevenueEvidenceAttestation>>(_attestations.Values.ToList());
        }
    }

    public sealed class UnitEconomicsAndTreasuryService : IUnitEconomicsAndTreasuryService
    {
        private readonly IUnitEconomicsAndTreasuryStore _store;

        public UnitEconomicsAndTreasuryService(IUnitEconomicsAndTreasuryStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<UnitEconomicsPolicy> SetPolicyAsync(UnitEconomicsPolicy policy, CancellationToken cancellationToken = default)
        {
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            await _store.SavePolicyAsync(policy, cancellationToken);
            return policy;
        }

        public async Task<UnitEconomicsPolicy> GetActivePolicyAsync(string tenantId, string businessObjectiveId, CancellationToken cancellationToken = default)
        {
            var policy = await _store.GetPolicyAsync(tenantId, businessObjectiveId, cancellationToken);
            if (policy != null) return policy;

            // Default fallback policy
            var defaultPolicy = UnitEconomicsPolicy.CreateDefaultSaaS(tenantId, businessObjectiveId);
            await _store.SavePolicyAsync(defaultPolicy, cancellationToken);
            return defaultPolicy;
        }

        public async Task<CustomerUnitEconomicsRecord> RecordCustomerUnitEconomicsAsync(
            string customerId,
            string contractId,
            decimal realizedCashRevenue,
            decimal directDeliveryCosts,
            decimal agentComputeAndTokenCosts,
            decimal softwareLicenseCosts,
            CancellationToken cancellationToken = default)
        {
            // Constitutional Law I41-E: "REVENUE != PROFIT". 
            // Growth without positive contribution margin is destruction of enterprise value.
            var record = new CustomerUnitEconomicsRecord(
                customerId,
                contractId,
                realizedCashRevenue,
                directDeliveryCosts,
                agentComputeAndTokenCosts,
                softwareLicenseCosts);

            await _store.SaveCustomerUnitPnlAsync(record, cancellationToken);
            return record;
        }

        public async Task<CohortLtvCacAnalysisRecord> AnalyzeCohortLtvCacAsync(
            string cohortId,
            string cohortPeriod,
            int customersAcquiredCount,
            decimal totalAcquisitionCost,
            decimal averageAnnualRevenuePerAccount,
            decimal grossMarginPercent,
            decimal annualChurnRatePercent,
            int paybackPeriodMonths,
            CancellationToken cancellationToken = default)
        {
            var record = new CohortLtvCacAnalysisRecord(
                cohortId,
                cohortPeriod,
                customersAcquiredCount,
                totalAcquisitionCost,
                averageAnnualRevenuePerAccount,
                grossMarginPercent,
                annualChurnRatePercent,
                paybackPeriodMonths);

            await _store.SaveCohortAnalysisAsync(record, cancellationToken);
            return record;
        }

        public async Task<TreasuryHealthSnapshot> RecordTreasurySnapshotAsync(
            decimal totalLiquidCashReserve,
            decimal monthlyBurnRate,
            decimal monthlyRealizedCashCollection,
            decimal approvedAgentBudgetPool,
            decimal approvedCampaignBudgetPool,
            CancellationToken cancellationToken = default)
        {
            var snapshot = new TreasuryHealthSnapshot(
                totalLiquidCashReserve,
                monthlyBurnRate,
                monthlyRealizedCashCollection,
                approvedAgentBudgetPool,
                approvedCampaignBudgetPool);

            await _store.SaveTreasurySnapshotAsync(snapshot, cancellationToken);
            return snapshot;
        }

        public async Task<ExternalRevenueEvidenceAttestation> AttestExternalRevenueEvidenceAsync(
            string invoiceId,
            string counterpartyId,
            decimal attestedAmount,
            string currency,
            string bankTransactionReference,
            string bankStatementDigestSha256,
            string thirdPartyProofRegistryDigest,
            EpistemicEvidenceLevel epistemicLevel = EpistemicEvidenceLevel.BankVerifiedCash,
            CancellationToken cancellationToken = default)
        {
            // Anchors revenue verification directly to external bank evidence and third-party proof registry digests
            var attestation = new ExternalRevenueEvidenceAttestation(
                invoiceId,
                counterpartyId,
                attestedAmount,
                currency,
                bankTransactionReference,
                bankStatementDigestSha256,
                thirdPartyProofRegistryDigest,
                epistemicLevel);

            await _store.SaveExternalAttestationAsync(attestation, cancellationToken);
            return attestation;
        }

        public async Task<ExternalRevenueEvidenceAttestation> ReconcileExternalEvidenceAsync(
            string attestationId,
            string authority,
            ExternalReconciliationStatus status,
            CancellationToken cancellationToken = default)
        {
            var attestation = await _store.GetExternalAttestationAsync(attestationId, cancellationToken);
            if (attestation == null)
                throw new KeyNotFoundException($"External revenue attestation '{attestationId}' not found.");

            attestation.Reconcile(authority, status);
            await _store.SaveExternalAttestationAsync(attestation, cancellationToken);
            return attestation;
        }

        public async Task<bool> VerifyRevenueRealizationIntegrityAsync(
            string attestationId,
            CancellationToken cancellationToken = default)
        {
            var attestation = await _store.GetExternalAttestationAsync(attestationId, cancellationToken);
            if (attestation == null) return false;

            // Strict Epistemic Invariant: Revenue realization requires EpistemicEvidenceLevel >= BankVerifiedCash (Level 5) and Reconciled status
            return attestation.EpistemicLevel >= EpistemicEvidenceLevel.BankVerifiedCash &&
                   attestation.Status == ExternalReconciliationStatus.Reconciled &&
                   !string.IsNullOrWhiteSpace(attestation.BankTransactionReference) &&
                   !string.IsNullOrWhiteSpace(attestation.BankStatementDigestSha256) &&
                   !string.IsNullOrWhiteSpace(attestation.ThirdPartyProofRegistryDigest);
        }
    }
}
