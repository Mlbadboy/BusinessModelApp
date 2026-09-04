using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Objectives;
using BusinessModelApp.Core.Domain.Strategy;
using BusinessModelApp.Core.Domain.WorldModel;

namespace BusinessModelApp.Core.Strategy
{
    public interface IStrategyEngine
    {
        /// <summary>
        /// Generates competing candidate strategy routes (A, B, C) for an objective.
        /// Ensures every external assumption is labeled with explicit provenance:
        /// VERIFIED_FACT, EXPLICIT_CEO_INPUT, HISTORICAL_COMPANY_DATA, AI_ESTIMATE, UNKNOWN.
        /// </summary>
        Task<IReadOnlyList<BusinessStrategy>> GenerateStrategyCandidatesAsync(
            BusinessObjective objective,
            CompanySnapshot snapshot,
            CancellationToken ct = default);

        /// <summary>
        /// Executes deterministic reverse funnel simulation:
        /// Target Revenue -> Required Wins -> Required Qualified Opps -> Required Meetings -> Required Prospects.
        /// Evaluates feasibility against available delivery capacity slots and evidence coverage.
        /// </summary>
        BusinessStrategy SimulateDeterministicFunnel(
            BusinessStrategy strategy,
            decimal targetRevenueINR,
            CompanySnapshot snapshot);
    }
}
