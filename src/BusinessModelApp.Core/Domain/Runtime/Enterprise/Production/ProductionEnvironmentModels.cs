using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Production
{
    public enum ProductionEnvironmentTier
    {
        Development = 0,
        Staging = 1,
        Production = 2
    }

    public enum ProductionEnvironmentState
    {
        Uninitialized = 0,
        Initializing = 1,
        Ready = 2,
        Degraded = 3,
        Halted = 4
    }

    public sealed class SecretBrokerToken
    {
        public string TokenId { get; init; } = Guid.NewGuid().ToString("N");
        public required string TenantId { get; init; }
        public required string ConnectorId { get; init; }
        public required string CapabilityScope { get; init; } // e.g. "EMAIL_SEND", "BANK_RECONCILE", "CRM_WRITE"
        public DateTime IssuedAtUtc { get; init; } = DateTime.UtcNow;
        public DateTime ExpiresAtUtc { get; init; }
        public string ScopedTokenDigestSha256 { get; init; } = string.Empty;
        public bool IsRevoked { get; private set; }

        public bool IsValid => !IsRevoked && DateTime.UtcNow <= ExpiresAtUtc;

        public void Revoke() => IsRevoked = true;
    }

    public sealed class ProductionReadinessCheck
    {
        public string CheckId { get; init; } = Guid.NewGuid().ToString("N");
        public bool DockerRuntimeReady { get; init; }
        public bool PostgresPersistenceReady { get; init; }
        public bool SecretBrokerReady { get; init; }
        public bool ExecutionFirewallReady { get; init; }
        public bool KillSwitchArmed { get; init; }
        public bool ClockSynchronized { get; init; }
        public bool TlsEnabled { get; init; }
        public DateTime CheckedAtUtc { get; init; } = DateTime.UtcNow;

        public bool IsFullyReady =>
            DockerRuntimeReady &&
            PostgresPersistenceReady &&
            SecretBrokerReady &&
            ExecutionFirewallReady &&
            KillSwitchArmed &&
            ClockSynchronized &&
            TlsEnabled;
    }

    public sealed class ProductionActivationRecord
    {
        public string ActivationId { get; init; } = Guid.NewGuid().ToString("N");
        public required string TenantId { get; init; }
        public ProductionEnvironmentTier Tier { get; init; } = ProductionEnvironmentTier.Production;
        public ProductionEnvironmentState State { get; private set; } = ProductionEnvironmentState.Ready;
        public required string ActivatedBySignoffId { get; init; }
        public DateTime ActivatedAtUtc { get; init; } = DateTime.UtcNow;
        public List<string> ActiveConnectorIds { get; } = new();

        public void Halt(string reason)
        {
            State = ProductionEnvironmentState.Halted;
        }

        public void Resume()
        {
            State = ProductionEnvironmentState.Ready;
        }
    }
}
