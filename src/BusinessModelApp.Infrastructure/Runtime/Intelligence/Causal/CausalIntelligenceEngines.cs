using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Causal;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Kernel;
using BusinessModelApp.Core.Interfaces.Runtime.Intelligence.Causal;
using BusinessModelApp.Core.Interfaces.Runtime.Intelligence.Kernel;

namespace BusinessModelApp.Infrastructure.Runtime.Intelligence.Causal
{
    // =========================================================================
    // 1. CAUSAL GRAPH ENGINE (DAG & CYCLE PREVENTION)
    // =========================================================================

    public sealed class CausalGraphEngine : ICausalGraphEngine
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, CausalNode>> _nodes = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, CausalEdge>> _edges = new();

        public Task<CausalGraph> GetGraphAsync(string tenantId, CancellationToken ct = default)
        {
            var tenantNodes = _nodes.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, CausalNode>()).Values.ToList();
            var tenantEdges = _edges.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, CausalEdge>()).Values.ToList();

            var graph = new CausalGraph(
                GraphId: $"graph-{tenantId}",
                TenantId: tenantId,
                Nodes: tenantNodes,
                Edges: tenantEdges,
                LastUpdatedUtc: DateTime.UtcNow);

            return Task.FromResult(graph);
        }

        public Task<CausalNode> AddNodeAsync(CausalNode node, CancellationToken ct = default)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            var tenantNodes = _nodes.GetOrAdd(node.TenantId, _ => new ConcurrentDictionary<string, CausalNode>());
            tenantNodes[node.NodeId] = node;
            return Task.FromResult(node);
        }

        public Task<CausalEdge> AddEdgeAsync(CausalEdge edge, CancellationToken ct = default)
        {
            if (edge == null) throw new ArgumentNullException(nameof(edge));

            var tenantNodes = _nodes.GetOrAdd(edge.TenantId, _ => new ConcurrentDictionary<string, CausalNode>());
            if (!tenantNodes.ContainsKey(edge.SourceNodeId))
                throw new KeyNotFoundException($"Source node '{edge.SourceNodeId}' does not exist in graph for tenant '{edge.TenantId}'.");
            if (!tenantNodes.ContainsKey(edge.TargetNodeId))
                throw new KeyNotFoundException($"Target node '{edge.TargetNodeId}' does not exist in graph for tenant '{edge.TenantId}'.");

            if (edge.SourceNodeId == edge.TargetNodeId)
                throw new InvalidOperationException($"Self-loops are forbidden in causal DAGs: {edge.SourceNodeId} -> {edge.TargetNodeId}.");

            var tenantEdges = _edges.GetOrAdd(edge.TenantId, _ => new ConcurrentDictionary<string, CausalEdge>());

            // Cycle Detection: Check if target can reach source (if so, adding edge creates a cycle)
            if (CanReach(edge.TenantId, edge.TargetNodeId, edge.SourceNodeId, tenantEdges.Values))
            {
                throw new InvalidOperationException($"Adding edge '{edge.SourceNodeId}' -> '{edge.TargetNodeId}' creates a cycle in the causal graph. DAG violation.");
            }

            tenantEdges[edge.EdgeId] = edge;
            return Task.FromResult(edge);
        }

        public Task<bool> ValidateAcyclicAsync(string tenantId, CancellationToken ct = default)
        {
            var tenantEdges = _edges.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, CausalEdge>()).Values;
            var tenantNodes = _nodes.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, CausalNode>()).Keys;

            var inDegree = tenantNodes.ToDictionary(n => n, _ => 0);
            var adj = tenantNodes.ToDictionary(n => n, _ => new List<string>());

            foreach (var edge in tenantEdges)
            {
                if (adj.ContainsKey(edge.SourceNodeId) && inDegree.ContainsKey(edge.TargetNodeId))
                {
                    adj[edge.SourceNodeId].Add(edge.TargetNodeId);
                    inDegree[edge.TargetNodeId]++;
                }
            }

            var queue = new Queue<string>(inDegree.Where(kvp => kvp.Value == 0).Select(kvp => kvp.Key));
            int visited = 0;

            while (queue.Count > 0)
            {
                var u = queue.Dequeue();
                visited++;
                foreach (var v in adj[u])
                {
                    inDegree[v]--;
                    if (inDegree[v] == 0)
                        queue.Enqueue(v);
                }
            }

            return Task.FromResult(visited == tenantNodes.Count());
        }

        public Task<IReadOnlyList<string>> GetTopologicalSortAsync(string tenantId, CancellationToken ct = default)
        {
            var tenantEdges = _edges.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, CausalEdge>()).Values;
            var tenantNodes = _nodes.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, CausalNode>()).Keys.ToList();

            var inDegree = tenantNodes.ToDictionary(n => n, _ => 0);
            var adj = tenantNodes.ToDictionary(n => n, _ => new List<string>());

            foreach (var edge in tenantEdges)
            {
                if (adj.ContainsKey(edge.SourceNodeId) && inDegree.ContainsKey(edge.TargetNodeId))
                {
                    adj[edge.SourceNodeId].Add(edge.TargetNodeId);
                    inDegree[edge.TargetNodeId]++;
                }
            }

            var queue = new Queue<string>(inDegree.Where(kvp => kvp.Value == 0).Select(kvp => kvp.Key));
            var order = new List<string>();

            while (queue.Count > 0)
            {
                var u = queue.Dequeue();
                order.Add(u);
                foreach (var v in adj[u])
                {
                    inDegree[v]--;
                    if (inDegree[v] == 0)
                        queue.Enqueue(v);
                }
            }

            if (order.Count != tenantNodes.Count)
                throw new InvalidOperationException("Graph contains cycles; topological order does not exist.");

            return Task.FromResult<IReadOnlyList<string>>(order);
        }

        public Task<IReadOnlyList<CausalNode>> GetAncestorsAsync(string tenantId, string nodeId, CancellationToken ct = default)
        {
            var tenantNodes = _nodes.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, CausalNode>());
            var tenantEdges = _edges.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, CausalEdge>()).Values;

            var incoming = new Dictionary<string, List<string>>();
            foreach (var e in tenantEdges)
            {
                if (!incoming.ContainsKey(e.TargetNodeId)) incoming[e.TargetNodeId] = new List<string>();
                incoming[e.TargetNodeId].Add(e.SourceNodeId);
            }

            var ancestors = new HashSet<string>();
            var queue = new Queue<string>();
            queue.Enqueue(nodeId);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (incoming.TryGetValue(current, out var parents))
                {
                    foreach (var p in parents)
                    {
                        if (ancestors.Add(p)) queue.Enqueue(p);
                    }
                }
            }

            var result = ancestors.Where(tenantNodes.ContainsKey).Select(id => tenantNodes[id]).ToList();
            return Task.FromResult<IReadOnlyList<CausalNode>>(result);
        }

        public Task<IReadOnlyList<CausalNode>> GetDescendantsAsync(string tenantId, string nodeId, CancellationToken ct = default)
        {
            var tenantNodes = _nodes.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, CausalNode>());
            var tenantEdges = _edges.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, CausalEdge>()).Values;

            var outgoing = new Dictionary<string, List<string>>();
            foreach (var e in tenantEdges)
            {
                if (!outgoing.ContainsKey(e.SourceNodeId)) outgoing[e.SourceNodeId] = new List<string>();
                outgoing[e.SourceNodeId].Add(e.TargetNodeId);
            }

            var descendants = new HashSet<string>();
            var queue = new Queue<string>();
            queue.Enqueue(nodeId);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (outgoing.TryGetValue(current, out var children))
                {
                    foreach (var c in children)
                    {
                        if (descendants.Add(c)) queue.Enqueue(c);
                    }
                }
            }

            var result = descendants.Where(tenantNodes.ContainsKey).Select(id => tenantNodes[id]).ToList();
            return Task.FromResult<IReadOnlyList<CausalNode>>(result);
        }

        public Task<IReadOnlyList<CausalEdge>> GetIncomingEdgesAsync(string tenantId, string nodeId, CancellationToken ct = default)
        {
            var tenantEdges = _edges.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, CausalEdge>()).Values;
            var incoming = tenantEdges.Where(e => e.TargetNodeId == nodeId).ToList();
            return Task.FromResult<IReadOnlyList<CausalEdge>>(incoming);
        }

        public Task<IReadOnlyList<CausalEdge>> GetOutgoingEdgesAsync(string tenantId, string nodeId, CancellationToken ct = default)
        {
            var tenantEdges = _edges.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, CausalEdge>()).Values;
            var outgoing = tenantEdges.Where(e => e.SourceNodeId == nodeId).ToList();
            return Task.FromResult<IReadOnlyList<CausalEdge>>(outgoing);
        }

        private static bool CanReach(string tenantId, string start, string target, IEnumerable<CausalEdge> edges)
        {
            var adj = new Dictionary<string, List<string>>();
            foreach (var e in edges)
            {
                if (!adj.ContainsKey(e.SourceNodeId)) adj[e.SourceNodeId] = new List<string>();
                adj[e.SourceNodeId].Add(e.TargetNodeId);
            }

            var visited = new HashSet<string>();
            var queue = new Queue<string>();
            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                if (cur == target) return true;

                if (adj.TryGetValue(cur, out var neighbors))
                {
                    foreach (var next in neighbors)
                    {
                        if (visited.Add(next)) queue.Enqueue(next);
                    }
                }
            }

            return false;
        }
    }

    // =========================================================================
    // 2. CAUSAL HYPOTHESIS ENGINE
    // =========================================================================

    public sealed class CausalHypothesisEngine : ICausalHypothesisEngine
    {
        private readonly ConcurrentDictionary<string, CausalHypothesis> _hypotheses = new();

        public Task<CausalHypothesis> FormulateHypothesisAsync(
            string tenantId,
            string causeMetricId,
            string effectMetricId,
            string mechanism,
            IEnumerable<string>? statedAssumptions = null,
            IEnumerable<string>? alternativeExplanationIds = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(causeMetricId)) throw new ArgumentException("Cause metric ID is required.", nameof(causeMetricId));
            if (string.IsNullOrWhiteSpace(effectMetricId)) throw new ArgumentException("Effect metric ID is required.", nameof(effectMetricId));
            if (string.IsNullOrWhiteSpace(mechanism)) throw new ArgumentException("Mechanism description is required.", nameof(mechanism));

            var hypothesisId = $"hyp-{Guid.NewGuid():N}";
            var assumptions = (statedAssumptions ?? Array.Empty<string>()).ToList();
            var alternatives = (alternativeExplanationIds ?? Array.Empty<string>()).ToList();

            var initialConfidence = 0.50m; // Initial prior hypothesis confidence
            var status = CausalHypothesisStatus.Hypothesized;
            var tier = EvidenceTier.ObservationalCorrelation;

            var hash = CausalHypothesis.ComputeIntegrityHash(
                tenantId, causeMetricId, effectMetricId, mechanism, initialConfidence, status, tier, Array.Empty<string>());

            var hypothesis = new CausalHypothesis(
                HypothesisId: hypothesisId,
                TenantId: tenantId,
                CauseMetricId: causeMetricId,
                EffectMetricId: effectMetricId,
                MechanismDescription: mechanism,
                SupportingEvidenceIds: new List<string>(),
                ContradictoryEvidenceIds: new List<string>(),
                StatedAssumptions: assumptions,
                MissingEvidence: new List<string> { "Experimental intervention data not yet available" },
                AlternativeExplanationIds: alternatives,
                CausalConfidenceScore: initialConfidence,
                Status: status,
                HighestEvidenceTier: tier,
                CreatedAtUtc: DateTime.UtcNow,
                IntegrityHash: hash);

            _hypotheses[hypothesisId] = hypothesis;
            return Task.FromResult(hypothesis);
        }

        public Task<CausalHypothesis> AttachEvidenceAsync(
            string hypothesisId,
            string evidenceId,
            bool isSupporting,
            EvidenceTier tier,
            CancellationToken ct = default)
        {
            if (!_hypotheses.TryGetValue(hypothesisId, out var existing))
                throw new KeyNotFoundException($"Causal hypothesis '{hypothesisId}' not found.");

            var supporting = existing.SupportingEvidenceIds.ToList();
            var contradictory = existing.ContradictoryEvidenceIds.ToList();

            if (isSupporting)
            {
                if (!supporting.Contains(evidenceId)) supporting.Add(evidenceId);
            }
            else
            {
                if (!contradictory.Contains(evidenceId)) contradictory.Add(evidenceId);
            }

            var highestTier = tier > existing.HighestEvidenceTier ? tier : existing.HighestEvidenceTier;

            // Adjust confidence score
            var score = existing.CausalConfidenceScore;
            if (isSupporting)
            {
                score = Math.Min(0.95m, score + 0.08m * (int)tier);
            }
            else
            {
                score = Math.Max(0.05m, score - 0.20m * (int)tier);
            }

            var newStatus = existing.Status;
            if (contradictory.Count > supporting.Count && score < 0.30m)
            {
                newStatus = CausalHypothesisStatus.Weakened;
            }
            else if (score < 0.15m)
            {
                newStatus = CausalHypothesisStatus.Refuted;
            }

            var hash = CausalHypothesis.ComputeIntegrityHash(
                existing.TenantId, existing.CauseMetricId, existing.EffectMetricId,
                existing.MechanismDescription, score, newStatus, highestTier, supporting.Concat(contradictory));

            var updated = existing with
            {
                SupportingEvidenceIds = supporting,
                ContradictoryEvidenceIds = contradictory,
                HighestEvidenceTier = highestTier,
                CausalConfidenceScore = score,
                Status = newStatus,
                IntegrityHash = hash
            };

            _hypotheses[hypothesisId] = updated;
            return Task.FromResult(updated);
        }

        public Task<CausalHypothesis?> GetHypothesisAsync(string hypothesisId, CancellationToken ct = default)
        {
            _hypotheses.TryGetValue(hypothesisId, out var hyp);
            return Task.FromResult(hyp);
        }

        public Task<IReadOnlyList<CausalHypothesis>> GetHypothesesForMetricAsync(string tenantId, string metricId, CancellationToken ct = default)
        {
            var list = _hypotheses.Values
                .Where(h => h.TenantId == tenantId && (h.CauseMetricId == metricId || h.EffectMetricId == metricId))
                .ToList();
            return Task.FromResult<IReadOnlyList<CausalHypothesis>>(list);
        }

        public Task<IReadOnlyList<CausalHypothesis>> GetAllHypothesesAsync(string tenantId, CancellationToken ct = default)
        {
            var list = _hypotheses.Values.Where(h => h.TenantId == tenantId).ToList();
            return Task.FromResult<IReadOnlyList<CausalHypothesis>>(list);
        }

        public Task<CausalHypothesis> UpdateHypothesisStatusAsync(string hypothesisId, CausalHypothesisStatus status, CancellationToken ct = default)
        {
            if (!_hypotheses.TryGetValue(hypothesisId, out var existing))
                throw new KeyNotFoundException($"Causal hypothesis '{hypothesisId}' not found.");

            var hash = CausalHypothesis.ComputeIntegrityHash(
                existing.TenantId, existing.CauseMetricId, existing.EffectMetricId,
                existing.MechanismDescription, existing.CausalConfidenceScore, status, existing.HighestEvidenceTier,
                existing.SupportingEvidenceIds.Concat(existing.ContradictoryEvidenceIds));

            var updated = existing with
            {
                Status = status,
                IntegrityHash = hash
            };

            _hypotheses[hypothesisId] = updated;
            return Task.FromResult(updated);
        }
    }

    // =========================================================================
    // 3. CONFOUNDER DETECTION ENGINE
    // =========================================================================

    public sealed class ConfounderDetector : IConfounderDetector
    {
        private readonly ICausalGraphEngine _graphEngine;

        public ConfounderDetector(ICausalGraphEngine graphEngine)
        {
            _graphEngine = graphEngine ?? throw new ArgumentNullException(nameof(graphEngine));
        }

        public async Task<ConfounderAssessment> AssessConfoundersAsync(
            string tenantId,
            string causeMetricId,
            string effectMetricId,
            CancellationToken ct = default)
        {
            var graph = await _graphEngine.GetGraphAsync(tenantId, ct);

            // Find common ancestors of cause and effect
            var causeAncestors = await _graphEngine.GetAncestorsAsync(tenantId, causeMetricId, ct);
            var effectAncestors = await _graphEngine.GetAncestorsAsync(tenantId, effectMetricId, ct);

            var commonAncestorIds = causeAncestors.Select(a => a.NodeId)
                .Intersect(effectAncestors.Select(a => a.NodeId))
                .Where(id => id != causeMetricId && id != effectMetricId)
                .ToList();

            bool isConfounded = commonAncestorIds.Count > 0;
            var riskLevel = ConfounderRiskLevel.None;

            if (commonAncestorIds.Count == 1) riskLevel = ConfounderRiskLevel.Medium;
            else if (commonAncestorIds.Count >= 2) riskLevel = ConfounderRiskLevel.High;

            var mitigation = isConfounded
                ? $"Conditioning on confounders [{string.Join(", ", commonAncestorIds)}] is required to block backdoor paths."
                : "No common ancestors detected in causal graph.";

            return new ConfounderAssessment(
                AssessmentId: $"conf-{Guid.NewGuid():N}",
                TenantId: tenantId,
                CauseMetricId: causeMetricId,
                EffectMetricId: effectMetricId,
                IsConfounded: isConfounded,
                ConfounderMetricIds: commonAncestorIds,
                RiskLevel: riskLevel,
                RequiredAdjustmentSet: commonAncestorIds,
                MitigationNotes: mitigation,
                AssessedAtUtc: DateTime.UtcNow);
        }
    }

    // =========================================================================
    // 4. TEMPORAL CAUSAL INVESTIGATOR (LEAD/LAG PRECEDENCE METROLOGY)
    // =========================================================================

    public sealed class TemporalCausalInvestigator : ITemporalCausalInvestigator
    {
        private readonly IKpiObservationStore _observationStore;

        public TemporalCausalInvestigator(IKpiObservationStore observationStore)
        {
            _observationStore = observationStore ?? throw new ArgumentNullException(nameof(observationStore));
        }

        public async Task<TemporalPrecedenceResult> InvestigateTemporalPrecedenceAsync(
            string tenantId,
            string metricA,
            string metricB,
            TimeSpan? maxLag = null,
            CancellationToken ct = default)
        {
            var fromUtc = DateTime.UtcNow.AddDays(-30);
            var toUtc = DateTime.UtcNow;
            var obsA = await _observationStore.GetObservationsAsync(tenantId, metricA, fromUtc, toUtc, ct);
            var obsB = await _observationStore.GetObservationsAsync(tenantId, metricB, fromUtc, toUtc, ct);

            if (obsA.Count < 3 || obsB.Count < 3)
            {
                return new TemporalPrecedenceResult(
                    ResultId: $"prec-{Guid.NewGuid():N}",
                    TenantId: tenantId,
                    MetricA: metricA,
                    MetricB: metricB,
                    OptimalLag: TimeSpan.Zero,
                    PrecedenceScore: 0.0m,
                    CrossCorrelationAtLag: 0.0m,
                    Direction: TemporalPrecedenceDirection.Indeterminate,
                    MeetsCandidatePrecedenceThreshold: false,
                    CaveatMessage: "Invariant I19-B: Insufficient observations to evaluate temporal precedence. UNKNOWN preserved.",
                    EvaluatedAtUtc: DateTime.UtcNow);
            }

            // Align by nearest timestamps
            var sortedA = obsA.OrderBy(o => o.ObservedAtUtc).Select(o => (double)o.Value).ToList();
            var sortedB = obsB.OrderBy(o => o.ObservedAtUtc).Select(o => (double)o.Value).ToList();

            int n = Math.Min(sortedA.Count, sortedB.Count);
            var aSlice = sortedA.Take(n).ToList();
            var bSlice = sortedB.Take(n).ToList();

            // Evaluate lag 1 cross correlation
            double bestCorr = 0.0;
            int bestLag = 0;

            for (int lag = 1; lag <= Math.Min(3, n / 2); lag++)
            {
                var aLagged = aSlice.Take(n - lag).ToList();
                var bForward = bSlice.Skip(lag).ToList();

                var corr = ComputePearson(aLagged, bForward);
                if (Math.Abs(corr) > Math.Abs(bestCorr))
                {
                    bestCorr = corr;
                    bestLag = lag;
                }
            }

            var precedenceScore = (decimal)bestCorr;
            var direction = TemporalPrecedenceDirection.Indeterminate;
            bool meetsThreshold = Math.Abs(precedenceScore) > 0.50m;

            if (meetsThreshold)
            {
                direction = precedenceScore > 0 ? TemporalPrecedenceDirection.Leading : TemporalPrecedenceDirection.Lagging;
            }

            var optimalTimeSpan = TimeSpan.FromHours(bestLag * 2);

            return new TemporalPrecedenceResult(
                ResultId: $"prec-{Guid.NewGuid():N}",
                TenantId: tenantId,
                MetricA: metricA,
                MetricB: metricB,
                OptimalLag: optimalTimeSpan,
                PrecedenceScore: Math.Round(precedenceScore, 4),
                CrossCorrelationAtLag: Math.Round((decimal)bestCorr, 4),
                Direction: direction,
                MeetsCandidatePrecedenceThreshold: meetsThreshold,
                CaveatMessage: "Invariant I19-B: Temporal precedence (lead/lag) is candidate observational evidence only; post hoc ergo propter hoc fallacy is rejected. Correlation and temporal order do NOT prove causation.",
                EvaluatedAtUtc: DateTime.UtcNow);
        }

        private static double ComputePearson(List<double> x, List<double> y)
        {
            if (x.Count != y.Count || x.Count < 2) return 0.0;
            double meanX = x.Average();
            double meanY = y.Average();

            double cov = 0.0, varX = 0.0, varY = 0.0;
            for (int i = 0; i < x.Count; i++)
            {
                double dx = x[i] - meanX;
                double dy = y[i] - meanY;
                cov += dx * dy;
                varX += dx * dx;
                varY += dy * dy;
            }

            if (varX <= 1e-12 || varY <= 1e-12) return 0.0;
            return cov / Math.Sqrt(varX * varY);
        }
    }

    // =========================================================================
    // 5. INTERVENTION / COUNTERFACTUAL SIMULATOR (DO-CALCULUS SANDBOX)
    // =========================================================================

    public sealed class InterventionSimulator : IInterventionSimulator
    {
        private readonly ICausalGraphEngine _graphEngine;
        private readonly IConfounderDetector _confounderDetector;

        public InterventionSimulator(ICausalGraphEngine graphEngine, IConfounderDetector confounderDetector)
        {
            _graphEngine = graphEngine ?? throw new ArgumentNullException(nameof(graphEngine));
            _confounderDetector = confounderDetector ?? throw new ArgumentNullException(nameof(confounderDetector));
        }

        public async Task<CausalIdentifiabilityStatus> CheckIdentifiabilityAsync(
            string tenantId,
            string targetMetricId,
            IEnumerable<string>? conditioningNodes = null,
            CancellationToken ct = default)
        {
            var ancestors = await _graphEngine.GetAncestorsAsync(tenantId, targetMetricId, ct);
            if (ancestors.Count == 0) return CausalIdentifiabilityStatus.Identified;

            var condSet = new HashSet<string>(conditioningNodes ?? Array.Empty<string>());

            // Check unblocked backdoor paths
            var incoming = await _graphEngine.GetIncomingEdgesAsync(tenantId, targetMetricId, ct);
            var backdoorEdges = incoming.Where(e => e.IsBackdoorPath).ToList();

            if (backdoorEdges.Count == 0)
                return CausalIdentifiabilityStatus.Identified;

            bool allBlocked = backdoorEdges.All(e => condSet.Contains(e.SourceNodeId));
            if (allBlocked)
                return CausalIdentifiabilityStatus.Identified;

            bool someBlocked = backdoorEdges.Any(e => condSet.Contains(e.SourceNodeId));
            return someBlocked ? CausalIdentifiabilityStatus.PartiallyIdentified : CausalIdentifiabilityStatus.NotIdentified;
        }

        public async Task<InterventionSimulationResult> SimulateInterventionAsync(
            InterventionSpec spec,
            CancellationToken ct = default)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));

            var identifiability = await CheckIdentifiabilityAsync(spec.TenantId, spec.TargetMetricId, spec.ConditioningNodes, ct);

            // Hardening Rule 2: If effect is NotIdentified, do NOT generate unjustified counterfactual claims!
            if (identifiability == CausalIdentifiabilityStatus.NotIdentified)
            {
                return new InterventionSimulationResult(
                    ResultId: $"sim-{Guid.NewGuid():N}",
                    TenantId: spec.TenantId,
                    TargetMetricId: spec.TargetMetricId,
                    Delta: spec.TargetValueDelta,
                    IdentifiabilityStatus: CausalIdentifiabilityStatus.NotIdentified,
                    PredictedMetricDeltas: new Dictionary<string, decimal>(),
                    UncertaintyRadius: 1.00m,
                    EpistemicKind: EpistemicKind.Simulation,
                    ActiveAssumptions: new[] { "Unblocked backdoor paths present; causal effect is not non-parametrically identifiable." },
                    Explanation: "Invariant I19-F: Causal effect is not identifiable from current DAG without conditioning on unobserved confounders. Counterfactual simulation fails-closed.",
                    SimulatedAtUtc: DateTime.UtcNow);
            }

            // Downstream effect simulation via DAG propagation
            var outgoingEdges = await _graphEngine.GetOutgoingEdgesAsync(spec.TenantId, spec.TargetMetricId, ct);
            var predicted = new Dictionary<string, decimal>();

            foreach (var edge in outgoingEdges)
            {
                var downstreamDelta = spec.TargetValueDelta * edge.EstimatedStrength;
                predicted[edge.TargetNodeId] = Math.Round(downstreamDelta, 4);
            }

            var uncertainty = identifiability == CausalIdentifiabilityStatus.Identified ? 0.15m : 0.45m;

            return new InterventionSimulationResult(
                ResultId: $"sim-{Guid.NewGuid():N}",
                TenantId: spec.TenantId,
                TargetMetricId: spec.TargetMetricId,
                Delta: spec.TargetValueDelta,
                IdentifiabilityStatus: identifiability,
                PredictedMetricDeltas: predicted,
                UncertaintyRadius: uncertainty,
                EpistemicKind: EpistemicKind.Simulation,
                ActiveAssumptions: new[]
                {
                    "Assumes structural DAG weights hold under local intervention (do-calculus)",
                    "Assumes no unobserved macro-regime shifts occur simultaneously"
                },
                Explanation: $"Simulated do({spec.TargetMetricId} += {spec.TargetValueDelta:F2}). Identifiability: {identifiability}. Predicted downstream effects on {predicted.Count} metrics.",
                SimulatedAtUtc: DateTime.UtcNow);
        }
    }

    // =========================================================================
    // 6. CAUSAL EVIDENCE EVALUATOR (SCORECARD & GATING)
    // =========================================================================

    public sealed class CausalEvidenceEvaluator : ICausalEvidenceEvaluator
    {
        private readonly ICausalHypothesisEngine _hypothesisEngine;
        private readonly IConfounderDetector _confounderDetector;

        public CausalEvidenceEvaluator(ICausalHypothesisEngine hypothesisEngine, IConfounderDetector confounderDetector)
        {
            _hypothesisEngine = hypothesisEngine ?? throw new ArgumentNullException(nameof(hypothesisEngine));
            _confounderDetector = confounderDetector ?? throw new ArgumentNullException(nameof(confounderDetector));
        }

        public async Task<CausalEvidenceScorecard> EvaluateEvidenceAsync(
            string tenantId,
            string hypothesisId,
            CancellationToken ct = default)
        {
            var hypothesis = await _hypothesisEngine.GetHypothesisAsync(hypothesisId, ct);
            if (hypothesis == null)
                throw new KeyNotFoundException($"Hypothesis '{hypothesisId}' not found.");

            var confounders = await _confounderDetector.AssessConfoundersAsync(tenantId, hypothesis.CauseMetricId, hypothesis.EffectMetricId, ct);

            decimal directScore = hypothesis.SupportingEvidenceIds.Count > 0 ? Math.Min(1.0m, hypothesis.SupportingEvidenceIds.Count * 0.25m) : 0.0m;
            decimal temporalScore = hypothesis.HighestEvidenceTier >= EvidenceTier.TemporalPrecedence ? 0.70m : 0.20m;
            decimal statisticalScore = hypothesis.CausalConfidenceScore;
            decimal experimentalScore = hypothesis.HighestEvidenceTier >= EvidenceTier.ControlledExperiment ? 0.90m :
                                        hypothesis.HighestEvidenceTier >= EvidenceTier.NaturalExperiment ? 0.60m : 0.0m;

            decimal confounderPenalty = confounders.IsConfounded
                ? (confounders.RiskLevel == ConfounderRiskLevel.Critical ? 0.70m : 0.40m)
                : 0.0m;

            // Composite confidence computation (metrology support, not truth machine)
            decimal composite = (directScore * 0.20m) + (temporalScore * 0.20m) + (statisticalScore * 0.30m) + (experimentalScore * 0.30m) - confounderPenalty;
            composite = Math.Max(0.05m, Math.Min(0.99m, composite));

            // Hardening Rule 1: A high score alone NEVER mechanically establishes causality!
            // Causal status gate:
            bool gatingPassed = false;
            var recommendedStatus = CausalHypothesisStatus.Hypothesized;
            string rationale;

            if (confounders.IsConfounded && confounderPenalty > 0.35m)
            {
                gatingPassed = false;
                recommendedStatus = CausalHypothesisStatus.PreservedAsUnknown;
                rationale = "Invariant I19-E/H: Confounded backdoor path detected without adjustment. High score cannot overcome confounding. Status fails-closed to PreservedAsUnknown.";
            }
            else if (hypothesis.HighestEvidenceTier < EvidenceTier.TemporalPrecedence)
            {
                gatingPassed = false;
                recommendedStatus = CausalHypothesisStatus.Hypothesized;
                rationale = "Invariant I19-A: Pure correlation cannot promote hypothesis. Minimum temporal or experimental evidence missing.";
            }
            else if (experimentalScore > 0.50m && composite >= 0.65m)
            {
                gatingPassed = true;
                recommendedStatus = CausalHypothesisStatus.Plausible;
                rationale = "Causal gate passed: Experimental or quasi-experimental evidence present with unconfounded paths. Elevated to Plausible.";
            }
            else
            {
                gatingPassed = false;
                recommendedStatus = CausalHypothesisStatus.Hypothesized;
                rationale = "Sufficient evidence for active hypothesis, but insufficient experimental proof for promotion. Preserved as Hypothesized.";
            }

            return new CausalEvidenceScorecard(
                ScorecardId: $"score-{Guid.NewGuid():N}",
                TenantId: tenantId,
                HypothesisId: hypothesisId,
                DirectEvidenceScore: Math.Round(directScore, 4),
                TemporalEvidenceScore: Math.Round(temporalScore, 4),
                StatisticalEvidenceScore: Math.Round(statisticalScore, 4),
                ExperimentalEvidenceScore: Math.Round(experimentalScore, 4),
                ConfounderPenalty: Math.Round(confounderPenalty, 4),
                CompositeConfidenceScore: Math.Round(composite, 4),
                HighestTier: hypothesis.HighestEvidenceTier,
                GatingPassed: gatingPassed,
                RecommendedStatus: recommendedStatus,
                GatingRationale: rationale,
                EvaluatedAtUtc: DateTime.UtcNow);
        }
    }

    // =========================================================================
    // 7. UNIFIED CAUSAL INTELLIGENCE ENGINE (ORCHESTRATOR)
    // =========================================================================

    public sealed class CausalIntelligenceEngine : ICausalIntelligenceEngine
    {
        public ICausalGraphEngine Graph { get; }
        public ICausalHypothesisEngine Hypotheses { get; }
        public IConfounderDetector Confounders { get; }
        public ITemporalCausalInvestigator Temporal { get; }
        public IInterventionSimulator Interventions { get; }
        public ICausalEvidenceEvaluator Evaluator { get; }

        public CausalIntelligenceEngine(
            ICausalGraphEngine graph,
            ICausalHypothesisEngine hypotheses,
            IConfounderDetector confounders,
            ITemporalCausalInvestigator temporal,
            IInterventionSimulator interventions,
            ICausalEvidenceEvaluator evaluator)
        {
            Graph = graph ?? throw new ArgumentNullException(nameof(graph));
            Hypotheses = hypotheses ?? throw new ArgumentNullException(nameof(hypotheses));
            Confounders = confounders ?? throw new ArgumentNullException(nameof(confounders));
            Temporal = temporal ?? throw new ArgumentNullException(nameof(temporal));
            Interventions = interventions ?? throw new ArgumentNullException(nameof(interventions));
            Evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        }

        public async Task<CausalHypothesis> EvaluateFullCausalExplanationAsync(
            string tenantId,
            string causeMetricId,
            string effectMetricId,
            string initialMechanism,
            CancellationToken ct = default)
        {
            // 1. Formulate initial hypothesis
            var hypothesis = await Hypotheses.FormulateHypothesisAsync(
                tenantId, causeMetricId, effectMetricId, initialMechanism,
                statedAssumptions: new[] { "Local equilibrium holds", "Linear mechanism path" },
                ct: ct);

            // 2. Investigate temporal precedence
            var temporalResult = await Temporal.InvestigateTemporalPrecedenceAsync(tenantId, causeMetricId, effectMetricId, ct: ct);
            if (temporalResult.MeetsCandidatePrecedenceThreshold)
            {
                hypothesis = await Hypotheses.AttachEvidenceAsync(
                    hypothesis.HypothesisId, temporalResult.ResultId, isSupporting: true, EvidenceTier.TemporalPrecedence, ct);
            }

            // 3. Assess confounders & evaluate evidence scorecard
            var scorecard = await Evaluator.EvaluateEvidenceAsync(tenantId, hypothesis.HypothesisId, ct);

            // 4. Update status according to rigorous gating
            hypothesis = await Hypotheses.UpdateHypothesisStatusAsync(hypothesis.HypothesisId, scorecard.RecommendedStatus, ct);

            return hypothesis;
        }
    }
}
