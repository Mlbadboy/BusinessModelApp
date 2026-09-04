using System;
using System.Collections.Generic;
using BusinessModelApp.Core.Domain.Common;
using BusinessModelApp.Core.Domain.Objectives;

namespace BusinessModelApp.Core.Domain.Strategy
{
    public class StrategicAssumption
    {
        public string Key { get; set; } = string.Empty;
        public string AssumptionDescription { get; set; } = string.Empty;
        public string AssumedValue { get; set; } = string.Empty;
        public MetricProvenanceSource Source { get; set; } = MetricProvenanceSource.AiEstimate;
        public string? GroundingEvidenceHash { get; set; }
        public double Confidence { get; set; } = 0.50;
    }

    public class BusinessStrategy : Entity, ISoftDeletable
    {
        public Guid ObjectiveId { get; set; }
        public string StrategyVersion { get; set; } = "v1.0";
        public string StrategyName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = false;
        public bool IsRecommended { get; set; } = false;

        // Route Identification
        public string TargetMarket { get; set; } = string.Empty;
        public string TargetICP { get; set; } = string.Empty;
        public string OfferName { get; set; } = string.Empty;

        // Deterministic Reverse Funnel Economic Model
        public decimal TargetACV_INR { get; set; }
        public double ExpectedWinRate { get; set; }
        public int RequiredClosedDeals { get; set; }
        public decimal RequiredPipelineINR { get; set; }
        public int RequiredQualifiedOppsCount { get; set; }
        public int RequiredMeetingsCount { get; set; }
        public int RequiredProspectsCount { get; set; }

        // Delivery & FinOps Feasibility
        public int DeliveryCapacitySlotsRequired { get; set; } = 1;
        public decimal ExpectedGrossMarginPercent { get; set; } = 60.0m;
        public decimal EstimatedComputeSpendINR { get; set; } = 25000m;
        public StrategyFeasibilityState FeasibilityState { get; set; } = StrategyFeasibilityState.Feasible;
        public string FeasibilityReason { get; set; } = string.Empty;

        // Grounding & Assumptions (Requirement: Explicitly distinguish Verified vs Assumed vs AI Estimated)
        public string StrategicRationale { get; set; } = string.Empty;
        public string AssumptionsJson { get; set; } = "[]";
        public string IdentifiedRisksJson { get; set; } = "[]";
        public string RejectedAlternativesJson { get; set; } = "[]";
        public string EvidenceBundleHashesJson { get; set; } = "[]";

        public bool IsDeleted { get; private set; }

        public void MarkAsDeleted()
        {
            IsDeleted = true;
            UpdateTimestamps();
        }
    }
}
