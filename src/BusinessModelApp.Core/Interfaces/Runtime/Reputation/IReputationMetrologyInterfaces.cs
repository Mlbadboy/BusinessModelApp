using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.ExternalReality;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Domain.Runtime.Capabilities;
using BusinessModelApp.Core.Domain.Runtime.Fleet;
using BusinessModelApp.Core.Domain.Runtime.Reputation;
using BusinessModelApp.Core.Interfaces.Missions;

namespace BusinessModelApp.Core.Interfaces.Runtime.Reputation
{
    public interface ICapabilityRegistry
    {
        Task RegisterCapabilityAsync(CapabilityDefinitionRecord capability, CancellationToken ct = default);
        Task<CapabilityDefinitionRecord?> GetCapabilityAsync(CapabilityId capabilityId, CancellationToken ct = default);
        Task<IReadOnlyList<CapabilityDefinitionRecord>> ListCapabilitiesAsync(CancellationToken ct = default);
        Task BindAgentCapabilityAsync(AgentCapabilityBinding binding, CancellationToken ct = default);
        Task<bool> IsAgentBoundToCapabilityAsync(AgentDefinitionId agentId, CapabilityId capabilityId, CancellationToken ct = default);
        Task<IReadOnlyList<CapabilityId>> GetCapabilitiesForAgentAsync(AgentDefinitionId agentId, CancellationToken ct = default);
    }

    public interface ICausalAttributionEngine
    {
        Task<CausalAttributionRecord> EvaluateAttributionAsync(
            ExecutionAttemptId attemptId,
            MissionNodeRecord node,
            AgentOutcomeProposal proposal,
            NodeVerificationResult verification,
            CancellationToken ct = default);
    }

    public interface ICalibrationEngine
    {
        Task<OutcomeCalibrationRecord> CalculateCalibrationAsync(
            ExecutionAttemptId attemptId,
            MissionNodeRecord node,
            AgentOutcomeProposal proposal,
            NodeVerificationResult verification,
            StructuredDomainContext domainContext,
            MarketRegimeState regime,
            CancellationToken ct = default);
    }

    public interface IEmpiricalPerformanceEngine
    {
        Task<CapabilityPerformanceProfile> ProcessEvidenceTokenAsync(ReputationEvidenceToken token, CancellationToken ct = default);
        Task<CapabilityPerformanceProfile?> GetProfileAsync(Guid workspaceId, AgentDefinitionId agentId, CapabilityId capabilityId, StructuredDomainContext domainContext, MarketRegimeState regime, CancellationToken ct = default);
        Task<AgentPerformanceProfile?> GetAgentProfileAsync(Guid workspaceId, AgentDefinitionId agentId, CancellationToken ct = default);
        Task<CapabilityVersionProfile?> GetCapabilityProfileAsync(CapabilityId capabilityId, CancellationToken ct = default);
        Task ApplyQuarantineAsync(Guid workspaceId, AgentDefinitionId agentId, CapabilityId capabilityId, string reason, CancellationToken ct = default);
    }

    public interface IReputationAwareRouter
    {
        Task<RoutingDecisionRecord> RouteNodeWorkerAsync(
            MissionGraph graph,
            MissionNodeRecord node,
            IReadOnlyList<WorkerProcessRecord> candidates,
            TenantMissionPolicyContext tenantPolicy,
            MarketRegimeState regime = MarketRegimeState.Stable,
            CancellationToken ct = default);
    }

    public interface IReputationStore
    {
        Task SaveProfileAsync(CapabilityPerformanceProfile profile, CancellationToken ct = default);
        Task<CapabilityPerformanceProfile?> GetProfileAsync(Guid workspaceId, AgentDefinitionId agentId, CapabilityId capabilityId, string canonicalDomainKey, MarketRegimeState regime, CancellationToken ct = default);
        Task<IReadOnlyList<CapabilityPerformanceProfile>> GetProfileHistoryAsync(Guid profileId, CancellationToken ct = default);

        Task SaveAgentProfileAsync(AgentPerformanceProfile profile, CancellationToken ct = default);
        Task<AgentPerformanceProfile?> GetAgentProfileAsync(Guid workspaceId, AgentDefinitionId agentId, CancellationToken ct = default);

        Task SaveCapabilityProfileAsync(CapabilityVersionProfile profile, CancellationToken ct = default);
        Task<CapabilityVersionProfile?> GetCapabilityProfileAsync(CapabilityId capabilityId, CancellationToken ct = default);

        Task SaveEvidenceTokenAsync(ReputationEvidenceToken token, CancellationToken ct = default);
        Task<IReadOnlyList<ReputationEvidenceToken>> GetEvidenceTokensForAttemptAsync(ExecutionAttemptId attemptId, CancellationToken ct = default);

        Task SaveRoutingDecisionAsync(RoutingDecisionRecord decision, CancellationToken ct = default);
        Task<RoutingDecisionRecord?> GetRoutingDecisionAsync(Guid decisionId, CancellationToken ct = default);

        Task BindWorkerAgentAsync(WorkerProcessId workerId, AgentDefinitionId agentId, CancellationToken ct = default);
        Task<AgentDefinitionId?> GetAgentForWorkerAsync(WorkerProcessId workerId, CancellationToken ct = default);
        Task QuarantineCapabilityProfilesAsync(Guid workspaceId, AgentDefinitionId agentId, CapabilityId capabilityId, string reason, CancellationToken ct = default);
    }
}
