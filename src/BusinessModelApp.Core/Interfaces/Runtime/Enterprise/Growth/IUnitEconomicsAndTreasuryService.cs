using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth;

namespace BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Growth
{
    public interface IUnitEconomicsAndTreasuryStore
    {
        Task SavePolicyAsync(UnitEconomicsPolicy policy, CancellationToken cancellationToken = default);
        Task<UnitEconomicsPolicy?> GetPolicyAsync(string tenantId, string businessObjectiveId, CancellationToken cancellationToken = default);

        Task SaveCustomerUnitPnlAsync(CustomerUnitEconomicsRecord record, CancellationToken cancellationToken = default);
        Task<CustomerUnitEconomicsRecord?> GetCustomerUnitPnlAsync(string customerId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CustomerUnitEconomicsRecord>> ListCustomerUnitPnlsAsync(CancellationToken cancellationToken = default);

        Task SaveCohortAnalysisAsync(CohortLtvCacAnalysisRecord record, CancellationToken cancellationToken = default);
        Task<CohortLtvCacAnalysisRecord?> GetCohortAnalysisAsync(string cohortId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CohortLtvCacAnalysisRecord>> ListCohortAnalysesAsync(CancellationToken cancellationToken = default);

        Task SaveTreasurySnapshotAsync(TreasuryHealthSnapshot snapshot, CancellationToken cancellationToken = default);
        Task<TreasuryHealthSnapshot?> GetLatestTreasurySnapshotAsync(CancellationToken cancellationToken = default);

        Task SaveExternalAttestationAsync(ExternalRevenueEvidenceAttestation attestation, CancellationToken cancellationToken = default);
        Task<ExternalRevenueEvidenceAttestation?> GetExternalAttestationAsync(string attestationId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ExternalRevenueEvidenceAttestation>> ListExternalAttestationsAsync(CancellationToken cancellationToken = default);
    }

    public interface IUnitEconomicsAndTreasuryService
    {
        Task<UnitEconomicsPolicy> SetPolicyAsync(UnitEconomicsPolicy policy, CancellationToken cancellationToken = default);
        Task<UnitEconomicsPolicy> GetActivePolicyAsync(string tenantId, string businessObjectiveId, CancellationToken cancellationToken = default);

        Task<CustomerUnitEconomicsRecord> RecordCustomerUnitEconomicsAsync(
            string customerId,
            string contractId,
            decimal realizedCashRevenue,
            decimal directDeliveryCosts,
            decimal agentComputeAndTokenCosts,
            decimal softwareLicenseCosts,
            CancellationToken cancellationToken = default);

        Task<CohortLtvCacAnalysisRecord> AnalyzeCohortLtvCacAsync(
            string cohortId,
            string cohortPeriod,
            int customersAcquiredCount,
            decimal totalAcquisitionCost,
            decimal averageAnnualRevenuePerAccount,
            decimal grossMarginPercent,
            decimal annualChurnRatePercent,
            int paybackPeriodMonths,
            CancellationToken cancellationToken = default);

        Task<TreasuryHealthSnapshot> RecordTreasurySnapshotAsync(
            decimal totalLiquidCashReserve,
            decimal monthlyBurnRate,
            decimal monthlyRealizedCashCollection,
            decimal approvedAgentBudgetPool,
            decimal approvedCampaignBudgetPool,
            CancellationToken cancellationToken = default);

        Task<ExternalRevenueEvidenceAttestation> AttestExternalRevenueEvidenceAsync(
            string invoiceId,
            string counterpartyId,
            decimal attestedAmount,
            string currency,
            string bankTransactionReference,
            string bankStatementDigestSha256,
            string thirdPartyProofRegistryDigest,
            EpistemicEvidenceLevel epistemicLevel = EpistemicEvidenceLevel.BankVerifiedCash,
            CancellationToken cancellationToken = default);

        Task<ExternalRevenueEvidenceAttestation> ReconcileExternalEvidenceAsync(
            string attestationId,
            string authority,
            ExternalReconciliationStatus status,
            CancellationToken cancellationToken = default);

        Task<bool> VerifyRevenueRealizationIntegrityAsync(
            string attestationId,
            CancellationToken cancellationToken = default);
    }
}
