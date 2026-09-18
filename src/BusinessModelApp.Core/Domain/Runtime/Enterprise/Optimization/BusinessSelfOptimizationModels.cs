using System;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Optimization
{
    public enum OptimizationDomain
    {
        AgentRouting = 0,
        ModelProviderSelection = 1,
        ResourceAllocationWeight = 2,
        OutreachScheduleTiming = 3
    }

    public enum AdaptationStatus
    {
        PendingVerification = 0,
        CertifiedSafe = 1,
        Applied = 2,
        RejectedUnsafe = 3,
        RejectedLowConfidence = 4
    }

    public sealed class AdaptationProposal
    {
        public string ProposalId { get; init; } = Guid.NewGuid().ToString("N");
        public required string TenantId { get; init; }
        public OptimizationDomain Domain { get; init; }
        public required string TargetParameterName { get; init; }
        public decimal CurrentValue { get; init; }
        public decimal ProposedValue { get; init; }
        public decimal CausalConfidenceScore { get; init; } // 0.00 to 1.00
        public int EmpiricalObservationsCount { get; init; }
        public decimal SafetyCeilingMin { get; init; }
        public decimal SafetyCeilingMax { get; init; }
        public AdaptationStatus Status { get; private set; } = AdaptationStatus.PendingVerification;
        public string EvaluationReason { get; private set; } = string.Empty;
        public DateTime ProposedAtUtc { get; init; } = DateTime.UtcNow;
        public DateTime? EvaluatedAtUtc { get; private set; }

        public void EvaluateSafety(decimal minConfidenceThreshold = 0.80m, int minObservationsRequired = 50)
        {
            EvaluatedAtUtc = DateTime.UtcNow;

            // Invariant 1: Low empirical confidence cannot self-modify
            if (CausalConfidenceScore < minConfidenceThreshold || EmpiricalObservationsCount < minObservationsRequired)
            {
                Status = AdaptationStatus.RejectedLowConfidence;
                EvaluationReason = $"Rejected: Confidence ({CausalConfidenceScore:P1} < {minConfidenceThreshold:P1}) or Observations ({EmpiricalObservationsCount} < {minObservationsRequired}) insufficient.";
                return;
            }

            // Invariant 2: Value must remain within strict bounded safety ceilings
            if (ProposedValue < SafetyCeilingMin || ProposedValue > SafetyCeilingMax)
            {
                Status = AdaptationStatus.RejectedUnsafe;
                EvaluationReason = $"Rejected Unsafe: Proposed value {ProposedValue} violates constitutional safety ceiling bounds [{SafetyCeilingMin}, {SafetyCeilingMax}].";
                return;
            }

            Status = AdaptationStatus.CertifiedSafe;
            EvaluationReason = "Certified Safe: Bounded adaptation verified within policy limits.";
        }

        public void Apply()
        {
            if (Status != AdaptationStatus.CertifiedSafe)
                throw new InvalidOperationException($"Cannot apply adaptation in status '{Status}'. Only CertifiedSafe adaptations may be applied.");
            Status = AdaptationStatus.Applied;
        }
    }
}
