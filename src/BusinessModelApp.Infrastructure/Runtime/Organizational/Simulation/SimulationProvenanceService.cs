using BusinessModelApp.Core.Domain.Runtime.Organizational.Simulation;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational.Simulation;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Simulation;

public sealed class SimulationProvenanceService : ISimulationProvenanceService
{
    public Task<SimulationProvenanceTrace> GenerateTraceAsync(
        SimulationRunMetadata run,
        SimulationScenario scenario,
        SimulationWorldSnapshot snapshot,
        IReadOnlyList<SimulationOutcome> outcomes)
    {
        if (run == null) throw new ArgumentNullException(nameof(run));
        if (scenario == null) throw new ArgumentNullException(nameof(scenario));
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

        var trace = new SimulationProvenanceTrace
        {
            RunId = run.SimulationRunId,
            ScenarioId = scenario.ScenarioId,
            WhySimulated = $"Counterfactual strategic testing under hypothesis: '{scenario.Description}'.",
            WhyTheseAgents = $"Synthesized {scenario.AgentPopulationSize} synthetic agent personas reflecting current customer and competitor segmentations.",
            WhyThisPopulation = $"Population size calibrated to capture statistically valid distribution tail risks without exceeding compute budget.",
            WhyThisHorizon = $"Simulated across {scenario.TimeHorizonDays} days with {scenario.TimeStepDays}-day epochs to observe medium-term stabilization.",
            WhyTheseVariables = $"Tested sensitivity on decision variables: {string.Join(", ", scenario.DecisionVariables.Select(d => $"{d.Name} ({d.OriginalValue}->{d.SimulatedValue})"))}.",
            WhyThisModel = $"Model version {run.ModelVersion} with random seed {run.RandomSeed} ensures bit-for-bit reproducibility.",
            WhyThisProvider = $"Provider '{run.ProviderId}' selected for sandboxed execution compliance under Invariant I34.",
            WhyThisResult = $"Outcomes project {outcomes.Count} multi-dimensional metrics with average confidence {Math.Round(outcomes.Average(o => o.Confidence), 2):P0}."
        };

        return Task.FromResult(trace);
    }
}
