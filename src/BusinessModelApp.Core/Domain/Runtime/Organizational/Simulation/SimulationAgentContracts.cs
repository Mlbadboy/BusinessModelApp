namespace BusinessModelApp.Core.Domain.Runtime.Organizational.Simulation;

public enum SimulationPersonaType
{
    Customer,
    Prospect,
    Employee,
    Manager,
    Executive,
    Competitor,
    Supplier,
    Partner,
    MarketActor,
    Regulator,
    SyntheticAgent
}

public enum SimulationDecisionStyle
{
    Rational,
    RiskAverse,
    Aggressive,
    Opportunistic,
    Heuristic,
    Random
}

public sealed class AgentEconomicProfile
{
    public double Budget { get; set; } = 1000.0;
    public double PriceSensitivity { get; set; } = 0.5;
    public double BrandLoyalty { get; set; } = 0.5;
    public double QualityExpectation { get; set; } = 0.7;
    public double SwitchingCostBarrier { get; set; } = 0.3;
}

/// <summary>
/// Memory record stored strictly inside the simulation sandbox (I34-I).
/// </summary>
public sealed class SimulationMemoryItem
{
    public string MemoryId { get; set; } = Guid.NewGuid().ToString("N");
    public int SimulationStep { get; set; }
    public string Context { get; set; } = string.Empty;
    public string Observation { get; set; } = string.Empty;
    public double PerceivedUtility { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Synthetic agent persona participating in a simulation.
/// Has zero production credentials, permissions, or real-world identities (I34-H, I34-W).
/// </summary>
public sealed class SimulationAgent
{
    public string AgentId { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public SimulationPersonaType Persona { get; set; } = SimulationPersonaType.Customer;
    public SimulationDecisionStyle DecisionStyle { get; set; } = SimulationDecisionStyle.Rational;
    public string GoalDescription { get; set; } = string.Empty;
    public double RiskTolerance { get; set; } = 0.5;
    public string Domain { get; set; } = "GeneralCommerce";

    public AgentEconomicProfile EconomicProfile { get; set; } = new();
    public List<SimulationMemoryItem> Memory { get; set; } = new();
    public Dictionary<string, double> Relationships { get; set; } = new();

    // Security assertions:
    public bool HasProductionCredentials => false;
    public bool HasExecutionPermitRights => false;
    public bool CanCallRealWorldConnectors => false;
}

/// <summary>
/// Event recorded during agent-to-agent or agent-to-environment interactions.
/// </summary>
public sealed class AgentInteractionEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");
    public int StepIndex { get; set; }
    public string InitiatorAgentId { get; set; } = string.Empty;
    public string TargetAgentId { get; set; } = string.Empty;
    public string InteractionType { get; set; } = "Negotiate";
    public string MessagePayloadSanitized { get; set; } = string.Empty;
    public double OutcomeUtility { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
