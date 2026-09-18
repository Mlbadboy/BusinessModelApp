using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.ControlTower;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Finance;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Lifecycle;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.ControlTower;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Finance;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Lifecycle;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Production;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Recovery;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    /// <summary>
    /// Phase 4.18 Batch 4180: Production Business Control Tower Domain Tests.
    /// </summary>
    public class Phase4Batch4180ControlTowerTests
    {
        private static string Sha256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        [Fact]
        public async Task ControlTower_ProjectsUnifiedTelemetryAcrossAllSubsystems()
        {
            var financeStore = new InMemoryFinancialRealityStore();
            var lifecycleStore = new InMemoryCustomerLifecycleRuntimeStore();
            var recoveryStore = new InMemoryAutonomousBusinessRecoveryStore();
            var incidentRecoveryService = new ProductionIncidentRecoveryService();

            var service = new BusinessControlTowerService(
                financeStore,
                lifecycleStore,
                recoveryStore,
                incidentRecoveryService);

            // Seed financial receipt
            await financeStore.SaveCashReceiptAsync(new BankCashReceipt
            {
                TenantId = "tenant-alpha",
                InvoiceId = "inv-01",
                AmountINR = 2_500_000m,
                BankWireReference = "UTR-99182371",
                BankStatementDigestSha256 = Sha256("bank-digest-data")
            }.Validate());

            // Seed customer account
            var cust = new CustomerAccount { TenantId = "tenant-alpha", CompanyName = "Enterprise Global" };
            cust.SetInitialARR(2_500_000m);
            await lifecycleStore.SaveAccountAsync(cust);

            var dashboard = await service.GetExecutiveDashboardAsync("tenant-alpha");

            Assert.Equal("tenant-alpha", dashboard.TenantId);
            Assert.Equal(2_500_000m, dashboard.TotalBankCashBalanceINR);
            Assert.Equal(2_500_000m, dashboard.TotalRealizedRevenueINR);
            Assert.Equal(1, dashboard.TotalActiveCustomers);
            Assert.Equal(0, dashboard.AtRiskCustomersCount);
            Assert.False(dashboard.KillSwitchEngaged);
            Assert.Equal(GrowthHealthGrade.Exemplary, dashboard.OverallHealthGrade);
        }
    }
}
