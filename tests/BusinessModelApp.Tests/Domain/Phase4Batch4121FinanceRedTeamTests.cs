using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Finance;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Finance;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    /// <summary>
    /// Phase 4.12 Batch 4121: Autonomous Finance Red Team Tests (FIN01 - FIN04).
    /// </summary>
    public class Phase4Batch4121FinanceRedTeamTests
    {
        private static string Sha256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        [Fact]
        public async Task FIN01_IssueInvoiceWithoutBatch6Permit_FailsClosed()
        {
            var store = new InMemoryFinancialRealityStore();
            var service = new FinancialRealityKernelService(store);

            var invoice = await service.DraftInvoiceAsync(
                "tenant-alpha",
                "cust-01",
                "contract-01",
                500_000m,
                DateTime.UtcNow.AddDays(15));

            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                service.ApproveAndIssueInvoiceAsync(invoice.InvoiceId, ""));

            Assert.Contains("Batch 6 permit required", ex.Message);
        }

        [Fact]
        public async Task FIN02_ReconcilePaymentOnDraftInvoice_FailsClosed()
        {
            var store = new InMemoryFinancialRealityStore();
            var service = new FinancialRealityKernelService(store);

            var invoice = await service.DraftInvoiceAsync(
                "tenant-alpha",
                "cust-01",
                "contract-01",
                250_000m,
                DateTime.UtcNow.AddDays(15));

            // Attempting reconciliation while still Draft (not issued)
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ReconcileBankPaymentAsync(
                    invoice.InvoiceId,
                    250_000m,
                    "UTR123",
                    Sha256("bank-line")));

            Assert.Contains("Cannot reconcile payment for invoice in status 'Draft'", ex.Message);
        }

        [Fact]
        public async Task FIN03_ReconcilePaymentWithInvalidDigest_FailsClosed()
        {
            var store = new InMemoryFinancialRealityStore();
            var service = new FinancialRealityKernelService(store);

            var invoice = await service.DraftInvoiceAsync(
                "tenant-alpha",
                "cust-01",
                "contract-01",
                100_000m,
                DateTime.UtcNow.AddDays(15));

            await service.ApproveAndIssueInvoiceAsync(invoice.InvoiceId, "PERMIT-VALID-123");

            // Attempting reconciliation with invalid digest (not 64 chars)
            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                service.ReconcileBankPaymentAsync(
                    invoice.InvoiceId,
                    100_000m,
                    "UTR-VALID-881",
                    "short-fake-digest"));

            Assert.Contains("64-character SHA-256", ex.Message);
        }

        [Fact]
        public async Task FIN04_NegativeInvoiceSubtotalOrCost_ThrowsOutOfRange()
        {
            var store = new InMemoryFinancialRealityStore();
            var service = new FinancialRealityKernelService(store);

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                service.DraftInvoiceAsync("tenant-alpha", "cust-01", "contract-01", -10_000m, DateTime.UtcNow));

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                service.RecordDeliveryCostAsync("tenant-alpha", "cust-01", DeliveryCostCategory.AgentCompute, -500m, "Negative cost"));
        }
    }
}
