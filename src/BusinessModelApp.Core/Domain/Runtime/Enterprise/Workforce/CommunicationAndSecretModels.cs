using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce
{
    public enum CommunicationChannel
    {
        EMAIL = 1,
        WHATSAPP = 2,
        LINKEDIN = 3,
        SLACK = 4,
        TEAMS = 5,
        VOICE = 6,
        CALENDAR = 7
    }

    public class CommunicationIntent
    {
        public string IntentId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string AgentId { get; set; } = string.Empty;
        public CommunicationChannel Channel { get; set; } = CommunicationChannel.EMAIL;
        public string RecipientAddress { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string BodyContent { get; set; } = string.Empty;
        public int RiskTier { get; set; } = 2; // R1 = internal notification, R2 = standard draft/outreach, R3 = contract/pricing commitment (requires PRG-1)
        public bool IsApproved { get; set; } = false;
        public string? ApprovedBy { get; set; }
        public bool IsDispatched { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ScopedCredential
    {
        public string CredentialId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string CapabilityId { get; set; } = string.Empty;
        public string MaskedToken { get; set; } = "sec_****_vault";
        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(1);
        public bool IsRevoked { get; set; } = false;
        public bool IsValid => !IsRevoked && DateTime.UtcNow < ExpiresAt;
    }
}
