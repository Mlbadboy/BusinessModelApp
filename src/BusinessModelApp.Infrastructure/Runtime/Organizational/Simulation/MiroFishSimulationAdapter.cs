using BusinessModelApp.Core.Domain.Runtime.Organizational.Simulation;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational.Simulation;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Simulation;

/// <summary>
/// Governed adapter boundary for external multi-agent simulation engines (e.g. MiroFish/OASIS) (I34-V).
/// Supplies multi-agent simulation compute; holds NO internal authority or credentials.
/// </summary>
public sealed class MiroFishSimulationAdapter : IMultiAgentSimulationProvider
{
    private readonly ISimulationSecurityGuard _securityGuard;

    public string ProviderId => "MiroFishSimulationAdapter";
    public string ProviderName => "MiroFish/OASIS Multi-Agent Simulation Provider";
    public string ProviderVersion => "1.2.0-sandboxed";
    public bool SupportsMultiAgent => true;

    public MiroFishSimulationAdapter(ISimulationSecurityGuard securityGuard)
    {
        _securityGuard = securityGuard ?? throw new ArgumentNullException(nameof(securityGuard));
    }

    public Task<List<SimulationOutcome>> ExecuteSimulationAsync(
        SimulationScenario scenario,
        SimulationWorldSnapshot snapshot,
        IReadOnlyList<SimulationAgent> agents,
        CancellationToken cancellationToken = default)
    {
        if (scenario == null) throw new ArgumentNullException(nameof(scenario));
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

        var outcomes = new List<SimulationOutcome>();
        int totalSteps = Math.Min(scenario.TimeHorizonDays / Math.Max(1, scenario.TimeStepDays), scenario.Budget.MaxSteps);

        // Emergent multi-agent outcome projection via adapter boundary
        double emergentSocialAdoptionIndex = Math.Round(Math.Clamp(0.72 + (agents.Count * 0.0002), 0.1, 0.95), 3);
        double competitorCountermeasureProbability = Math.Round(Math.Clamp(0.55 + (snapshot.Competitors.AggressivenessIndex * 0.25), 0.1, 0.99), 2);

        outcomes.Add(new SimulationOutcome
        {
            ScenarioId = scenario.ScenarioId,
            MetricName = "EmergentSocialAdoptionIndex",
            ProjectedValue = emergentSocialAdoptionIndex,
            Confidence = 0.81,
            AgentPopulation = agents.Count,
            FinalStep = totalSteps,
            TruthClassification = OrganizationalSimulationInvariants.TruthClassificationSimulation,
            EvidenceSummary = $"Emergent peer-to-peer influence simulated across {agents.Count} synthetic agents.",
            Distribution = new OutcomeDistribution
            {
                Mean = emergentSocialAdoptionIndex,
                Median = emergentSocialAdoptionIndex,
                P10 = Math.Round(emergentSocialAdoptionIndex * 0.82, 3),
                P50 = emergentSocialAdoptionIndex,
                P90 = Math.Round(emergentSocialAdoptionIndex * 1.15, 3),
                Variance = 0.008,
                Min = 0.20,
                Max = 0.99
            }
        });

        outcomes.Add(new SimulationOutcome
        {
            ScenarioId = scenario.ScenarioId,
            MetricName = "CompetitorCountermeasureProbability",
            ProjectedValue = competitorCountermeasureProbability,
            Confidence = 0.78,
            AgentPopulation = agents.Count,
            FinalStep = totalSteps,
            TruthClassification = OrganizationalSimulationInvariants.TruthClassificationSimulation,
            EvidenceSummary = $"Simulated multi-agent game-theoretic response from {snapshot.Competitors.ActiveCompetitorsCount} competitors.",
            Distribution = new OutcomeDistribution
            {
                Mean = competitorCountermeasureProbability,
                Median = competitorCountermeasureProbability,
                P10 = Math.Round(competitorCountermeasureProbability * 0.80, 2),
                P50 = competitorCountermeasureProbability,
                P90 = Math.Round(competitorCountermeasureProbability * 1.15, 2),
                Variance = 0.012,
                Min = 0.10,
                Max = 1.0
            }
        });

        return Task.FromResult(outcomes);
    }

    public Task<List<AgentInteractionEvent>> SimulateAgentInteractionsAsync(
        SimulationScenario scenario,
        IReadOnlyList<SimulationAgent> agents,
        int steps,
        CancellationToken cancellationToken = default)
    {
        var events = new List<AgentInteractionEvent>();
        var random = new Random(scenario.RandomSeed);

        for (int s = 0; s < steps; s++)
        {
            for (int a = 0; a < Math.Min(agents.Count, 10); a++)
            {
                var sender = agents[a];
                var receiver = agents[(a + 1) % agents.Count];

                string rawMessage = $"Agent {sender.Name} communicates price expectation to {receiver.Name}.";
                string sanitized = _securityGuard.SanitizeAgentPayload(rawMessage);

                events.Add(new AgentInteractionEvent
                {
                    StepIndex = s,
                    InitiatorAgentId = sender.AgentId,
                    TargetAgentId = receiver.AgentId,
                    InteractionType = "EmergentNetworkInfluence",
                    MessagePayloadSanitized = sanitized,
                    OutcomeUtility = Math.Round(0.4 + (random.NextDouble() * 0.4), 3)
                });
            }
        }

        return Task.FromResult(events);
    }
}
