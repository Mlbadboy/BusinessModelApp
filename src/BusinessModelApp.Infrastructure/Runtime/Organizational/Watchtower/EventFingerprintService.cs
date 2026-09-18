using System;
using System.Security.Cryptography;
using System.Text;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Watchtower
{
    public class EventFingerprintService : IEventFingerprintService
    {
        public string ComputeFingerprint(string sourceSystem, string eventType, string entityId, string sourceRecordId)
        {
            var raw = $"{sourceSystem?.Trim().ToUpperInvariant()}:{eventType?.Trim().ToUpperInvariant()}:{entityId?.Trim()}:{sourceRecordId?.Trim()}";
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }

        public string ComputeProvenanceHash(string tenantId, string fingerprint, DateTime observedAt, string evidenceHash)
        {
            var raw = $"{tenantId?.Trim()}:{fingerprint}:{observedAt:O}:{evidenceHash?.Trim()}";
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }
    }
}
