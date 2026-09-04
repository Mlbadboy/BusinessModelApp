using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Connectors;
using BusinessModelApp.Infrastructure.Data;
using BusinessModelApp.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public class CharlieConnectGovernanceTests
    {
        private AppDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private IConfiguration CreateTestConfig()
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                { "Security:VaultMasterKey", "TestMasterKeyForCharlieOperatingSystem2026" }
            };
            return new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        }

        [Fact]
        public async Task ConnectorVault_ShouldEncryptAndDecrypt_UsingAesGcmAndWorkspaceSalt()
        {
            using var context = CreateInMemoryDbContext();
            var config = CreateTestConfig();
            var vault = new ConnectorVaultService(context, config, new NullLogger<ConnectorVaultService>());

            var wsId1 = Guid.NewGuid();
            var wsId2 = Guid.NewGuid();
            string secretKey = "vapi_live_sec_99342019948201";

            // Encrypt for Workspace 1
            string encrypted = vault.Encrypt(secretKey, wsId1);
            encrypted.Should().NotBeNullOrWhiteSpace();
            encrypted.Should().NotBe(secretKey);

            // Decrypt in same workspace -> Succeeded
            string decrypted = vault.Decrypt(encrypted, wsId1);
            decrypted.Should().Be(secretKey);

            // Cross-workspace decryption attempt -> MUST throw CryptographicException (Tenant isolation)
            Action crossTenantDecrypt = () => vault.Decrypt(encrypted, wsId2);
            crossTenantDecrypt.Should().Throw<CryptographicException>();
        }

        [Fact]
        public async Task ConnectorCapabilityService_ShouldEnforceDefaultPermissions_AndAllowCustomization()
        {
            using var context = CreateInMemoryDbContext();
            var capService = new ConnectorCapabilityService(context, new NullLogger<ConnectorCapabilityService>());
            var wsId = Guid.NewGuid();

            // Check default Google Workspace capabilities
            var defaults = await capService.GetCapabilitiesAsync(wsId, ConnectorProvider.GoogleWorkspace);
            defaults["read_email"].Should().Be(CapabilityPermissionMode.Allowed);
            defaults["send_email"].Should().Be(CapabilityPermissionMode.RequireApproval);

            // Update tenant preference: Allow send_email directly
            await capService.UpdateCapabilitiesAsync(wsId, ConnectorProvider.GoogleWorkspace, new Dictionary<string, CapabilityPermissionMode>
            {
                { "send_email", CapabilityPermissionMode.Allowed }
            });

            var updated = await capService.GetCapabilitiesAsync(wsId, ConnectorProvider.GoogleWorkspace);
            updated["send_email"].Should().Be(CapabilityPermissionMode.Allowed);
            updated["read_email"].Should().Be(CapabilityPermissionMode.Allowed);
        }

        [Fact]
        public async Task ConnectorHealthService_ShouldExecuteAll7Probes_AndReportHealthyWhenConfigured()
        {
            using var context = CreateInMemoryDbContext();
            var config = CreateTestConfig();
            var vault = new ConnectorVaultService(context, config, new NullLogger<ConnectorVaultService>());
            var capService = new ConnectorCapabilityService(context, new NullLogger<ConnectorCapabilityService>());
            var health = new ConnectorHealthService(context, vault, capService, new NullLogger<ConnectorHealthService>());

            var wsId = Guid.NewGuid();

            // 1. Initial State: Disconnected -> 2/7 probes pass (Governance + Audit only)
            var initialReport = await health.RunHealthProbesAsync(wsId, ConnectorProvider.GoogleWorkspace);
            initialReport.TotalProbes.Should().Be(7);
            initialReport.ProbesPassed.Should().Be(2);
            initialReport.IsHealthy.Should().BeFalse();

            // 2. Store OAuth tokens in Vault
            await vault.StoreOAuthTokensAsync(
                wsId,
                null,
                ConnectorProvider.GoogleWorkspace,
                "oauth_access_token_12345",
                "oauth_refresh_token_67890",
                DateTime.UtcNow.AddHours(2),
                new[] { "gmail.read", "gmail.send" },
                "mayur@bitbloom.in");

            // 3. Re-run Probes: Should pass all 7/7 probes
            var healthyReport = await health.RunHealthProbesAsync(wsId, ConnectorProvider.GoogleWorkspace);
            healthyReport.TotalProbes.Should().Be(7);
            healthyReport.ProbesPassed.Should().Be(7);
            healthyReport.IsHealthy.Should().BeTrue();
            healthyReport.Status.Should().Be(ConnectorStatus.Healthy);

            // 4. Summaries Aggregator: Should correctly report 7/7
            var allSummaries = await health.GetAllConnectorSummariesAsync(wsId);
            allSummaries.Should().HaveCount(9); // All 9 providers
            var google = allSummaries.Find(s => s.Provider == "GoogleWorkspace");
            google.Should().NotBeNull();
            google!.IsHealthy.Should().BeTrue();
            google.AccountIdentifier.Should().Be("mayur@bitbloom.in");
            google.ProbesPassed.Should().Be(7);
        }

        [Fact]
        public async Task DisconnectConnector_ShouldPurgeVaultCredentials_AndSetStatusToRevoked()
        {
            using var context = CreateInMemoryDbContext();
            var config = CreateTestConfig();
            var vault = new ConnectorVaultService(context, config, new NullLogger<ConnectorVaultService>());
            var wsId = Guid.NewGuid();

            await vault.StoreApiKeysAsync(wsId, null, ConnectorProvider.Razorpay, "rzp_live_key_99812", "rzp_sec_99128", "merchant_bitbloom");

            var credsBefore = await vault.GetDecryptedCredentialsAsync(wsId, ConnectorProvider.Razorpay);
            credsBefore.Should().NotBeNull();
            credsBefore!.ApiKey.Should().Be("rzp_live_key_99812");

            // Revoke
            bool revoked = await vault.RevokeCredentialsAsync(wsId, ConnectorProvider.Razorpay);
            revoked.Should().BeTrue();

            var credsAfter = await vault.GetDecryptedCredentialsAsync(wsId, ConnectorProvider.Razorpay);
            credsAfter!.ApiKey.Should().BeNull();
            credsAfter.AccessToken.Should().BeNull();
        }
    }
}
