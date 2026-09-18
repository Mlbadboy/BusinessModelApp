using BusinessModelApp.Core.Domain.Runtime.Organizational.Simulation;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational.Simulation;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Simulation;

public sealed class SimulationAgentRuntime : ISimulationAgentRuntime
{
    private readonly ISimulationSecurityGuard _securityGuard;

    public SimulationAgentRuntime(ISimulationSecurityGuard securityGuard)
    {
        _securityGuard = securityGuard ?? throw new ArgumentNullException(nameof(securityGuard));
    }

    public List<SimulationAgent> SynthesizePopulation(
        SimulationScenario scenario,
        SimulationWorldSnapshot snapshot)
    {
        if (scenario == null) throw new ArgumentNullException(nameof(scenario));
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

        var agents = new List<SimulationAgent>();
        var random = new Random(scenario.RandomSeed);

        int count = Math.Min(scenario.AgentPopulationSize, scenario.Budget.MaxAgents);

        for (int i = 0; i < count; i++)
        {
            var persona = (SimulationPersonaType)(i % 11);
            var decisionStyle = (SimulationDecisionStyle)(random.Next(0, 6));

            var agent = new SimulationAgent
            {
                AgentId = $"agent-{scenario.ScenarioId[..8]}-{i:D4}",
                Name = $"Synthetic_{persona}_{i}",
                Persona = persona,
                DecisionStyle = decisionStyle,
                GoalDescription = $"Simulate {persona} optimization under scenario {scenario.Name}",
                RiskTolerance = Math.Round(0.2 + (random.NextDouble() * 0.6), 2),
                Domain = "OrganizationalBehavior",
                EconomicProfile = new AgentEconomicProfile
                {
                    Budget = Math.Round(500.0 + (random.NextDouble() * 5000.0), 2),
                    PriceSensitivity = Math.Round(0.3 + (random.NextDouble() * 0.5), 2),
                    BrandLoyalty = Math.Round(0.4 + (random.NextDouble() * 0.5), 2),
                    QualityExpectation = Math.Round(0.6 + (random.NextDouble() * 0.3), 2),
                    SwitchingCostBarrier = Math.Round(0.2 + (random.NextDouble() * 0.6), 2)
                }
            };

            // Assert security
            if (!_securityGuard.ValidateNoProductionCredentialExposed(agent))
            {
                throw new InvalidOperationException("Security violation: Synthetic agent exposed production credentials! (I34-W)");
            }

            agents.Add(agent);
        }

        return agents;
    }

    public Task<List<AgentInteractionEvent>> RunAgentStepAsync(
        int stepIndex,
        List<SimulationAgent> agents,
        SimulationScenario scenario)
    {
        var events = new List<AgentInteractionEvent>();
        var random = new Random(scenario.RandomSeed + stepIndex);

        for (int i = 0; i < Math.Min(agents.Count, 20); i++)
        {
            var initiator = agents[i];
            var target = agents[(i + 1) % agents.Count];

            string rawPayload = $"Step {stepIndex}: {initiator.Persona} evaluates value proposition of {target.Persona}.";
            string sanitized = _securityGuard.SanitizeAgentPayload(rawPayload);

            double utility = Math.Round(0.5 + (random.NextDouble() * 0.5) - (initiator.EconomicProfile.PriceSensitivity * 0.2), 3);

            // Record memory strictly in sandbox (I34-I)
            initiator.Memory.Add(new SimulationMemoryItem
            {
                SimulationStep = stepIndex,
                Context = $"Interaction with {target.Name}",
                Observation = sanitized,
                PerceivedUtility = utility
            });

            events.Add(new AgentInteractionEvent
            {
                StepIndex = stepIndex,
                InitiatorAgentId = initiator.AgentId,
                TargetAgentId = target.AgentId,
                InteractionType = "SimulatedInteraction",
                MessagePayloadSanitized = sanitized,
                OutcomeUtility = utility
            });
        }

        return Task.FromResult(events);
    }
}
