using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth
{
    /// <summary>
    /// Reconciliation status for external revenue evidence.
    /// </summary>
    public enum ExternalReconciliationStatus
    {
        PendingVerification,
        Reconciled,
        Discrepant,
        Rejected
    }

    /// <summary>
    /// Fully loaded customer unit economics and contribution margin record (Law I41-E & I41-G).
    /// </summary>
    public sealed class CustomerUnitEconomicsRecord
    {
        public string CustomerId { get; }
        public string ContractId { get; }
        public decimal RealizedCashRevenue { get; }
        public decimal DirectDeliveryCosts { get; }
        public decimal AgentComputeAndTokenCosts { get; }
        public decimal SoftwareLicenseCosts { get; }
        public decimal TotalLoadedVariableCosts => DirectDeliveryCosts + AgentComputeAndTokenCosts + SoftwareLicenseCosts;
        public decimal GrossProfit => RealizedCashRevenue - DirectDeliveryCosts;
        public decimal GrossMarginPercent => RealizedCashRevenue > 0 ? (GrossProfit / RealizedCashRevenue) * 100m : 0m;
        public decimal NetContributionMargin => RealizedCashRevenue - TotalLoadedVariableCosts;
        public decimal ContributionMarginPercent => RealizedCashRevenue > 0 ? (NetContributionMargin / RealizedCashRevenue) * 100m : 0m;
        public DateTime CalculatedAtUtc { get; }

        public CustomerUnitEconomicsRecord(
            string customerId,
            string contractId,
            decimal realizedCashRevenue,
            decimal directDeliveryCosts,
            decimal agentComputeAndTokenCosts,
            decimal softwareLicenseCosts,
            DateTime? calculatedAtUtc = null)
        {
            if (string.IsNullOrWhiteSpace(customerId))
                throw new ArgumentException("Customer ID is required.", nameof(customerId));
            if (string.IsNullOrWhiteSpace(contractId))
                throw new ArgumentException("Contract ID is required.", nameof(contractId));
            if (realizedCashRevenue < 0)
                throw new ArgumentException("Realized cash revenue cannot be negative.", nameof(realizedCashRevenue));

            CustomerId = customerId;
            ContractId = contractId;
            RealizedCashRevenue = realizedCashRevenue;
            DirectDeliveryCosts = Math.Max(0, directDeliveryCosts);
            AgentComputeAndTokenCosts = Math.Max(0, agentComputeAndTokenCosts);
            SoftwareLicenseCosts = Math.Max(0, softwareLicenseCosts);
            CalculatedAtUtc = calculatedAtUtc ?? DateTime.UtcNow;
        }

        public bool IsSustainableUnderPolicy(UnitEconomicsPolicy policy) =>
            GrossMarginPercent >= policy.MinGrossMarginPercent &&
            ContributionMarginPercent >= policy.MinContributionMarginPercent;
    }

    /// <summary>
    /// Cohort-level CAC, LTV, and payback analysis record enforcing Law I41-N.
    /// </summary>
    public sealed class CohortLtvCacAnalysisRecord
    {
        public string CohortId { get; }
        public string CohortPeriod { get; }
        public int CustomersAcquiredCount { get; }
        public decimal TotalAcquisitionCost { get; }
        public decimal CacPerCustomer => CustomersAcquiredCount > 0 ? TotalAcquisitionCost / CustomersAcquiredCount : 0m;
        public decimal AverageAnnualRevenuePerAccount { get; }
        public decimal GrossMarginPercent { get; }
        public decimal AnnualChurnRatePercent { get; }
        public decimal EstimatedLtv { get; }
        public decimal LtvToCacRatio => CacPerCustomer > 0 ? EstimatedLtv / CacPerCustomer : 0m;
        public int PaybackPeriodMonths { get; }
        public bool IsEconomicallySustainable => LtvToCacRatio >= GrowthConstitutionalInvariants.MinLtvToCacRatio &&
                                                 PaybackPeriodMonths <= GrowthConstitutionalInvariants.MaxPaybackPeriodMonths;
        public DateTime ComputedAtUtc { get; }

        public CohortLtvCacAnalysisRecord(
            string cohortId,
            string cohortPeriod,
            int customersAcquiredCount,
            decimal totalAcquisitionCost,
            decimal averageAnnualRevenuePerAccount,
            decimal grossMarginPercent,
            decimal annualChurnRatePercent,
            int paybackPeriodMonths,
            DateTime? computedAtUtc = null)
        {
            if (string.IsNullOrWhiteSpace(cohortId))
                throw new ArgumentException("Cohort ID is required.", nameof(cohortId));
            if (customersAcquiredCount <= 0)
                throw new ArgumentException("Customers acquired count must be greater than zero.", nameof(customersAcquiredCount));
            if (totalAcquisitionCost < 0)
                throw new ArgumentException("Total acquisition cost cannot be negative.", nameof(totalAcquisitionCost));

            CohortId = cohortId;
            CohortPeriod = cohortPeriod ?? string.Empty;
            CustomersAcquiredCount = customersAcquiredCount;
            TotalAcquisitionCost = totalAcquisitionCost;
            AverageAnnualRevenuePerAccount = averageAnnualRevenuePerAccount;
            GrossMarginPercent = grossMarginPercent;
            AnnualChurnRatePercent = Math.Max(1.0m, annualChurnRatePercent); // Minimum 1% churn to prevent div by zero
            PaybackPeriodMonths = paybackPeriodMonths;
            ComputedAtUtc = computedAtUtc ?? DateTime.UtcNow;

            // LTV = (ARPA * Gross Margin %) / Churn Rate
            decimal annualMargin = AverageAnnualRevenuePerAccount * (GrossMarginPercent / 100m);
            EstimatedLtv = annualMargin / (AnnualChurnRatePercent / 100m);
        }

        public bool IsSustainableUnderPolicy(UnitEconomicsPolicy policy) =>
            policy.ValidateCohortEconomics(LtvToCacRatio, PaybackPeriodMonths, AnnualChurnRatePercent, out _);
    }

    /// <summary>
    /// Treasury health snapshot with cash reserves, runway, and autonomous budget adjustments.
    /// </summary>
    public sealed class TreasuryHealthSnapshot
    {
        public string SnapshotId { get; }
        public decimal TotalLiquidCashReserve { get; }
        public decimal MonthlyBurnRate { get; }
        public decimal MonthlyRealizedCashCollection { get; }
        public decimal NetMonthlyCashFlow => MonthlyRealizedCashCollection - MonthlyBurnRate;
        public decimal RunwayMonths => MonthlyBurnRate > 0 ? TotalLiquidCashReserve / MonthlyBurnRate : 999m;
        public decimal ApprovedAgentBudgetPool { get; }
        public decimal ApprovedCampaignBudgetPool { get; }
        public DateTime TimestampUtc { get; }

        public TreasuryHealthSnapshot(
            decimal totalLiquidCashReserve,
            decimal monthlyBurnRate,
            decimal monthlyRealizedCashCollection,
            decimal approvedAgentBudgetPool,
            decimal approvedCampaignBudgetPool,
            string? snapshotId = null,
            DateTime? timestampUtc = null)
        {
            SnapshotId = snapshotId ?? Guid.NewGuid().ToString("N");
            TotalLiquidCashReserve = Math.Max(0, totalLiquidCashReserve);
            MonthlyBurnRate = Math.Max(0, monthlyBurnRate);
            MonthlyRealizedCashCollection = Math.Max(0, monthlyRealizedCashCollection);
            ApprovedAgentBudgetPool = Math.Max(0, approvedAgentBudgetPool);
            ApprovedCampaignBudgetPool = Math.Max(0, approvedCampaignBudgetPool);
            TimestampUtc = timestampUtc ?? DateTime.UtcNow;
        }
    }

    /// <summary>
    /// External proof attestation anchor reconciling verified commercial revenue with external bank transactions.
    /// Strictly enforces the Epistemic Evidence Hierarchy (Level 0 through Level 6).
    /// </summary>
    public sealed class ExternalRevenueEvidenceAttestation
    {
        public string AttestationId { get; }
        public string InvoiceId { get; }
        public string CounterpartyId { get; }
        public decimal AttestedAmount { get; }
        public string Currency { get; }
        public string BankTransactionReference { get; }
        public string BankStatementDigestSha256 { get; }
        public string ThirdPartyProofRegistryDigest { get; }
        public EpistemicEvidenceLevel EpistemicLevel { get; }
        public ExternalReconciliationStatus Status { get; private set; }
        public string ReconciledByAuthority { get; private set; } = string.Empty;
        public DateTime AttestedAtUtc { get; }
        public DateTime? ReconciledAtUtc { get; private set; }

        public bool IsBankVerified => EpistemicLevel >= EpistemicEvidenceLevel.BankVerifiedCash && Status == ExternalReconciliationStatus.Reconciled;
        public bool IsRealizedRevenue => EpistemicLevel == EpistemicEvidenceLevel.RealizedRevenue && Status == ExternalReconciliationStatus.Reconciled;

        public ExternalRevenueEvidenceAttestation(
            string invoiceId,
            string counterpartyId,
            decimal attestedAmount,
            string currency,
            string bankTransactionReference,
            string bankStatementDigestSha256,
            string thirdPartyProofRegistryDigest,
            EpistemicEvidenceLevel epistemicLevel = EpistemicEvidenceLevel.BankVerifiedCash,
            string? attestationId = null,
            DateTime? attestedAtUtc = null)
        {
            if (string.IsNullOrWhiteSpace(invoiceId))
                throw new ArgumentException("Invoice ID is required.", nameof(invoiceId));
            if (string.IsNullOrWhiteSpace(counterpartyId))
                throw new ArgumentException("Counterparty ID is required.", nameof(counterpartyId));
            if (attestedAmount <= 0)
                throw new ArgumentException("Attested amount must be positive.", nameof(attestedAmount));
            if (string.IsNullOrWhiteSpace(bankTransactionReference))
                throw new ArgumentException("Bank transaction reference is required.", nameof(bankTransactionReference));
            if (string.IsNullOrWhiteSpace(bankStatementDigestSha256))
                throw new ArgumentException("Bank statement SHA-256 digest is required.", nameof(bankStatementDigestSha256));
            if (string.IsNullOrWhiteSpace(thirdPartyProofRegistryDigest))
                throw new ArgumentException("Third-party proof registry digest is required.", nameof(thirdPartyProofRegistryDigest));

            AttestationId = attestationId ?? Guid.NewGuid().ToString("N");
            InvoiceId = invoiceId;
            CounterpartyId = counterpartyId;
            AttestedAmount = attestedAmount;
            Currency = currency ?? "USD";
            BankTransactionReference = bankTransactionReference.Trim();
            BankStatementDigestSha256 = bankStatementDigestSha256.Trim();
            ThirdPartyProofRegistryDigest = thirdPartyProofRegistryDigest.Trim();
            EpistemicLevel = epistemicLevel;
            Status = ExternalReconciliationStatus.PendingVerification;
            AttestedAtUtc = attestedAtUtc ?? DateTime.UtcNow;
        }

        public void Reconcile(string authority, ExternalReconciliationStatus status)
        {
            if (string.IsNullOrWhiteSpace(authority))
                throw new ArgumentException("Reconciliation authority required.", nameof(authority));
            if (status == ExternalReconciliationStatus.PendingVerification)
                throw new ArgumentException("Target status cannot be PendingVerification.", nameof(status));

            Status = status;
            ReconciledByAuthority = authority.Trim();
            ReconciledAtUtc = DateTime.UtcNow;
        }
    }
}
