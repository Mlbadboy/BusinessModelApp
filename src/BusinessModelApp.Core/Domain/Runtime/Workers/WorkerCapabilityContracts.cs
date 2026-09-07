using System;
using System.Collections.Generic;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Responsibilities;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Domain.Runtime.Capabilities;
using BusinessModelApp.Core.Domain.Runtime.Reputation;

namespace BusinessModelApp.Core.Domain.Runtime.Workers
{
    /// <summary>
    /// Universal Capability Definition: Describes WHAT can be performed,
    /// which modalities are permitted, and the required governance tiers.
    /// </summary>
    public class UniversalCapabilityDefinition
    {
        public CapabilityId CapabilityId { get; init; }
        public string Version { get; init; } = "1.0.0";
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public List<WorkerModality> AllowedModalities { get; init; } = new();
        public CapabilityRiskTier RequiredRiskTier { get; init; } = CapabilityRiskTier.R1_LowAnalytical;
        public AutonomyTier RequiredAutonomyTier { get; init; } = AutonomyTier.L3_Prepare;
        public bool IsConsequential { get; init; }
        public string InputSchemaJson { get; init; } = "{}";
        public string OutputSchemaJson { get; init; } = "{}";
        public string VerificationCriteria { get; init; } = string.Empty;
        public TimeSpan DefaultTimeout { get; init; } = TimeSpan.FromMinutes(2);
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Request submitted to the Worker Resolver to select the optimal worker and modality.
    /// </summary>
    public class WorkerResolutionRequest
    {
        public CapabilityId CapabilityId { get; init; }
        public Guid WorkspaceId { get; init; }
        public MissionId MissionId { get; init; }
        public MissionNodeId? NodeId { get; init; }
        public StructuredDomainContext DomainContext { get; init; } = new();
        public AutonomyTier AutonomyCeiling { get; init; } = AutonomyTier.L5_ExecuteBounded;
        public double MaxBudgetINR { get; init; } = 100_000.0;
        public WorkerModality? PreferredModality { get; init; }
    }

    /// <summary>
    /// Deterministic decision emitted by the Universal Worker Resolver.
    /// </summary>
    public class WorkerResolutionDecision
    {
        public Guid DecisionId { get; init; } = Guid.NewGuid();
        public Guid WorkspaceId { get; init; }
        public CapabilityId CapabilityId { get; init; }
        public WorkerDefinitionId? SelectedWorkerDefinitionId { get; init; }
        public WorkerModality SelectedModality { get; init; }
        public WorkerModality? FallbackModality { get; init; }
        public double ModalityFitnessScore { get; init; }
        public double EmpiricalReputationScore { get; init; }
        public bool IsAdmissible { get; init; }
        public string? InadmissibilityReason { get; init; }
        public string SelectionRationale { get; init; } = string.Empty;
        public DateTimeOffset ResolvedAt { get; init; } = DateTimeOffset.UtcNow;
    }
}
