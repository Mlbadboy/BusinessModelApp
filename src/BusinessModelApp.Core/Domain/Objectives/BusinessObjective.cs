using System;
using System.Collections.Generic;
using BusinessModelApp.Core.Agents;
using BusinessModelApp.Core.Domain.Common;
using BusinessModelApp.Core.Domain.Reality;

namespace BusinessModelApp.Core.Domain.Objectives
{
    public class BusinessObjective : Entity, ISoftDeletable
    {
        public Guid WorkspaceId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Economic Targets (Desired State)
        public decimal TargetRevenueINR { get; set; }
        public decimal TargetMarginPercent { get; set; } = 55.0m;
        public decimal TargetCashCollectedINR { get; set; }

        // Timeline
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime TargetDeadline { get; set; }
        public int TimeframeDays => Math.Max(1, (int)(TargetDeadline - StartDate).TotalDays);

        // Scope & Market Focus
        public string TargetMarketsJson { get; set; } = "[]";
        public string OfferPortfolioJson { get; set; } = "[]";
        public decimal MaximumBudgetINR { get; set; } = 100000m;
        public double MaximumRiskScore { get; set; } = 0.40;

        // Governance & Autonomy
        public AutonomyLevel AllowedAutonomyLevel { get; set; } = AutonomyLevel.Level3_ControlledAutonomy;
        public ObjectiveStatus Status { get; set; } = ObjectiveStatus.Active;
        public string ConstraintsJson { get; set; } = "{}";
        public string SuccessCriteriaJson { get; set; } = "{}";

        // Dual-State Progress: Desired targets vs Observed Reality Engine verification
        public DesiredCommercialState DesiredState { get; set; } = DesiredCommercialState.Qualified;
        public ObservedObjectiveProgress ObservedProgress { get; set; } = new ObservedObjectiveProgress();

        public bool IsDeleted { get; private set; }
        public byte[] RowVersion { get; set; } = Guid.NewGuid().ToByteArray();

        public void MarkAsDeleted()
        {
            IsDeleted = true;
            UpdateTimestamps();
        }

        public void UpdateProgress(decimal verifiedRevenue, decimal verifiedCash, decimal verifiedPipeline, int verifiedProspects, RevenueBaselineState baselineState, string? evidenceHash)
        {
            ObservedProgress.BaselineState = baselineState;
            ObservedProgress.VerifiedRevenueINR = verifiedRevenue;
            ObservedProgress.VerifiedCashINR = verifiedCash;
            ObservedProgress.VerifiedPipelineINR = verifiedPipeline;
            ObservedProgress.VerifiedProspectsCount = verifiedProspects;
            ObservedProgress.RevenueGapINR = Math.Max(0m, TargetRevenueINR - verifiedRevenue);
            ObservedProgress.CashGapINR = Math.Max(0m, TargetCashCollectedINR - verifiedCash);
            ObservedProgress.LastObservedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(evidenceHash))
            {
                ObservedProgress.GroundingEvidenceHashesJson = System.Text.Json.JsonSerializer.Serialize(new List<string> { evidenceHash });
                ObservedProgress.OverallStatus = VerificationStatus.VerifiedFact;
            }

            UpdateTimestamps();
        }
    }
}
