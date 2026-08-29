using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Connectors;
using BusinessModelApp.Infrastructure.Services;
using Xunit;

namespace BusinessModelApp.Tests.Connectors
{
    public class CharlieConnectTests
    {
        private readonly CharlieConnectService _service = new();

        [Fact]
        public async Task GetConnections_ReturnsAllProvidersWithCapabilityState()
        {
            var wsId = Guid.NewGuid();
            var connections = await _service.GetConnectionsAsync(wsId);

            Assert.NotEmpty(connections);
            Assert.Contains(connections, c => c.Provider == ConnectionProvider.GoogleWorkspace);
            Assert.Contains(connections, c => c.Provider == ConnectionProvider.Razorpay);
            Assert.Contains(connections, c => c.Provider == ConnectionProvider.GooglePlaces);
        }

        [Fact]
        public void CapabilityRegistry_PermanentlyBlocksDestructiveDelete()
        {
            foreach (ConnectionProvider provider in Enum.GetValues(typeof(ConnectionProvider)))
            {
                var cap = ConnectorCapabilityRegistry.GetCapability(provider);
                Assert.True(cap.IsDeletePermanentlyBlocked, $"Delete must be permanently blocked for {provider}");
            }
        }

        [Fact]
        public async Task ValidateActionPermission_BlocksDeleteRecordsAction()
        {
            var wsId = Guid.NewGuid();
            var isDeleteAllowed = await _service.ValidateActionPermissionAsync(wsId, ConnectionProvider.HubSpot, "DeleteRecords");
            var isPurgeAllowed = await _service.ValidateActionPermissionAsync(wsId, ConnectionProvider.Salesforce, "Purge");
            var isReadAllowed = await _service.ValidateActionPermissionAsync(wsId, ConnectionProvider.GoogleWorkspace, "ReadEmails");

            Assert.False(isDeleteAllowed);
            Assert.False(isPurgeAllowed);
            Assert.True(isReadAllowed);
        }

        [Fact]
        public async Task ConnectAndTest_ActivatesProviderWithScopes()
        {
            var wsId = Guid.NewGuid();
            var conn = await _service.ConnectProviderAsync(wsId, ConnectionProvider.GoogleWorkspace, "director@bitbloom.in", new List<string> { "gmail.send", "calendar.events" });

            Assert.Equal(ConnectionStatus.ConnectedActive, conn.Status);
            Assert.Equal("director@bitbloom.in", conn.AccountIdentifier);
            Assert.Contains("gmail.send", conn.GrantedScopes);

            var tested = await _service.TestConnectionAsync(wsId, ConnectionProvider.GoogleWorkspace);
            Assert.True(tested.IsHealthy);
            Assert.NotNull(tested.LastTestedAt);
        }
    }
}
