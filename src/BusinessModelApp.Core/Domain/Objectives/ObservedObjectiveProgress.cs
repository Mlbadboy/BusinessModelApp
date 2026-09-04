using System;
using System.Collections.Generic;
using BusinessModelApp.Core.Domain.Reality;

namespace BusinessModelApp.Core.Domain.Objectives
{
    /// <summary>
    /// Real-world observed progress evaluated strictly by the Reality Engine.
    /// Distinguishes between what Charlie desires vs. what reality has certified.
    /// </summary>
    public class ObservedObjectiveProgress
    {
        public RevenueBaselineState BaselineState { get; set; } = RevenueBaselineState.Unavailable;
        public string BaselineExplanation { get; set; } = "Revenue baseline: UNAVAILABLE. Authoritative gateway or bank reconciliation pending.";

        // Grounded Numbers
        public decimal VerifiedRevenueINR { get; set; } = 0m;
        public decimal VerifiedCashINR { get; set; } = 0m;
        public decimal VerifiedPipelineINR { get; set; } = 0m;
        public int VerifiedProspectsCount { get; set; } = 0;

        // Gap Analysis (Calculated against Target)
        public decimal RevenueGapINR { get; set; } = 0m;
        public decimal CashGapINR { get; set; } = 0m;

        // Verification Lineage
        public DateTime? LastObservedAt { get; set; }
        public string GroundingEvidenceHashesJson { get; set; } = "[]";
        public VerificationStatus OverallStatus { get; set; } = VerificationStatus.Unverified;
    }
}
