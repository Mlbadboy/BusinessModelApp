using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Decisions;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Objectives;
using BusinessModelApp.Core.Domain.Strategy;
using BusinessModelApp.Core.Domain.WorldModel;

namespace BusinessModelApp.Core.Services
{
    public class CharlieExecutivePipelineResult
    {
        public BusinessObjective Objective { get; set; } = default!;
        public CompanySnapshot Snapshot { get; set; } = default!;
        public RevenueBaseline RevenueBaseline { get; set; } = default!;
        public StrategyCandidate SelectedStrategy { get; set; } = default!;
        public DecisionRecord DecisionRecord { get; set; } = default!;
        public DurableMission Mission { get; set; } = default!;
        public bool ExecutionWallEnforced { get; set; } = true; // Hard Phase 1 Boundary: REAL TOOLS DISABLED
        public string ExecutiveSummary { get; set; } = string.Empty;
    }

    public interface ICharlieExecutiveService
    {
        /// <summary>
        /// Executes Charlie CEO's end-to-end executive pipeline:
        /// CEO Prompt -> Verified World Model -> Four-State Revenue Baseline -> Strategic Simulations
        /// -> Constitution Check -> Immutable DecisionRecord -> Durable Mission Preparation.
        /// 
        /// Invariant: Does not execute unapproved real-world side effects. Halts at READY_FOR_EXECUTION.
        /// </summary>
        Task<CharlieExecutivePipelineResult> ProcessExecutiveMandateAsync(
            string ceoPrompt,
            Guid workspaceId,
            CancellationToken ct = default);
    }
}
