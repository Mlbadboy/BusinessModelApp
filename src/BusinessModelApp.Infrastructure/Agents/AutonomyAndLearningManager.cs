using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using BusinessModelApp.Core.Agents;

namespace BusinessModelApp.Infrastructure.Agents
{
    public class AutonomyManager : IAutonomyManager
    {
        private readonly ConcurrentDictionary<string, AutonomyTier> _agentTiers = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, List<ShadowModeComparison>> _shadowHistory = new(StringComparer.OrdinalIgnoreCase);

        public AutonomyTier GetCurrentTier(string agentId)
        {
            if (string.IsNullOrWhiteSpace(agentId)) return AutonomyTier.L0_Observer;
            return _agentTiers.TryGetValue(agentId, out var tier) ? tier : AutonomyTier.L0_Observer;
        }

        public void SetTierDirect(string agentId, AutonomyTier tier)
        {
            if (string.IsNullOrWhiteSpace(agentId)) return;
            _agentTiers[agentId] = tier;
        }

        public void RecordShadowComparison(string agentId, string proposedActionJson, string actualHumanActionJson, double fidelityScore)
        {
            if (string.IsNullOrWhiteSpace(agentId)) return;

            var comparison = new ShadowModeComparison
            {
                AgentId = agentId,
                ProposedActionJson = proposedActionJson,
                ActualHumanActionJson = actualHumanActionJson,
                FidelityScore = Math.Clamp(fidelityScore, 0.0, 1.0),
                EvaluatedAtUtc = DateTime.UtcNow
            };

            var list = _shadowHistory.GetOrAdd(agentId, _ => new List<ShadowModeComparison>());
            lock (list)
            {
                list.Add(comparison);
            }
        }

        public double GetAverageShadowFidelity(string agentId)
        {
            if (string.IsNullOrWhiteSpace(agentId)) return 0.0;
            if (!_shadowHistory.TryGetValue(agentId, out var list)) return 0.0;

            lock (list)
            {
                return list.Count == 0 ? 0.0 : list.Average(c => c.FidelityScore);
            }
        }

        public bool RequestPromotion(string agentId, AutonomyTier targetTier, string operatorId, bool humanApproved)
        {
            if (string.IsNullOrWhiteSpace(agentId)) return false;

            // Invariant: Agents cannot self-promote. Must be an external operator.
            if (string.IsNullOrWhiteSpace(operatorId) || operatorId.Equals(agentId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Invariant: Explicit human approval is mandatory for any tier promotion
            if (!humanApproved)
            {
                return false;
            }

            var current = GetCurrentTier(agentId);
            if (targetTier <= current)
            {
                return false;
            }

            // Invariant: For promotion to Copilot (L2) or higher, verified shadow fidelity must be >= 0.85
            if (targetTier >= AutonomyTier.L2_Copilot)
            {
                var avgFidelity = GetAverageShadowFidelity(agentId);
                if (avgFidelity < 0.85)
                {
                    return false;
                }
            }

            _agentTiers[agentId] = targetTier;
            return true;
        }
    }

    public class GovernedLearningLoop : IGovernedLearningLoop
    {
        private readonly ConcurrentDictionary<string, LearningCandidate> _candidates = new(StringComparer.OrdinalIgnoreCase);

        public LearningCandidate ProposeImprovement(string targetArea, string proposedImprovement)
        {
            var candidate = new LearningCandidate
            {
                TargetArea = targetArea ?? string.Empty,
                ProposedImprovement = proposedImprovement ?? string.Empty,
                Status = LearningCandidateStatus.Proposed,
                CreatedAtUtc = DateTime.UtcNow
            };

            _candidates[candidate.CandidateId] = candidate;
            return candidate;
        }

        public void RecordBenchmarkEvaluation(string candidateId, double benchmarkScore)
        {
            if (_candidates.TryGetValue(candidateId, out var candidate))
            {
                candidate.BenchmarkScore = Math.Clamp(benchmarkScore, 0.0, 1.0);
                candidate.Status = LearningCandidateStatus.Evaluating;
            }
        }

        public bool ApproveCandidate(string candidateId, string governanceApproverId)
        {
            if (string.IsNullOrWhiteSpace(governanceApproverId)) return false;
            if (!_candidates.TryGetValue(candidateId, out var candidate)) return false;

            // Invariant: Benchmark score must be at least 0.85 before governance approval can be granted
            if (candidate.BenchmarkScore < 0.85)
            {
                candidate.Status = LearningCandidateStatus.Rejected;
                return false;
            }

            candidate.Status = LearningCandidateStatus.ApprovedByGovernance;
            candidate.ApprovedBy = governanceApproverId;
            candidate.ApprovedAtUtc = DateTime.UtcNow;
            return true;
        }

        public IReadOnlyList<LearningCandidate> GetPendingCandidates()
        {
            return _candidates.Values
                .Where(c => c.Status == LearningCandidateStatus.Proposed || c.Status == LearningCandidateStatus.Evaluating)
                .OrderBy(c => c.CreatedAtUtc)
                .ToList();
        }

        public IReadOnlyList<LearningCandidate> GetApprovedCandidates()
        {
            return _candidates.Values
                .Where(c => c.Status == LearningCandidateStatus.ApprovedByGovernance)
                .OrderByDescending(c => c.ApprovedAtUtc)
                .ToList();
        }
    }
}
