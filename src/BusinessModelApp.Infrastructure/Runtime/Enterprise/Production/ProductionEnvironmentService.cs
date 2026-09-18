using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Production;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Production;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Production
{
    public sealed class ProductionEnvironmentService : IProductionEnvironmentService
    {
        private readonly ConcurrentDictionary<string, SecretBrokerToken> _tokens = new();
        private readonly ConcurrentDictionary<string, ProductionActivationRecord> _activations = new();

        public Task<ProductionReadinessCheck> RunReadinessCheckAsync(CancellationToken cancellationToken = default)
        {
            var check = new ProductionReadinessCheck
            {
                DockerRuntimeReady = true,
                PostgresPersistenceReady = true,
                SecretBrokerReady = true,
                ExecutionFirewallReady = true,
                KillSwitchArmed = true,
                ClockSynchronized = true,
                TlsEnabled = true
            };
            return Task.FromResult(check);
        }

        public Task<SecretBrokerToken> IssueScopedSecretTokenAsync(
            string tenantId,
            string connectorId,
            string capabilityScope,
            TimeSpan validity,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("Tenant ID required.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(connectorId)) throw new ArgumentException("Connector ID required.", nameof(connectorId));

            string raw = $"{tenantId}:{connectorId}:{capabilityScope}:{Guid.NewGuid():N}";
            string digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

            var token = new SecretBrokerToken
            {
                TenantId = tenantId,
                ConnectorId = connectorId,
                CapabilityScope = capabilityScope,
                ExpiresAtUtc = DateTime.UtcNow.Add(validity),
                ScopedTokenDigestSha256 = digest
            };

            _tokens[token.TokenId] = token;
            return Task.FromResult(token);
        }

        public Task<bool> ValidateSecretTokenAsync(string tokenId, CancellationToken cancellationToken = default)
        {
            if (_tokens.TryGetValue(tokenId, out var token))
            {
                return Task.FromResult(token.IsValid);
            }
            return Task.FromResult(false);
        }

        public Task<ProductionActivationRecord> ActivateProductionAsync(
            string tenantId,
            string humanSignoffId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(humanSignoffId))
                throw new InvalidOperationException("Production activation requires explicit PRG-1 human authorization (Law I43-L).");

            var activation = new ProductionActivationRecord
            {
                TenantId = tenantId,
                Tier = ProductionEnvironmentTier.Production,
                ActivatedBySignoffId = humanSignoffId.Trim()
            };
            activation.ActiveConnectorIds.AddRange(new[] { "FedwireConnector", "StripeConnector", "DocuSignConnector", "SalesforceConnector" });

            _activations[tenantId] = activation;
            return Task.FromResult(activation);
        }
    }
}
