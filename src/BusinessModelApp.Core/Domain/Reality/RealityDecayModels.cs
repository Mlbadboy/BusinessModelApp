using System;
using BusinessModelApp.Core.Domain.WorldModel;

namespace BusinessModelApp.Core.Domain.Reality
{
    public enum FreshnessState
    {
        VERIFIED = 1,
        AGING = 2,
        STALE = 3,
        UNKNOWN = 4
    }

    public class RealityDecayPolicy
    {
        public double HalfLifeDays { get; set; } = 90.0;
        public double AgingThresholdDays { get; set; } = 30.0;
        public double StaleThresholdDays { get; set; } = 60.0;
        public double UnknownThresholdDays { get; set; } = 120.0;
        public double MinimumUsableConfidence { get; set; } = 0.60;

        public static RealityDecayPolicy Default => new();
        public static RealityDecayPolicy StrictFinancial => new()
        {
            HalfLifeDays = 45.0,
            AgingThresholdDays = 15.0,
            StaleThresholdDays = 30.0,
            UnknownThresholdDays = 60.0,
            MinimumUsableConfidence = 0.75
        };
    }

    public class FreshnessEvaluation
    {
        public FreshnessState State { get; set; } = FreshnessState.VERIFIED;
        public double OriginalConfidence { get; set; }
        public double EffectiveConfidence { get; set; }
        public TimeSpan Age { get; set; }
        public bool IsUsableForPlanning { get; set; }
        public string EvaluationReason { get; set; } = string.Empty;
    }

    public interface IRealityDecayEngine
    {
        FreshnessEvaluation EvaluateFreshness<T>(TruthMetric<T> metric, DateTime asOfUtc, RealityDecayPolicy? policy = null);
        TruthMetric<T> ApplyDecay<T>(TruthMetric<T> metric, DateTime asOfUtc, RealityDecayPolicy? policy = null);
        bool CanUseForPlanning<T>(TruthMetric<T> metric, DateTime asOfUtc, RealityDecayPolicy? policy = null);
    }
}
