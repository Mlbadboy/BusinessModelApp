using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Domain.Runtime.Constraints;

namespace BusinessModelApp.Core.Interfaces.Runtime.Constraints
{
    public interface IBusinessConstraintStore
    {
        Task SaveConstraintAsync(BusinessConstraintDefinition constraint, CancellationToken ct = default);
        Task<BusinessConstraintDefinition?> GetConstraintAsync(Guid workspaceId, ConstraintId constraintId, CancellationToken ct = default);
        Task<IReadOnlyList<BusinessConstraintDefinition>> GetActiveConstraintsAsync(Guid workspaceId, CancellationToken ct = default);
        Task<IReadOnlyList<BusinessConstraintDefinition>> GetConstraintsByTypeAsync(Guid workspaceId, ConstraintType type, CancellationToken ct = default);

        Task SaveStrategicPolicyAsync(StrategicRegimePolicy policy, CancellationToken ct = default);
        Task<StrategicRegimePolicy?> GetStrategicPolicyAsync(Guid workspaceId, CancellationToken ct = default);

        Task SaveReservationAsync(ResourceReservation reservation, CancellationToken ct = default);
        Task<ResourceReservation?> GetReservationAsync(ReservationId reservationId, CancellationToken ct = default);
        Task<IReadOnlyList<ResourceReservation>> GetActiveReservationsAsync(Guid workspaceId, ResourceClass resourceClass, CancellationToken ct = default);

        Task SaveArbitrationDecisionAsync(ResourceArbitrationDecision decision, CancellationToken ct = default);
        Task<ResourceArbitrationDecision?> GetArbitrationDecisionAsync(Guid decisionId, CancellationToken ct = default);

        Task SaveDigitalTwinStateAsync(DigitalTwinBusinessState state, CancellationToken ct = default);
        Task<DigitalTwinBusinessState?> GetDigitalTwinStateAsync(Guid workspaceId, CancellationToken ct = default);
    }

    public interface IConstraintFreshnessEngine
    {
        bool IsFresh(DateTimeOffset telemetryTimestamp, TimeSpan freshnessRequirement, out TimeSpan age);
    }

    public interface IBusinessConstraintEngine
    {
        Task<BusinessFeasibilityResult> EvaluateFeasibilityAsync(
            Guid workspaceId,
            PreFlightEffectProposal proposal,
            DigitalTwinBusinessState? currentTwinState = null,
            CancellationToken ct = default);

        Task<ConstraintEvaluationResult> EvaluateSingleConstraintAsync(
            BusinessConstraintDefinition constraint,
            DigitalTwinBusinessState twinState,
            PreFlightEffectProposal? proposedEffect = null,
            CancellationToken ct = default);
    }

    public interface IStrategicRegimeEngine
    {
        Task<StrategicRegimePolicy> GetActivePolicyAsync(Guid workspaceId, CancellationToken ct = default);
        Task SetRegimeAsync(Guid workspaceId, StrategicRegimeType regime, string authority, CancellationToken ct = default);
        double ComputeStrategicUtility(StrategicRegimePolicy policy, ArbitrationCandidate candidate);
    }

    public interface IResourceReservationEngine
    {
        Task<ReservationResult> RequestReservationAsync(
            Guid workspaceId,
            MissionId missionId,
            MissionNodeId? nodeId,
            ResourceClass resourceClass,
            double requestedAmount,
            string idempotencyKey,
            TimeSpan? ttl = null,
            CancellationToken ct = default);

        Task<bool> CommitReservationAsync(ReservationId reservationId, double consumedAmount, CancellationToken ct = default);
        Task<bool> ReleaseReservationAsync(ReservationId reservationId, CancellationToken ct = default);
        Task<double> GetAvailableResourceAmountAsync(Guid workspaceId, ResourceClass resourceClass, CancellationToken ct = default);
    }

    public interface IStrategicArbitrationEngine
    {
        Task<ResourceArbitrationDecision> ArbitrateAsync(
            Guid workspaceId,
            ResourceClass resourceClass,
            IReadOnlyList<ArbitrationCandidate> candidates,
            CancellationToken ct = default);
    }

    public interface IPreFlightSimulationEngine
    {
        Task<PreFlightSimulationResult> SimulateEffectAsync(
            Guid workspaceId,
            PreFlightEffectProposal proposal,
            DigitalTwinBusinessState currentTwinState,
            CancellationToken ct = default);
    }

    public interface ISafeAlternativeEngine
    {
        Task<IReadOnlyList<SafeAlternativeProposal>> GenerateAlternativesAsync(
            Guid workspaceId,
            PreFlightEffectProposal blockedProposal,
            DigitalTwinBusinessState twinState,
            IReadOnlyList<ConstraintEvaluationResult> hardViolations,
            CancellationToken ct = default);
    }
}
