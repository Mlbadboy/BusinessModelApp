using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Operations;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Operations
{
    /// <summary>
    /// Governs signal observation and transforms validated signals into commercial opportunities.
    /// Law I42-D: Signal != Opportunity.
    /// </summary>
    public sealed class MarketIntelligenceAndOpportunityEngine
    {
        private readonly List<string> _observedSignals = new();

        public bool EvaluateSignalToOpportunity(
            string marketSignal,
            decimal estimatedBudgetINR,
            decimal icpScore,
            out string qualificationReason)
        {
            if (string.IsNullOrWhiteSpace(marketSignal))
            {
                qualificationReason = "Empty market signal cannot be qualified.";
                return false;
            }

            if (icpScore < 0.70m)
            {
                qualificationReason = $"ICP score ({icpScore:F2}) is below qualification threshold (0.70).";
                return false;
            }

            if (estimatedBudgetINR <= 0)
            {
                qualificationReason = "Opportunity lacks verified budget intent.";
                return false;
            }

            qualificationReason = "Signal validated against ICP criteria and budget intent.";
            return true;
        }
    }

    /// <summary>
    /// Autonomous Customer Operations managing post-contract onboarding, TTV, health, and retention interventions.
    /// </summary>
    public sealed class CustomerOperationsAndRetentionEngine
    {
        public bool EvaluateChurnIntervention(
            decimal customerHealthScore,
            ChurnRiskLevel currentRisk,
            out string recommendedAction)
        {
            if (currentRisk >= ChurnRiskLevel.High || customerHealthScore < 70.0m)
            {
                recommendedAction = "Trigger Emergency Executive Sponsor Review & Dedicated Technical Solutions Sprint.";
                return true;
            }

            if (currentRisk == ChurnRiskLevel.Medium)
            {
                recommendedAction = "Schedule Proactive Quarterly Business Review & Value Assurance Checkpoint.";
                return true;
            }

            recommendedAction = "Customer healthy; proceed with standard expansion telemetry monitoring.";
            return false;
        }
    }

    /// <summary>
    /// Financial controller calculating loaded contribution margin and enforcing "Do Not Pursue" boundaries.
    /// </summary>
    public sealed class BusinessUnitEconomicsController
    {
        public bool EvaluatePursuitFeasibility(
            decimal projectedRevenueINR,
            decimal fullyLoadedCostINR,
            UnitEconomicsPolicy policy,
            out string decisionSummary)
        {
            if (projectedRevenueINR <= 0)
            {
                decisionSummary = "DO NOT PURSUE: Revenue projection must be positive.";
                return false;
            }

            decimal marginPercent = ((projectedRevenueINR - fullyLoadedCostINR) / projectedRevenueINR) * 100.0m;
            if (marginPercent < policy.MinGrossMarginPercent)
            {
                decisionSummary = $"DO NOT PURSUE: Projected gross margin ({marginPercent:F1}%) violates policy minimum of {policy.MinGrossMarginPercent:F1}%.";
                return false;
            }

            decisionSummary = $"PURSUIT APPROVED: Projected margin ({marginPercent:F1}%) satisfies policy ({policy.MinGrossMarginPercent:F1}%).";
            return true;
        }
    }
}
