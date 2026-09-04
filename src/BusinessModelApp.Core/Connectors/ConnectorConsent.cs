using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Connectors
{
    public enum ConnectorType
    {
        WebSearch = 0,
        CompanyIntelligence = 1,
        ProspectDiscovery = 2,
        CRM = 3,
        EmailCommunication = 4,
        CalendarScheduling = 5,
        ProposalCatalog = 6
    }

    [Flags]
    public enum ConnectorScope
    {
        None = 0,
        ReadMetadata = 1 << 0,
        ReadContent = 1 << 1,
        Draft = 1 << 2,
        SendOutbound = 1 << 3,
        CreateRecords = 1 << 4,
        UpdateRecords = 1 << 5,
        ScheduleMeetings = 1 << 6,
        DeleteRecords = 1 << 7 // Explicitly forbidden for agents in standard autonomy
    }

    public class ConnectorConsent
    {
        public Guid TenantId { get; set; }
        public ConnectorType Connector { get; set; }
        public ConnectorScope GrantedScopes { get; set; }
        public int RateLimitPerMinute { get; set; } = 30;
        public int DailyMaxOutbound { get; set; } = 100;
        public int OutboundSentToday { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public DateTime AuthorizedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }
        public List<string> PermittedDomains { get; set; } = new List<string>();

        public bool CanPerform(ConnectorScope scope)
        {
            if (!IsActive) return false;
            if (ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value) return false;
            
            // Hard security boundary: Delete is never permitted for autonomous agents
            if (scope.HasFlag(ConnectorScope.DeleteRecords)) return false;

            return (GrantedScopes & scope) == scope;
        }

        public bool TryConsumeOutboundQuota(int count = 1)
        {
            if (OutboundSentToday + count > DailyMaxOutbound) return false;
            OutboundSentToday += count;
            return true;
        }
    }
}
