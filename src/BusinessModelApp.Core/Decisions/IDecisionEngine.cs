using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Decisions;
using BusinessModelApp.Core.Domain.Objectives;
using BusinessModelApp.Core.Domain.Strategy;
using BusinessModelApp.Core.Domain.WorldModel;

namespace BusinessModelApp.Core.Decisions
{
    public class WhyCharlieExplanation
    {
        public Guid DecisionId { get; set; }
        public string ObjectiveTitle { get; set; } = string.Empty;
        public decimal RevenueGapINR { get; set; }
        public string SelectedStrategyName { get; set; } = string.Empty;
        public string SelectedAlternative { get; set; } = string.Empty;
        public List<string> AlternativesConsidered { get; set; } = new();
        public decimal ExpectedRevenueINR { get; set; }
        public decimal ExpectedMarginPercent { get; set; }
        public double WinProbability { get; set; }
        public double DeliveryFeasibilityScore { get; set; }
        public string DecisionRationale { get; set; } = string.Empty;
        public List<string> GroundingEvidenceHashes { get; set; } = new();
        public List<string> ConstitutionRulesEvaluated { get; set; } = new();
        public DateTime DecidedAt { get; set; }
    }

    public interface IDecisionEngine
    {
        /// <summary>
        /// Evaluates constitutional rules, economic trade-offs, and commits an immutable DecisionRecord.
        /// Invariant: Once committed, record is immutable. Supersession uses SupersedesDecisionId.
        /// </summary>
        Task<DecisionRecord> CommitStrategyDecisionAsync(
            BusinessObjective objective,
            BusinessStrategy selectedStrategy,
            IReadOnlyList<BusinessStrategy> competingAlternatives,
            CompanySnapshot snapshot,
            Guid? supersedesDecisionId = null,
            CancellationToken ct = default);

        /// <summary>
        /// Explains the decision chain for the executive:
        /// Objective -> Gap -> Alternatives -> Trade-offs -> Evidence Hashes -> Constitution Compliance.
        /// </summary>
        Task<WhyCharlieExplanation?> GetWhyCharlieExplanationAsync(
            Guid decisionId,
            CancellationToken ct = default);
    }
}
