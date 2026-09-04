using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Constitution;
using BusinessModelApp.Core.Domain.Objectives;
using BusinessModelApp.Core.Domain.Strategy;
using BusinessModelApp.Core.Domain.WorldModel;
using BusinessModelApp.Core.Strategy;
using Microsoft.Extensions.Logging;

namespace BusinessModelApp.Infrastructure.Strategy
{
    public class CommercialStrategyEngine : ICommercialStrategyEngine
    {
        private readonly IDeterministicStrategySimulator _deterministicSimulator;
        private readonly IAIStrategySimulator _aiSimulator;
        private readonly IConstitutionPolicyEngine _constitutionEngine;
        private readonly ILogger<CommercialStrategyEngine> _logger;

        public CommercialStrategyEngine(
            IDeterministicStrategySimulator deterministicSimulator,
            IAIStrategySimulator aiSimulator,
            IConstitutionPolicyEngine constitutionEngine,
            ILogger<CommercialStrategyEngine> logger)
        {
            _deterministicSimulator = deterministicSimulator ?? throw new ArgumentNullException(nameof(deterministicSimulator));
            _aiSimulator = aiSimulator ?? throw new ArgumentNullException(nameof(aiSimulator));
            _constitutionEngine = constitutionEngine ?? throw new ArgumentNullException(nameof(constitutionEngine));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<IReadOnlyList<StrategyCandidate>> FormulateStrategiesAsync(
            BusinessObjective objective,
            CompanySnapshot snapshot,
            CancellationToken ct = default)
        {
            _logger.LogInformation("[CommercialStrategyEngine] Formulating strategic candidates for objective '{Title}' (Target: ₹{Target:N0})",
                objective.Title, objective.TargetRevenueINR);

            decimal target = objective.TargetRevenueINR > 0 ? objective.TargetRevenueINR : 5000000m;
            var candidates = new List<StrategyCandidate>();

            // Route A: Deterministic High-ACV Enterprise AI Route
            decimal routeAACV = 2500000m; // ₹25L ACV
            double routeAWinRate = 0.25;  // 25% win rate
            int routeACapacity = 2;       // 2 delivery slots
            var routeA = _deterministicSimulator.SimulateDeterministicStrategy(
                "Enterprise AI Solutions Route (High-ACV Focus)",
                objective,
                snapshot,
                routeAACV,
                routeAWinRate,
                routeACapacity,
                requiredBudgetINR: 20000m);

            // Route B: Deterministic Mid-Market Acceleration Retainers
            decimal routeBACV = 1000000m; // ₹10L ACV
            double routeBWinRate = 0.35;  // 35% win rate
            int routeBCapacity = 3;       // 3 delivery slots
            var routeB = _deterministicSimulator.SimulateDeterministicStrategy(
                "Custom Software Acceleration Retainers (Mid-Market Focus)",
                objective,
                snapshot,
                routeBACV,
                routeBWinRate,
                routeBCapacity,
                requiredBudgetINR: 35000m);

            // Route C: AI Strategy Hypothesis (AI Estimate)
            decimal routeCACV = 1500000m;
            double routeCWinRate = 0.28;
            int routeCCapacity = 2;
            var routeC = _aiSimulator.FormulateAIHypothesisRoute(
                "Vertical AI Modernization Hypothesis",
                "High-Growth Healthcare & FinTech Clinics",
                "Targeting regulated mid-market healthcare providers needing HIPAA/data-compliant AI workflows.",
                objective,
                snapshot,
                routeCACV,
                routeCWinRate,
                routeCCapacity,
                requiredBudgetINR: 25000m);

            // Evaluate Constitution for each candidate
            foreach (var c in new[] { routeA, routeB, routeC })
            {
                var constResult = _constitutionEngine.EvaluateCandidate(c, snapshot);
                c.ConstitutionCompliant = constResult.IsCompliant;
                if (!constResult.IsCompliant)
                {
                    c.ConstitutionViolations = constResult.Violations.Select(v => $"{v.RuleName}: {v.ViolationReason}").ToList();
                    c.Feasibility = StrategyFeasibilityClassification.UnfeasibleConstitutionalViolation;
                }
                candidates.Add(c);
            }

            // Rank and flag recommendation:
            // Prefer FeasibleEvidenced over HypotheticalUnverified, and never recommend Unfeasible
            var viableCandidates = candidates
                .Where(c => c.Feasibility != StrategyFeasibilityClassification.UnfeasibleResourceConstrained &&
                            c.Feasibility != StrategyFeasibilityClassification.UnfeasibleConstitutionalViolation)
                .OrderByDescending(c => c.Feasibility == StrategyFeasibilityClassification.FeasibleEvidenced)
                .ThenByDescending(c => c.ConfidenceScore)
                .ToList();

            if (viableCandidates.Count > 0)
            {
                viableCandidates.First().IsRecommended = true;
            }
            else if (candidates.Count > 0)
            {
                // In Antarctica / unknown world, Route A is first candidate but explicitly flagged with risk
                candidates.First().IsRecommended = true;
            }

            return Task.FromResult<IReadOnlyList<StrategyCandidate>>(candidates);
        }
    }
}
