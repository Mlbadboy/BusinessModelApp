using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using BusinessModelApp.Core.Connectors;

namespace BusinessModelApp.Infrastructure.Connectors
{
    public class CapabilityRegistryService : ICapabilityRegistry
    {
        private readonly ConcurrentDictionary<string, ConnectorHealthProfile> _connectors = new(StringComparer.OrdinalIgnoreCase);

        public CapabilityRegistryService()
        {
            SeedStandardConnectors();
        }

        private void SeedStandardConnectors()
        {
            RegisterConnector(new ConnectorHealthProfile
            {
                ConnectorId = "gmail-enterprise",
                Name = "Google Workspace Gmail",
                Status = ConnectorStatus.Healthy,
                IsAuthenticated = true,
                RemainingQuota = 5000,
                LatencyMs = 200,
                ErrorRate = 0.005,
                FreshnessScore = 0.99,
                SupportedCapabilities = new List<string> { "CAPABILITY_SEND_EMAIL", "CAPABILITY_READ_INBOX" },
                AllowedTenants = new List<string> { "*" }
            });

            RegisterConnector(new ConnectorHealthProfile
            {
                ConnectorId = "salesforce-crm-prod",
                Name = "Salesforce Enterprise CRM",
                Status = ConnectorStatus.Healthy,
                IsAuthenticated = true,
                RemainingQuota = 20000,
                LatencyMs = 350,
                ErrorRate = 0.01,
                FreshnessScore = 0.98,
                SupportedCapabilities = new List<string> { "CAPABILITY_PROSPECT_RESEARCH", "CAPABILITY_UPDATE_OPPORTUNITY" },
                AllowedTenants = new List<string> { "*" }
            });

            RegisterConnector(new ConnectorHealthProfile
            {
                ConnectorId = "razorpay-live",
                Name = "Razorpay Payment Gateway",
                Status = ConnectorStatus.Healthy,
                IsAuthenticated = true,
                RemainingQuota = 100000,
                LatencyMs = 150,
                ErrorRate = 0.002,
                FreshnessScore = 1.0,
                SupportedCapabilities = new List<string> { "CAPABILITY_PROCESS_PAYMENT", "CAPABILITY_GENERATE_INVOICE" },
                AllowedTenants = new List<string> { "*" }
            });
        }

        public void RegisterConnector(ConnectorHealthProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            _connectors[profile.ConnectorId] = profile;
        }

        public CapabilityResolutionResult ResolveCapability(string capabilityName, string tenantId)
        {
            if (string.IsNullOrWhiteSpace(capabilityName)) throw new ArgumentNullException(nameof(capabilityName));
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));

            var candidates = _connectors.Values
                .Where(c => c.SupportedCapabilities.Contains(capabilityName, StringComparer.OrdinalIgnoreCase))
                .Where(c => c.AllowedTenants.Contains("*") || c.AllowedTenants.Contains(tenantId, StringComparer.OrdinalIgnoreCase))
                .Where(c => c.Status != ConnectorStatus.Disconnected)
                .Where(c => c.IsAuthenticated && c.RemainingQuota > 0)
                .ToList();

            if (!candidates.Any())
            {
                return new CapabilityResolutionResult
                {
                    IsAvailable = false,
                    ResolutionReason = $"No healthy, authenticated connector available for capability '{capabilityName}' for tenant '{tenantId}'."
                };
            }

            // Select healthiest connector (lowest latency and error rate)
            var selected = candidates
                .OrderBy(c => c.Status == ConnectorStatus.Degraded ? 1 : 0)
                .ThenBy(c => c.LatencyMs)
                .ThenBy(c => c.ErrorRate)
                .First();

            return new CapabilityResolutionResult
            {
                IsAvailable = true,
                SelectedConnectorId = selected.ConnectorId,
                ConnectorName = selected.Name,
                EstimatedLatencyMs = selected.LatencyMs,
                ResolutionReason = $"Resolved to {selected.Name} (Latency: {selected.LatencyMs}ms, Error rate: {selected.ErrorRate:P1})."
            };
        }

        public IReadOnlyList<ConnectorHealthProfile> GetAllConnectors()
        {
            return _connectors.Values.ToList();
        }
    }
}
