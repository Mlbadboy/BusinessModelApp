using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Growth;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public class Phase4Batch452UnitEconomicsTests
    {
        [Fact]
        public async Task RecordCustomerUnitEconomics_CalculatesFullyLoadedPnlAndMargins()
        {
            var store = new InMemoryUnitEconomicsAndTreasuryStore();
            var service = new UnitEconomicsAndTreasuryService(store);

            // Revenue: $100,000
            // Direct Delivery: $20,000
            // Agent Compute & Tokens: $10,000
            // Software Licenses: $5,000
            // Total Loaded Variable Costs: $35,000
            // Gross Profit: $80,000 (80% margin)
            // Net Contribution Margin: $65,000 (65% margin)
            var pnl = await service.RecordCustomerUnitEconomicsAsync(
                "cust-001",
                "contract-001",
                realizedCashRevenue: 100_000m,
                directDeliveryCosts: 20_000m,
                agentComputeAndTokenCosts: 10_000m,
                softwareLicenseCosts: 5_000m);

            Assert.NotNull(pnl);
            Assert.Equal(100_000m, pnl.RealizedCashRevenue);
            Assert.Equal(35_000m, pnl.TotalLoadedVariableCosts);
            Assert.Equal(80_000m, pnl.GrossProfit);
            Assert.Equal(80.0m, pnl.GrossMarginPercent);
            Assert.Equal(65_000m, pnl.NetContributionMargin);
            Assert.Equal(65.0m, pnl.ContributionMarginPercent);
        }

        [Fact]
        public async Task RecordCustomerUnitEconomics_UnprofitableDelivery_ShowsNegativeContribution()
        {
            var store = new InMemoryUnitEconomicsAndTreasuryStore();
            var service = new UnitEconomicsAndTreasuryService(store);

            // Revenue: $10,000, Total Costs: $15,000 -> Negative Contribution -$5,000 (Law I41-E: Revenue != Profit)
            var pnl = await service.RecordCustomerUnitEconomicsAsync(
                "cust-unprofitable",
                "contract-002",
                realizedCashRevenue: 10_000m,
                directDeliveryCosts: 8_000m,
                agentComputeAndTokenCosts: 5_000m,
                softwareLicenseCosts: 2_000m);

            Assert.Equal(-5_000m, pnl.NetContributionMargin);
            Assert.True(pnl.NetContributionMargin < 0m);
        }

        [Fact]
        public async Task AnalyzeCohortLtvCac_SustainableCohort_PassesSovereignGrowthStandards()
        {
            var store = new InMemoryUnitEconomicsAndTreasuryStore();
            var service = new UnitEconomicsAndTreasuryService(store);

            // 10 customers acquired, $20,000 total spend -> CAC = $2,000/customer
            // ARPA = $24,000/year, Margin = 70%, Churn = 10%
            // Annual Margin = $16,800
            // LTV = $16,800 / 0.10 = $168,000
            // LTV : CAC = 168,000 / 2,000 = 84.0x (>> 3.0x)
            // Payback = 2 months (<= 12 months)
            var cohort = await service.AnalyzeCohortLtvCacAsync(
                "cohort-2026-q1",
                "2026-Q1",
                customersAcquiredCount: 10,
                totalAcquisitionCost: 20_000m,
                averageAnnualRevenuePerAccount: 24_000m,
                grossMarginPercent: 70m,
                annualChurnRatePercent: 10m,
                paybackPeriodMonths: 2);

            Assert.NotNull(cohort);
            Assert.Equal(2_000m, cohort.CacPerCustomer);
            Assert.Equal(168_000m, cohort.EstimatedLtv);
            Assert.Equal(84.0m, cohort.LtvToCacRatio);
            Assert.True(cohort.IsEconomicallySustainable);
        }

        [Fact]
        public async Task AnalyzeCohortLtvCac_UnsustainableCohort_ViolatesSovereignStandards()
        {
            var store = new InMemoryUnitEconomicsAndTreasuryStore();
            var service = new UnitEconomicsAndTreasuryService(store);

            // 5 customers acquired, $50,000 spend -> CAC = $10,000
            // ARPA = $10,000, Margin = 40%, Churn = 25%
            // LTV = 4,000 / 0.25 = $16,000
            // LTV : CAC = 16,000 / 10,000 = 1.6x (< 3.0x, violates Law I41-N)
            // Payback = 18 months (> 12 months, violates Law I41-N)
            var cohort = await service.AnalyzeCohortLtvCacAsync(
                "cohort-bad",
                "2026-Q2-Test",
                customersAcquiredCount: 5,
                totalAcquisitionCost: 50_000m,
                averageAnnualRevenuePerAccount: 10_000m,
                grossMarginPercent: 40m,
                annualChurnRatePercent: 25m,
                paybackPeriodMonths: 18);

            Assert.False(cohort.IsEconomicallySustainable);
            Assert.True(cohort.LtvToCacRatio < GrowthConstitutionalInvariants.MinLtvToCacRatio);
            Assert.True(cohort.PaybackPeriodMonths > GrowthConstitutionalInvariants.MaxPaybackPeriodMonths);
        }

        [Fact]
        public async Task RecordTreasurySnapshot_CalculatesNetCashFlowAndRunway()
        {
            var store = new InMemoryUnitEconomicsAndTreasuryStore();
            var service = new UnitEconomicsAndTreasuryService(store);

            // Liquid Reserves: $600,000
            // Monthly Burn: $50,000
            // Monthly Collection: $80,000
            // Net Cash Flow: +$30,000/month
            // Runway: 12 months
            var snapshot = await service.RecordTreasurySnapshotAsync(
                totalLiquidCashReserve: 600_000m,
                monthlyBurnRate: 50_000m,
                monthlyRealizedCashCollection: 80_000m,
                approvedAgentBudgetPool: 25_000m,
                approvedCampaignBudgetPool: 15_000m);

            Assert.NotNull(snapshot);
            Assert.Equal(30_000m, snapshot.NetMonthlyCashFlow);
            Assert.Equal(12.0m, snapshot.RunwayMonths);

            var latest = await store.GetLatestTreasurySnapshotAsync();
            Assert.NotNull(latest);
            Assert.Equal(snapshot.SnapshotId, latest!.SnapshotId);
        }

        [Fact]
        public async Task ExternalRevenueEvidenceAttestation_ReconciliationLifecycle()
        {
            var store = new InMemoryUnitEconomicsAndTreasuryStore();
            var service = new UnitEconomicsAndTreasuryService(store);

            // Attest external bank deposit evidence for $85,000 invoice
            var attestation = await service.AttestExternalRevenueEvidenceAsync(
                invoiceId: "INV-2026-0099",
                counterpartyId: "CP-ACME-GLOBAL",
                attestedAmount: 85_000m,
                currency: "USD",
                bankTransactionReference: "JPMC-WIRE-REF-99281729",
                bankStatementDigestSha256: "E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855",
                thirdPartyProofRegistryDigest: "PROOF_DIGEST_NOTARY_771928");

            Assert.NotNull(attestation);
            Assert.Equal(ExternalReconciliationStatus.PendingVerification, attestation.Status);

            // Reconcile via official corporate authority
            var reconciled = await service.ReconcileExternalEvidenceAsync(
                attestation.AttestationId,
                "CorporateTreasuryAuditor",
                ExternalReconciliationStatus.Reconciled);

            Assert.Equal(ExternalReconciliationStatus.Reconciled, reconciled.Status);
            Assert.Equal("CorporateTreasuryAuditor", reconciled.ReconciledByAuthority);
            Assert.NotNull(reconciled.ReconciledAtUtc);
        }

        [Fact]
        public async Task ExternalRevenueEvidenceAttestation_MissingFields_ThrowsArgumentException()
        {
            var store = new InMemoryUnitEconomicsAndTreasuryStore();
            var service = new UnitEconomicsAndTreasuryService(store);

            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await service.AttestExternalRevenueEvidenceAsync(
                    invoiceId: "",
                    counterpartyId: "CP",
                    attestedAmount: 1000m,
                    currency: "USD",
                    bankTransactionReference: "REF",
                    bankStatementDigestSha256: "DIGEST",
                    thirdPartyProofRegistryDigest: "REGISTRY");
            });

            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await service.AttestExternalRevenueEvidenceAsync(
                    invoiceId: "INV",
                    counterpartyId: "CP",
                    attestedAmount: -100m,
                    currency: "USD",
                    bankTransactionReference: "REF",
                    bankStatementDigestSha256: "DIGEST",
                    thirdPartyProofRegistryDigest: "REGISTRY");
            });
        }
    }
}
