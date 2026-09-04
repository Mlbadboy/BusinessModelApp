using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Objectives;
using BusinessModelApp.Core.Domain.Strategy;
using BusinessModelApp.Core.Domain.WorldModel;

namespace BusinessModelApp.Core.Strategy
{
    public interface ICommercialStrategyEngine
    {
        /// <summary>
        /// Formulates competing strategy candidates (deterministic routes + AI hypotheses),
        /// calculates reverse funnels, applies capacity/feasibility checks, and ranks them.
        /// Invariant: Every assumption carries provenance (VERIFIED_FACT, EXPLICIT_CEO_INPUT, HISTORICAL_COMPANY_DATA, AI_ESTIMATE, UNKNOWN).
        /// </summary>
        Task<IReadOnlyList<StrategyCandidate>> FormulateStrategiesAsync(
            BusinessObjective objective,
            CompanySnapshot snapshot,
            CancellationToken ct = default);
    }
}
