using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using BusinessModelApp.Core.Security;

namespace BusinessModelApp.Infrastructure.Security
{
    public class SecretBroker : ISecretBroker
    {
        private readonly byte[] _signingKey;
        private readonly ConcurrentDictionary<string, string> _vaultSecrets = new(StringComparer.OrdinalIgnoreCase);

        public SecretBroker()
        {
            _signingKey = Encoding.UTF8.GetBytes("charlie-enterprise-governed-secret-key-2026");
            SeedConnectorSecrets();
        }

        private void SeedConnectorSecrets()
        {
            _vaultSecrets["gmail-connector"] = "vault_sec_gmail_oauth_token_encrypted_9918";
            _vaultSecrets["salesforce-crm"] = "vault_sec_sf_access_token_encrypted_4421";
            _vaultSecrets["razorpay-payments"] = "vault_sec_rzp_live_key_encrypted_8872";
        }

        public CapabilityToken IssueToken(string agentId, string tenantId, string capability, decimal maxSpendINR, TimeSpan ttl)
        {
            if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentNullException(nameof(agentId));
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (string.IsNullOrWhiteSpace(capability)) throw new ArgumentNullException(nameof(capability));

            var token = new CapabilityToken
            {
                TokenId = Guid.NewGuid(),
                AgentId = agentId,
                TenantId = tenantId,
                CapabilityName = capability,
                MaxAuthorizedSpendINR = maxSpendINR,
                IssuedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.Add(ttl)
            };

            token.TokenSignature = ComputeSignature(token);
            return token;
        }

        public bool ValidateToken(CapabilityToken token, string expectedCapability, string expectedTenantId)
        {
            if (token == null || token.IsExpired) return false;

            if (!string.Equals(token.CapabilityName, expectedCapability, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.Equals(token.TenantId, expectedTenantId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var expectedSignature = ComputeSignature(token);
            return string.Equals(token.TokenSignature, expectedSignature, StringComparison.Ordinal);
        }

        public string ResolveConnectorSecret(CapabilityToken token, string connectorId)
        {
            if (token == null) throw new ArgumentNullException(nameof(token));
            if (token.IsExpired)
            {
                throw new UnauthorizedAccessException($"Capability token {token.TokenId} has expired at {token.ExpiresAtUtc}.");
            }

            var expectedSig = ComputeSignature(token);
            if (!string.Equals(token.TokenSignature, expectedSig, StringComparison.Ordinal))
            {
                throw new UnauthorizedAccessException($"Tampered capability token signature detected on token {token.TokenId}.");
            }

            if (!_vaultSecrets.TryGetValue(connectorId, out var secret))
            {
                throw new KeyNotFoundException($"Connector secret for '{connectorId}' not found in vault.");
            }

            // Invariant: Raw secret is returned directly to connector adapter at boundary, never stored in agent memory
            return secret;
        }

        private string ComputeSignature(CapabilityToken token)
        {
            var raw = $"{token.TokenId}:{token.AgentId}:{token.TenantId}:{token.CapabilityName}:{token.MaxAuthorizedSpendINR}:{token.ExpiresAtUtc.Ticks}";
            using var hmac = new HMACSHA256(_signingKey);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(hash);
        }
    }
}
