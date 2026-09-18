using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime.Missions;
using BusinessModelApp.Core.Domain.Runtime.Organizational;
using BusinessModelApp.Core.Interfaces.Runtime.Missions;

namespace BusinessModelApp.Infrastructure.Runtime.Missions.Coordination
{
    public class MissionAdmissionController : IMissionAdmissionController
    {
        private readonly IMissionCoordinationStore _store;
        private readonly TenantMissionConcurrencyPolicy _systemPolicy;
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, int>> _activeMissions = new();
        private long _fencingCounter = 100;
        private readonly object _counterLock = new();

        public MissionAdmissionController(
            IMissionCoordinationStore store,
            TenantMissionConcurrencyPolicy? systemPolicy = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _systemPolicy = systemPolicy ?? new TenantMissionConcurrencyPolicy();
        }

        public async Task<(bool Admitted, MissionAdmissionTicket? Ticket, string? Reason)> EvaluateAdmissionAsync(
            string tenantId,
            MissionGraphProposal proposal,
            string workId,
            WorkRiskTier riskTier,
            TenantMissionConcurrencyPolicy? policyOverride = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId is required.", nameof(tenantId));
            if (proposal == null) throw new ArgumentNullException(nameof(proposal));
            if (string.IsNullOrWhiteSpace(workId)) throw new ArgumentException("WorkId is required.", nameof(workId));

            var effectivePolicy = TenantMissionConcurrencyPolicy.ResolveEffective(_systemPolicy, policyOverride);

            // 1. Structural Checks (I27-B: Depth <= 5, Fanout <= 10)
            int nodeCount = proposal.ProposedNodes.Count;
            if (nodeCount > 25)
            {
                return (false, null, $"Proposal node count ({nodeCount}) exceeds max limit (25).");
            }

            int graphDepth = ComputeGraphDepth(proposal);
            if (graphDepth > 5)
            {
                return (false, null, $"Proposal graph depth ({graphDepth}) exceeds constitutional limit (5) per I27-B.");
            }

            int maxFanout = ComputeMaxFanout(proposal);
            if (maxFanout > 10)
            {
                return (false, null, $"Proposal max fanout ({maxFanout}) exceeds constitutional limit (10) per I27-B.");
            }

            // 2. Budget Ceiling Checks
            if (proposal.EstimatedBudgetTokens > effectivePolicy.MaxTokenBudget)
            {
                return (false, null, $"Estimated budget tokens ({proposal.EstimatedBudgetTokens}) exceeds policy limit ({effectivePolicy.MaxTokenBudget}).");
            }

            if (proposal.EstimatedCostUsd > effectivePolicy.MaxCostUsd)
            {
                return (false, null, $"Estimated cost USD (${proposal.EstimatedCostUsd}) exceeds policy limit (${effectivePolicy.MaxCostUsd}).");
            }

            // 3. Concurrency Ceiling Checks
            var tenantActive = _activeMissions.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, int>());
            int currentActiveCount = tenantActive.Count;
            int currentActiveNodes = tenantActive.Values.Sum();

            long fencingToken;
            lock (_counterLock)
            {
                fencingToken = Interlocked.Increment(ref _fencingCounter);
            }

            var ticket = new MissionAdmissionTicket
            {
                TicketId = $"TICK-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                TenantId = tenantId,
                MissionId = proposal.MissionId.ToString(),
                WorkId = workId,
                MissionGraphHash = proposal.ProposalId.ToString(),
                RiskTier = riskTier,
                FencingToken = fencingToken,
                IssuedUtc = DateTime.UtcNow,
                ExpiresUtc = DateTime.UtcNow.Add(effectivePolicy.MaxMissionLifetime),
                PolicySnapshotHash = $"CONCURRENCY-v1:ACTIVE-{effectivePolicy.MaxActiveMissions}:NODES-{effectivePolicy.MaxActiveNodes}"
            };

            // Check if active limit reached -> Queueing per I27-F
            if (currentActiveCount >= effectivePolicy.MaxActiveMissions ||
                (currentActiveNodes + nodeCount) > effectivePolicy.MaxActiveNodes)
            {
                // Check queued limit
                var existingTickets = await _store.ListTicketsAsync(tenantId, ct);
                int queuedCount = existingTickets.Count(t => t.Decision == AdmissionDecisionStatus.Queued && t.ExpiresUtc > DateTime.UtcNow);

                if (queuedCount >= effectivePolicy.MaxQueuedMissions)
                {
                    ticket.Decision = AdmissionDecisionStatus.Rejected;
                    ticket.ComputeProvenance();
                    await _store.SaveTicketAsync(ticket, ct);
                    return (false, ticket, $"Tenant mission concurrency and queue limit ({effectivePolicy.MaxQueuedMissions}) reached.");
                }

                ticket.Decision = AdmissionDecisionStatus.Queued;
                ticket.ComputeProvenance();
                await _store.SaveTicketAsync(ticket, ct);
                return (false, ticket, $"Admitted into mission coordination queue (Active: {currentActiveCount}/{effectivePolicy.MaxActiveMissions}).");
            }

            // Admitted into execution fabric
            tenantActive[proposal.MissionId.ToString()] = nodeCount;
            ticket.Decision = AdmissionDecisionStatus.Admitted;
            ticket.ComputeProvenance();
            await _store.SaveTicketAsync(ticket, ct);

            return (true, ticket, null);
        }

        public Task CompleteMissionAsync(string tenantId, string missionId, CancellationToken ct = default)
        {
            if (_activeMissions.TryGetValue(tenantId, out var dict))
            {
                dict.TryRemove(missionId, out _);
            }
            return Task.CompletedTask;
        }

        private static int ComputeGraphDepth(MissionGraphProposal proposal)
        {
            if (proposal.ProposedNodes.Count == 0) return 0;
            var adjacency = new Dictionary<string, List<string>>();
            foreach (var n in proposal.ProposedNodes)
            {
                adjacency[n.NodeId] = new List<string>();
            }
            foreach (var e in proposal.ProposedEdges)
            {
                if (adjacency.ContainsKey(e.SourceNodeId))
                {
                    adjacency[e.SourceNodeId].Add(e.TargetNodeId);
                }
            }

            int maxDepth = 1;
            foreach (var node in proposal.ProposedNodes)
            {
                int depth = DfsDepth(node.NodeId, adjacency, new HashSet<string>());
                if (depth > maxDepth) maxDepth = depth;
            }
            return maxDepth;
        }

        private static int DfsDepth(string current, Dictionary<string, List<string>> adj, HashSet<string> visited)
        {
            if (visited.Contains(current)) return 0; // Avoid infinite loop in cyclic input
            visited.Add(current);
            int maxChild = 0;
            if (adj.TryGetValue(current, out var children))
            {
                foreach (var c in children)
                {
                    int d = DfsDepth(c, adj, new HashSet<string>(visited));
                    if (d > maxChild) maxChild = d;
                }
            }
            return 1 + maxChild;
        }

        private static int ComputeMaxFanout(MissionGraphProposal proposal)
        {
            if (proposal.ProposedEdges.Count == 0) return 0;
            return proposal.ProposedEdges
                .GroupBy(e => e.SourceNodeId)
                .Select(g => g.Count())
                .DefaultIfEmpty(0)
                .Max();
        }
    }
}
