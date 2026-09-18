using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Forecasting;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Kernel;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Radar;
using BusinessModelApp.Core.Interfaces.Runtime.Intelligence.Radar;

namespace BusinessModelApp.Infrastructure.Runtime.Intelligence.Radar
{
    // =========================================================================
    // 1. DETERMINISTIC SIGNIFICANCE SCORER (I21-F)
    // =========================================================================

    public class SignificanceScorer : ISignificanceScorer
    {
        private readonly ConcurrentDictionary<string, RadarSignificancePolicy> _policies = new();
        private readonly RadarSignificancePolicy _defaultPolicy;

        public SignificanceScorer()
        {
            _defaultPolicy = new RadarSignificancePolicy
            {
                PolicyId = "POLICY-RADAR-SYSTEM-V1",
                Version = "1.0.0",
                TenantId = "*",
                ImpactWeight = 0.35,
                ProbabilityWeight = 0.25,
                UrgencyWeight = 0.15,
                PersistenceWeight = 0.15,
                ExposureWeight = 0.10,
                MinimumConfidenceThreshold = 40.0,
                DecayRateLambda = 0.05,
                FreshnessRequirementDays = 14,
                EvidenceReinforcementBoost = 15.0
            };
            _defaultPolicy.ComputeIntegrityHash();
        }

        public RadarSignificancePolicy GetActivePolicy(string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId)) return _defaultPolicy;
            return _policies.TryGetValue(tenantId, out var policy) ? policy : _defaultPolicy;
        }

        public void RegisterPolicy(RadarSignificancePolicy policy)
        {
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            policy.ComputeIntegrityHash();
            _policies[policy.TenantId] = policy;
        }

        public RadarSignificanceBreakdown ScoreSignal(
            RadarSignalType type,
            decimal monetaryImpactINR,
            double probabilityPercent,
            SignalUrgency urgency,
            int persistenceEpochs,
            double exposurePercent,
            double confidencePercent,
            RadarSignificancePolicy policy)
        {
            policy ??= _defaultPolicy;

            // 1. Explicit Normalization to [0.0, 100.0]
            double normalizedImpact = Math.Clamp((double)(monetaryImpactINR / 100000.0m), 0.0, 100.0);
            double normalizedProbability = Math.Clamp(probabilityPercent, 0.0, 100.0);
            double normalizedUrgency = urgency switch
            {
                SignalUrgency.Low => 25.0,
                SignalUrgency.Medium => 50.0,
                SignalUrgency.High => 75.0,
                SignalUrgency.Immediate => 100.0,
                _ => 25.0
            };
            double normalizedPersistence = Math.Clamp(persistenceEpochs * 20.0, 0.0, 100.0);
            double normalizedExposure = Math.Clamp(exposurePercent, 0.0, 100.0);
            double normalizedConfidence = Math.Clamp(confidencePercent, 0.0, 100.0);

            // 2. Deterministic Weighted Composite Significance (I21-F)
            double rawSignificance =
                (policy.ImpactWeight * normalizedImpact) +
                (policy.ProbabilityWeight * normalizedProbability) +
                (policy.UrgencyWeight * normalizedUrgency) +
                (policy.PersistenceWeight * normalizedPersistence) +
                (policy.ExposureWeight * normalizedExposure);

            double significanceScore = Math.Clamp(rawSignificance, 0.0, 100.0);

            // 3. Pre-Flight Confidence Gate & Priority Derivation
            RadarPriority priority;
            if (normalizedConfidence < policy.MinimumConfidenceThreshold)
            {
                // Invariant I21-E: Low evidence cannot trigger high/critical alarm
                priority = RadarPriority.Informational;
            }
            else if (significanceScore >= 80.0 && normalizedUrgency >= 75.0)
            {
                priority = RadarPriority.Critical;
            }
            else if (significanceScore >= 65.0)
            {
                priority = RadarPriority.High;
            }
            else if (significanceScore >= 40.0)
            {
                priority = RadarPriority.Medium;
            }
            else if (significanceScore >= 20.0)
            {
                priority = RadarPriority.Low;
            }
            else
            {
                priority = RadarPriority.Informational;
            }

            return new RadarSignificanceBreakdown
            {
                ImpactScore = Math.Round(normalizedImpact, 2),
                ProbabilityScore = Math.Round(normalizedProbability, 2),
                UrgencyScore = Math.Round(normalizedUrgency, 2),
                PersistenceScore = Math.Round(normalizedPersistence, 2),
                ExposureScore = Math.Round(normalizedExposure, 2),
                ConfidenceScore = Math.Round(normalizedConfidence, 2),
                SignificanceScore = Math.Round(significanceScore, 2),
                Priority = priority
            };
        }
    }

    // =========================================================================
    // 2. OPPORTUNITY DETECTOR (I21, I21-D, I21-E)
    // =========================================================================

    public class OpportunityDetector : IOpportunityDetector
    {
        private readonly ISignificanceScorer _scorer;

        public OpportunityDetector(ISignificanceScorer scorer)
        {
            _scorer = scorer ?? throw new ArgumentNullException(nameof(scorer));
        }

        public Task<IReadOnlyList<RadarSignal>> DetectOpportunitiesAsync(
            string tenantId,
            IReadOnlyList<KpiObservation> observations,
            IReadOnlyList<ForecastOutput> forecasts,
            CancellationToken ct = default)
        {
            var signals = new List<RadarSignal>();
            var policy = _scorer.GetActivePolicy(tenantId);

            // Family A: Telemetry-driven positive inflection opportunities
            if (observations != null && observations.Count > 0)
            {
                var grouped = observations.GroupBy(o => o.MetricId);
                foreach (var group in grouped)
                {
                    var sorted = group.OrderBy(o => o.ObservedAtUtc).ToList();
                    if (sorted.Count >= 2)
                    {
                        var latest = sorted[^1];
                        var previous = sorted[^2];

                        // Detect conversion rate or revenue growth jump
                        if (latest.Value > previous.Value && previous.Value > 0)
                        {
                            var growthRatio = (double)((latest.Value - previous.Value) / previous.Value);
                            if (growthRatio >= 0.10) // 10%+ gain
                            {
                                var impactINR = latest.Value * 1000m;
                                var confidence = 85.0; // High confidence from direct observation
                                var epistemic = confidence < policy.MinimumConfidenceThreshold
                                    ? RadarEpistemicTier.Unknown
                                    : RadarEpistemicTier.FactBacked;

                                var breakdown = _scorer.ScoreSignal(
                                    RadarSignalType.Opportunity,
                                    impactINR,
                                    80.0,
                                    growthRatio >= 0.25 ? SignalUrgency.High : SignalUrgency.Medium,
                                    sorted.Count,
                                    60.0,
                                    confidence,
                                    policy);

                                var opp = new RadarSignal
                                {
                                    TenantId = tenantId,
                                    Type = RadarSignalType.Opportunity,
                                    OpportunityCategory = OpportunityCategory.RevenueExpansion,
                                    Title = $"Revenue Expansion Surge on {latest.MetricId}",
                                    Description = $"Metric {latest.MetricId} expanded by {growthRatio:P1} between epochs. Strong revenue acceleration detected.",
                                    EstimatedMonetaryImpactINR = impactINR,
                                    EpistemicTier = epistemic,
                                    LifecycleState = RadarSignalLifecycleState.Active,
                                    Urgency = growthRatio >= 0.25 ? SignalUrgency.High : SignalUrgency.Medium,
                                    Priority = breakdown.Priority,
                                    Breakdown = breakdown,
                                    Linkage = new RadarSignalLinkage
                                    {
                                        MetricObservationIds = sorted.Select(s => s.ObservationId).ToList()
                                    },
                                    InitialStrength = breakdown.SignificanceScore,
                                    CurrentStrength = breakdown.SignificanceScore
                                };
                                signals.Add(opp);
                            }
                        }
                        // Detect cost/CAC reduction (operational efficiency improvement)
                        else if (latest.Value < previous.Value && previous.Value > 0 &&
                                 (group.Key.Contains("cac", StringComparison.OrdinalIgnoreCase) ||
                                  group.Key.Contains("cost", StringComparison.OrdinalIgnoreCase) ||
                                  group.Key.Contains("expense", StringComparison.OrdinalIgnoreCase) ||
                                  group.Key.Contains("churn", StringComparison.OrdinalIgnoreCase)))
                        {
                            var dropRatio = (double)((previous.Value - latest.Value) / previous.Value);
                            if (dropRatio >= 0.10) // 10%+ cost reduction
                            {
                                var impactINR = (previous.Value - latest.Value) * 1000m;
                                var confidence = 85.0;
                                var epistemic = confidence < policy.MinimumConfidenceThreshold
                                    ? RadarEpistemicTier.Unknown
                                    : RadarEpistemicTier.FactBacked;

                                var breakdown = _scorer.ScoreSignal(
                                    RadarSignalType.Opportunity,
                                    impactINR,
                                    80.0,
                                    dropRatio >= 0.25 ? SignalUrgency.High : SignalUrgency.Medium,
                                    sorted.Count,
                                    60.0,
                                    confidence,
                                    policy);

                                var opp = new RadarSignal
                                {
                                    TenantId = tenantId,
                                    Type = RadarSignalType.Opportunity,
                                    OpportunityCategory = OpportunityCategory.OperationalEfficiency,
                                    Title = $"Operational Efficiency Surge on {latest.MetricId}",
                                    Description = $"Metric {latest.MetricId} reduced by {dropRatio:P1} between epochs. Positive operational efficiency detected.",
                                    EstimatedMonetaryImpactINR = impactINR,
                                    EpistemicTier = epistemic,
                                    LifecycleState = RadarSignalLifecycleState.Active,
                                    Urgency = dropRatio >= 0.25 ? SignalUrgency.High : SignalUrgency.Medium,
                                    Priority = breakdown.Priority,
                                    Breakdown = breakdown,
                                    Linkage = new RadarSignalLinkage
                                    {
                                        MetricObservationIds = sorted.Select(s => s.ObservationId).ToList()
                                    },
                                    InitialStrength = breakdown.SignificanceScore,
                                    CurrentStrength = breakdown.SignificanceScore
                                };
                                signals.Add(opp);
                            }
                        }
                    }
                }
            }

            // Family B: Forecast-derived forward-looking opportunities
            if (forecasts != null)
            {
                foreach (var fc in forecasts)
                {
                    if (fc.ApplicabilityStatus == ForecastApplicabilityStatus.Valid && fc.Intervals.Count > 0)
                    {
                        var lastInterval = fc.Intervals[^1];
                        if (lastInterval.MedianValue > 0 && fc.HorizonSteps > 0)
                        {
                            // Forward expansion opportunity
                            var impactINR = lastInterval.MedianValue * 500m;
                            var forecastConfidence = Math.Clamp((double)lastInterval.ConfidenceLevel * 100.0, 0.0, 100.0);

                            // I21-D: Confidence clamped to forecast coverage
                            var epistemic = forecastConfidence < policy.MinimumConfidenceThreshold
                                ? RadarEpistemicTier.Unknown
                                : RadarEpistemicTier.ForecastDerived;

                            var breakdown = _scorer.ScoreSignal(
                                RadarSignalType.Opportunity,
                                impactINR,
                                70.0,
                                SignalUrgency.Medium,
                                3,
                                50.0,
                                forecastConfidence,
                                policy);

                            var opp = new RadarSignal
                            {
                                TenantId = tenantId,
                                Type = RadarSignalType.Opportunity,
                                OpportunityCategory = OpportunityCategory.ProductGap,
                                Title = $"Projected Expansion Pipeline for {fc.MetricId}",
                                Description = $"Forecast indicates projected growth to {lastInterval.MedianValue:F1} within horizon {lastInterval.StepAhead}.",
                                EstimatedMonetaryImpactINR = impactINR,
                                EpistemicTier = epistemic,
                                LifecycleState = RadarSignalLifecycleState.Active,
                                Urgency = SignalUrgency.Medium,
                                Priority = breakdown.Priority,
                                Breakdown = breakdown,
                                Linkage = new RadarSignalLinkage
                                {
                                    ForecastOutputId = fc.ForecastId,
                                    ForecastConfidence = forecastConfidence
                                },
                                InitialStrength = breakdown.SignificanceScore,
                                CurrentStrength = breakdown.SignificanceScore
                            };
                            signals.Add(opp);
                        }
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<RadarSignal>>(signals);
        }
    }

    // =========================================================================
    // 3. THREAT DETECTOR (I21, I21-B, I21-D, I21-E)
    // =========================================================================

    public class ThreatDetector : IThreatDetector
    {
        private readonly ISignificanceScorer _scorer;

        public ThreatDetector(ISignificanceScorer scorer)
        {
            _scorer = scorer ?? throw new ArgumentNullException(nameof(scorer));
        }

        public Task<IReadOnlyList<RadarSignal>> DetectThreatsAsync(
            string tenantId,
            IReadOnlyList<KpiObservation> observations,
            IReadOnlyList<ForecastOutput> forecasts,
            IReadOnlyList<RegimeAssessment>? regimeAssessments = null,
            CancellationToken ct = default)
        {
            var signals = new List<RadarSignal>();
            var policy = _scorer.GetActivePolicy(tenantId);

            // Family A: Telemetry-driven margin compression / revenue deterioration
            if (observations != null && observations.Count > 0)
            {
                var grouped = observations.GroupBy(o => o.MetricId);
                foreach (var group in grouped)
                {
                    var sorted = group.OrderBy(o => o.ObservedAtUtc).ToList();
                    if (sorted.Count >= 2)
                    {
                        var latest = sorted[^1];
                        var previous = sorted[^2];

                        if (latest.Value < previous.Value && previous.Value > 0)
                        {
                            var dropRatio = (double)((previous.Value - latest.Value) / previous.Value);
                            if (dropRatio >= 0.12) // 12%+ drop
                            {
                                var impactINR = (previous.Value - latest.Value) * 1500m;
                                var urgency = dropRatio >= 0.25 ? SignalUrgency.Immediate : SignalUrgency.High;
                                var confidence = 90.0;
                                var epistemic = confidence < policy.MinimumConfidenceThreshold
                                    ? RadarEpistemicTier.Unknown
                                    : RadarEpistemicTier.FactBacked;

                                var breakdown = _scorer.ScoreSignal(
                                    RadarSignalType.Threat,
                                    impactINR,
                                    85.0,
                                    urgency,
                                    sorted.Count,
                                    75.0,
                                    confidence,
                                    policy);

                                var threat = new RadarSignal
                                {
                                    TenantId = tenantId,
                                    Type = RadarSignalType.Threat,
                                    ThreatCategory = ThreatCategory.CommercialDeterioration,
                                    Title = $"Commercial Deterioration Drop on {latest.MetricId}",
                                    Description = $"Metric {latest.MetricId} fell by {dropRatio:P1} between observation cycles. Rapid mitigation investigation recommended.",
                                    EstimatedMonetaryImpactINR = impactINR,
                                    EpistemicTier = epistemic,
                                    LifecycleState = RadarSignalLifecycleState.Active,
                                    Urgency = urgency,
                                    Priority = breakdown.Priority,
                                    Breakdown = breakdown,
                                    Linkage = new RadarSignalLinkage
                                    {
                                        MetricObservationIds = sorted.Select(s => s.ObservationId).ToList()
                                    },
                                    InitialStrength = breakdown.SignificanceScore,
                                    CurrentStrength = breakdown.SignificanceScore
                                };
                                signals.Add(threat);
                            }
                        }
                    }
                }
            }

            // Family B: Structural Regime Shift Assessments
            if (regimeAssessments != null)
            {
                foreach (var regime in regimeAssessments)
                {
                    if (regime.Status == RegimeShiftStatus.Detected)
                    {
                        var impactINR = 2500000m;
                        var confidence = 75.0;
                        var epistemic = RadarEpistemicTier.ForecastDerived;

                        var breakdown = _scorer.ScoreSignal(
                            RadarSignalType.Threat,
                            impactINR,
                            80.0,
                            SignalUrgency.High,
                            2,
                            80.0,
                            confidence,
                            policy);

                        var threat = new RadarSignal
                        {
                            TenantId = tenantId,
                            Type = RadarSignalType.Threat,
                            ThreatCategory = ThreatCategory.DemandShock,
                            Title = $"Structural Regime Shift Alert on {regime.MetricId}",
                            Description = $"Structural break detected in time-series residuals (Shift magnitude: {regime.ShiftMagnitude:F2}). Model applicability degraded.",
                            EstimatedMonetaryImpactINR = impactINR,
                            EpistemicTier = epistemic,
                            LifecycleState = RadarSignalLifecycleState.Active,
                            Urgency = SignalUrgency.High,
                            Priority = breakdown.Priority,
                            Breakdown = breakdown,
                            Linkage = new RadarSignalLinkage
                            {
                                ForecastConfidence = confidence
                            },
                            InitialStrength = breakdown.SignificanceScore,
                            CurrentStrength = breakdown.SignificanceScore
                        };
                        signals.Add(threat);
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<RadarSignal>>(signals);
        }
    }

    // =========================================================================
    // 4. RADAR CONSTRAINT EVALUATOR (I21-G)
    // =========================================================================

    public class RadarConstraintEvaluator : IRadarConstraintEvaluator
    {
        private const decimal CapitalCeilingINR = 2000000m; // ₹20L capital threshold

        public Task<ConstraintFeasibilityResult> EvaluateConstraintsAsync(
            string tenantId,
            RadarSignal signal,
            CancellationToken ct = default)
        {
            if (signal == null) throw new ArgumentNullException(nameof(signal));

            var result = new ConstraintFeasibilityResult();

            // Check capital feasibility
            if (signal.Type == RadarSignalType.Opportunity && signal.EstimatedMonetaryImpactINR > CapitalCeilingINR)
            {
                result.Status = ConstraintFeasibilityStatus.Constrained;
                result.ViolationReason = $"Estimated commitment of ₹{signal.EstimatedMonetaryImpactINR:N0} exceeds capital ceiling of ₹{CapitalCeilingINR:N0}.";
                result.SafeAlternatives.Add("Execute Phase 1 discovery pilot with capital capped under ₹5,00,000.");
                result.SafeAlternatives.Add("Partner with co-delivery ecosystem to distribute upfront capital expenditure.");
            }
            else
            {
                result.Status = ConstraintFeasibilityStatus.Satisfied;
            }

            return Task.FromResult(result);
        }
    }

    // =========================================================================
    // 5. IN-MEMORY RADAR SIGNAL STORE (I21-I, I21-J, I21-K)
    // =========================================================================

    public class InMemoryRadarSignalStore : IRadarSignalStore
    {
        // Partitioned by (TenantId, SignalId)
        private readonly ConcurrentDictionary<string, RadarSignal> _signals = new();
        private readonly ConcurrentDictionary<string, RadarAnalysisRecord> _records = new();

        private static string Key(string tenantId, string id) => $"{tenantId}::{id}";

        public Task SaveSignalAsync(RadarSignal signal, CancellationToken ct = default)
        {
            if (signal == null) throw new ArgumentNullException(nameof(signal));
            _signals[Key(signal.TenantId, signal.Id)] = signal;
            return Task.CompletedTask;
        }

        public Task<RadarSignal?> GetSignalAsync(string tenantId, string signalId, CancellationToken ct = default)
        {
            _signals.TryGetValue(Key(tenantId, signalId), out var signal);
            return Task.FromResult<RadarSignal?>(signal);
        }

        public Task<IReadOnlyList<RadarSignal>> GetSignalsAsync(
            string tenantId,
            RadarSignalType? type = null,
            RadarSignalLifecycleState? state = null,
            CancellationToken ct = default)
        {
            var query = _signals.Values.Where(s => s.TenantId == tenantId);
            if (type.HasValue) query = query.Where(s => s.Type == type.Value);
            if (state.HasValue) query = query.Where(s => s.LifecycleState == state.Value);

            var list = query.OrderByDescending(s => s.Breakdown.SignificanceScore).ToList();
            return Task.FromResult<IReadOnlyList<RadarSignal>>(list);
        }

        public Task<bool> UpdateLifecycleStateAsync(
            string tenantId,
            string signalId,
            RadarSignalLifecycleState newState,
            string reason,
            CancellationToken ct = default)
        {
            var key = Key(tenantId, signalId);
            if (!_signals.TryGetValue(key, out var signal)) return Task.FromResult(false);

            // Valid lifecycle progression
            signal.LifecycleState = newState;
            signal.LastEvaluatedAtUtc = DateTime.UtcNow;
            return Task.FromResult(true);
        }

        public Task ApplyTemporalDecayAsync(string tenantId, TimeSpan elapsed, CancellationToken ct = default)
        {
            var days = Math.Max(0.1, elapsed.TotalDays);
            var tenantSignals = _signals.Values.Where(s => s.TenantId == tenantId).ToList();

            foreach (var signal in tenantSignals)
            {
                if (signal.LifecycleState == RadarSignalLifecycleState.Active || signal.LifecycleState == RadarSignalLifecycleState.Detected)
                {
                    // Exponential decay formula: Strength(t) = S0 * exp(-lambda * t)
                    var decayed = signal.CurrentStrength * Math.Exp(-0.05 * days);
                    signal.CurrentStrength = Math.Round(Math.Max(0.0, decayed), 2);
                    signal.LastEvaluatedAtUtc = DateTime.UtcNow;

                    if (signal.CurrentStrength < 15.0)
                    {
                        signal.LifecycleState = RadarSignalLifecycleState.Expired;
                    }
                }
            }

            return Task.CompletedTask;
        }

        public Task<bool> ReinforceSignalAsync(string tenantId, string signalId, double boost, CancellationToken ct = default)
        {
            var key = Key(tenantId, signalId);
            if (!_signals.TryGetValue(key, out var signal)) return Task.FromResult(false);

            signal.CurrentStrength = Math.Round(Math.Min(100.0, signal.CurrentStrength + boost), 2);
            signal.ObservationReinforcementCount++;
            signal.LastEvaluatedAtUtc = DateTime.UtcNow;

            if (signal.LifecycleState == RadarSignalLifecycleState.Expired && signal.CurrentStrength >= 20.0)
            {
                signal.LifecycleState = RadarSignalLifecycleState.Active;
            }

            return Task.FromResult(true);
        }

        public Task SaveAnalysisRecordAsync(RadarAnalysisRecord record, CancellationToken ct = default)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            record.ComputeIntegrityHash();
            _records[Key(record.TenantId, record.SignalId)] = record;
            return Task.CompletedTask;
        }

        public Task<RadarAnalysisRecord?> GetAnalysisRecordAsync(string tenantId, string signalId, CancellationToken ct = default)
        {
            _records.TryGetValue(Key(tenantId, signalId), out var record);
            return Task.FromResult<RadarAnalysisRecord?>(record);
        }
    }

    // =========================================================================
    // 6. OPPORTUNITY / THREAT RADAR ORCHESTRATOR (I21, I21-I, I21-J)
    // =========================================================================

    public class OpportunityThreatRadarOrchestrator : IOpportunityThreatRadarOrchestrator
    {
        private readonly IOpportunityDetector _oppDetector;
        private readonly IThreatDetector _threatDetector;
        private readonly IRadarConstraintEvaluator _constraintEvaluator;
        private readonly IRadarSignalStore _store;
        private readonly ISignificanceScorer _scorer;

        public OpportunityThreatRadarOrchestrator(
            IOpportunityDetector oppDetector,
            IThreatDetector threatDetector,
            IRadarConstraintEvaluator constraintEvaluator,
            IRadarSignalStore store,
            ISignificanceScorer scorer)
        {
            _oppDetector = oppDetector ?? throw new ArgumentNullException(nameof(oppDetector));
            _threatDetector = threatDetector ?? throw new ArgumentNullException(nameof(threatDetector));
            _constraintEvaluator = constraintEvaluator ?? throw new ArgumentNullException(nameof(constraintEvaluator));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _scorer = scorer ?? throw new ArgumentNullException(nameof(scorer));
        }

        public async Task<RadarScanResult> ExecuteRadarScanAsync(
            string tenantId,
            IReadOnlyList<KpiObservation> observations,
            IReadOnlyList<ForecastOutput> forecasts,
            IReadOnlyList<RegimeAssessment>? regimeAssessments = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(tenantId)) throw new ArgumentException("TenantId is required", nameof(tenantId));

            // 1. Run Detectors
            var opportunities = await _oppDetector.DetectOpportunitiesAsync(tenantId, observations, forecasts, ct);
            var threats = await _threatDetector.DetectThreatsAsync(tenantId, observations, forecasts, regimeAssessments, ct);

            var allSignals = opportunities.Concat(threats).ToList();
            var policy = _scorer.GetActivePolicy(tenantId);
            int constrainedCount = 0;

            // 2. Evaluate Constraints & Generate Provenance
            foreach (var signal in allSignals)
            {
                var feasibility = await _constraintEvaluator.EvaluateConstraintsAsync(tenantId, signal, ct);
                signal.ConstraintStatus = feasibility.Status;
                signal.ConstraintViolationReason = feasibility.ViolationReason;
                signal.SafeAlternatives = feasibility.SafeAlternatives;
                signal.Linkage.ConstraintSnapshotId = feasibility.ConstraintSnapshotId;

                if (feasibility.Status == ConstraintFeasibilityStatus.Constrained)
                {
                    constrainedCount++;
                }

                // 3. Cryptographic Provenance Record (I21-I)
                var record = new RadarAnalysisRecord
                {
                    TenantId = tenantId,
                    SignalId = signal.Id,
                    InputObservationHashes = string.Join(",", signal.Linkage.MetricObservationIds),
                    CausalEvidenceHashes = signal.Linkage.CausalHypothesisId ?? "NONE",
                    ForecastHashes = signal.Linkage.ForecastOutputId ?? "NONE",
                    ConstraintSnapshotHash = feasibility.ConstraintSnapshotId,
                    ScoringPolicyId = policy.PolicyId,
                    ScoringPolicyVersion = policy.Version,
                    AlgorithmVersion = "3.8.3-RADAR-V1"
                };
                signal.ProvenanceHash = record.ComputeIntegrityHash();

                // Persist signal and provenance
                await _store.SaveSignalAsync(signal, ct);
                await _store.SaveAnalysisRecordAsync(record, ct);
            }

            return new RadarScanResult
            {
                TenantId = tenantId,
                OpportunitiesDetected = opportunities.Count,
                ThreatsDetected = threats.Count,
                SignalsConstrained = constrainedCount,
                Signals = allSignals,
                ScanCompletedAtUtc = DateTime.UtcNow
            };
        }
    }
}
