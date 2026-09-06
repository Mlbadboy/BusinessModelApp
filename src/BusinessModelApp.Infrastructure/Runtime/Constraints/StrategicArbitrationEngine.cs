using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Constraints;
using BusinessModelApp.Core.Interfaces.Runtime.Constraints;

namespace BusinessModelApp.Infrastructure.Runtime.Constraints
{
    public class StrategicArbitrationEngine : IStrategicArbitrationEngine
    {
        private readonly IBusinessConstraintStore _store;
        private readonly IStrategicRegimeEngine _regimeEngine;
        private readonly IResourceReservationEngine _reservationEngine;

        public StrategicArbitrationEngine(
            IBusinessConstraintStore store,
            IStrategicRegimeEngine regimeEngine,
            IResourceReservationEngine reservationEngine)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _regimeEngine = regimeEngine ?? throw new ArgumentNullException(nameof(regimeEngine));
            _reservationEngine = reservationEngine ?? throw new ArgumentNullException(nameof(reservationEngine));
        }

        public async Task<ResourceArbitrationDecision> ArbitrateAsync(
            Guid workspaceId,
            ResourceClass resourceClass,
            IReadOnlyList<ArbitrationCandidate> candidates,
            CancellationToken ct = default)
        {
            if (candidates == null || candidates.Count == 0)
                throw new ArgumentException("Candidate list cannot be empty for arbitration.", nameof(candidates));

            var policy = await _regimeEngine.GetActivePolicyAsync(workspaceId, ct);
            double available = await _reservationEngine.GetAvailableResourceAmountAsync(workspaceId, resourceClass, ct);

            var evaluations = new List<CandidateArbitrationEvaluation>();

            foreach (var candidate in candidates)
            {
                // Step 1: Evaluate hard feasibility bounds
                bool canAfford = candidate.RequestedAmount <= available;
                bool passesHard = canAfford && candidate.RiskScore < 0.90;

                string? ineligibility = null;
                if (!canAfford)
                {
                    ineligibility = $"Requested amount ₹{candidate.RequestedAmount:N2} exceeds available ₹{available:N2}.";
                }
                else if (candidate.RiskScore >= 0.90)
                {
                    ineligibility = $"Candidate risk score {candidate.RiskScore:F2} exceeds maximum safety ceiling 0.90.";
                }

                // Step 2 & 3: Compute strategic utility and risk-adjusted scores
                double strategicUtility = _regimeEngine.ComputeStrategicUtility(policy, candidate);
                double riskAdjustedScore = strategicUtility * (1.0 - (candidate.RiskScore * 0.5));

                evaluations.Add(new CandidateArbitrationEvaluation
                {
                    MissionId = candidate.MissionId,
                    NodeId = candidate.NodeId,
                    PassesHardConstraints = passesHard,
                    IneligibilityReason = ineligibility,
                    StrategicUtilityScore = Math.Round(strategicUtility, 4),
                    RiskAdjustedScore = Math.Round(riskAdjustedScore, 4),
                    LexicographicRank = (int)candidate.Priority
                });
            }

            // Step 4-7: Deterministic ranking:
            // 1. Must pass hard constraints
            // 2. Mission Priority (P0 > P1 > P2 > P3)
            // 3. Risk-adjusted strategic score
            // 4. Requested amount efficiency (return per unit committed)
            // 5. Tie-breaker: stable GUID comparison (Zero randomness, Zero LLM bidding)
            var eligible = evaluations.Where(e => e.PassesHardConstraints).ToList();

            CandidateArbitrationEvaluation? winner = eligible
                .OrderBy(e => e.LexicographicRank) // P0 (0) beats P1 (1)
                .ThenByDescending(e => e.RiskAdjustedScore)
                .ThenByDescending(e => candidates.First(c => c.MissionId == e.MissionId).ExpectedReturnOnInvestment / Math.Max(candidates.First(c => c.MissionId == e.MissionId).RequestedAmount, 1.0))
                .ThenBy(e => e.MissionId.Value) // deterministic tie-breaker
                .FirstOrDefault();

            ReservationId? resId = null;
            double allocated = 0.0;
            string rationale;

            if (winner != null)
            {
                var winCandidate = candidates.First(c => c.MissionId == winner.MissionId);
                var resResult = await _reservationEngine.RequestReservationAsync(
                    workspaceId,
                    winner.MissionId,
                    winner.NodeId,
                    resourceClass,
                    winCandidate.RequestedAmount,
                    $"arbitration_{winner.MissionId.Value}",
                    TimeSpan.FromMinutes(15),
                    ct);

                if (resResult.IsGranted && resResult.Reservation != null)
                {
                    resId = resResult.Reservation.ReservationId;
                    allocated = winCandidate.RequestedAmount;
                    rationale = $"Mission '{winner.MissionId.Value}' selected. Met hard constraints, priority P{(int)winCandidate.Priority}, strategic score {winner.RiskAdjustedScore:F3} under regime '{policy.Regime}', atomically reserved {allocated:N2} {resourceClass}.";
                }
                else
                {
                    rationale = $"Winner '{winner.MissionId.Value}' could not reserve resources atomically: {resResult.FailureReason}";
                    winner = null;
                }
            }
            else
            {
                rationale = "No eligible candidate passed hard business constraints and resource availability.";
            }

            var rawAudit = $"{workspaceId}:{resourceClass}:{policy.Regime}:{policy.PolicyVersion}:{winner?.MissionId.Value}:{allocated}";
            using var sha = SHA256.Create();
            var auditHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(rawAudit)));

            var decision = new ResourceArbitrationDecision
            {
                WorkspaceId = workspaceId,
                ResourceClass = resourceClass,
                TotalAvailableAmount = available,
                StrategicRegime = policy.Regime,
                CandidateEvaluations = evaluations,
                WinningMissionId = winner?.MissionId,
                WinningNodeId = winner?.NodeId,
                AllocatedAmount = allocated,
                ReservationId = resId,
                SelectionRationale = rationale,
                TieBreakReason = "Deterministic lexicographic ranking with priority & risk adjustment",
                ArbitratedAt = DateTimeOffset.UtcNow,
                AuditHash = auditHash
            };

            await _store.SaveArbitrationDecisionAsync(decision, ct);
            return decision;
        }
    }
}
