using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Organizational;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational
{
    public class WorkPortfolioPrioritizer : IWorkPortfolioPrioritizer
    {
        private readonly PortfolioSchedulingPolicy _policy;

        public WorkPortfolioPrioritizer(PortfolioSchedulingPolicy? policy = null)
        {
            _policy = policy ?? new PortfolioSchedulingPolicy();
        }

        public Task<IReadOnlyList<WorkPortfolioRank>> RankPortfolioAsync(
            string tenantId,
            IReadOnlyList<WorkItem> activeItems,
            int maxItems = 50,
            CancellationToken ct = default)
        {
            if (activeItems == null || activeItems.Count == 0)
            {
                return Task.FromResult<IReadOnlyList<WorkPortfolioRank>>(Array.Empty<WorkPortfolioRank>());
            }

            var now = DateTime.UtcNow;
            var ranks = new List<WorkPortfolioRank>();

            foreach (var item in activeItems)
            {
                // Age calculation
                double ageMinutes = Math.Max(0.0, (now - item.CreatedUtc).TotalMinutes);

                // Deadline pressure calculation
                double deadlinePressure = 0.0;
                if (item.Deadline != null)
                {
                    var totalWindow = (item.Deadline.HardCutoffUtc - item.CreatedUtc).TotalMinutes;
                    var remaining = (item.Deadline.HardCutoffUtc - now).TotalMinutes;
                    if (remaining <= 0)
                    {
                        deadlinePressure = 1.0; // Breached or right at deadline
                    }
                    else if (totalWindow > 0)
                    {
                        deadlinePressure = Math.Clamp(1.0 - (remaining / totalWindow), 0.0, 1.0);
                    }
                }

                // Theory of Constraints bottleneck severity (R3+ or Critical priority indicates high bottleneck impact)
                double bottleneckSeverity = 0.3;
                if (item.Priority == WorkPriority.Critical) bottleneckSeverity = 0.9;
                else if (item.Priority == WorkPriority.High) bottleneckSeverity = 0.7;
                else if (item.RiskTier >= WorkRiskTier.R3_Consequential) bottleneckSeverity = 0.8;

                // Strategic importance
                double strategicImportance = 0.5;
                if (item.Priority == WorkPriority.Critical) strategicImportance = 0.95;
                else if (item.Priority == WorkPriority.High) strategicImportance = 0.75;

                // Calculate deterministic score with starvation protection
                double rankScore = _policy.CalculateRankScore(
                    item.Priority,
                    item.Urgency,
                    bottleneckSeverity,
                    strategicImportance,
                    deadlinePressure,
                    ageMinutes);

                double starvationBoost = ageMinutes > _policy.StarvationThresholdMinutes
                    ? Math.Clamp((ageMinutes - _policy.StarvationThresholdMinutes) / 240.0, 0.0, 1.0)
                    : 0.0;

                string nextAction = DetermineNextAction(item);

                ranks.Add(new WorkPortfolioRank
                {
                    WorkId = item.WorkId,
                    ResponsibilityId = item.ResponsibilityId,
                    ComputedRankScore = rankScore,
                    Priority = item.Priority,
                    Urgency = item.Urgency,
                    Risk = item.RiskTier,
                    DeadlinePressure = Math.Round(deadlinePressure, 4),
                    StrategicImportance = strategicImportance,
                    DependencyCriticality = item.DependencyIds.Count > 0 ? 0.8 : 0.2,
                    BottleneckSeverity = bottleneckSeverity,
                    AgeMinutes = Math.Round(ageMinutes, 1),
                    StarvationBoost = Math.Round(starvationBoost, 4),
                    NextAction = nextAction
                });
            }

            // Deterministic ordering: descending by ComputedRankScore, then by WorkId for tie-breaking
            var ordered = ranks
                .OrderByDescending(r => r.ComputedRankScore)
                .ThenBy(r => r.WorkId, StringComparer.Ordinal)
                .Take(maxItems)
                .ToList();

            return Task.FromResult<IReadOnlyList<WorkPortfolioRank>>(ordered);
        }

        private static string DetermineNextAction(WorkItem item) => item.State switch
        {
            WorkState.Detected => "Qualify and formalize objective",
            WorkState.Qualified => "Decompose into WorkPlan",
            WorkState.Planned => "Assign to execution agent or fleet",
            WorkState.Assigned => "Prepare operational dependencies",
            WorkState.Preparing => "Resolve blockers or stage governance",
            WorkState.WaitingForGovernance => "Awaiting PRG-1 human review",
            WorkState.GovernanceApproved => "Admit to Mission Runtime DAG",
            WorkState.ExecutionAdmitted => "Dispatch mission to Execution Firewall",
            WorkState.Executing => "Monitor execution telemetry",
            WorkState.Verifying => "Validate outcome evidence",
            WorkState.Completed => "Measure realized impact",
            WorkState.Measured => "Close work cycle",
            WorkState.Blocked => "Unblock HardBlock prerequisites",
            WorkState.Paused => "Resume or cancel paused work",
            WorkState.Escalated => "Resolve executive escalation",
            _ => "Inspect work status"
        };
    }
}
