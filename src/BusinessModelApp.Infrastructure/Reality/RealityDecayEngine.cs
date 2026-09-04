using System;
using BusinessModelApp.Core.Domain.Objectives;
using BusinessModelApp.Core.Domain.Reality;
using BusinessModelApp.Core.Domain.WorldModel;

namespace BusinessModelApp.Infrastructure.Reality
{
    public class RealityDecayEngine : IRealityDecayEngine
    {
        public FreshnessEvaluation EvaluateFreshness<T>(TruthMetric<T> metric, DateTime asOfUtc, RealityDecayPolicy? policy = null)
        {
            if (metric == null) throw new ArgumentNullException(nameof(metric));
            var activePolicy = policy ?? RealityDecayPolicy.Default;

            var age = asOfUtc > metric.ObservedAt ? asOfUtc - metric.ObservedAt : TimeSpan.Zero;
            var ageDays = age.TotalDays;

            // Exponential decay formula: EffectiveConfidence = OriginalConfidence * 2^(-ageDays / halfLife)
            double decayFactor = Math.Pow(2.0, -ageDays / activePolicy.HalfLifeDays);
            double effectiveConfidence = Math.Clamp(Math.Round(metric.Confidence * decayFactor, 4), 0.0, 1.0);

            FreshnessState state;
            bool isUsable;
            string reason;

            if (ageDays <= activePolicy.AgingThresholdDays)
            {
                state = FreshnessState.VERIFIED;
                isUsable = effectiveConfidence >= activePolicy.MinimumUsableConfidence;
                reason = $"Fresh metric observed {ageDays:F1} days ago with effective confidence {effectiveConfidence:P1}";
            }
            else if (ageDays <= activePolicy.StaleThresholdDays)
            {
                state = FreshnessState.AGING;
                isUsable = effectiveConfidence >= activePolicy.MinimumUsableConfidence;
                reason = $"Aging metric observed {ageDays:F1} days ago. Effective confidence degraded to {effectiveConfidence:P1}";
            }
            else if (ageDays <= activePolicy.UnknownThresholdDays)
            {
                state = FreshnessState.STALE;
                isUsable = false; // STALE metrics are strictly non-usable for verified planning
                reason = $"Stale metric observed {ageDays:F1} days ago exceeds stale threshold {activePolicy.StaleThresholdDays} days";
            }
            else
            {
                state = FreshnessState.UNKNOWN;
                isUsable = false;
                reason = $"Metric observed {ageDays:F1} days ago has decayed past usable horizon into UNKNOWN state";
            }

            return new FreshnessEvaluation
            {
                State = state,
                OriginalConfidence = metric.Confidence,
                EffectiveConfidence = effectiveConfidence,
                Age = age,
                IsUsableForPlanning = isUsable,
                EvaluationReason = reason
            };
        }

        public TruthMetric<T> ApplyDecay<T>(TruthMetric<T> metric, DateTime asOfUtc, RealityDecayPolicy? policy = null)
        {
            if (metric == null) throw new ArgumentNullException(nameof(metric));
            var evaluation = EvaluateFreshness(metric, asOfUtc, policy);

            metric.Confidence = evaluation.EffectiveConfidence;

            if (evaluation.State == FreshnessState.STALE || evaluation.State == FreshnessState.UNKNOWN)
            {
                metric.VerificationStatus = VerificationStatus.Unverified;
                metric.Provenance = MetricProvenanceSource.UNKNOWN;
            }

            return metric;
        }

        public bool CanUseForPlanning<T>(TruthMetric<T> metric, DateTime asOfUtc, RealityDecayPolicy? policy = null)
        {
            var evaluation = EvaluateFreshness(metric, asOfUtc, policy);
            return evaluation.IsUsableForPlanning;
        }
    }
}
