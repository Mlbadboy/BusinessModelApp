using System;
using System.Collections.Generic;
using BusinessModelApp.Core.Domain.Responsibilities;
using BusinessModelApp.Core.Domain.Runtime.Reputation;

namespace BusinessModelApp.Core.Domain.Runtime.Capabilities
{
    public enum CapabilityRiskTier
    {
        R0_Informational = 0,
        R1_LowAnalytical = 1,
        R2_MediumPredictive = 2,
        R3_HighOperational = 3,
        R4_CriticalFinancial = 4,
        R5_IrreversibleStrategic = 5
    }

    /// <summary>
    /// Formal registry definition of an executable capability.
    /// Foundation for Batch 3.4 routing, Batch 3.6 workers, and Batch 3.7 capability factory.
    /// </summary>
    public record CapabilityDefinitionRecord
    {
        public CapabilityId CapabilityId { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public StructuredDomainContext DomainContext { get; init; } = StructuredDomainContext.Default;

        public string InputSchemaJson { get; init; } = "{}";
        public string OutputSchemaJson { get; init; } = "{}";

        public AutonomyTier RequiredAutonomyTier { get; init; } = AutonomyTier.L1_Advise;
        public CapabilityRiskTier RiskTier { get; init; } = CapabilityRiskTier.R1_LowAnalytical;

        public TimeSpan DefaultTimeout { get; init; } = TimeSpan.FromMinutes(5);
        public int DefaultMaxRetries { get; init; } = 2;

        public long EstimatedTokens { get; init; } = 5_000;
        public decimal EstimatedCostUsd { get; init; } = 0.25m;

        public string? EvaluationSuiteId { get; init; }
        public bool IsActive { get; init; } = true;
        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? DeprecatedAt { get; init; }

        public bool IsColdStartSafe => RiskTier <= CapabilityRiskTier.R2_MediumPredictive;
    }

    /// <summary>
    /// Governed binding authorizing an AgentDefinition to provide a specific Capability.
    /// </summary>
    public record AgentCapabilityBinding
    {
        public Guid BindingId { get; init; } = Guid.NewGuid();
        public AgentDefinitionId AgentDefinitionId { get; init; }
        public CapabilityId CapabilityId { get; init; }
        public AutonomyTier MaxPermittedAutonomyTier { get; init; } = AutonomyTier.L1_Advise;
        public bool IsEnabled { get; init; } = true;
        public DateTimeOffset BoundAt { get; init; } = DateTimeOffset.UtcNow;
    }
}
