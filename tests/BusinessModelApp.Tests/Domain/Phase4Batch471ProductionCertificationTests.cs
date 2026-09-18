using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Production;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Production;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    /// <summary>
    /// Phase 4.7 Sub-Batch 4.7.20: Master Production Certification Suite.
    /// Certifies P1 (Environment/Secrets), P2 (Governance/Kill Switch),
    /// P3 (Business Reality & Unbroken Lineage), and P4 (Repeated Multi-Cycle Autonomy).
    /// </summary>
    public class Phase4Batch471ProductionCertificationTests
    {
        [Fact]
        public async Task P1_ProductionEnvironment_And_SecretBroker_Certified()
        {
            var envService = new ProductionEnvironmentService();

            // 1. Run full production readiness check
            var check = await envService.RunReadinessCheckAsync();
            Assert.True(check.IsFullyReady);
            Assert.True(check.DockerRuntimeReady);
            Assert.True(check.PostgresPersistenceReady);
            Assert.True(check.SecretBrokerReady);
            Assert.True(check.ExecutionFirewallReady);
            Assert.True(check.KillSwitchArmed);
            Assert.True(check.ClockSynchronized);
            Assert.True(check.TlsEnabled);

            // 2. Issue and validate capability-scoped secret token
            var token = await envService.IssueScopedSecretTokenAsync(
                "tenant-enterprise-prod", "FedwireConnector", "WIRE_RECONCILE", TimeSpan.FromMinutes(15));

            Assert.True(token.IsValid);
            Assert.Equal("FedwireConnector", token.ConnectorId);
            Assert.Equal(64, token.ScopedTokenDigestSha256.Length);

            bool isValid = await envService.ValidateSecretTokenAsync(token.TokenId);
            Assert.True(isValid);

            token.Revoke();
            bool isRevokedValid = await envService.ValidateSecretTokenAsync(token.TokenId);
            Assert.False(isRevokedValid);
        }

        [Fact]
        public async Task P2_ProductionGovernance_And_KillSwitch_Certified()
        {
            var envService = new ProductionEnvironmentService();
            var incidentService = new ProductionIncidentRecoveryService();

            // 1. Activate production environment with explicit PRG-1 signoff
            var activation = await envService.ActivateProductionAsync(
                "tenant-enterprise-prod", "PRG1-EXECUTIVE-BOARD-AUTH-991");

            Assert.Equal(ProductionEnvironmentTier.Production, activation.Tier);
            Assert.Equal(ProductionEnvironmentState.Ready, activation.State);
            Assert.Contains("FedwireConnector", activation.ActiveConnectorIds);

            // 2. Verify Kill Switch trigger & reset operations
            var killSwitch = await incidentService.TriggerKillSwitchAsync(
                "tenant-enterprise-prod", "ChiefRiskOfficer", "Precautionary audit trigger");

            Assert.True(killSwitch.IsTriggered);
            Assert.Equal("ChiefRiskOfficer", killSwitch.TriggeredByAuthority);

            var resetSwitch = await incidentService.ResetKillSwitchAsync(
                "tenant-enterprise-prod", "ChiefRiskOfficer");

            Assert.False(resetSwitch.IsTriggered);
        }

        [Fact]
        public async Task P3_BusinessReality_And_CompleteLineage_Certified()
        {
            var verifier = new ProductionEvidenceVerifier();
            var incidentService = new ProductionIncidentRecoveryService();

            // 1. Record idempotent external effect
            var effect = new ExternalEffectRecord
            {
                TenantId = "tenant-enterprise-prod",
                ConnectorName = "DocuSignConnector",
                ExternalProvider = "DocuSign",
                IdempotencyKey = "IDEMP-DOCUSIGN-CONTRACT-88192",
                RequestPayloadHashSha256 = "A1B2C3D4E5F60718293A4B5C6D7E8F90A1B2C3D4E5F60718293A4B5C6D7E8F90"
            };
            effect.MarkSuccess("DOCUSIGN-ENVELOPE-9921", "B2C3D4E5F60718293A4B5C6D7E8F90A1B2C3D4E5F60718293A4B5C6D7E8F90A1");

            await incidentService.RecordExternalEffectAsync(effect);
            Assert.Equal(ExternalEffectState.Success, effect.State);

            // 2. Verify SHA-256 digests
            bool bankDigestValid = await verifier.VerifyBankStatementDigestAsync(
                "WIRE-FED-JPMC-99827361", "1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF");
            Assert.True(bankDigestValid);

            bool contractDigestValid = await verifier.VerifyContractDigestAsync(
                "CTR-ENTERPRISE-01", "ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890");
            Assert.True(contractDigestValid);

            // 3. Record unbroken end-to-end lineage
            var lineage = new ProductionLineageRecord
            {
                TenantId = "tenant-enterprise-prod",
                BusinessObjectiveId = "BO-PROD-SCALE-2026",
                GrowthObjectiveId = "GO-MIDMARKET-2026",
                OpportunityId = "OPP-HORIZON-PAYMENTS",
                MissionId = "MIS-COMMERCIAL-CLOSE-01",
                PermitId = "PERMIT-BATCH6-AUTH-991",
                ExternalEffectId = effect.ExternalEffectId,
                ContractId = "CTR-ENTERPRISE-01",
                InvoiceId = "INV-HORIZON-2026-001",
                BankPaymentReferenceId = "WIRE-FED-JPMC-99827361",
                RealizedCashINR = 500_000m,
                DirectDeliveryCostsINR = 100_000m,
                AgentComputeCostsINR = 30_000m
            };

            var recordedLineage = await verifier.RecordLineageAsync(lineage);
            Assert.True(recordedLineage.IsCompleteLineage);
            Assert.Equal(370_000m, recordedLineage.NetContributionINR);
        }

        [Fact]
        public async Task P4_RepeatedAutonomy_And_MasterCertificationReport_Certified()
        {
            var envService = new ProductionEnvironmentService();
            var verifier = new ProductionEvidenceVerifier();
            var incidentService = new ProductionIncidentRecoveryService();
            var certService = new ProductionCertificationService(envService, verifier, incidentService);

            var report = await certService.EvaluateCertificationAsync(
                "tenant-enterprise-prod", "BO-PROD-SCALE-2026");

            Assert.NotNull(report);
            Assert.Equal(ProductionGateStatus.Certified, report.P1EnvironmentStatus);
            Assert.Equal(ProductionGateStatus.Certified, report.P2GovernanceStatus);
            Assert.Equal(ProductionGateStatus.Certified, report.P3BusinessRealityStatus);
            Assert.Equal(ProductionGateStatus.Certified, report.P4RepeatedAutonomyStatus);
            Assert.True(report.IsFullyProductionCertified);
            Assert.True(report.TotalNetContributionINR > 0);
            Assert.Equal("CorporateExecutiveProductionBoard", report.CertifiedByAuthority);
        }
    }
}
