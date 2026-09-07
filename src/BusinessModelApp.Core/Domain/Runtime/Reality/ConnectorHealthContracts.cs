using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Reality
{
    /// <summary>
    /// Governed external connector categories.
    /// </summary>
    public enum ConnectorType
    {
        Crm,
        Email,
        WhatsApp,
        Sms,
        Payments,
        Calendar,
        Browser,
        Mcp
    }

    /// <summary>
    /// Health status of an external connector.
    /// </summary>
    public enum ConnectorStatus
    {
        Connected,
        Degraded,
        Disconnected,
        NotConfigured,
        AuthenticationFailed
    }

    /// <summary>
    /// Verified reality record for an external integration connector.
    /// </summary>
    public sealed record ConnectorHealthRecord(
        ConnectorType Type,
        string Name,
        ConnectorStatus Status,
        string TenantId,
        DateTimeOffset? LastSuccessfulOperation,
        DateTimeOffset? LastFailure,
        string? FailureReason,
        string CredentialState, // "Valid", "Missing", "Expired", "Revoked"
        IReadOnlyList<string> SupportedCapabilities,
        string EpistemicNote
    );

    /// <summary>
    /// Comprehensive system-level reality monitor for the Charlie Business OS.
    /// </summary>
    public sealed record SystemRealityOverview(
        RealityMode Mode,
        string TenantId,
        RealityStatus DatabaseStatus,
        RealityStatus DigitalTwinStatus,
        RealityStatus MissionRuntimeStatus,
        RealityStatus WorkerFabricStatus,
        RealityStatus BrainFabricStatus,
        RealityStatus EventBusStatus,
        RealityStatus ExecutionFirewallStatus,
        string BrainProvider,
        string BrainModel,
        int ActiveMissionsCount,
        int ProvisionedWorkersCount,
        int PendingApprovalsCount,
        DateTimeOffset? LastRealEventTimestamp,
        IReadOnlyList<ConnectorHealthRecord> Connectors,
        DateTimeOffset GeneratedAt
    );
}
