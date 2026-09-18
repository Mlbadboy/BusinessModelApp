using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Acquisition;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.ControlTower;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Delivery;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Finance;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Lifecycle;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Acquisition;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.ControlTower;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Delivery;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Finance;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Lifecycle;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Production;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Recovery;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    /// <summary>
    /// Phase 4.19 Batch 4190: Real-World Business Validation End-to-End Pipeline.
    /// Validates full chain from Acquisition -> Delivery -> Value -> Invoice -> L5 Bank Cash -> Realized Revenue.
    /// </summary>
    public class Phase4Batch4190RealWorldValidationTests
    {
        private static string Sha256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        [Fact]
        public async Task FullGovernedAutonomousBusinessPipeline_PassesCertification()
        {
            // 1. Initialize all subsystem runtimes
            var acqStore = new InMemoryCustomerAcquisitionRuntimeStore();
            var acqService = new CustomerAcquisitionRuntimeService(acqStore);

            var delivStore = new InMemoryCustomerDeliveryRuntimeStore();
            var delivService = new CustomerDeliveryRuntimeService(delivStore);

            var lifeStore = new InMemoryCustomerLifecycleRuntimeStore();
            var lifeService = new CustomerLifecycleRuntimeService(lifeStore);

            var finStore = new InMemoryFinancialRealityStore();
            var finService = new FinancialRealityKernelService(finStore);

            var recStore = new InMemoryAutonomousBusinessRecoveryStore();
            var recService = new AutonomousBusinessRecoveryService(recStore);
            var incidentRecoveryService = new ProductionIncidentRecoveryService();

            var towerService = new BusinessControlTowerService(finStore, lifeStore, recStore, incidentRecoveryService);

            // 2. Acquisition Step: Governed Outreach with Batch 6 permit
            var task = await acqService.PrepareOutreachAsync(
                "tenant-enterprise-01",
                "PROSPECT-ACME",
                "cfo@acme-global.com",
                "Autonomous Financial Ledger Close",
                "Pitching automated real-time bank reconciliation...",
                lastContactedUtc: null);

            var executedOutreach = await acqService.ExecuteGovernedOutreachAsync(
                task.TaskId,
                "BATCH6-PERMIT-OUTREACH-001",
                "SES-MESSAGE-REF-99812");
            Assert.Equal(OutreachDeliveryStatus.Sent, executedOutreach.DeliveryStatus);

            // 3. Customer Lifecycle Registration (Contract Signed)
            var customer = await lifeService.RegisterAccountAsync(
                "tenant-enterprise-01",
                "Acme Global Inc",
                initialArrINR: 1_200_000m);

            // 4. Delivery & Value Realization
            var m1 = new DeliveryMilestone { Title = "Platform Setup", TargetDateUtc = DateTime.UtcNow.AddDays(7) };
            var valueMetric = new ValueRealizationMetric
            {
                ProjectId = "p-01",
                MetricName = "MonthlyReconHoursSaved",
                BaselineValue = 0m,
                TargetValue = 100m,
                Unit = "Hours"
            };

            var project = await delivService.CreateProjectAsync(
                "tenant-enterprise-01",
                customer.CustomerId,
                "CONTRACT-MSA-ACME",
                new List<DeliveryMilestone> { m1 },
                new List<ValueRealizationMetric> { valueMetric });

            await delivService.AdvanceProjectStatusAsync(project.ProjectId, DeliveryProjectStatus.Onboarding);
            await delivService.AdvanceProjectStatusAsync(project.ProjectId, DeliveryProjectStatus.InProgress);

            m1.Complete(Sha256("artifact-platform-setup"));
            await delivService.AdvanceProjectStatusAsync(project.ProjectId, DeliveryProjectStatus.DeliveredPendingAcceptance);

            var signoffHash = Sha256("cfo-signoff-delivery-acceptance-cert");
            await delivService.AdvanceProjectStatusAsync(project.ProjectId, DeliveryProjectStatus.DeliveryAccepted, signoffHash);
            await delivService.AdvanceProjectStatusAsync(project.ProjectId, DeliveryProjectStatus.ValueRealizationUnderway);

            var datadogProof = Sha256("datadog-connector-telemetry-110-hours-verified");
            await delivService.RecordMetricMeasurementAsync(
                project.ProjectId,
                valueMetric.MetricId,
                110m,
                EpistemicEvidenceLevel.ConnectorObserved,
                datadogProof);

            await delivService.AdvanceProjectStatusAsync(project.ProjectId, DeliveryProjectStatus.ValueRealized);
            Assert.Equal(DeliveryProjectStatus.ValueRealized, project.Status);

            // 5. Invoicing & L5 Bank Cash Reconciliation
            var invoice = await finService.DraftInvoiceAsync(
                "tenant-enterprise-01",
                customer.CustomerId,
                "CONTRACT-MSA-ACME",
                1_200_000m,
                DateTime.UtcNow.AddDays(30));

            await finService.ApproveAndIssueInvoiceAsync(invoice.InvoiceId, "BATCH6-PERMIT-INVOICE-001");

            var bankStatementDigest = Sha256("HDFC-BANK-WIRE-SETTLEMENT-UTR-9918231");
            var receipt = await finService.ReconcileBankPaymentAsync(
                invoice.InvoiceId,
                invoice.TotalAmountINR,
                "HDFCN9918231",
                bankStatementDigest);

            Assert.Equal(EpistemicEvidenceLevel.BankVerifiedCash, receipt.EvidenceLevel);

            // 6. Record loaded compute/delivery cost
            await finService.RecordDeliveryCostAsync(
                "tenant-enterprise-01",
                customer.CustomerId,
                DeliveryCostCategory.AgentCompute,
                85_000m,
                "Full autonomous monthly inference");

            // 7. Executive Control Tower Projection
            var dashboard = await towerService.GetExecutiveDashboardAsync("tenant-enterprise-01");

            Assert.Equal("tenant-enterprise-01", dashboard.TenantId);
            Assert.Equal(1_416_000m, dashboard.TotalBankCashBalanceINR); // 1.2M + 18% GST
            Assert.Equal(1_331_000m, dashboard.GrossContributionMarginINR);
            Assert.Equal(1, dashboard.TotalActiveCustomers);
            Assert.False(dashboard.KillSwitchEngaged);
            Assert.Equal(GrowthHealthGrade.Exemplary, dashboard.OverallHealthGrade);
        }
    }
}
