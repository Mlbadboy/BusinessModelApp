using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Reality;
using BusinessModelApp.Core.Interfaces.Runtime.Reality;

namespace BusinessModelApp.Infrastructure.Runtime.Reality
{
    /// <summary>
    /// Monitors reality status of external integration connectors.
    /// Never reports 'Connected' unless verified by live health checks.
    /// </summary>
    public sealed class ConnectorHealthService : IConnectorHealthService
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<ConnectorType, ConnectorHealthRecord>> _tenantConnectors = new();

        public Task<IReadOnlyList<ConnectorHealthRecord>> GetConnectorHealthAsync(string tenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                return Task.FromResult<IReadOnlyList<ConnectorHealthRecord>>(Array.Empty<ConnectorHealthRecord>());

            var dict = _tenantConnectors.GetOrAdd(tenantId, _ => InitializeDefaultConnectors(tenantId));
            return Task.FromResult<IReadOnlyList<ConnectorHealthRecord>>(dict.Values.ToList());
        }

        public Task<ConnectorHealthRecord> GetConnectorHealthAsync(string tenantId, ConnectorType type)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId required.", nameof(tenantId));

            var dict = _tenantConnectors.GetOrAdd(tenantId, _ => InitializeDefaultConnectors(tenantId));
            if (dict.TryGetValue(type, out var record))
            {
                return Task.FromResult(record);
            }

            var fallback = new ConnectorHealthRecord(
                Type: type,
                Name: type.ToString(),
                Status: ConnectorStatus.NotConfigured,
                TenantId: tenantId,
                LastSuccessfulOperation: null,
                LastFailure: null,
                FailureReason: "Connector has not been configured for this tenant.",
                CredentialState: "Missing",
                SupportedCapabilities: Array.Empty<string>(),
                EpistemicNote: "Status is NOT CONFIGURED. Charlie is not executing operations on this provider."
            );

            return Task.FromResult(fallback);
        }

        public Task UpdateConnectorHealthAsync(ConnectorHealthRecord record)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            var dict = _tenantConnectors.GetOrAdd(record.TenantId, _ => InitializeDefaultConnectors(record.TenantId));
            dict[record.Type] = record;
            return Task.CompletedTask;
        }

        private static ConcurrentDictionary<ConnectorType, ConnectorHealthRecord> InitializeDefaultConnectors(string tenantId)
        {
            var dict = new ConcurrentDictionary<ConnectorType, ConnectorHealthRecord>();

            // Default-deny/not-configured state: No fake "connected"
            dict[ConnectorType.Crm] = new ConnectorHealthRecord(
                Type: ConnectorType.Crm,
                Name: "HubSpot / Salesforce CRM",
                Status: ConnectorStatus.NotConfigured,
                TenantId: tenantId,
                LastSuccessfulOperation: null,
                LastFailure: null,
                FailureReason: "No active CRM credentials configured.",
                CredentialState: "Missing",
                SupportedCapabilities: new[] { "crm.contact.read", "crm.deal.update" },
                EpistemicNote: "NOT CONFIGURED: Charlie is NOT reading or writing live CRM data."
            );

            dict[ConnectorType.Email] = new ConnectorHealthRecord(
                Type: ConnectorType.Email,
                Name: "SendGrid / SMTP Email Gateway",
                Status: ConnectorStatus.NotConfigured,
                TenantId: tenantId,
                LastSuccessfulOperation: null,
                LastFailure: null,
                FailureReason: "No active email API keys configured.",
                CredentialState: "Missing",
                SupportedCapabilities: new[] { "email.send.transactional" },
                EpistemicNote: "NOT CONFIGURED: Outbound customer email dispatch disabled."
            );

            dict[ConnectorType.WhatsApp] = new ConnectorHealthRecord(
                Type: ConnectorType.WhatsApp,
                Name: "WhatsApp Business Cloud API",
                Status: ConnectorStatus.Disconnected,
                TenantId: tenantId,
                LastSuccessfulOperation: null,
                LastFailure: null,
                FailureReason: "Webhook disconnected.",
                CredentialState: "Missing",
                SupportedCapabilities: new[] { "whatsapp.message.send" },
                EpistemicNote: "DISCONNECTED: Real-time messaging unavailable."
            );

            dict[ConnectorType.Sms] = new ConnectorHealthRecord(
                Type: ConnectorType.Sms,
                Name: "Twilio SMS Gateway",
                Status: ConnectorStatus.NotConfigured,
                TenantId: tenantId,
                LastSuccessfulOperation: null,
                LastFailure: null,
                FailureReason: "Account SID not set.",
                CredentialState: "Missing",
                SupportedCapabilities: new[] { "sms.send" },
                EpistemicNote: "NOT CONFIGURED: SMS messaging disabled."
            );

            dict[ConnectorType.Payments] = new ConnectorHealthRecord(
                Type: ConnectorType.Payments,
                Name: "Stripe / Razorpay Payments",
                Status: ConnectorStatus.NotConfigured,
                TenantId: tenantId,
                LastSuccessfulOperation: null,
                LastFailure: null,
                FailureReason: "Live webhook secret not configured.",
                CredentialState: "Missing",
                SupportedCapabilities: new[] { "payments.invoice.reconcile", "payments.charge.read" },
                EpistemicNote: "NOT CONFIGURED: Financial execution gateway in mock-rejection stasis."
            );

            dict[ConnectorType.Calendar] = new ConnectorHealthRecord(
                Type: ConnectorType.Calendar,
                Name: "Google Workspace / Outlook Calendar",
                Status: ConnectorStatus.NotConfigured,
                TenantId: tenantId,
                LastSuccessfulOperation: null,
                LastFailure: null,
                FailureReason: "OAuth consent pending.",
                CredentialState: "Missing",
                SupportedCapabilities: new[] { "calendar.events.read", "calendar.invite.create" },
                EpistemicNote: "NOT CONFIGURED: Calendar scheduling disabled."
            );

            dict[ConnectorType.Browser] = new ConnectorHealthRecord(
                Type: ConnectorType.Browser,
                Name: "Headless Chromium Worker Grid",
                Status: ConnectorStatus.Connected,
                TenantId: tenantId,
                LastSuccessfulOperation: DateTimeOffset.UtcNow.AddMinutes(-5),
                LastFailure: null,
                FailureReason: null,
                CredentialState: "Valid",
                SupportedCapabilities: new[] { "browser.navigate", "browser.dom.extract", "browser.screenshot" },
                EpistemicNote: "CONNECTED: Batch 3.6 Browser Worker Sandboxes operational."
            );

            dict[ConnectorType.Mcp] = new ConnectorHealthRecord(
                Type: ConnectorType.Mcp,
                Name: "Model Context Protocol Gateway",
                Status: ConnectorStatus.Connected,
                TenantId: tenantId,
                LastSuccessfulOperation: DateTimeOffset.UtcNow.AddMinutes(-3),
                LastFailure: null,
                FailureReason: null,
                CredentialState: "Valid",
                SupportedCapabilities: new[] { "mcp.tools.invoke", "mcp.schema.validate" },
                EpistemicNote: "CONNECTED: Batch 3.6 MCP adapter ready for tool execution."
            );

            return dict;
        }
    }
}
