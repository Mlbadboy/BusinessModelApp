using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Connectors;

namespace BusinessModelApp.Core.Interfaces
{
    public class DecryptedCredentials
    {
        public ConnectorProvider Provider { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public string? ApiKey { get; set; }
        public string? ApiSecret { get; set; }
        public string? AccountIdentifier { get; set; }
        public DateTime? TokenExpiresAt { get; set; }
        public bool HasValidAccessToken => !string.IsNullOrWhiteSpace(AccessToken) && (TokenExpiresAt == null || TokenExpiresAt > DateTime.UtcNow.AddMinutes(1));
    }

    public interface IConnectorVaultService
    {
        string Encrypt(string plainText, Guid workspaceId);
        string Decrypt(string cipherText, Guid workspaceId);

        Task StoreOAuthTokensAsync(
            Guid workspaceId,
            Guid? organizationId,
            ConnectorProvider provider,
            string accessToken,
            string? refreshToken,
            DateTime? expiresAt,
            IEnumerable<string>? scopes = null,
            string? accountIdentifier = null,
            CancellationToken ct = default);

        Task StoreApiKeysAsync(
            Guid workspaceId,
            Guid? organizationId,
            ConnectorProvider provider,
            string apiKey,
            string? apiSecret,
            string? accountIdentifier = null,
            CancellationToken ct = default);

        Task<DecryptedCredentials?> GetDecryptedCredentialsAsync(
            Guid workspaceId,
            ConnectorProvider provider,
            CancellationToken ct = default);

        Task<bool> RevokeCredentialsAsync(
            Guid workspaceId,
            ConnectorProvider provider,
            CancellationToken ct = default);
    }
}
