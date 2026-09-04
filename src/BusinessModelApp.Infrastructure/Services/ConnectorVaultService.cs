using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Connectors;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BusinessModelApp.Infrastructure.Services
{
    public class ConnectorVaultService : IConnectorVaultService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ConnectorVaultService> _logger;
        private readonly byte[] _masterKey;

        public ConnectorVaultService(
            AppDbContext context,
            IConfiguration configuration,
            ILogger<ConnectorVaultService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            string masterSecret = configuration["Security:VaultMasterKey"] ?? "CharlieAIBusinessOperatingSystemMasterVaultKey2026";
            using var sha = SHA256.Create();
            _masterKey = sha.ComputeHash(Encoding.UTF8.GetBytes(masterSecret));
        }

        public string Encrypt(string plainText, Guid workspaceId)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;

            var workspaceKey = DeriveWorkspaceKey(workspaceId);
            byte[] nonce = new byte[12]; // standard 96-bit nonce for GCM
            RandomNumberGenerator.Fill(nonce);

            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] cipherBytes = new byte[plainBytes.Length];
            byte[] tag = new byte[16]; // 128-bit authentication tag

            using var aesGcm = new AesGcm(workspaceKey, 16);
            aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);

            // Payload: [Nonce (12)] + [Tag (16)] + [Cipher (N)]
            byte[] result = new byte[nonce.Length + tag.Length + cipherBytes.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, nonce.Length + tag.Length, cipherBytes.Length);

            return Convert.ToBase64String(result);
        }

        public string Decrypt(string cipherText, Guid workspaceId)
        {
            if (string.IsNullOrEmpty(cipherText)) return string.Empty;

            try
            {
                byte[] raw = Convert.FromBase64String(cipherText);
                if (raw.Length < 28) throw new CryptographicException("Ciphertext payload is truncated or invalid.");

                var workspaceKey = DeriveWorkspaceKey(workspaceId);
                byte[] nonce = new byte[12];
                byte[] tag = new byte[16];
                int cipherLength = raw.Length - 28;
                byte[] cipherBytes = new byte[cipherLength];
                byte[] plainBytes = new byte[cipherLength];

                Buffer.BlockCopy(raw, 0, nonce, 0, 12);
                Buffer.BlockCopy(raw, 12, tag, 0, 16);
                Buffer.BlockCopy(raw, 28, cipherBytes, 0, cipherLength);

                using var aesGcm = new AesGcm(workspaceKey, 16);
                aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);

                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt connector credential for workspace {WorkspaceId}", workspaceId);
                throw new CryptographicException("Decryption failed. Token may be corrupted or key mismatch.", ex);
            }
        }

        public async Task StoreOAuthTokensAsync(
            Guid workspaceId,
            Guid? organizationId,
            ConnectorProvider provider,
            string accessToken,
            string? refreshToken,
            DateTime? expiresAt,
            IEnumerable<string>? scopes = null,
            string? accountIdentifier = null,
            CancellationToken ct = default)
        {
            var entity = await _context.Connectors.FirstOrDefaultAsync(c => c.WorkspaceId == workspaceId && c.Provider == provider, ct);
            if (entity == null)
            {
                entity = new ConnectorEntity
                {
                    WorkspaceId = workspaceId,
                    OrganizationId = organizationId,
                    Provider = provider,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Connectors.Add(entity);
            }

            entity.Status = ConnectorStatus.Authenticated;
            entity.AccountIdentifier = accountIdentifier ?? entity.AccountIdentifier;
            entity.EncryptedAccessToken = Encrypt(accessToken, workspaceId);
            entity.EncryptedRefreshToken = !string.IsNullOrWhiteSpace(refreshToken) ? Encrypt(refreshToken, workspaceId) : entity.EncryptedRefreshToken;
            entity.TokenExpiresAt = expiresAt;
            entity.ScopesJson = JsonSerializer.Serialize(scopes ?? new List<string>());
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Stored encrypted OAuth credentials for provider {Provider} in workspace {WorkspaceId}", provider, workspaceId);
        }

        public async Task StoreApiKeysAsync(
            Guid workspaceId,
            Guid? organizationId,
            ConnectorProvider provider,
            string apiKey,
            string? apiSecret,
            string? accountIdentifier = null,
            CancellationToken ct = default)
        {
            var entity = await _context.Connectors.FirstOrDefaultAsync(c => c.WorkspaceId == workspaceId && c.Provider == provider, ct);
            if (entity == null)
            {
                entity = new ConnectorEntity
                {
                    WorkspaceId = workspaceId,
                    OrganizationId = organizationId,
                    Provider = provider,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Connectors.Add(entity);
            }

            entity.Status = ConnectorStatus.Configured;
            entity.AccountIdentifier = accountIdentifier ?? entity.AccountIdentifier;
            entity.EncryptedApiKey = Encrypt(apiKey, workspaceId);
            entity.EncryptedApiSecret = !string.IsNullOrWhiteSpace(apiSecret) ? Encrypt(apiSecret, workspaceId) : null;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Stored encrypted API keys for provider {Provider} in workspace {WorkspaceId}", provider, workspaceId);
        }

        public async Task<DecryptedCredentials?> GetDecryptedCredentialsAsync(
            Guid workspaceId,
            ConnectorProvider provider,
            CancellationToken ct = default)
        {
            var entity = await _context.Connectors.FirstOrDefaultAsync(c => c.WorkspaceId == workspaceId && c.Provider == provider, ct);
            if (entity == null) return null;

            return new DecryptedCredentials
            {
                Provider = entity.Provider,
                AccountIdentifier = entity.AccountIdentifier,
                AccessToken = !string.IsNullOrWhiteSpace(entity.EncryptedAccessToken) ? Decrypt(entity.EncryptedAccessToken, workspaceId) : null,
                RefreshToken = !string.IsNullOrWhiteSpace(entity.EncryptedRefreshToken) ? Decrypt(entity.EncryptedRefreshToken, workspaceId) : null,
                ApiKey = !string.IsNullOrWhiteSpace(entity.EncryptedApiKey) ? Decrypt(entity.EncryptedApiKey, workspaceId) : null,
                ApiSecret = !string.IsNullOrWhiteSpace(entity.EncryptedApiSecret) ? Decrypt(entity.EncryptedApiSecret, workspaceId) : null,
                TokenExpiresAt = entity.TokenExpiresAt
            };
        }

        public async Task<bool> RevokeCredentialsAsync(
            Guid workspaceId,
            ConnectorProvider provider,
            CancellationToken ct = default)
        {
            var entity = await _context.Connectors.FirstOrDefaultAsync(c => c.WorkspaceId == workspaceId && c.Provider == provider, ct);
            if (entity == null) return false;

            entity.Status = ConnectorStatus.Revoked;
            entity.EncryptedAccessToken = null;
            entity.EncryptedRefreshToken = null;
            entity.EncryptedApiKey = null;
            entity.EncryptedApiSecret = null;
            entity.TokenExpiresAt = null;
            entity.ProbesPassed = 0;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Revoked credentials for provider {Provider} in workspace {WorkspaceId}", provider, workspaceId);
            return true;
        }

        private byte[] DeriveWorkspaceKey(Guid workspaceId)
        {
            using var hmac = new HMACSHA256(_masterKey);
            return hmac.ComputeHash(workspaceId.ToByteArray());
        }
    }
}
