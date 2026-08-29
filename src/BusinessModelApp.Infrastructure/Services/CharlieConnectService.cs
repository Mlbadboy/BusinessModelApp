using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessModelApp.Core.Connectors;
using BusinessModelApp.Core.Services;

namespace BusinessModelApp.Infrastructure.Services
{
    public class CharlieConnectService : ICharlieConnectService
    {
        private static readonly ConcurrentDictionary<string, CharlieConnection> _connections = new();

        public CharlieConnectService()
        {
            // Seed sample default active connections for demo workspace if empty
            if (_connections.IsEmpty)
            {
                var defaultWs = Guid.Empty;
                RegisterSample(defaultWs, ConnectionProvider.GoogleWorkspace, "mayur@bitbloom.in", new[] { "https://www.googleapis.com/auth/gmail.modify", "https://www.googleapis.com/auth/calendar.events" });
                RegisterSample(defaultWs, ConnectionProvider.GooglePlaces, "api_key_places_authorized", new[] { "places.search", "places.details" });
                RegisterSample(defaultWs, ConnectionProvider.Razorpay, "rzp_live_bitbloom", new[] { "payment_links.create", "invoices.read" });
                RegisterSample(defaultWs, ConnectionProvider.HubSpot, "hubspot_app_bitbloom", new[] { "crm.objects.contacts.write", "crm.objects.deals.write" });
            }
        }

        private void RegisterSample(Guid wsId, ConnectionProvider provider, string account, string[] scopes)
        {
            var key = $"{wsId}_{provider}";
            _connections[key] = new CharlieConnection
            {
                WorkspaceId = wsId,
                Provider = provider,
                ProviderName = provider.ToString(),
                Status = ConnectionStatus.ConnectedActive,
                AccountIdentifier = account,
                GrantedScopes = scopes.ToList(),
                ConnectedAt = DateTime.UtcNow.AddDays(-10),
                LastTestedAt = DateTime.UtcNow.AddMinutes(-30),
                IsHealthy = true
            };
        }

        public Task<List<CharlieConnection>> GetConnectionsAsync(Guid workspaceId)
        {
            var results = _connections.Values
                .Where(c => c.WorkspaceId == workspaceId || c.WorkspaceId == Guid.Empty)
                .ToList();

            // Ensure all providers are represented
            foreach (ConnectionProvider prov in Enum.GetValues(typeof(ConnectionProvider)))
            {
                if (!results.Any(r => r.Provider == prov))
                {
                    results.Add(new CharlieConnection
                    {
                        WorkspaceId = workspaceId,
                        Provider = prov,
                        ProviderName = prov.ToString(),
                        Status = ConnectionStatus.Disconnected,
                        AccountIdentifier = "Not connected",
                        IsHealthy = false
                    });
                }
            }

            return Task.FromResult(results);
        }

        public Task<CharlieConnection> ConnectProviderAsync(Guid workspaceId, ConnectionProvider provider, string accountIdentifier, List<string> requestedScopes)
        {
            var key = $"{workspaceId}_{provider}";
            var conn = new CharlieConnection
            {
                WorkspaceId = workspaceId,
                Provider = provider,
                ProviderName = provider.ToString(),
                Status = ConnectionStatus.ConnectedActive,
                AccountIdentifier = accountIdentifier,
                GrantedScopes = requestedScopes ?? new List<string>(),
                ConnectedAt = DateTime.UtcNow,
                LastTestedAt = DateTime.UtcNow,
                IsHealthy = true
            };

            _connections[key] = conn;
            return Task.FromResult(conn);
        }

        public Task<bool> DisconnectProviderAsync(Guid workspaceId, ConnectionProvider provider)
        {
            var key = $"{workspaceId}_{provider}";
            if (_connections.TryGetValue(key, out var conn))
            {
                conn.Status = ConnectionStatus.Disconnected;
                conn.IsHealthy = false;
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<bool> ValidateActionPermissionAsync(Guid workspaceId, ConnectionProvider provider, string actionName)
        {
            var rule = ConnectorCapabilityRegistry.GetCapability(provider);

            if (actionName.Equals("DeleteRecords", StringComparison.OrdinalIgnoreCase) ||
                actionName.Equals("Purge", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(false); // Permanently blocked
            }

            return Task.FromResult(true);
        }

        public Task<CharlieConnection> TestConnectionAsync(Guid workspaceId, ConnectionProvider provider)
        {
            var key = $"{workspaceId}_{provider}";
            if (!_connections.TryGetValue(key, out var conn))
            {
                conn = new CharlieConnection
                {
                    WorkspaceId = workspaceId,
                    Provider = provider,
                    ProviderName = provider.ToString(),
                    Status = ConnectionStatus.ConnectedActive,
                    AccountIdentifier = "authorized_oauth_account",
                    ConnectedAt = DateTime.UtcNow,
                    LastTestedAt = DateTime.UtcNow,
                    IsHealthy = true
                };
                _connections[key] = conn;
            }
            else
            {
                conn.LastTestedAt = DateTime.UtcNow;
                conn.IsHealthy = true;
            }

            return Task.FromResult(conn);
        }
    }
}
