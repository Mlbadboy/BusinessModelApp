using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Operations;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Production;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Production;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    /// <summary>
    /// Phase 4.8 Batch 481: Production Activation Red Team & Security Bounds Tests.
    /// </summary>
    public class Phase4Batch481ActivationRedTeamTests
    {
        [Fact]
        public async Task ActivationWithoutSignoff_And_BudgetExhaustion_FailClosed()
        {
            var store = new InMemoryProductionActivationKernelStore();
            var service = new ProductionActivationKernelService(store);

            await service.RequestTenantActivationAsync("tenant-attack", "BO-ATTACK", "Attack Entity LLC");

            // 1. Activation without PRG-1 signoff throws
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.AuthorizeTenantActivationAsync("tenant-attack", ""));

            // 2. Deploying config without signoff throws
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.DeployConfigVersionAsync("tenant-attack", "MALICIOUS-CONFIG", ""));

            // 3. Rollback without signoff throws
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.RollbackTenantConfigAsync("tenant-attack", 1, ""));

            // 4. Connector budget exhaustion marks connector degraded
            var connector = new ProductionConnectorRegistration
            {
                TenantId = "tenant-attack",
                ConnectorName = "StripeConnector",
                ExternalProvider = "Stripe",
                CapabilityScope = "PAYMENT_CHARGE",
                MonthlyBudgetCeilingINR = 10_000m
            };
            connector.UpdateHealth(ConnectorHealthStatus.ProductionEnabled);
            connector.RecordUsage(15_000m); // Over budget

            Assert.Equal(ConnectorHealthStatus.Degraded, connector.HealthStatus);
        }

        [Fact]
        public async Task MalformedDigestPromotion_Rejected()
        {
            var store = new InMemoryProductionActivationKernelStore();
            var service = new ProductionActivationKernelService(store);

            var malformedEvent = new ProductionRealityEvent
            {
                TenantId = "tenant-attack",
                BusinessObjectiveId = "BO-01",
                EventType = ProductionRealityEventType.PaymentSettled,
                EvidenceLevel = EpistemicEvidenceLevel.CounterpartyAttested,
                EvidenceDigestSha256 = "INVALID-SHORT-DIGEST" // Not 64 hex chars!
            };

            var result = await service.PromoteEvidenceAsync(malformedEvent, "AuditorAuthority");
            Assert.False(result.IsPromoted);
            Assert.Contains("Invalid SHA-256 cryptographic digest", result.PromotionRationale);
        }
    }
}
