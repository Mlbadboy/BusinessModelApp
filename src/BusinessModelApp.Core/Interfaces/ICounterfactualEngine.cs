using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Learning;

namespace BusinessModelApp.Core.Interfaces
{
    /// <summary>
    /// Governed Counterfactual Reasoning Engine.
    /// Invariant: Counterfactual outputs are strictly HYPOTHESIS / SIMULATION and NEVER FACT!
    /// </summary>
    public interface ICounterfactualEngine
    {
        Task<CounterfactualSimulation> SimulateCounterfactualAsync(
            Guid workspaceId,
            Guid sourceMissionId,
            string interventionDescription,
            Dictionary<string, string> changedVariables,
            CancellationToken ct = default);

        Task<IReadOnlyList<CounterfactualSimulation>> GetSimulationsForMissionAsync(
            Guid workspaceId,
            Guid missionId,
            CancellationToken ct = default);
    }
}
