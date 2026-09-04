using System;
using System.Collections.Generic;
using BusinessModelApp.Core.Domain.WorldModel;

namespace BusinessModelApp.Core.Domain.Strategy
{
    public class ReverseFunnelStage
    {
        public string StageName { get; set; } = string.Empty;
        public int RequiredCount { get; set; }
        public decimal RequiredValueINR { get; set; }
        public TruthMetric<double> ConversionRateFromPrevious { get; set; } = new();
        public string MetricRationale { get; set; } = string.Empty;
    }

    public class ReverseFunnelResult
    {
        public decimal TargetRevenueINR { get; set; }
        public TruthMetric<decimal> TargetACVINR { get; set; } = new();
        public TruthMetric<double> WinRate { get; set; } = new();
        public TruthMetric<double> QualifiedLeadToOpportunityRate { get; set; } = new();
        public TruthMetric<double> ConversationToQualifiedLeadRate { get; set; } = new();
        public TruthMetric<double> OutreachToConversationRate { get; set; } = new();
        public TruthMetric<double> AccountToOutreachRate { get; set; } = new();

        public int RequiredClosedDeals { get; set; }
        public decimal RequiredPipelineINR { get; set; }
        public int RequiredOpportunities { get; set; }
        public int RequiredQualifiedLeads { get; set; }
        public int RequiredConversations { get; set; }
        public int RequiredOutreachContacts { get; set; }
        public int RequiredTargetAccounts { get; set; }

        public List<ReverseFunnelStage> Stages { get; set; } = new();
        public bool IsEvidenceBacked => WinRate.IsGroundedFact && TargetACVINR.IsGroundedFact;
    }
}
