using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Constraints;
using BusinessModelApp.Core.Interfaces.Runtime.Constraints;

namespace BusinessModelApp.Infrastructure.Runtime.Constraints
{
    public class SafeAlternativeEngine : ISafeAlternativeEngine
    {
        private readonly IPreFlightSimulationEngine _simulator;

        public SafeAlternativeEngine(IPreFlightSimulationEngine simulator)
        {
            _simulator = simulator ?? throw new ArgumentNullException(nameof(simulator));
        }

        public async Task<IReadOnlyList<SafeAlternativeProposal>> GenerateAlternativesAsync(
            Guid workspaceId,
            PreFlightEffectProposal blockedProposal,
            DigitalTwinBusinessState twinState,
            IReadOnlyList<ConstraintEvaluationResult> hardViolations,
            CancellationToken ct = default)
        {
            var alternatives = new List<SafeAlternativeProposal>();

            // Alternative 1: Controlled Pilot (20% of budget)
            var pilotEffect = blockedProposal with
            {
                CashOutflowINR = blockedProposal.CashOutflowINR * 0.20,
                ExpectedRevenueINR = blockedProposal.ExpectedRevenueINR * 0.25,
                ProjectedBurnRateChangeINR = blockedProposal.ProjectedBurnRateChangeINR * 0.20
            };
            var simPilot = await _simulator.SimulateEffectAsync(workspaceId, pilotEffect, twinState, ct);
            if (simPilot.IsPermissible)
            {
                alternatives.Add(new SafeAlternativeProposal
                {
                    Title = "Controlled Low-Budget Pilot Experiment",
                    Description = "Execute 20% budget test run to establish empirical proof points without violating liquidity constraints.",
                    AlternativeEffect = pilotEffect,
                    CostReductionPercentage = 80.0,
                    FeasibilityScore = 0.90,
                    PassesAllHardConstraints = true,
                    Rationale = "Passes all cash reserve and burn rate constraints cleanly."
                });
            }

            // Alternative 2: Organic / Existing Customer Focus (Zero additional paid spend)
            var organicEffect = blockedProposal with
            {
                CashOutflowINR = 0.0,
                ExpectedRevenueINR = blockedProposal.ExpectedRevenueINR * 0.40,
                ProjectedBurnRateChangeINR = 0.0,
                ActionCategory = "OrganicRetention"
            };
            var simOrganic = await _simulator.SimulateEffectAsync(workspaceId, organicEffect, twinState, ct);
            if (simOrganic.IsPermissible)
            {
                alternatives.Add(new SafeAlternativeProposal
                {
                    Title = "Organic & Retention Channel Pivot",
                    Description = "Reallocate focus to existing high-LTV customer base using zero paid media spend.",
                    AlternativeEffect = organicEffect,
                    CostReductionPercentage = 100.0,
                    FeasibilityScore = 0.95,
                    PassesAllHardConstraints = true,
                    Rationale = "Preserves 100% of cash reserves while generating incremental margin."
                });
            }

            // Alternative 3: Asset Preparation Without Consequential External Spend
            var prepEffect = blockedProposal with
            {
                CashOutflowINR = 5000.0, // minor compute/token prep only
                ExpectedRevenueINR = 0.0,
                ProjectedBurnRateChangeINR = 0.0,
                AdditionalApprovalLoad = 0,
                ActionCategory = "AssetPreparation"
            };
            var simPrep = await _simulator.SimulateEffectAsync(workspaceId, prepEffect, twinState, ct);
            if (simPrep.IsPermissible)
            {
                alternatives.Add(new SafeAlternativeProposal
                {
                    Title = "Prepare Assets in Staging",
                    Description = "Synthesize campaign copy, pricing models, and target lists in Staging; defer live activation until runway improves.",
                    AlternativeEffect = prepEffect,
                    CostReductionPercentage = 99.0,
                    FeasibilityScore = 0.99,
                    PassesAllHardConstraints = true,
                    Rationale = "Readies high-impact assets with negligible immediate cash outlay."
                });
            }

            return alternatives;
        }
    }
}
