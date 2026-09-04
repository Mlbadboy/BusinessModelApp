using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Connectors
{
    public enum ConnectorStatus
    {
        Healthy = 1,
        Degraded = 2,
        Disconnected = 3
    }

    public class ConnectorHealthProfile
    {
        public string ConnectorId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public ConnectorStatus Status { get; set; } = ConnectorStatus.Healthy;
        public bool IsAuthenticated { get; set; } = true;
        public int RemainingQuota { get; set; } = 1000;
        public int LatencyMs { get; set; } = 250;
        public double ErrorRate { get; set; } = 0.01; // 1%
        public double FreshnessScore { get; set; } = 0.98;
        public List<string> SupportedCapabilities { get; set; } = new();
        public List<string> AllowedTenants { get; set; } = new() { "*" }; // Wildcard = all tenants
    }

    public class CapabilityResolutionResult
    {
        public bool IsAvailable { get; set; }
        public string SelectedConnectorId { get; set; } = string.Empty;
        public string ConnectorName { get; set; } = string.Empty;
        public int EstimatedLatencyMs { get; set; }
        public string ResolutionReason { get; set; } = string.Empty;
    }

    public interface ICapabilityRegistry
    {
        void RegisterConnector(ConnectorHealthProfile profile);
        CapabilityResolutionResult ResolveCapability(string capabilityName, string tenantId);
        IReadOnlyList<ConnectorHealthProfile> GetAllConnectors();
    }
}
