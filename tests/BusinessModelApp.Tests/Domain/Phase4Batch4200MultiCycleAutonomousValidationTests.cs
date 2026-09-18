using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Acquisition;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Allocation;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.ControlTower;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Finance;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Lifecycle;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Optimization;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Acquisition;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Allocation;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.ControlTower;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Finance;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Lifecycle;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Optimization;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Production;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Recovery;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    /// <summary>
    /// Phase 4.20 Batch 4200: Multi-Cycle Autonomous Business Validation.
    /// Validates repeated closed loops: Cycle 1 -> Outcome -> Causal Learning -> Bounded Adaptation -> Cycle 2 -> Expansion -> Cycle 3.
    /// </summary>
    public class Phase4Batch4200MultiCycleAutonomousValidationTests
    {
        private static string Sha256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        [Fact]
        public async Task MultiCycleAutonomousOperations_AcrossThreeCycles_DemonstratesSustainableSelfImprovingGrowth()
        {
            var acqStore = new InMemoryCustomerAcquisitionRuntimeStore();
            var acqService = new CustomerAcquisitionRuntimeService(acqStore);

            var lifeStore = new InMemoryCustomerLifecycleRuntimeStore();
            var lifeService = new CustomerLifecycleRuntimeService(lifeStore);

            var finStore = new InMemoryFinancialRealityStore();
            var finService = new FinancialRealityKernelService(finStore);

            var allocStore = new InMemoryBusinessResourceAllocationStore();
            var allocService = new BusinessResourceAllocationService(allocStore);

            var optStore = new InMemoryBusinessSelfOptimizationStore();
            var optService = new BusinessSelfOptimizationService(optStore);

            var recStore = new InMemoryAutonomousBusinessRecoveryStore();
            var towerService = new BusinessControlTowerService(finStore, lifeStore, recStore);

            // Configure Resource Budget for Compute and Marketing
            await allocService.ConfigureBudgetAsync("tenant-scale", ResourcePoolType.ComputeTokens, 5_000_000m, 20.0m);
            await allocService.ConfigureBudgetAsync("tenant-scale", ResourcePoolType.MarketingSpendINR, 500_000m, 10.0m);

            // ==========================================
            // CYCLE 1: Baseline Autonomous Acquisition & Operation
            // ==========================================
            var custA = await lifeService.RegisterAccountAsync("tenant-scale", "Customer Alpha", 1_000_000m);
            var inv1 = await finService.DraftInvoiceAsync("tenant-scale", custA.CustomerId, "MSA-01", 1_000_000m, DateTime.UtcNow.AddDays(30));
            await finService.ApproveAndIssueInvoiceAsync(inv1.InvoiceId, "PERMIT-B6-INV-1");
            await finService.ReconcileBankPaymentAsync(inv1.InvoiceId, inv1.TotalAmountINR, "UTR-C1-001", Sha256("bank-wire-digest-c1"));
            await finService.RecordDeliveryCostAsync("tenant-scale", custA.CustomerId, DeliveryCostCategory.AgentCompute, 50_000m, "Cycle 1 compute");

            var c1Dashboard = await towerService.GetExecutiveDashboardAsync("tenant-scale");
            Assert.Equal(1_180_000m, c1Dashboard.TotalBankCashBalanceINR);
            Assert.Equal(1, c1Dashboard.TotalActiveCustomers);

            // ==========================================
            // LEARNING & BOUNDED ADAPTATION
            // ==========================================
            var adaptation = await optService.ProposeAdaptationAsync(
                "tenant-scale",
                OptimizationDomain.AgentRouting,
                "InferenceConcurrencyScale",
                currentValue: 1.0m,
                proposedValue: 1.3m,
                causalConfidenceScore: 0.95m,
                empiricalObservationsCount: 150,
                safetyCeilingMin: 0.50m,
                safetyCeilingMax: 2.00m);

            var certified = await optService.EvaluateAndCertifyAsync(adaptation.ProposalId);
            Assert.Equal(AdaptationStatus.CertifiedSafe, certified.Status);
            await optService.ApplyAdaptationAsync(adaptation.ProposalId);

            // ==========================================
            // CYCLE 2: Second Customer + Expansion of Customer Alpha
            // ==========================================
            var custB = await lifeService.RegisterAccountAsync("tenant-scale", "Customer Beta", 1_500_000m);
            var inv2 = await finService.DraftInvoiceAsync("tenant-scale", custB.CustomerId, "MSA-02", 1_500_000m, DateTime.UtcNow.AddDays(30));
            await finService.ApproveAndIssueInvoiceAsync(inv2.InvoiceId, "PERMIT-B6-INV-2");
            await finService.ReconcileBankPaymentAsync(inv2.InvoiceId, inv2.TotalAmountINR, "UTR-C2-002", Sha256("bank-wire-digest-c2"));

            // Customer Alpha Expansion (+500,000 ARR)
            var expA = await lifeService.IdentifyExpansionAsync("tenant-scale", custA.CustomerId, "RealTimeLedgerSync", 500_000m, 0.95m);
            await lifeService.CloseWonExpansionAsync(expA.ExpansionId, Sha256("signed-expansion-contract-cust-a"));

            var c2Dashboard = await towerService.GetExecutiveDashboardAsync("tenant-scale");
            Assert.Equal(2, c2Dashboard.TotalActiveCustomers);
            Assert.Equal(2_950_000m, c2Dashboard.TotalBankCashBalanceINR); // 1.18M + 1.77M
            Assert.True(c2Dashboard.NetRevenueRetentionPct > 100m); // NRR Expansion verified

            // ==========================================
            // CYCLE 3: Sustained Multi-Tenant Autonomy
            // ==========================================
            var healthReport = await lifeService.CalculateRetentionMetricsAsync("tenant-scale", 2_500_000m, 0m, 0m);
            Assert.Equal(3_000_000m, healthReport.EndingARR_INR);
            Assert.Equal(120m, healthReport.NetRevenueRetentionPct);
            Assert.Equal(100m, healthReport.GrossRevenueRetentionPct);
        }
    }
}
