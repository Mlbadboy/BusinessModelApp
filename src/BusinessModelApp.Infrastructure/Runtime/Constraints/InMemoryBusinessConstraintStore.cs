using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Constraints;
using BusinessModelApp.Core.Interfaces.Runtime.Constraints;

namespace BusinessModelApp.Infrastructure.Runtime.Constraints
{
    public class InMemoryBusinessConstraintStore : IBusinessConstraintStore
    {
        // Key: $"{workspaceId}:{constraintId}"
        private readonly ConcurrentDictionary<string, BusinessConstraintDefinition> _constraints = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<Guid, List<BusinessConstraintDefinition>> _constraintHistory = new();

        // Key: workspaceId
        private readonly ConcurrentDictionary<Guid, StrategicRegimePolicy> _strategicPolicies = new();
        private readonly ConcurrentDictionary<Guid, DigitalTwinBusinessState> _twinStates = new();

        // Key: reservationId
        private readonly ConcurrentDictionary<Guid, ResourceReservation> _reservations = new();

        // Key: decisionId
        private readonly ConcurrentDictionary<Guid, ResourceArbitrationDecision> _arbitrations = new();

        public Task SaveConstraintAsync(BusinessConstraintDefinition constraint, CancellationToken ct = default)
        {
            if (constraint == null) throw new ArgumentNullException(nameof(constraint));

            var key = $"{constraint.WorkspaceId}:{constraint.ConstraintId.Value}";
            constraint.VersionHash = constraint.ComputeHash();
            _constraints[key] = constraint;

            _constraintHistory.AddOrUpdate(
                constraint.ConstraintId.Value,
                new List<BusinessConstraintDefinition> { constraint },
                (_, list) =>
                {
                    lock (list) { list.Add(constraint); }
                    return list;
                });

            return Task.CompletedTask;
        }

        public Task<BusinessConstraintDefinition?> GetConstraintAsync(Guid workspaceId, ConstraintId constraintId, CancellationToken ct = default)
        {
            var key = $"{workspaceId}:{constraintId.Value}";
            _constraints.TryGetValue(key, out var constraint);
            return Task.FromResult(constraint);
        }

        public Task<IReadOnlyList<BusinessConstraintDefinition>> GetActiveConstraintsAsync(Guid workspaceId, CancellationToken ct = default)
        {
            var active = _constraints.Values
                .Where(c => c.WorkspaceId == workspaceId && c.IsActive && (c.ExpiresAt == null || c.ExpiresAt > DateTimeOffset.UtcNow))
                .ToList();
            return Task.FromResult<IReadOnlyList<BusinessConstraintDefinition>>(active);
        }

        public Task<IReadOnlyList<BusinessConstraintDefinition>> GetConstraintsByTypeAsync(Guid workspaceId, ConstraintType type, CancellationToken ct = default)
        {
            var matched = _constraints.Values
                .Where(c => c.WorkspaceId == workspaceId && c.Type == type && c.IsActive)
                .ToList();
            return Task.FromResult<IReadOnlyList<BusinessConstraintDefinition>>(matched);
        }

        public Task SaveStrategicPolicyAsync(StrategicRegimePolicy policy, CancellationToken ct = default)
        {
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            _strategicPolicies[policy.WorkspaceId] = policy;
            return Task.CompletedTask;
        }

        public Task<StrategicRegimePolicy?> GetStrategicPolicyAsync(Guid workspaceId, CancellationToken ct = default)
        {
            _strategicPolicies.TryGetValue(workspaceId, out var policy);
            return Task.FromResult(policy);
        }

        public Task SaveReservationAsync(ResourceReservation reservation, CancellationToken ct = default)
        {
            if (reservation == null) throw new ArgumentNullException(nameof(reservation));
            _reservations[reservation.ReservationId.Value] = reservation;
            return Task.CompletedTask;
        }

        public Task<ResourceReservation?> GetReservationAsync(ReservationId reservationId, CancellationToken ct = default)
        {
            _reservations.TryGetValue(reservationId.Value, out var reservation);
            return Task.FromResult(reservation);
        }

        public Task<IReadOnlyList<ResourceReservation>> GetActiveReservationsAsync(Guid workspaceId, ResourceClass resourceClass, CancellationToken ct = default)
        {
            var now = DateTimeOffset.UtcNow;
            var active = _reservations.Values
                .Where(r => r.WorkspaceId == workspaceId &&
                            r.ResourceClass == resourceClass &&
                            (r.State == ReservationState.Reserved || r.State == ReservationState.Committed) &&
                            r.ExpiresAt > now)
                .ToList();
            return Task.FromResult<IReadOnlyList<ResourceReservation>>(active);
        }

        public Task SaveArbitrationDecisionAsync(ResourceArbitrationDecision decision, CancellationToken ct = default)
        {
            if (decision == null) throw new ArgumentNullException(nameof(decision));
            _arbitrations[decision.DecisionId] = decision;
            return Task.CompletedTask;
        }

        public Task<ResourceArbitrationDecision?> GetArbitrationDecisionAsync(Guid decisionId, CancellationToken ct = default)
        {
            _arbitrations.TryGetValue(decisionId, out var decision);
            return Task.FromResult(decision);
        }

        public Task SaveDigitalTwinStateAsync(DigitalTwinBusinessState state, CancellationToken ct = default)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            _twinStates[state.WorkspaceId] = state;
            return Task.CompletedTask;
        }

        public Task<DigitalTwinBusinessState?> GetDigitalTwinStateAsync(Guid workspaceId, CancellationToken ct = default)
        {
            _twinStates.TryGetValue(workspaceId, out var state);
            return Task.FromResult(state);
        }
    }
}
