namespace BusinessModelApp.Core.Domain.Runtime.Organizational.Simulation;

public sealed class OutcomeDistribution
{
    public double Mean { get; set; }
    public double Median { get; set; }
    public double P10 { get; set; }
    public double P50 { get; set; }
    public double P90 { get; set; }
    public double Variance { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
}

/// <summary>
/// Simulated outcome projection. Explicitly marked as Simulation (I34-B).
/// </summary>
public sealed class SimulationOutcome
{
    public string OutcomeId { get; set; } = Guid.NewGuid().ToString("N");
    public string ScenarioId { get; set; } = string.Empty;
    public string RunId { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public double ProjectedValue { get; set; }
    public OutcomeDistribution Distribution { get; set; } = new();
    public double Confidence { get; set; } = 0.85;
    public int AgentPopulation { get; set; }
    public int FinalStep { get; set; }
    public string TruthClassification { get; set; } = OrganizationalSimulationInvariants.TruthClassificationSimulation;
    public string EvidenceSummary { get; set; } = string.Empty;
}

/// <summary>
/// Empirical calibration record comparing historical simulation to real-world outcome.
/// </summary>
public sealed class SimulationPerformanceRecord
{
    public string RecordId { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = string.Empty;
    public string ScenarioType { get; set; } = "PricingAdjustment";
    public string Domain { get; set; } = "ConsumerRetail";
    public string MarketRegime { get; set; } = "StableGrowth";
    public string ProviderId { get; set; } = "DeterministicSimulationEngine";
    public string EngineVersion { get; set; } = "3.9.9";
    public int TimeHorizonDays { get; set; } = 90;

    public double PredictedMetricValue { get; set; }
    public double ActualMetricValue { get; set; }
    public double CalibrationError { get; set; }
    public bool DirectionalAccuracy { get; set; }
    public double ObservedReliability { get; set; }
    public DateTime CalibratedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Explainability trace exposing full provenance of a simulation run (I34-U).
/// </summary>
public sealed class SimulationProvenanceTrace
{
    public string TraceId { get; set; } = Guid.NewGuid().ToString("N");
    public string RunId { get; set; } = string.Empty;
    public string ScenarioId { get; set; } = string.Empty;
    public string WhySimulated { get; set; } = string.Empty;
    public string WhyTheseAgents { get; set; } = string.Empty;
    public string WhyThisPopulation { get; set; } = string.Empty;
    public string WhyThisHorizon { get; set; } = string.Empty;
    public string WhyTheseVariables { get; set; } = string.Empty;
    public string WhyThisModel { get; set; } = string.Empty;
    public string WhyThisProvider { get; set; } = string.Empty;
    public string WhyThisResult { get; set; } = string.Empty;
    public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Comparison across counterfactual simulation scenarios.
/// </summary>
public sealed class SimulationComparisonResult
{
    public string ComparisonId { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = string.Empty;
    public List<string> ComparedScenarioIds { get; set; } = new();
    public Dictionary<string, List<SimulationOutcome>> OutcomesByScenario { get; set; } = new();
    public Dictionary<string, double> ComparativeYieldScores { get; set; } = new();
    public string TradeoffAnalysis { get; set; } = string.Empty;
    public string TruthClassification { get; set; } = OrganizationalSimulationInvariants.TruthClassificationSimulation;
}
