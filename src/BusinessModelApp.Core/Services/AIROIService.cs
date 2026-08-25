using System;
using System.Collections.Generic;
using System.Linq;
using BusinessModelApp.Core.AI;
using BusinessModelApp.Core.Domain.Commercial;

namespace BusinessModelApp.Core.Services
{
    public enum AIAttributionStatus
    {
        NoCommercialData = 0,
        IncompleteAttribution = 1,
        VerifiedAttribution = 2
    }

    public class AIROIResult
    {
        public Guid WorkspaceId { get; set; }
        public decimal TotalAISpend { get; set; }
        public decimal AttributedAISpend { get; set; }
        public decimal UnattributedAISpend { get; set; }
        public decimal AttributedClosedWonRevenue { get; set; }
        public double NetAIRoiRatio { get; set; }
        public AIAttributionStatus AttributionStatus { get; set; } = AIAttributionStatus.NoCommercialData;
        public int TotalAttributedInferences { get; set; }
        public int TotalClosedWonDealsWithAI { get; set; }
        public string AttributionSummary { get; set; } = string.Empty;
    }

    public interface IAIROIService
    {
        AIROIResult CalculateDeterministicRoi(
            Guid workspaceId,
            IReadOnlyList<AICallRecord> aiCalls,
            IReadOnlyList<Opportunity> opportunities,
            IReadOnlyList<Lead> leads);
    }

    public class AIROIService : IAIROIService
    {
        public AIROIResult CalculateDeterministicRoi(
            Guid workspaceId,
            IReadOnlyList<AICallRecord> aiCalls,
            IReadOnlyList<Opportunity> opportunities,
            IReadOnlyList<Lead> leads)
        {
            var result = new AIROIResult { WorkspaceId = workspaceId };

            if (aiCalls == null || aiCalls.Count == 0)
            {
                result.AttributionSummary = "No recorded AI inferences in current period.";
                return result;
            }

            result.TotalAISpend = aiCalls.Sum(c => c.EstimatedCost ?? 0.05m);

            // 1. Identify Closed-Won Opportunities
            var wonOpps = opportunities?.Where(o => o.Stage == OpportunityStage.ClosedWon).ToList() 
                          ?? new List<Opportunity>();

            var wonOppIds = new HashSet<Guid>(wonOpps.Select(o => o.Id));
            var wonLeadIds = new HashSet<Guid>(wonOpps.Where(o => o.LeadId != Guid.Empty).Select(o => o.LeadId));

            // 2. Identify AI Calls specifically linked to won deals or their precursor leads
            var attributedCalls = aiCalls.Where(c => 
                (c.OpportunityId.HasValue && wonOppIds.Contains(c.OpportunityId.Value)) ||
                (c.LeadId.HasValue && wonLeadIds.Contains(c.LeadId.Value))).ToList();

            result.TotalAttributedInferences = attributedCalls.Count;
            result.AttributedAISpend = attributedCalls.Sum(c => c.EstimatedCost ?? 0.05m);
            result.UnattributedAISpend = Math.Max(0m, result.TotalAISpend - result.AttributedAISpend);

            // 3. Find unique won opportunities with verified AI touchpoints
            var oppsWithAI = wonOpps.Where(o => 
                attributedCalls.Any(c => c.OpportunityId == o.Id || (o.LeadId != Guid.Empty && c.LeadId == o.LeadId))).ToList();

            result.TotalClosedWonDealsWithAI = oppsWithAI.Count;
            result.AttributedClosedWonRevenue = oppsWithAI.Sum(o => o.EstimatedValue);

            // 4. Deterministic ROI Calculation
            if (result.AttributedAISpend > 0m && result.AttributedClosedWonRevenue > 0m)
            {
                result.NetAIRoiRatio = (double)((result.AttributedClosedWonRevenue - result.AttributedAISpend) / result.AttributedAISpend);
                result.AttributionStatus = AIAttributionStatus.VerifiedAttribution;
                result.AttributionSummary = $"Verified AI ROI: {result.NetAIRoiRatio:F1}x return on {result.AttributedAISpend:C0} attributed AI spend across {result.TotalClosedWonDealsWithAI} closed deals.";
            }
            else if (wonOpps.Count > 0 && result.AttributedAISpend == 0m)
            {
                result.NetAIRoiRatio = 0.0;
                result.AttributionStatus = AIAttributionStatus.IncompleteAttribution;
                result.AttributionSummary = "Commercial revenue exists, but precursor AI touchpoints have incomplete linkage.";
            }
            else
            {
                result.NetAIRoiRatio = 0.0;
                result.AttributionStatus = AIAttributionStatus.NoCommercialData;
                result.AttributionSummary = "Awaiting closed-won revenue conversions to establish net ROI attribution.";
            }

            return result;
        }
    }
}
