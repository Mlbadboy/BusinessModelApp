using System;

namespace BusinessModelApp.Core.Security
{
    public class CapabilityToken
    {
        public Guid TokenId { get; set; } = Guid.NewGuid();
        public string AgentId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string CapabilityName { get; set; } = string.Empty;
        public decimal MaxAuthorizedSpendINR { get; set; }
        public DateTime IssuedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAtUtc { get; set; }
        public string TokenSignature { get; set; } = string.Empty;

        public bool IsExpired => DateTime.UtcNow > ExpiresAtUtc;
    }

    public interface ISecretBroker
    {
        CapabilityToken IssueToken(string agentId, string tenantId, string capability, decimal maxSpendINR, TimeSpan ttl);
        bool ValidateToken(CapabilityToken token, string expectedCapability, string expectedTenantId);
        string ResolveConnectorSecret(CapabilityToken token, string connectorId);
    }
}
