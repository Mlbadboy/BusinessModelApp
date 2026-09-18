using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Finance;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Finance;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    /// <summary>
    /// Phase 4.12 Batch 4120: Autonomous Finance & Treasury Kernel Tests.
    /// </summary>
    public class Phase4Batch4120FinanceKernelTests
    {
        private static string Sha256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        [Fact]
        public async Task EndToEndFinancialFlow_InvoiceToBankReconciliationToContributionMargin()
        {
            var store = new InMemoryFinancialRealityStore();
            var service = new FinancialRealityKernelService(store);

            // 1. Draft commercial invoice for 1,000,000 INR + 18% GST (1,180,000 Total)
            var invoice = await service.DraftInvoiceAsync(
                "tenant-enterprise",
                "cust-acme-corp",
                "contract-msa-101",
                1_000_000m,
                DateTime.UtcNow.AddDays(30));

            Assert.Equal(InvoiceStatus.Draft, invoice.Status);
            Assert.Equal(180_000m, invoice.TaxAmountINR);
            Assert.Equal(1_180_000m, invoice.TotalAmountINR);

            // 2. Approve and Issue with Batch 6 Permit
            var issued = await service.ApproveAndIssueInvoiceAsync(invoice.InvoiceId, "BATCH6-PERMIT-FIN-9901");
            Assert.Equal(InvoiceStatus.Issued, issued.Status);
            Assert.Equal("BATCH6-PERMIT-FIN-9901", issued.Batch6PermitId);
            Assert.NotNull(issued.IssuedAtUtc);

            // 3. Reconcile Bank Wire Payment with SHA-256 statement digest (L5 Cash)
            var bankDigest = Sha256("HDFC-BANK-STATEMENT-LINE-UTR-991823719283-CONFIRMED-SETTLED");
            var receipt = await service.ReconcileBankPaymentAsync(
                invoice.InvoiceId,
                1_180_000m,
                "HDFCN991823719283",
                bankDigest);

            Assert.Equal(EpistemicEvidenceLevel.BankVerifiedCash, receipt.EvidenceLevel);
            Assert.Equal(InvoiceStatus.Paid, invoice.Status);
            Assert.NotNull(invoice.PaidAtUtc);

            // 4. Record loaded delivery costs (Compute, API fees)
            await service.RecordDeliveryCostAsync(
                "tenant-enterprise",
                "cust-acme-corp",
                DeliveryCostCategory.AgentCompute,
                65_000m,
                "Cluster LLM inference for monthly autonomous operation");

            await service.RecordDeliveryCostAsync(
                "tenant-enterprise",
                "cust-acme-corp",
                DeliveryCostCategory.PaymentProcessingFee,
                23_600m,
                "2% Gateway wire processing charges");

            // 5. Calculate customer-level gross contribution margin
            var report = await service.GetCustomerProfitabilityAsync("tenant-enterprise", "cust-acme-corp");
            Assert.Equal(1_180_000m, report.TotalInvoicedINR);
            Assert.Equal(1_180_000m, report.TotalCashCollectedINR);
            Assert.Equal(88_600m, report.TotalDeliveryCostINR);
            Assert.Equal(1_091_400m, report.GrossContributionMarginINR);
            Assert.True(report.GrossContributionMarginPct > 90m);

            // 6. Check Treasury cash position & runway
            var treasury = await service.GetTreasuryPositionAsync("tenant-enterprise", 10_000_000m, 500_000m);
            Assert.Equal(10_000_000m, treasury.TotalBankCashBalanceINR);
            Assert.Equal(1_180_000m, treasury.MonthlyCashInflowINR);
            Assert.Equal(0m, treasury.MonthlyNetBurnINR); // Inflow > Outflow -> Net burn = 0
            Assert.Equal(999m, treasury.RunwayMonths);
        }
    }
}
