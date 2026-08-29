using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Connectors
{
    public enum ConnectionProvider
    {
        GoogleWorkspace, // Gmail + Google Calendar
        MicrosoftGraph,  // Outlook + Outlook Calendar
        Salesforce,
        HubSpot,
        GooglePlaces,
        Razorpay,
        Stripe,
        LinkedIn
    }

    public enum ConnectionStatus
    {
        Disconnected,
        PendingOAuth,
        ConnectedActive,
        Revoked,
        FailedTest
    }

    public class CharlieConnection
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid WorkspaceId { get; set; }
        public ConnectionProvider Provider { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public ConnectionStatus Status { get; set; } = ConnectionStatus.Disconnected;
        public string AccountIdentifier { get; set; } = string.Empty; // e.g. mayur@bitbloom.in
        public List<string> GrantedScopes { get; set; } = new();
        public string EncryptedAuthPayload { get; set; } = string.Empty;
        public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastTestedAt { get; set; }
        public bool IsHealthy { get; set; } = true;
        public int DailyCallQuota { get; set; } = 1000;
        public int ConsumedDailyQuota { get; set; } = 0;
    }

    public class ProviderCapabilityRule
    {
        public ConnectionProvider Provider { get; set; }
        public bool CanRead { get; set; }
        public bool CanDraft { get; set; }
        public bool CanSend { get; set; }
        public bool CanManageCalendar { get; set; }
        public bool CanSearchPublicData { get; set; }
        public bool CanCreateCRMLeads { get; set; }
        public bool CanCollectPayments { get; set; }
        public bool IsDeletePermanentlyBlocked { get; set; } = true;
        public string PermissionDescription { get; set; } = string.Empty;
    }

    public static class ConnectorCapabilityRegistry
    {
        public static readonly Dictionary<ConnectionProvider, ProviderCapabilityRule> Capabilities = new()
        {
            [ConnectionProvider.GoogleWorkspace] = new ProviderCapabilityRule
            {
                Provider = ConnectionProvider.GoogleWorkspace,
                CanRead = true,
                CanDraft = true,
                CanSend = true,
                CanManageCalendar = true,
                CanSearchPublicData = false,
                CanCreateCRMLeads = false,
                CanCollectPayments = false,
                IsDeletePermanentlyBlocked = true,
                PermissionDescription = "Read emails, draft replies, send governed outreach, schedule calendar meetings. Email/Calendar deletion blocked."
            },
            [ConnectionProvider.MicrosoftGraph] = new ProviderCapabilityRule
            {
                Provider = ConnectionProvider.MicrosoftGraph,
                CanRead = true,
                CanDraft = true,
                CanSend = true,
                CanManageCalendar = true,
                CanSearchPublicData = false,
                CanCreateCRMLeads = false,
                CanCollectPayments = false,
                IsDeletePermanentlyBlocked = true,
                PermissionDescription = "Outlook email reading/sending, Outlook calendar events. Deletion blocked."
            },
            [ConnectionProvider.GooglePlaces] = new ProviderCapabilityRule
            {
                Provider = ConnectionProvider.GooglePlaces,
                CanRead = true,
                CanDraft = false,
                CanSend = false,
                CanManageCalendar = false,
                CanSearchPublicData = true,
                CanCreateCRMLeads = false,
                CanCollectPayments = false,
                IsDeletePermanentlyBlocked = true,
                PermissionDescription = "Authorized business discovery, address, phone, website, ratings, and customer reviews."
            },
            [ConnectionProvider.HubSpot] = new ProviderCapabilityRule
            {
                Provider = ConnectionProvider.HubSpot,
                CanRead = true,
                CanDraft = false,
                CanSend = false,
                CanManageCalendar = false,
                CanSearchPublicData = false,
                CanCreateCRMLeads = true,
                CanCollectPayments = false,
                IsDeletePermanentlyBlocked = true,
                PermissionDescription = "Bi-directional lead, contact, and deal sync. Destructive record purge permanently blocked."
            },
            [ConnectionProvider.Salesforce] = new ProviderCapabilityRule
            {
                Provider = ConnectionProvider.Salesforce,
                CanRead = true,
                CanDraft = false,
                CanSend = false,
                CanManageCalendar = false,
                CanSearchPublicData = false,
                CanCreateCRMLeads = true,
                CanCollectPayments = false,
                IsDeletePermanentlyBlocked = true,
                PermissionDescription = "Enterprise lead & opportunity sync. Destructive record purge blocked."
            },
            [ConnectionProvider.Razorpay] = new ProviderCapabilityRule
            {
                Provider = ConnectionProvider.Razorpay,
                CanRead = true,
                CanDraft = false,
                CanSend = false,
                CanManageCalendar = false,
                CanSearchPublicData = false,
                CanCreateCRMLeads = false,
                CanCollectPayments = true,
                IsDeletePermanentlyBlocked = true,
                PermissionDescription = "Generate payment links, invoices, and receive webhook notifications on completed payment in INR."
            },
            [ConnectionProvider.Stripe] = new ProviderCapabilityRule
            {
                Provider = ConnectionProvider.Stripe,
                CanRead = true,
                CanDraft = false,
                CanSend = false,
                CanManageCalendar = false,
                CanSearchPublicData = false,
                CanCreateCRMLeads = false,
                CanCollectPayments = true,
                IsDeletePermanentlyBlocked = true,
                PermissionDescription = "Global payment link checkout & webhook reconciliation."
            },
            [ConnectionProvider.LinkedIn] = new ProviderCapabilityRule
            {
                Provider = ConnectionProvider.LinkedIn,
                CanRead = true,
                CanDraft = true,
                CanSend = false, // Governed messaging requires specific OAuth scope
                CanManageCalendar = false,
                CanSearchPublicData = true,
                CanCreateCRMLeads = true,
                CanCollectPayments = false,
                IsDeletePermanentlyBlocked = true,
                PermissionDescription = "Authenticated identity, authorized company page analytics, and sanctioned business profile intelligence."
            }
        };

        public static ProviderCapabilityRule GetCapability(ConnectionProvider provider)
        {
            return Capabilities.TryGetValue(provider, out var rule) 
                ? rule 
                : new ProviderCapabilityRule { Provider = provider, IsDeletePermanentlyBlocked = true };
        }
    }
}
