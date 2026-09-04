using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessModelApp.Core.Domain.Connectors
{
    public enum ConnectorProvider
    {
        GoogleWorkspace = 1,
        Microsoft365 = 2,
        Vapi = 3,
        Retell = 4,
        Twilio = 5,
        Razorpay = 6,
        Stripe = 7,
        GitHub = 8,
        Vercel = 9
    }

    public enum ConnectorStatus
    {
        Disconnected = 0,
        Configured = 1,
        Authenticated = 2,
        Healthy = 3,
        Degraded = 4,
        Expired = 5,
        Revoked = 6,
        Error = 7
    }

    public enum CapabilityPermissionMode
    {
        Denied = 0,
        RequireApproval = 1,
        Allowed = 2
    }

    /// <summary>
    /// Governed tenant connector entity with securely encrypted credentials.
    /// </summary>
    public class ConnectorEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid WorkspaceId { get; set; }

        public Guid? OrganizationId { get; set; }

        [Required]
        public ConnectorProvider Provider { get; set; }

        public ConnectorStatus Status { get; set; } = ConnectorStatus.Disconnected;

        [MaxLength(200)]
        public string? AccountIdentifier { get; set; } // e.g. "mayur@bitbloom.in" or Razorpay Merchant ID

        // Secure Vault (Encrypted at rest with AES-256-GCM - NEVER exposed via API DTOs)
        public string? EncryptedAccessToken { get; set; }
        public string? EncryptedRefreshToken { get; set; }
        public string? EncryptedApiKey { get; set; }
        public string? EncryptedApiSecret { get; set; }

        public DateTime? TokenExpiresAt { get; set; }
        public string ScopesJson { get; set; } = "[]";

        /// <summary>
        /// JSON-serialized Dictionary&lt;string, CapabilityPermissionMode&gt;
        /// </summary>
        public string CapabilitiesJson { get; set; } = "{}";

        public int ProbesPassed { get; set; } = 0;
        public int TotalProbes { get; set; } = 7;
        public DateTime? LastHealthCheckAt { get; set; }
        public string? LastHealthReportJson { get; set; }
        public string? ErrorMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ConnectorHealthProbe
    {
        public string ProbeId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool Passed { get; set; }
        public string? Details { get; set; }
        public int LatencyMs { get; set; }
    }

    public class ConnectorHealthReportDto
    {
        public ConnectorProvider Provider { get; set; }
        public ConnectorStatus Status { get; set; }
        public int ProbesPassed { get; set; }
        public int TotalProbes { get; set; }
        public bool IsHealthy => ProbesPassed == TotalProbes && TotalProbes > 0;
        public List<ConnectorHealthProbe> Probes { get; set; } = new();
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    }

    // Public DTOs (Strictly zero credential leakage)
    public class ConnectorSummaryDto
    {
        public string Provider { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // Communication | Telephony | Payments | Delivery
        public string Status { get; set; } = string.Empty;
        public string? AccountIdentifier { get; set; }
        public Dictionary<string, string> Capabilities { get; set; } = new();
        public int ProbesPassed { get; set; }
        public int TotalProbes { get; set; } = 7;
        public bool IsHealthy { get; set; }
        public DateTime? LastHealthCheckAt { get; set; }
        public DateTime? TokenExpiresAt { get; set; }
    }

    public class UpdateCapabilitiesDto
    {
        public Dictionary<string, CapabilityPermissionMode> Capabilities { get; set; } = new();
    }

    public class ConfigureKeysDto
    {
        public string? ApiKey { get; set; }
        public string? ApiSecret { get; set; }
        public string? AccountIdentifier { get; set; }
        public Dictionary<string, CapabilityPermissionMode>? InitialCapabilities { get; set; }
    }

    public class OAuthCallbackDto
    {
        public string Code { get; set; } = string.Empty;
        public string? State { get; set; }
        public string? RedirectUri { get; set; }
    }
}
