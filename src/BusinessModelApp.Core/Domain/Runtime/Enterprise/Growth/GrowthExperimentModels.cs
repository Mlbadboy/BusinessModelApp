using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth
{
    /// <summary>
    /// Lifecycle state for a growth experiment.
    /// </summary>
    public enum GrowthExperimentStatus
    {
        Draft,
        Approved,
        Running,
        ConcludedWinner,
        ConcludedInconclusive,
        TerminatedEarly
    }

    /// <summary>
    /// Lifecycle state for an autonomous growth mission.
    /// </summary>
    public enum GrowthMissionStatus
    {
        Planned,
        Active,
        PausedGuardrailBreach,
        Completed,
        Aborted
    }

    /// <summary>
    /// Scientific growth experiment testing hypotheses on acquisition, pricing, or retention (Law I41-J).
    /// </summary>
    public sealed class GrowthExperimentRecord
    {
        public string ExperimentId { get; }
        public string HypothesisStatement { get; }
        public string PrimaryMetricName { get; }
        public decimal BaselineMetricValue { get; }
        public decimal MinimumDetectableEffectPercent { get; }
        public int RequiredSampleSize { get; }
        public decimal AllocatedBudget { get; }
        public string GovernanceSignoffId { get; }
        public GrowthExperimentStatus Status { get; private set; }
        public int CurrentSampleSize { get; private set; }
        public decimal ControlMetricValue { get; private set; }
        public decimal VariantMetricValue { get; private set; }
        public decimal PValue { get; private set; }
        public bool IsStatisticallySignificant => PValue > 0 && PValue < 0.05m && CurrentSampleSize >= RequiredSampleSize;
        public string ConclusionSummary { get; private set; } = string.Empty;
        public DateTime CreatedAtUtc { get; }
        public DateTime? ConcludedAtUtc { get; private set; }

        public GrowthExperimentRecord(
            string hypothesisStatement,
            string primaryMetricName,
            decimal baselineMetricValue,
            decimal minimumDetectableEffectPercent,
            int requiredSampleSize,
            decimal allocatedBudget,
            string governanceSignoffId,
            string? experimentId = null,
            DateTime? createdAtUtc = null)
        {
            if (string.IsNullOrWhiteSpace(hypothesisStatement))
                throw new ArgumentException("Hypothesis statement is required.", nameof(hypothesisStatement));
            if (string.IsNullOrWhiteSpace(primaryMetricName))
                throw new ArgumentException("Primary metric name is required.", nameof(primaryMetricName));
            if (requiredSampleSize <= 0)
                throw new ArgumentException("Required sample size must be positive.", nameof(requiredSampleSize));
            if (allocatedBudget <= 0)
                throw new ArgumentException("Allocated budget must be positive.", nameof(allocatedBudget));
            if (string.IsNullOrWhiteSpace(governanceSignoffId))
                throw new InvalidOperationException("Growth experiment requires governance signoff (Law I41-J).");

            ExperimentId = experimentId ?? Guid.NewGuid().ToString("N");
            HypothesisStatement = hypothesisStatement.Trim();
            PrimaryMetricName = primaryMetricName.Trim();
            BaselineMetricValue = baselineMetricValue;
            MinimumDetectableEffectPercent = minimumDetectableEffectPercent;
            RequiredSampleSize = requiredSampleSize;
            AllocatedBudget = allocatedBudget;
            GovernanceSignoffId = governanceSignoffId;
            Status = GrowthExperimentStatus.Approved;
            CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow;
        }

        public void RecordObservations(int sampleCount, decimal controlValue, decimal variantValue, decimal pValue)
        {
            CurrentSampleSize = sampleCount;
            ControlMetricValue = controlValue;
            VariantMetricValue = variantValue;
            PValue = Math.Clamp(pValue, 0.0m, 1.0m);
            Status = GrowthExperimentStatus.Running;
        }

        public void ConcludeExperiment(GrowthExperimentStatus finalStatus, string summary)
        {
            if (finalStatus != GrowthExperimentStatus.ConcludedWinner &&
                finalStatus != GrowthExperimentStatus.ConcludedInconclusive &&
                finalStatus != GrowthExperimentStatus.TerminatedEarly)
            {
                throw new ArgumentException("Invalid final experiment status.", nameof(finalStatus));
            }

            Status = finalStatus;
            ConclusionSummary = summary ?? string.Empty;
            ConcludedAtUtc = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// End-to-end autonomous growth mission orchestrating agents, budgets, and automated guardrails.
    /// </summary>
    public sealed class GrowthMissionPlan
    {
        public string MissionId { get; }
        public string GrowthObjectiveId { get; }
        public string MissionName { get; }
        public string TargetIcpDescription { get; }
        public decimal BudgetLimit { get; }
        public decimal MinTargetLtvToCac { get; }
        public decimal MinTargetMarginPercent { get; }
        public List<string> AssignedAgentRoles { get; } = new();
        public GrowthMissionStatus Status { get; private set; }
        public string GuardrailBreachReason { get; private set; } = string.Empty;
        public decimal CurrentSpend { get; private set; }
        public decimal CurrentRealizedRevenue { get; private set; }
        public DateTime CreatedAtUtc { get; }

        public GrowthMissionPlan(
            string growthObjectiveId,
            string missionName,
            string targetIcpDescription,
            decimal budgetLimit,
            decimal minTargetLtvToCac = 3.0m,
            decimal minTargetMarginPercent = 35.0m,
            string? missionId = null,
            DateTime? createdAtUtc = null)
        {
            if (string.IsNullOrWhiteSpace(growthObjectiveId))
                throw new ArgumentException("Parent GrowthObjectiveId is required (Law I41-Y).", nameof(growthObjectiveId));
            if (string.IsNullOrWhiteSpace(missionName))
                throw new ArgumentException("Mission name is required.", nameof(missionName));
            if (budgetLimit <= 0)
                throw new ArgumentException("Budget limit must be positive.", nameof(budgetLimit));

            if (minTargetLtvToCac < GrowthConstitutionalInvariants.MinLtvToCacRatio)
            {
                throw new InvalidOperationException(
                    $"Mission target LTV:CAC {minTargetLtvToCac}x is below the sovereign minimum of {GrowthConstitutionalInvariants.MinLtvToCacRatio}x (Law I41-N).");
            }
            if (minTargetMarginPercent < GrowthConstitutionalInvariants.MinGrossMarginPercent)
            {
                throw new InvalidOperationException(
                    $"Mission target margin {minTargetMarginPercent}% is below the sovereign minimum of {GrowthConstitutionalInvariants.MinGrossMarginPercent}% (Law I41-G).");
            }

            MissionId = missionId ?? Guid.NewGuid().ToString("N");
            GrowthObjectiveId = growthObjectiveId;
            MissionName = missionName.Trim();
            TargetIcpDescription = targetIcpDescription ?? string.Empty;
            BudgetLimit = budgetLimit;
            MinTargetLtvToCac = minTargetLtvToCac;
            MinTargetMarginPercent = minTargetMarginPercent;
            Status = GrowthMissionStatus.Planned;
            CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow;
        }

        public void AssignAgent(string agentRole)
        {
            if (!string.IsNullOrWhiteSpace(agentRole))
                AssignedAgentRoles.Add(agentRole.Trim());
        }

        public void Activate()
        {
            Status = GrowthMissionStatus.Active;
        }

        public void UpdateProgress(decimal addedSpend, decimal addedRealizedRevenue)
        {
            CurrentSpend += Math.Max(0, addedSpend);
            CurrentRealizedRevenue += Math.Max(0, addedRealizedRevenue);

            // Guardrail 1: Budget overrun check
            if (CurrentSpend > BudgetLimit)
            {
                Status = GrowthMissionStatus.PausedGuardrailBreach;
                GuardrailBreachReason = $"Spend ${CurrentSpend:N2} exceeded approved budget limit ${BudgetLimit:N2}.";
            }
        }

        public void TriggerGuardrailBreach(string reason)
        {
            Status = GrowthMissionStatus.PausedGuardrailBreach;
            GuardrailBreachReason = reason ?? "Automated guardrail triggered.";
        }

        public void MarkComplete()
        {
            Status = GrowthMissionStatus.Completed;
        }
    }
}
