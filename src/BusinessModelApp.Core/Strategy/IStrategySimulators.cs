using System;
using System.Collections.Generic;
using BusinessModelApp.Core.Domain.Objectives;
using BusinessModelApp.Core.Domain.Strategy;
using BusinessModelApp.Core.Domain.WorldModel;

namespace BusinessModelApp.Core.Strategy
{
    public interface IDeterministicStrategySimulator
    {
        /// <summary>
        /// Simulates strategy candidate purely deterministically from verified data, historical performance,
        /// and explicit CEO parameters. Must be 100% reproducible.
        /// </summary>
        StrategyCandidate SimulateDeterministicStrategy(
            string strategyName,
            BusinessObjective objective,
            CompanySnapshot snapshot,
            decimal targetACVINR,
            double winRate,
            int capacitySlotsRequired,
            decimal requiredBudgetINR);
    }

    public interface IAIStrategySimulator
    {
        /// <summary>
        /// Generates AI-assisted strategy hypothesis routes.
        /// Invariant: All generated assumptions MUST be explicitly tagged AI_ESTIMATE or UNKNOWN.
        /// Outputs cannot be classified as FEASIBLE_EVIDENCED without grounding evidence.
        /// </summary>
        StrategyCandidate FormulateAIHypothesisRoute(
            string hypothesisName,
            string targetICP,
            string strategicConcept,
            BusinessObjective objective,
            CompanySnapshot snapshot,
            decimal estimatedACVINR,
            double estimatedWinRate,
            int capacitySlotsRequired,
            decimal requiredBudgetINR);
    }
}
