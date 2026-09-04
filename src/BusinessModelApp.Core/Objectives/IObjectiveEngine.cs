using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Objectives;
using BusinessModelApp.Core.Domain.WorldModel;

namespace BusinessModelApp.Core.Objectives
{
    public interface IObjectiveEngine
    {
        /// <summary>
        /// Parses CEO natural language prompt into a structured BusinessObjective.
        /// Extracts target revenue, target deadline, market hints, and offer portfolio.
        /// </summary>
        Task<BusinessObjective> IngestCeoPromptAsync(
            string prompt,
            Guid workspaceId,
            CancellationToken ct = default);

        /// <summary>
        /// Evaluates current grounded company reality against the objective targets.
        /// Computes verified baseline, pipeline, and explicit revenue/cash gaps.
        /// </summary>
        Task<BusinessObjective> ReconcileObjectiveProgressAsync(
            Guid objectiveId,
            CompanySnapshot snapshot,
            CancellationToken ct = default);
    }
}
