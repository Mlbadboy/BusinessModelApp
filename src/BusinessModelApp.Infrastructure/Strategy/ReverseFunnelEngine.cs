using System;
using System.Collections.Generic;
using BusinessModelApp.Core.Domain.Objectives;
using BusinessModelApp.Core.Domain.Strategy;
using BusinessModelApp.Core.Domain.WorldModel;
using BusinessModelApp.Core.Strategy;
using Microsoft.Extensions.Logging;

namespace BusinessModelApp.Infrastructure.Strategy
{
    public class ReverseFunnelEngine : IReverseFunnelEngine
    {
        private readonly ILogger<ReverseFunnelEngine> _logger;

        public ReverseFunnelEngine(ILogger<ReverseFunnelEngine> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ReverseFunnelResult CalculateFunnel(
            decimal targetRevenueINR,
            TruthMetric<decimal> acvMetric,
            TruthMetric<double> winRateMetric,
            CompanySnapshot snapshot)
        {
            _logger.LogInformation("[ReverseFunnelEngine] Computing reverse funnel for Target: ₹{Target:N0}, ACV: ₹{ACV:N0} (Source: {Source})",
                targetRevenueINR, acvMetric.Value, acvMetric.Source);

            decimal effectiveACV = acvMetric.Value > 0 ? acvMetric.Value : 1000000m;
            double effectiveWinRate = winRateMetric.Value > 0 ? winRateMetric.Value : 0.20;

            // 1. Deals = Ceiling(Target / ACV)
            int requiredDeals = (int)Math.Ceiling(targetRevenueINR / effectiveACV);
            if (requiredDeals <= 0) requiredDeals = 1;

            // 2. Opportunities = Ceiling(Deals / WinRate)
            int requiredOpps = (int)Math.Ceiling(requiredDeals / effectiveWinRate);
            decimal requiredPipeline = requiredOpps * effectiveACV;

            // 3. Qualified Leads: Check if snapshot has verified qualification rate
            TruthMetric<double> leadToOppRate = snapshot.Commercial.HistoricalWinRate.IsGroundedFact
                ? TruthMetric<double>.Historical(0.40, snapshot.Id, "Historical CRM Lead-to-Opp conversion rate", 0.9)
                : TruthMetric<double>.Estimated(0.35, "Industry B2B AI benchmark (Hypothesis only)", 0.45);

            int requiredQualifiedLeads = (int)Math.Ceiling(requiredOpps / leadToOppRate.Value);

            // 4. Conversations: Discovery meetings / demos
            TruthMetric<double> convToLeadRate = TruthMetric<double>.Estimated(0.50, "Estimated meeting-to-qualified lead rate", 0.5);
            int requiredConversations = (int)Math.Ceiling(requiredQualifiedLeads / convToLeadRate.Value);

            // 5. Outreach Contacts: Calls / emails / linkedin touches
            TruthMetric<double> outreachToConvRate = TruthMetric<double>.Estimated(0.15, "Estimated cold/warm outreach meeting booking rate", 0.4);
            int requiredOutreach = (int)Math.Ceiling(requiredConversations / outreachToConvRate.Value);

            // 6. Target Accounts: Assuming ~1.5 contacts per account
            TruthMetric<double> acctToOutreachRate = TruthMetric<double>.Estimated(0.70, "Estimated target accounts with reachable decision makers", 0.5);
            int requiredAccounts = (int)Math.Ceiling(requiredOutreach / 1.5);

            var result = new ReverseFunnelResult
            {
                TargetRevenueINR = targetRevenueINR,
                TargetACVINR = acvMetric,
                WinRate = winRateMetric,
                QualifiedLeadToOpportunityRate = leadToOppRate,
                ConversationToQualifiedLeadRate = convToLeadRate,
                OutreachToConversationRate = outreachToConvRate,
                AccountToOutreachRate = acctToOutreachRate,
                RequiredClosedDeals = requiredDeals,
                RequiredOpportunities = requiredOpps,
                RequiredPipelineINR = requiredPipeline,
                RequiredQualifiedLeads = requiredQualifiedLeads,
                RequiredConversations = requiredConversations,
                RequiredOutreachContacts = requiredOutreach,
                RequiredTargetAccounts = requiredAccounts
            };

            result.Stages = new List<ReverseFunnelStage>
            {
                new ReverseFunnelStage { StageName = "Target Revenue", RequiredCount = 1, RequiredValueINR = targetRevenueINR, MetricRationale = "Direct CEO Mandate" },
                new ReverseFunnelStage { StageName = "Closed Won Deals", RequiredCount = requiredDeals, RequiredValueINR = targetRevenueINR, MetricRationale = $"Requires {requiredDeals} deals at ACV of ₹{effectiveACV:N0}" },
                new ReverseFunnelStage { StageName = "Qualified Opportunities", RequiredCount = requiredOpps, RequiredValueINR = requiredPipeline, MetricRationale = $"Requires ₹{requiredPipeline:N0} pipeline at {effectiveWinRate * 100:N0}% win rate" },
                new ReverseFunnelStage { StageName = "Qualified Leads", RequiredCount = requiredQualifiedLeads, RequiredValueINR = requiredPipeline, MetricRationale = $"Requires {requiredQualifiedLeads} qualified leads at {leadToOppRate.Value * 100:N0}% conversion" },
                new ReverseFunnelStage { StageName = "Meetings / Conversations", RequiredCount = requiredConversations, RequiredValueINR = 0, MetricRationale = $"Requires {requiredConversations} qualified executive conversations" },
                new ReverseFunnelStage { StageName = "Outreach Touches", RequiredCount = requiredOutreach, RequiredValueINR = 0, MetricRationale = $"Requires {requiredOutreach} governed multi-channel touches" },
                new ReverseFunnelStage { StageName = "Target Accounts", RequiredCount = requiredAccounts, RequiredValueINR = 0, MetricRationale = $"Requires {requiredAccounts} verified ICP accounts" }
            };

            return result;
        }
    }
}
