using System.Security.Cryptography;
using System.Text;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Brain;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Responsibility;

/// <summary>
/// Constitutional Invariant I37 — Bounded Continuous Responsibility & Mission Loop Sovereignty.
/// CONTINUOUS != UNBOUNDED != UNSUPERVISED != STATELESS.
/// </summary>
public static class DurableResponsibilityInvariants
{
    public const string PrimaryInvariant = "CONTINUOUS != UNBOUNDED != UNSUPERVISED != STATELESS";

    // 26 Sub-laws I37-A through I37-Z
    public const string I37_A_ContinuousNotUnbounded = "I37-A: Continuous autonomous operation is structured in discrete checkpointed cycles, never unbounded while(true) loops.";
    public const string I37_B_CycleStateProgression = "I37-B: Cycle state progression follows strict ordering: Init -> Sample -> Reason -> Formulate -> Govern -> Dispatch -> Checkpoint -> Complete.";
    public const string I37_C_BoundedReasoningBudget = "I37-C: Every cycle operates within a bounded reasoning budget (max iterations, timeout, compute caps).";
    public const string I37_D_WorkFormulationGating = "I37-D: Formulated work items must respect 3.9.7 OARA capacity allocations and 3.9.8 portfolio balance.";
    public const string I37_E_GovernanceGateEnforcement = "I37-E: High-consequence work proposals (Tier 3/4) must halt for PRG-1 human approval before dispatch.";
    public const string I37_F_NonExecutionSovereignty = "I37-F: The responsibility loop formulates proposals and dispatches to orchestrators; it cannot execute external mutations directly.";
    public const string I37_G_MultiTenantCycleIsolation = "I37-G: Responsibility cycles, checkpoints, and mappings are strictly isolated by TenantId.";
    public const string I37_H_TenantPenetrationDefense = "I37-H: Accessing cycles or checkpoints belonging to another tenant throws UnauthorizedAccessException.";
    public const string I37_I_HumanPauseOverride = "I37-I: A human supervisor can pause, resume, or abort the continuous cycle at any point.";
    public const string I37_J_CheckpointImmutability = "I37-J: Checkpoints are stored append-only with an invariant cryptographic SHA-256 CycleHash.";
    public const string I37_K_EpistemicGrounding = "I37-K: Work items must be grounded in verified or live cognitive state from Batch 4.0 Brain.";
    public const string I37_L_AntiThrashingCadence = "I37-L: Cycles enforce minimum cooldown intervals between repeated formulations for the same domain.";
    public const string I37_M_CrashRecoveryResilience = "I37-M: System can recover state deterministically from the latest valid checkpoint without losing cycle continuity.";
    public const string I37_N_DeterministicAuditTrace = "I37-N: Every cycle records trigger reason, cognitive snapshot ID, budget consumption, and work outcomes.";
    public const string I37_O_FailClosedOnBudgetBreach = "I37-O: Exceeding reasoning budget or timeout terminates the cycle cleanly in a fail-closed Degraded state.";
    public const string I37_P_NoRogueTaskCreation = "I37-P: Formulated tasks must link to an authenticated Ambient Responsibility and Worker Persona.";
    public const string I37_Q_PriorityLexicographicOrder = "I37-Q: Work items are ordered lexicographically by consequence tier and urgency score.";
    public const string I37_R_SimulatedSegregation = "I37-R: Simulations evaluated during cycle reasoning cannot dispatch live production work items.";
    public const string I37_S_ZeroDirectSelfMutation = "I37-S: The responsibility coordinator cannot rewrite cycle policies or increase its own reasoning caps.";
    public const string I37_T_EpistemicUnknownRespect = "I37-T: Epistemic gaps detected by the Brain halt speculative work in unobserved domains.";
    public const string I37_U_Batch6FirewallRespect = "I37-U: All dispatched work destined for real-world connectors must possess a valid Batch 6 execution permit.";
    public const string I37_V_ContradictionHalt = "I37-V: Critical cognitive contradictions halt automated work formulation until human review resolves discrepancy.";
    public const string I37_W_ResourceDebtTracking = "I37-W: Cycles record cumulative resource consumption to prevent hidden organizational debt.";
    public const string I37_X_GracefulCycleCompletion = "I37-X: Every cycle terminates in an explicit terminal state (Completed, Paused, Aborted).";
    public const string I37_Y_PersonaSpecialization = "I37-Y: Tasks are dispatched exclusively to verified specialized workers from Batch 3.6 Fabric.";
    public const string I37_Z_OperatingHeartbeatIntegrity = "I37-Z: The continuous loop represents Charlie's governed heartbeat, serving human intent without replacing human authority.";
}

public enum ResponsibilityCycleState
{
    CycleInitialized,
    StateSampled,
    ReasoningBounded,
    WorkFormulated,
    GovernanceChecked,
    Dispatched,
    Checkpointed,
    CycleCompleted,
    PausedByHuman,
    CycleAborted
}

public enum CycleTriggerReason
{
    ScheduledHeartbeat,
    AttentionThresholdExceeded,
    EmergencySignal,
    HumanRequested
}

public enum ResponsibilityPriority
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Bounded reasoning budget ensuring finite compute and time limits per cycle (I37-C, I37-O).
/// </summary>
public sealed class BoundedReasoningBudget
{
    public int MaxIterations { get; set; } = 5;
    public int IterationsConsumed { get; set; }
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public int TokenLimit { get; set; } = 4000;
    public int TokensConsumed { get; set; }

    public bool IsBreached(TimeSpan elapsed) =>
        IterationsConsumed > MaxIterations ||
        TokensConsumed > TokenLimit ||
        elapsed > Timeout;
}

/// <summary>
/// Explicit mapping between a cognitive attention priority and an ambient worker responsibility (I37-P).
/// </summary>
public sealed class DurableResponsibilityMapping
{
    public string MappingId { get; set; } = Guid.NewGuid().ToString("N");
    public string AttentionArea { get; set; } = string.Empty;
    public string TargetResponsibilityId { get; set; } = string.Empty;
    public string AssignedWorkerRole { get; set; } = "OperationsWorker";
    public ResponsibilityPriority Priority { get; set; } = ResponsibilityPriority.Medium;
}

/// <summary>
/// Formulated work item ready for governance evaluation and dispatch (I37-D, I37-E).
/// </summary>
public sealed class FormulatedWorkItem
{
    public string WorkItemId { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string ConsequenceTier { get; set; } = "Tier2_SoftGovernance"; // Tier1, Tier2, Tier3, Tier4
    public bool RequiresHumanApproval => ConsequenceTier == "Tier3_HardGovernance" || ConsequenceTier == "Tier4_MultiParty";
    public string TargetWorkerRole { get; set; } = "OperationsWorker";
    public double RequiredCapacityPercentage { get; set; } = 10.0;
    public bool IsDispatched { get; set; }
    public DateTime FormulatedUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Cryptographically hashed state checkpoint enabling crash recovery and auditability (I37-J, I37-M).
/// </summary>
public sealed class ResponsibilityCycleCheckpoint
{
    public string CheckpointId { get; set; } = Guid.NewGuid().ToString("N");
    public string CycleId { get; set; } = string.Empty;
    public long SequenceNumber { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public ResponsibilityCycleState State { get; set; }
    public int FormulatedWorkCount { get; set; }
    public int DispatchedWorkCount { get; set; }
    public string StateSnapshotId { get; set; } = string.Empty;
    public string CheckpointHash { get; set; } = string.Empty;
    public DateTime CheckpointedUtc { get; set; } = DateTime.UtcNow;

    public void ComputeCheckpointHash()
    {
        var raw = $"{TenantId}:{CycleId}:{SequenceNumber}:{State}:{FormulatedWorkCount}:{DispatchedWorkCount}:{StateSnapshotId}:{CheckpointedUtc:O}";
        using var sha = SHA256.Create();
        CheckpointHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
    }
}

/// <summary>
/// Discrete, checkpointed Durable Responsibility Cycle representing one heartbeat iteration (I37-A, I37-B).
/// </summary>
public sealed class DurableResponsibilityCycle
{
    public string CycleId { get; set; } = Guid.NewGuid().ToString("N");
    public long CycleSequenceNumber { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public CycleTriggerReason TriggerReason { get; set; } = CycleTriggerReason.ScheduledHeartbeat;
    public ResponsibilityCycleState State { get; set; } = ResponsibilityCycleState.CycleInitialized;

    public BoundedReasoningBudget Budget { get; set; } = new();
    public ExecutiveCognitiveState? SampledCognitiveState { get; set; }
    public List<DurableResponsibilityMapping> ActiveMappings { get; set; } = new();
    public List<FormulatedWorkItem> FormulatedWorkItems { get; set; } = new();
    public List<string> DispatchedMissionIds { get; set; } = new();

    public string CycleHash { get; set; } = string.Empty;
    public DateTime StartedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedUtc { get; set; }

    public void ComputeCycleHash()
    {
        var raw = $"{TenantId}:{CycleId}:{CycleSequenceNumber}:{TriggerReason}:{State}:{FormulatedWorkItems.Count}:{DispatchedMissionIds.Count}:{StartedUtc:O}";
        using var sha = SHA256.Create();
        CycleHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
    }
}
