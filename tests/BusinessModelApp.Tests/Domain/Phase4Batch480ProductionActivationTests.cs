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
    /// Phase 4.8 Batch 480: Production Business Activation & Configuration Lifecycle Tests.
    /// </summary>
    public class Phase4Batch480ProductionActivationTests
    {
        [Fact]
        public async Task TenantActivation_FullLifecycle_And_ConfigRollback_Succeeds()
        {
            var store = new InMemoryProductionActivationKernelStore();
            var service = new ProductionActivationKernelService(store);

            // 1. Request tenant activation
            var activation = await service.RequestTenantActivationAsync(
                "tenant-production-alpha", "BO-ENTERPRISE-2026", "Alpha Corp International LLC");

            Assert.Equal(ProductionActivationStatus.PendingApproval, activation.Status);
            Assert.Equal("tenant-production-alpha", activation.TenantId);

            // 2. Authorize via PRG-1 executive human signoff
            var active = await service.AuthorizeTenantActivationAsync(
                "tenant-production-alpha", "PRG1-SIGNOFF-EXEC-BOARD-001");

            Assert.Equal(ProductionActivationStatus.Active, active.Status);
            Assert.Equal("PRG1-SIGNOFF-EXEC-BOARD-001", active.HumanSignoffId);

            // 3. Register production connector with budget & rate limit
            var connector = await service.RegisterConnectorAsync(new ProductionConnectorRegistration
            {
                TenantId = "tenant-production-alpha",
                ConnectorName = "FedwireCorporateConnector",
                ExternalProvider = "JPMorganChase",
                CapabilityScope = "BANK_WIRE_RECONCILE",
                RateLimitPerMinute = 120,
                MonthlyBudgetCeilingINR = 100_000m
            });

            Assert.Equal(ConnectorHealthStatus.ProductionEnabled, connector.HealthStatus);

            // 4. Deploy Version 1 and Version 2 configs
            var v1 = await service.DeployConfigVersionAsync(
                "tenant-production-alpha", "CONFIG-PAYLOAD-V1", "PRG1-AUTH-001");
            Assert.Equal(1, v1.VersionNumber);

            var v2 = await service.DeployConfigVersionAsync(
                "tenant-production-alpha", "CONFIG-PAYLOAD-V2", "PRG1-AUTH-002");
            Assert.Equal(2, v2.VersionNumber);

            // 5. Rollback to Version 1
            var rolledBack = await service.RollbackTenantConfigAsync(
                "tenant-production-alpha", 1, "PRG1-ROLLBACK-AUTH-003");

            Assert.Equal(ProductionActivationStatus.RolledBack, rolledBack.Status);
            Assert.Equal(1, rolledBack.ActiveConfigVersionNumber);
        }

        [Fact]
        public async Task EvidencePromotion_StrictEpistemicValidation_Succeeds()
        {
            var store = new InMemoryProductionActivationKernelStore();
            var service = new ProductionActivationKernelService(store);

            // 1. Attempt to promote Level 0 Fixture -> MUST BE REJECTED
            var fixtureEvent = new ProductionRealityEvent
            {
                TenantId = "tenant-production-alpha",
                BusinessObjectiveId = "BO-01",
                EventType = ProductionRealityEventType.PaymentSettled,
                EvidenceLevel = EpistemicEvidenceLevel.InternalFixture,
                EvidenceDigestSha256 = "1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF"
            };

            var rejectedResult = await service.PromoteEvidenceAsync(fixtureEvent, "AuditorAuthority");
            Assert.False(rejectedResult.IsPromoted);
            Assert.Contains("Lower evidence levels", rejectedResult.PromotionRationale);

            // 2. Promote Level 4 Counterparty-Attested with 64-char SHA256 digest -> PASSES
            var attestedEvent = new ProductionRealityEvent
            {
                TenantId = "tenant-production-alpha",
                BusinessObjectiveId = "BO-01",
                EventType = ProductionRealityEventType.PaymentSettled,
                EvidenceLevel = EpistemicEvidenceLevel.CounterpartyAttested,
                EvidenceDigestSha256 = "A1B2C3D4E5F60718293A4B5C6D7E8F90A1B2C3D4E5F60718293A4B5C6D7E8F90",
                ExternalReferenceId = "WIRE-FED-JPMC-99182736"
            };

            var promotedResult = await service.PromoteEvidenceAsync(attestedEvent, "AuditorAuthority");
            Assert.True(promotedResult.IsPromoted);
            Assert.Equal(EpistemicEvidenceLevel.BankVerifiedCash, promotedResult.ResultingLevel);
            Assert.Equal(ExternalReconciliationStatus.Reconciled, attestedEvent.VerificationStatus);
        }
    }
}
