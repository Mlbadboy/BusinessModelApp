using System;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Experiments
{
    public enum GrowthExperimentType
    {
        OutreachMessaging = 0,
        PricingStrategy = 1,
        OnboardingFlow = 2,
        RetentionIntervention = 3
    }

    public enum GrowthExperimentStatus
    {
        Draft = 0,
        Active = 1,
        Concluded = 2,
        RolledBack = 3,
        TerminatedBudgetLimit = 4
    }

    public enum ExperimentDecision
    {
        Inconclusive = 0,
        Continue = 1,
        Promote = 2,
        Rollback = 3,
        Invalid = 4
    }

    public sealed class GrowthExperiment
    {
        public string ExperimentId { get; init; } = Guid.NewGuid().ToString("N");
        public required string TenantId { get; init; }
        public required string Title { get; init; }
        public required string Hypothesis { get; init; }
        public GrowthExperimentType Type { get; init; }
        public decimal BaselineConversionRate { get; init; }
        public decimal TargetConversionRate { get; init; }
        public int ControlSampleSize { get; private set; }
        public int ControlSuccesses { get; private set; }
        public int VariantSampleSize { get; private set; }
        public int VariantSuccesses { get; private set; }
        public decimal BudgetAllocatedINR { get; init; }
        public decimal BudgetSpentINR { get; private set; }
        public decimal RiskCeiling { get; init; } = 0.20m; // Max 20% degradation tolerance before emergency stop
        public GrowthExperimentStatus Status { get; private set; } = GrowthExperimentStatus.Draft;
        public ExperimentDecision Decision { get; private set; } = ExperimentDecision.Inconclusive;
        public string OutcomeNotes { get; private set; } = string.Empty;
        public DateTime StartedAtUtc { get; private set; }
        public DateTime? ConcludedAtUtc { get; private set; }

        public decimal ControlRate => ControlSampleSize > 0 ? (decimal)ControlSuccesses / ControlSampleSize : 0m;
        public decimal VariantRate => VariantSampleSize > 0 ? (decimal)VariantSuccesses / VariantSampleSize : 0m;
        public decimal UpliftPct => ControlRate > 0 ? ((VariantRate - ControlRate) / ControlRate) * 100m : 0m;

        public void Start()
        {
            if (Status != GrowthExperimentStatus.Draft) throw new InvalidOperationException("Experiment must be in Draft to start.");
            Status = GrowthExperimentStatus.Active;
            StartedAtUtc = DateTime.UtcNow;
        }

        public void RecordObservations(int controlSamples, int controlSuccesses, int variantSamples, int variantSuccesses, decimal spendINR)
        {
            if (Status != GrowthExperimentStatus.Active) throw new InvalidOperationException("Cannot record observations on inactive experiment.");

            ControlSampleSize += controlSamples;
            ControlSuccesses += controlSuccesses;
            VariantSampleSize += variantSamples;
            VariantSuccesses += variantSuccesses;
            BudgetSpentINR += spendINR;

            // Check budget overrun
            if (BudgetSpentINR > BudgetAllocatedINR)
            {
                Status = GrowthExperimentStatus.TerminatedBudgetLimit;
                Decision = ExperimentDecision.Invalid;
                OutcomeNotes = $"Emergency Stop: Budget limit of ₹{BudgetAllocatedINR:N0} exceeded (Spent: ₹{BudgetSpentINR:N0}).";
                ConcludedAtUtc = DateTime.UtcNow;
                return;
            }

            // Check severe negative risk degradation (Variant degraded more than RiskCeiling)
            if (ControlSampleSize >= 50 && VariantSampleSize >= 50 && VariantRate < ControlRate * (1m - RiskCeiling))
            {
                Status = GrowthExperimentStatus.RolledBack;
                Decision = ExperimentDecision.Rollback;
                OutcomeNotes = $"Auto-Rollback: Variant conversion ({VariantRate:P1}) degraded beyond risk ceiling ({RiskCeiling:P0}) relative to control ({ControlRate:P1}).";
                ConcludedAtUtc = DateTime.UtcNow;
            }
        }

        public void ConcludeAndEvaluate(int minimumSamplesPerArm = 100)
        {
            if (Status != GrowthExperimentStatus.Active) return;

            ConcludedAtUtc = DateTime.UtcNow;
            if (ControlSampleSize < minimumSamplesPerArm || VariantSampleSize < minimumSamplesPerArm)
            {
                Status = GrowthExperimentStatus.Concluded;
                Decision = ExperimentDecision.Inconclusive;
                OutcomeNotes = $"Inconclusive: Sample size ({Math.Min(ControlSampleSize, VariantSampleSize)}) below minimum requirement ({minimumSamplesPerArm}).";
                return;
            }

            if (VariantRate > ControlRate && VariantRate >= TargetConversionRate)
            {
                Status = GrowthExperimentStatus.Concluded;
                Decision = ExperimentDecision.Promote;
                OutcomeNotes = $"Promote: Variant achieved {VariantRate:P1} (+{UpliftPct:F1}% uplift vs control {ControlRate:P1}), exceeding target of {TargetConversionRate:P1}.";
            }
            else if (VariantRate < ControlRate)
            {
                Status = GrowthExperimentStatus.RolledBack;
                Decision = ExperimentDecision.Rollback;
                OutcomeNotes = $"Rollback: Variant underperformed control ({VariantRate:P1} vs {ControlRate:P1}).";
            }
            else
            {
                Status = GrowthExperimentStatus.Concluded;
                Decision = ExperimentDecision.Inconclusive;
                OutcomeNotes = "Inconclusive: No statistically significant divergence.";
            }
        }
    }
}
