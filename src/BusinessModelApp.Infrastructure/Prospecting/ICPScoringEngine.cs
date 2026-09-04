using System;
using System.Collections.Generic;
using BusinessModelApp.Core.Prospecting;

namespace BusinessModelApp.Infrastructure.Prospecting
{
    public class ICPScoringEngine : IICPScoringEngine
    {
        public decimal ScoreAccountICP(
            DiscoveredCandidateAccount account, 
            string targetIndustry, 
            decimal targetDealValueINR, 
            out Dictionary<string, decimal> scoreBreakdown)
        {
            scoreBreakdown = new Dictionary<string, decimal>();

            // 1. Industry Fit (Max 25 pts)
            decimal industryScore = 0m;
            if (!string.IsNullOrWhiteSpace(account.Industry) && !string.IsNullOrWhiteSpace(targetIndustry))
            {
                if (account.Industry.Contains(targetIndustry, StringComparison.OrdinalIgnoreCase) ||
                    targetIndustry.Contains(account.Industry, StringComparison.OrdinalIgnoreCase))
                {
                    industryScore = 25m;
                }
                else if (account.Industry.Contains("Tech", StringComparison.OrdinalIgnoreCase) ||
                         account.Industry.Contains("Finance", StringComparison.OrdinalIgnoreCase) ||
                         account.Industry.Contains("Banking", StringComparison.OrdinalIgnoreCase))
                {
                    industryScore = 18m;
                }
                else
                {
                    industryScore = 10m;
                }
            }
            scoreBreakdown["IndustryFit"] = industryScore;

            // 2. Company Size & Headcount Scale (Max 20 pts)
            decimal sizeScore = 0m;
            if (account.Headcount >= 5000) sizeScore = 20m;
            else if (account.Headcount >= 1000) sizeScore = 18m;
            else if (account.Headcount >= 500) sizeScore = 15m;
            else if (account.Headcount >= 100) sizeScore = 10m;
            else sizeScore = 4m;
            scoreBreakdown["CompanyScale"] = sizeScore;

            // 3. Transformation & Modernization Signals (Max 20 pts)
            decimal signalScore = 0m;
            if (account.Signals != null && account.Signals.Count > 0)
            {
                int highImpactSignals = account.Signals.FindAll(s => 
                    s.Type == SignalType.AITransformation || 
                    s.Type == SignalType.RegulatoryCompliance || 
                    s.Type == SignalType.TechnologyModernization).Count;

                signalScore = Math.Min(20m, (account.Signals.Count * 6m) + (highImpactSignals * 4m));
            }
            scoreBreakdown["TransformationSignals"] = signalScore;

            // 4. Budget & Deal Viability (Max 20 pts)
            decimal budgetScore = 0m;
            if (account.EstimatedAnnualRevenueINR >= 1000000000m) // >= ₹100 Cr
            {
                budgetScore = 20m;
            }
            else if (account.EstimatedAnnualRevenueINR >= 250000000m) // >= ₹25 Cr
            {
                budgetScore = 16m;
            }
            else if (account.EstimatedAnnualRevenueINR >= 50000000m) // >= ₹5 Cr
            {
                budgetScore = 12m;
            }
            else
            {
                budgetScore = 6m;
            }
            scoreBreakdown["BudgetViability"] = budgetScore;

            // 5. Urgency & Strategic Mandate (Max 15 pts)
            decimal urgencyScore = 0m;
            bool hasComplianceMandate = account.Signals != null && 
                account.Signals.Exists(s => s.Type == SignalType.RegulatoryCompliance);
            bool hasExecutiveHiring = account.Signals != null && 
                account.Signals.Exists(s => s.Type == SignalType.ExecutiveHiring);

            if (hasComplianceMandate && hasExecutiveHiring) urgencyScore = 15m;
            else if (hasComplianceMandate || hasExecutiveHiring) urgencyScore = 12m;
            else urgencyScore = 8m;
            scoreBreakdown["UrgencyMandate"] = urgencyScore;

            decimal totalScore = industryScore + sizeScore + signalScore + budgetScore + urgencyScore;
            return Math.Clamp(totalScore, 0m, 100m);
        }
    }
}
