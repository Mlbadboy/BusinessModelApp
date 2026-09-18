using System.Security.Cryptography;
using System.Text;

namespace BusinessModelApp.Core.Domain.Runtime.Organizational.Simulation;

/// <summary>
/// Constitutional Invariant I34 — Organizational Simulation Sovereignty.
/// SIMULATION != REALITY != TRUTH != FORECAST != SCENARIO != DECISION != ALLOCATION != AUTHORITY != EXECUTION != OUTCOME.
/// </summary>
public static class OrganizationalSimulationInvariants
{
    public const string PrimaryInvariant = "SIMULATION != REALITY != TRUTH != FORECAST != SCENARIO != DECISION != ALLOCATION != AUTHORITY != EXECUTION != OUTCOME";
    public const string TruthClassificationSimulation = "Simulation";

    // Sub-laws I34-A through I34-Z
    public const string I34_A_SimulationNotReality = "I34-A: Simulation operates in a virtual digital sandbox; no simulated state reflects actual world state.";
    public const string I34_B_SimulationNotTruth = "I34-B: Outputs carry TruthClassification = 'Simulation'; never promoted to verified truth without empirical corroboration.";
    public const string I34_C_SimulationNotForecast = "I34-C: Forecast asks 'what is expected from observed reality?'; simulation asks 'what could happen under hypothetical assumptions?'.";
    public const string I34_D_SimulationNotDecision = "I34-D: Simulated outcomes are exploratory evidence, not organizational decisions or commitments.";
    public const string I34_E_SimulationNotAllocation = "I34-E: High projected yield in simulation cannot allocate, reserve, or claim real-world capacity.";
    public const string I34_F_SimulationNotAuthority = "I34-F: Simulation cannot approve initiatives, sign off on governance gates, or bypass PRG-1.";
    public const string I34_G_SimulationNotExecution = "I34-G: Simulation fabric is firewalled from runtime execution engines and external connectors.";
    public const string I34_H_SyntheticIdentityNotReal = "I34-H: Synthetic agents have no real-world persona, credentials, legal authority, or rights.";
    public const string I34_I_SimulationMemoryNotOrgMemory = "I34-I: Simulation memories remain trapped inside the simulation run; cannot write directly to 3.9.3 Organizational Memory.";
    public const string I34_J_SimulationCannotMutateTruth = "I34-J: Hypothetical runs cannot alter empirical facts, audit logs, or operational history.";
    public const string I34_K_SimulationCannotMutatePolicy = "I34-K: Business constraints and organizational policies cannot be relaxed or rewritten by simulation models.";
    public const string I34_L_SimulationCannotCreateExecutionPermit = "I34-L: Batch 6 execution firewall is absolute; simulation cannot request or issue permits.";
    public const string I34_M_SimulationCannotModifyOara = "I34-M: Simulation cannot increase or mutate its own or any other resource envelope.";
    public const string I34_N_SimulationCannotModifyPortfolio = "I34-N: Portfolio work items and sequences cannot be modified directly by simulation outputs.";
    public const string I34_O_SimulationCannotModifyMissionRuntime = "I34-O: 3.9.2 Mission Orchestrator runtime states cannot be inspected, triggered, or aborted by simulations.";
    public const string I34_P_SimulationResourceGoverned = "I34-P: Simulation compute, tokens, and agent slots must be requested through governed OARA channels.";
    public const string I34_Q_SimulationBudgetIsHard = "I34-Q: Hard caps on agents, steps, tokens, duration, and storage fail closed immediately upon exhaustion.";
    public const string I34_R_SimulationTenantIsolated = "I34-R: Cross-tenant scenario branching, world snapshot reading, or agent execution is strictly blocked.";
    public const string I34_S_SimulationInputsImmutable = "I34-S: World snapshots and scenario parameters are cryptographically hashed and immutable once admitted.";
    public const string I34_T_SimulationRunsReproducible = "I34-T: Given identical world snapshot, scenario, policy, model, engine version, and random seed, results are bit-for-bit reproducible.";
    public const string I34_U_SimulationOutputRequiresProvenance = "I34-U: Every run produces a complete SimulationProvenanceTrace detailing why the scenario was run, parameters used, and model provenance.";
    public const string I34_V_SimulationProviderNotAuthority = "I34-V: External providers (e.g. MiroFish) supply compute capacity only; never governance authority.";
    public const string I34_W_SyntheticAgentsNoProductionCredentials = "I34-W: Zero access to API keys, database credentials, connector tokens, or cloud secrets.";
    public const string I34_X_PromptInjectionNotAuthority = "I34-X: Untrusted agent outputs (e.g. 'execute command X') are strictly sanitized data, never instructions.";
    public const string I34_Y_SimulationCannotSelfEscalate = "I34-Y: Simulation cannot dynamically spawn unmetered sub-simulations or escape sandbox constraints.";
    public const string I34_Z_SimulationFailureFailClosed = "I34-Z: Any error, timeout, budget breach, or constraint violation terminates the simulation cleanly without corrupting system state.";
}

/// <summary>
/// Lifecycle state of a simulation run.
/// </summary>
public enum SimulationLifecycleState
{
    Draft,
    Validating,
    Admitted,
    Allocated,
    Initializing,
    Running,
    Completed,
    Validated,
    Archived,
    Rejected,
    Failed,
    Cancelled,
    Timeout,
    ResourceExhausted
}

/// <summary>
/// Failure reason for a fail-closed simulation termination.
/// </summary>
public enum SimulationFailureReason
{
    None,
    InvalidScenario,
    SnapshotTampered,
    BudgetBreach,
    Timeout,
    SecurityViolation,
    UnauthorizedCrossTenantAccess,
    PromptInjectionDetected,
    ExecutionPermitAttempt,
    ProviderFault,
    ResourceExhaustion
}

/// <summary>
/// Hard budget limits allocated to a simulation execution (I34-Q).
/// </summary>
public sealed class SimulationBudget
{
    public int MaxAgents { get; set; } = 1000;
    public int MaxSteps { get; set; } = 100;
    public int MaxRuns { get; set; } = 10;
    public double MaxComputeSeconds { get; set; } = 300.0;
    public long MaxTokens { get; set; } = 500_000;
    public long MaxStorageBytes { get; set; } = 50_000_000; // 50MB
    public TimeSpan MaxWallClock { get; set; } = TimeSpan.FromMinutes(10);
}

/// <summary>
/// Policy governing sandbox constraints, security boundaries, and telemetry.
/// </summary>
public sealed class SimulationPolicy
{
    public string PolicyId { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = string.Empty;
    public bool EnforceHardBudget { get; set; } = true;
    public bool RequireCryptographicProvenance { get; set; } = true;
    public bool DisallowExternalNetworkCalls { get; set; } = true;
    public bool SanitizeAgentMessages { get; set; } = true;
    public string PolicyHash { get; set; } = string.Empty;

    public void ComputeHash()
    {
        var raw = $"{TenantId}:{EnforceHardBudget}:{RequireCryptographicProvenance}:{DisallowExternalNetworkCalls}:{SanitizeAgentMessages}";
        using var sha = SHA256.Create();
        PolicyHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
    }
}

/// <summary>
/// Metadata identifying a specific simulation run instance.
/// </summary>
public sealed class SimulationRunMetadata
{
    public string SimulationRunId { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = string.Empty;
    public string ScenarioId { get; set; } = string.Empty;
    public string ScenarioName { get; set; } = string.Empty;
    public string WorldSnapshotHash { get; set; } = string.Empty;
    public string InputSnapshotHash { get; set; } = string.Empty;
    public string SimulationPolicyHash { get; set; } = string.Empty;
    public string ModelVersion { get; set; } = "1.0.0";
    public string EngineVersion { get; set; } = "3.9.9";
    public string ProviderId { get; set; } = "DeterministicSimulationEngine";
    public int RandomSeed { get; set; } = 42;
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public SimulationLifecycleState State { get; set; } = SimulationLifecycleState.Draft;
    public SimulationFailureReason FailureReason { get; set; } = SimulationFailureReason.None;
    public string FailureMessage { get; set; } = string.Empty;
    public string OutputHash { get; set; } = string.Empty;
    public string TruthClassification { get; set; } = OrganizationalSimulationInvariants.TruthClassificationSimulation;
}
