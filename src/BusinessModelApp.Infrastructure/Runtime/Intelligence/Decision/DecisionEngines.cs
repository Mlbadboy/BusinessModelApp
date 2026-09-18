using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Decision;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Radar;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Scenario;
using BusinessModelApp.Core.Interfaces.Runtime.Intelligence.Decision;
using BusinessModelApp.Core.Interfaces.Runtime.Intelligence.Radar;
using BusinessModelApp.Core.Interfaces.Runtime.Intelligence.Scenario;

namespace BusinessModelApp.Infrastructure.Runtime.Intelligence.Decision
{
    // =========================================================================
    // 1. DECISION SCORER (I23-C, I23-G, I23-N)
    // =========================================================================

    public class DecisionScorer : IDecisionScorer
    {
        public DecisionScoreBreakdown ScoreCandidate(
            DecisionCandidate candidate,
            DecisionPolicy policy,
            EvidenceAssessment evidence)
        {
            if (candidate == null) throw new ArgumentNullException(nameof(candidate));
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            if (evidence == null) throw new ArgumentNullException(nameof(evidence));

            // Validate inputs against NaN / Infinity
            if (double.IsNaN((double)candidate.ExpectedReturnINR) || double.IsInfinity((double)candidate.ExpectedReturnINR) ||
                double.IsNaN((double)candidate.DownsideRiskP10INR) || double.IsInfinity((double)candidate.DownsideRiskP10INR) ||
                string.IsNullOrWhiteSpace(candidate.Title))
            {
                throw new ArgumentException("Candidate contains invalid numerical values (NaN/Infinity) or missing title.");
            }

            // 1. Expected Value Score: Normalized [0, 100]
            double evScore = Math.Clamp((double)(candidate.ExpectedReturnINR / 10000m) + 50.0, 0.0, 100.0);

            // 2. Downside Risk Score: Lower downside (less negative) gives higher score
            double downsideScore = candidate.DownsideRiskP10INR >= 0
                ? 100.0
                : Math.Clamp(100.0 - Math.Abs((double)candidate.DownsideRiskP10INR) / 10000.0, 0.0, 100.0);

            // 3. Reversibility Score: Type 1 (95), Type 2 (60), Type 3 (20)
            double revScore = candidate.Reversibility switch
            {
                ReversibilityTier.Type1_EasilyReversible => 95.0,
                ReversibilityTier.Type2_PartiallyReversible => 60.0,
                ReversibilityTier.Type3_IrreversibleOneWayDoor => 20.0,
                _ => 50.0
            };

            // 4. Time to Impact Score: Faster impact gives higher score
            double timeDays = Math.Max(1.0, candidate.EstimatedTimeToImpact.TotalDays);
            double timeScore = Math.Clamp(100.0 - (timeDays / 90.0) * 50.0, 10.0, 100.0);

            // 5. Implementation Complexity Score: Lower complexity gives higher score
            double compScore = Math.Clamp(100.0 - candidate.ImplementationComplexityScore, 0.0, 100.0);

            // 6. Strategic Fit Score: Deterministic concordance with Tenant Strategic Regime
            double strategicScore = CalculateStrategicFit(candidate.Category, policy.ActiveStrategicRegime);

            // 7. Weighted Composite MCDA Score
            double composite = (policy.ExpectedValueWeight * evScore) +
                               (policy.DownsideRiskWeight * downsideScore) +
                               (policy.ReversibilityWeight * revScore) +
                               (policy.TimeToImpactWeight * timeScore) +
                               (policy.ComplexityWeight * compScore) +
                               (policy.StrategicFitWeight * strategicScore);

            // Penalty for low confidence or constraint violation
            if (evidence.CompositeDecisionConfidence < policy.MinimumConfidenceThreshold)
            {
                composite *= 0.6; // 40% penalty for ungrounded evidence
            }

            if (candidate.FeasibilityStatus == DecisionFeasibilityStatus.InfeasibleViolation)
            {
                composite = Math.Min(composite, 15.0); // Capped extremely low
            }
            else if (candidate.FeasibilityStatus == DecisionFeasibilityStatus.ConstrainedWithWaiver)
            {
                composite *= 0.85; // 15% penalty for required waiver
            }

            return new DecisionScoreBreakdown
            {
                ExpectedValueScore = Math.Round(evScore, 2),
                DownsideRiskScore = Math.Round(downsideScore, 2),
                ReversibilityScore = Math.Round(revScore, 2),
                TimeToImpactScore = Math.Round(timeScore, 2),
                ComplexityScore = Math.Round(compScore, 2),
                StrategicFitScore = Math.Round(strategicScore, 2),
                CompositeScore = Math.Round(composite, 2)
            };
        }

        public EvidenceAssessment AssessEvidence(
            string tenantId,
            double evidenceQuality,
            double evidenceCoverage,
            double causalConf,
            double forecastConf,
            double scenarioConf)
        {
            return EvidenceAssessment.CreateClamped(
                evidenceQuality,
                evidenceCoverage,
                causalConf,
                forecastConf,
                scenarioConf);
        }

        private static double CalculateStrategicFit(DecisionCategory category, StrategicRegime regime)
        {
            return (regime, category) switch
            {
                (StrategicRegime.MarginExpansion, DecisionCategory.PricingAdjustment) => 95.0,
                (StrategicRegime.MarginExpansion, DecisionCategory.CostOptimization) => 90.0,
                (StrategicRegime.AggressiveGrowth, DecisionCategory.GrowthAcceleration) => 95.0,
                (StrategicRegime.AggressiveGrowth, DecisionCategory.ProductExpansion) => 85.0,
                (StrategicRegime.CashConservation, DecisionCategory.CostOptimization) => 95.0,
                (StrategicRegime.CashConservation, DecisionCategory.RiskMitigation) => 90.0,
                (StrategicRegime.DefensiveHold, DecisionCategory.RiskMitigation) => 95.0,
                (StrategicRegime.DefensiveHold, DecisionCategory.OperationalPivoting) => 80.0,
                _ => 65.0 // Baseline neutral concordance
            };
        }
    }

    // =========================================================================
    // 2. DECISION REVERSIBILITY & REGRET EVALUATOR (I23-D, I23-E)
    // =========================================================================

    public class DecisionReversibilityEvaluator : IDecisionReversibilityEvaluator
    {
        public ReversibilityTier ClassifyReversibility(DecisionCandidate candidate)
        {
            if (candidate == null) throw new ArgumentNullException(nameof(candidate));

            // Status quo is always type 1
            if (candidate.IsDoNothingBaseline) return ReversibilityTier.Type1_EasilyReversible;

            // One-way doors: heavy capital expenditure, major price hikes (>25%), or long contracts
            if (candidate.DownsideRiskP10INR < -1000000m || candidate.ImplementationComplexityScore >= 80.0)
            {
                return ReversibilityTier.Type3_IrreversibleOneWayDoor;
            }

            if (candidate.Category == DecisionCategory.ProductExpansion || candidate.EstimatedTimeToImpact.TotalDays > 60)
            {
                return ReversibilityTier.Type2_PartiallyReversible;
            }

            return ReversibilityTier.Type1_EasilyReversible;
        }

        public decimal CalculateMinimaxRegret(
            DecisionCandidate candidate,
            IReadOnlyList<DecisionCandidate> allCandidates)
        {
            if (candidate == null || allCandidates == null || allCandidates.Count == 0) return 0m;

            // Maximum possible return among all available candidates
            var maxReturn = allCandidates.Max(c => c.ExpectedReturnINR);

            // Regret = best available outcome - current candidate outcome
            var regret = maxReturn - candidate.ExpectedReturnINR;
            return Math.Max(0m, regret);
        }
    }

    // =========================================================================
    // 3. DECISION CANDIDATE SYNTHESIZER (I23-E — MANDATORY MULTI-ALTERNATIVE)
    // =========================================================================

    public class DecisionCandidateSynthesizer : IDecisionCandidateSynthesizer
    {
        public Task<IReadOnlyList<DecisionCandidate>> SynthesizeCandidatesAsync(
            string tenantId,
            RadarSignal? signal,
            ScenarioComparisonResult? scenarioFrontier,
            CancellationToken ct = default)
        {
            var candidates = new List<DecisionCandidate>();
            var titleBase = signal?.Title ?? "Strategic Optimization";

            // 1. Primary Recommendation Candidate (Intervention Option A)
            candidates.Add(new DecisionCandidate
            {
                TenantId = tenantId,
                RadarSignalId = signal?.Id,
                LinkedScenarioId = scenarioFrontier?.RecommendedScenarioId,
                Title = $"Option A: Execute Planned Intervention for {titleBase}",
                Description = "Modeled primary strategic action to capture upside on the Pareto frontier.",
                Category = signal?.Type == RadarSignalType.Threat ? DecisionCategory.RiskMitigation : DecisionCategory.GrowthAcceleration,
                Reversibility = ReversibilityTier.Type1_EasilyReversible,
                IsDoNothingBaseline = false,
                DescriptiveActionSummary = new List<string>
                {
                    "Adjust pricing tiers according to elasticity curve",
                    "Optimize channel allocation"
                },
                ExpectedReturnINR = signal?.EstimatedMonetaryImpactINR ?? 150000m,
                DownsideRiskP10INR = -20000m,
                UpsidePotentialP90INR = 250000m,
                EstimatedTimeToImpact = TimeSpan.FromDays(21),
                ImplementationComplexityScore = 35.0,
                ExpectedOutcomes = new List<string> { "Revenue uplift of 8-12%", "Preserved gross margin" },
                UnintendedConsequences = new List<string> { "Slight customer acquisition friction during initial rollout" },
                FeasibilityStatus = DecisionFeasibilityStatus.Feasible,
                Validity = DecisionValidity.Valid,
                LifecycleState = DecisionLifecycleState.Synthesized
            });

            // 2. Counter-Alternative Candidate (Option B: Conservative / Staged Rollout)
            candidates.Add(new DecisionCandidate
            {
                TenantId = tenantId,
                RadarSignalId = signal?.Id,
                Title = $"Option B: Phased Rollout & Staged Testing",
                Description = "Conservative alternative mitigating downside risk with phased implementation.",
                Category = DecisionCategory.CostOptimization,
                Reversibility = ReversibilityTier.Type1_EasilyReversible,
                IsDoNothingBaseline = false,
                DescriptiveActionSummary = new List<string>
                {
                    "Roll out intervention to 10% pilot cohort",
                    "Evaluate 14-day telemetry before broad adoption"
                },
                ExpectedReturnINR = (signal?.EstimatedMonetaryImpactINR ?? 150000m) * 0.6m,
                DownsideRiskP10INR = -5000m,
                UpsidePotentialP90INR = 120000m,
                EstimatedTimeToImpact = TimeSpan.FromDays(45),
                ImplementationComplexityScore = 20.0,
                ExpectedOutcomes = new List<string> { "Controlled testing", "Negligible downside exposure" },
                UnintendedConsequences = new List<string> { "Slower time to full value realization" },
                FeasibilityStatus = DecisionFeasibilityStatus.Feasible,
                Validity = DecisionValidity.Valid,
                LifecycleState = DecisionLifecycleState.Synthesized
            });

            // 3. Mandatory Do-Nothing / Status Quo Alternative (Option C — Invariant I23-E)
            candidates.Add(new DecisionCandidate
            {
                TenantId = tenantId,
                RadarSignalId = signal?.Id,
                Title = "Option C: Do Nothing (Maintain Status Quo)",
                Description = "Preserve existing operating baseline without intervention. Used as the benchmark for regret and opportunity cost.",
                Category = DecisionCategory.OperationalPivoting,
                Reversibility = ReversibilityTier.Type1_EasilyReversible,
                IsDoNothingBaseline = true,
                DescriptiveActionSummary = new List<string>
                {
                    "Maintain current pricing and resource allocation",
                    "Monitor ongoing telemetry"
                },
                ExpectedReturnINR = 0m,
                DownsideRiskP10INR = signal?.Type == RadarSignalType.Threat ? -(signal.EstimatedMonetaryImpactINR) : 0m,
                UpsidePotentialP90INR = 0m,
                EstimatedTimeToImpact = TimeSpan.Zero,
                ImplementationComplexityScore = 0.0,
                ExpectedOutcomes = new List<string> { "Zero implementation risk", "Zero capital expenditure" },
                UnintendedConsequences = new List<string> { "Opportunity loss or exposure to unmitigated deterioration" },
                FeasibilityStatus = DecisionFeasibilityStatus.Feasible,
                Validity = DecisionValidity.Valid,
                LifecycleState = DecisionLifecycleState.Synthesized
            });

            return Task.FromResult<IReadOnlyList<DecisionCandidate>>(candidates);
        }
    }

    // =========================================================================
    // 4. IN-MEMORY DECISION STORE (I23-A, I23-B, I23-J, I23-K, I23-P)
    // =========================================================================

    public class InMemoryDecisionStore : IDecisionStore
    {
        private readonly ConcurrentDictionary<string, DecisionCandidate> _candidates = new();
        private readonly ConcurrentDictionary<string, DecisionRankingResult> _rankings = new();
        private readonly ConcurrentDictionary<string, DecisionProvenance> _provenance = new();

        private static string Key(string tenantId, string id) => $"{tenantId}::{id}";

        public Task SaveCandidateAsync(DecisionCandidate candidate, CancellationToken ct = default)
        {
            if (candidate == null) throw new ArgumentNullException(nameof(candidate));
            _candidates[Key(candidate.TenantId, candidate.CandidateId)] = candidate;
            return Task.CompletedTask;
        }

        public Task<DecisionCandidate?> GetCandidateAsync(string tenantId, string candidateId, CancellationToken ct = default)
        {
            _candidates.TryGetValue(Key(tenantId, candidateId), out var candidate);
            return Task.FromResult<DecisionCandidate?>(candidate);
        }

        public Task<IReadOnlyList<DecisionCandidate>> ListCandidatesAsync(
            string tenantId,
            DecisionLifecycleState? state = null,
            CancellationToken ct = default)
        {
            var query = _candidates.Values.Where(c => c.TenantId == tenantId);
            if (state.HasValue) query = query.Where(c => c.LifecycleState == state.Value);
            return Task.FromResult<IReadOnlyList<DecisionCandidate>>(query.ToList());
        }

        public Task<bool> UpdateLifecycleStateAsync(
            string tenantId,
            string candidateId,
            DecisionLifecycleState newState,
            string reason,
            bool isHumanActor = false,
            CancellationToken ct = default)
        {
            var key = Key(tenantId, candidateId);
            if (!_candidates.TryGetValue(key, out var candidate)) return Task.FromResult(false);

            // Invariant I23-A: Charlie CANNOT approve his own decisions autonomously!
            // ApprovedByHuman strictly requires external human / governance actor (PRG-1).
            if (newState == DecisionLifecycleState.ApprovedByHuman && !isHumanActor)
            {
                throw new InvalidOperationException(
                    "Autonomous self-approval blocked by Invariant I23-A. Decision approval requires explicit human/governance actor.");
            }

            candidate.LifecycleState = newState;
            return Task.FromResult(true);
        }

        public Task SaveRankingResultAsync(DecisionRankingResult ranking, CancellationToken ct = default)
        {
            if (ranking == null) throw new ArgumentNullException(nameof(ranking));
            ranking.ComputeIntegrityHash();
            _rankings[Key(ranking.TenantId, ranking.RankingId)] = ranking;
            return Task.CompletedTask;
        }

        public Task<DecisionRankingResult?> GetLatestRankingAsync(string tenantId, CancellationToken ct = default)
        {
            var latest = _rankings.Values
                .Where(r => r.TenantId == tenantId)
                .OrderByDescending(r => r.RankedAtUtc)
                .FirstOrDefault();
            return Task.FromResult<DecisionRankingResult?>(latest);
        }

        public Task SaveProvenanceAsync(DecisionProvenance provenance, CancellationToken ct = default)
        {
            if (provenance == null) throw new ArgumentNullException(nameof(provenance));
            _provenance[Key(provenance.TenantId, provenance.DecisionId)] = provenance;
            return Task.CompletedTask;
        }

        public Task<DecisionProvenance?> GetProvenanceAsync(string tenantId, string decisionId, CancellationToken ct = default)
        {
            _provenance.TryGetValue(Key(tenantId, decisionId), out var prov);
            return Task.FromResult<DecisionProvenance?>(prov);
        }

        public Task CheckAndExpireStaleDecisionsAsync(string tenantId, TimeSpan maxAge, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var tenantCandidates = _candidates.Values.Where(c => c.TenantId == tenantId).ToList();

            foreach (var c in tenantCandidates)
            {
                if (c.LifecycleState != DecisionLifecycleState.ApprovedByHuman &&
                    c.LifecycleState != DecisionLifecycleState.RejectedByHuman &&
                    c.LifecycleState != DecisionLifecycleState.Expired)
                {
                    if (now - c.CreatedAtUtc > maxAge)
                    {
                        c.LifecycleState = DecisionLifecycleState.Expired;
                        c.Validity = DecisionValidity.StaleEvidence;
                    }
                }
            }

            return Task.CompletedTask;
        }
    }

    // =========================================================================
    // 5. DECISION ORCHESTRATOR (I23-A..I23-P)
    // =========================================================================

    public class DecisionOrchestrator : IDecisionOrchestrator
    {
        private readonly IDecisionScorer _scorer;
        private readonly IDecisionReversibilityEvaluator _reversibilityEvaluator;
        private readonly IDecisionCandidateSynthesizer _synthesizer;
        private readonly IDecisionStore _store;
        private readonly IRadarSignalStore? _radarStore;
        private readonly IScenarioStore? _scenarioStore;

        public DecisionOrchestrator(
            IDecisionScorer scorer,
            IDecisionReversibilityEvaluator reversibilityEvaluator,
            IDecisionCandidateSynthesizer synthesizer,
            IDecisionStore store,
            IRadarSignalStore? radarStore = null,
            IScenarioStore? scenarioStore = null)
        {
            _scorer = scorer ?? throw new ArgumentNullException(nameof(scorer));
            _reversibilityEvaluator = reversibilityEvaluator ?? throw new ArgumentNullException(nameof(reversibilityEvaluator));
            _synthesizer = synthesizer ?? throw new ArgumentNullException(nameof(synthesizer));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _radarStore = radarStore;
            _scenarioStore = scenarioStore;
        }

        public async Task<DecisionRankingResult> RankDecisionsAsync(
            string tenantId,
            IReadOnlyList<DecisionCandidate> candidates,
            DecisionPolicy? policy = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (candidates == null || candidates.Count == 0)
            {
                throw new ArgumentException("At least one candidate is required for decision ranking.");
            }

            // Invariant I23-E: Multi-alternative check (must include DoNothing)
            if (!candidates.Any(c => c.IsDoNothingBaseline))
            {
                throw new InvalidOperationException("Invariant I23-E Violation: Candidates must include the mandatory 'Do Nothing' (status quo) baseline.");
            }

            policy ??= new DecisionPolicy { TenantId = tenantId };
            policy.ComputeIntegrityHash();

            var evaluations = new Dictionary<string, DecisionEvaluationRecord>();
            var scoredCandidates = new List<(DecisionCandidate Candidate, double Score)>();

            foreach (var candidate in candidates)
            {
                // Classify reversibility
                var reversibility = _reversibilityEvaluator.ClassifyReversibility(candidate);
                var minimaxRegret = _reversibilityEvaluator.CalculateMinimaxRegret(candidate, candidates);

                // Evidence Assessment
                var evidence = _scorer.AssessEvidence(
                    tenantId,
                    evidenceQuality: candidate.EpistemicTier == DecisionEpistemicTier.Unknown ? 30.0 : 85.0,
                    evidenceCoverage: 80.0,
                    causalConf: 85.0,
                    forecastConf: 80.0,
                    scenarioConf: 85.0);

                // Validity check
                var validity = candidate.FeasibilityStatus == DecisionFeasibilityStatus.InfeasibleViolation
                    ? DecisionValidity.ConflictedEvidence
                    : (evidence.CompositeDecisionConfidence < policy.MinimumConfidenceThreshold
                        ? DecisionValidity.InsufficientEvidence
                        : DecisionValidity.Valid);

                // Score candidate via MCDA
                var breakdown = _scorer.ScoreCandidate(candidate, policy, evidence);

                evaluations[candidate.CandidateId] = new DecisionEvaluationRecord
                {
                    CandidateId = candidate.CandidateId,
                    Breakdown = breakdown,
                    Evidence = evidence,
                    MinimaxRegretINR = minimaxRegret,
                    FeasibilityStatus = candidate.FeasibilityStatus,
                    Validity = validity,
                    EvaluatedAtUtc = DateTime.UtcNow
                };

                candidate.LifecycleState = DecisionLifecycleState.Evaluated;
                await _store.SaveCandidateAsync(candidate, ct);

                scoredCandidates.Add((candidate, breakdown.CompositeScore));
            }

            // Rank candidates by composite score (feasible candidates prioritized)
            var rankedList = scoredCandidates
                .OrderByDescending(s => s.Candidate.FeasibilityStatus == DecisionFeasibilityStatus.Feasible ? 1 : 0)
                .ThenByDescending(s => s.Score)
                .Select(s => s.Candidate)
                .ToList();

            // Set state to Ranked
            foreach (var c in rankedList)
            {
                c.LifecycleState = DecisionLifecycleState.Ranked;
                await _store.SaveCandidateAsync(c, ct);
            }

            // Recommended candidate is top-ranked feasible candidate
            var recommended = rankedList.FirstOrDefault(c => c.FeasibilityStatus == DecisionFeasibilityStatus.Feasible && !c.IsDoNothingBaseline)
                              ?? rankedList.First();

            recommended.LifecycleState = DecisionLifecycleState.Recommended;
            await _store.SaveCandidateAsync(recommended, ct);

            var doNothing = candidates.FirstOrDefault(c => c.IsDoNothingBaseline);

            var rationale = $"Candidate '{recommended.Title}' achieves highest composite score under Strategic Regime '{policy.ActiveStrategicRegime}'. " +
                            $"Expected return: INR {recommended.ExpectedReturnINR:N0} with Minimax Regret of INR {evaluations[recommended.CandidateId].MinimaxRegretINR:N0} relative to alternative paths. " +
                            $"Notice: RecommendedDecision != Approval != Execution. Review and human approval required before execution.";

            var result = new DecisionRankingResult
            {
                TenantId = tenantId,
                RankedCandidates = rankedList,
                Evaluations = evaluations,
                RecommendedCandidateId = recommended.CandidateId,
                DoNothingCandidateId = doNothing?.CandidateId,
                TradeoffRationale = rationale,
                PolicySnapshot = policy,
                RankedAtUtc = DateTime.UtcNow
            };
            result.ComputeIntegrityHash();

            // Save ranking and seal provenance
            await _store.SaveRankingResultAsync(result, ct);

            var prov = new DecisionProvenance
            {
                DecisionId = recommended.CandidateId,
                TenantId = tenantId,
                RadarSignalId = recommended.RadarSignalId,
                LinkedScenarioId = recommended.LinkedScenarioId,
                PolicyHash = policy.IntegrityHash,
                RankingHash = result.IntegrityHash
            };
            await _store.SaveProvenanceAsync(prov, ct);

            return result;
        }

        public async Task<DecisionRankingResult> SynthesizeAndRankFromRadarAndScenarioAsync(
            string tenantId,
            string radarSignalId,
            CancellationToken ct = default)
        {
            RadarSignal? signal = null;
            if (_radarStore != null)
            {
                signal = await _radarStore.GetSignalAsync(tenantId, radarSignalId, ct);
            }

            var candidates = await _synthesizer.SynthesizeCandidatesAsync(tenantId, signal, null, ct);
            return await RankDecisionsAsync(tenantId, candidates, null, ct);
        }
    }
}
