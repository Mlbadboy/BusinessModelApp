using BusinessModelApp.Api.Controllers;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Brain;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Responsibility;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Brain;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Responsibility;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Brain;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Responsibility;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BusinessModelApp.Tests.Domain;

public sealed class Phase4Batch41DurableResponsibilityTests
{
    private readonly IBrainAuditRepository _brainRepo;
    private readonly IEpistemicGapDetector _gapDetector;
    private readonly ICognitiveContradictionResolver _contradictionResolver;
    private readonly ICognitiveStateSynthesizer _synthesizer;
    private readonly IAutonomousBusinessBrainService _brainService;

    private readonly ICycleCheckpointRepository _checkpointRepo;
    private readonly IReasoningBudgetEnforcer _budgetEnforcer;
    private readonly IWorkFormulationEngine _formulationEngine;
    private readonly IResponsibilityCycleCoordinator _coordinator;
    private readonly IDurableResponsibilityService _responsibilityService;
    private readonly DurableResponsibilityController _controller;

    public Phase4Batch41DurableResponsibilityTests()
    {
        _brainRepo = new InMemoryBrainRepository();
        _gapDetector = new EpistemicGapDetector();
        _contradictionResolver = new CognitiveContradictionResolver();
        _synthesizer = new CognitiveStateSynthesizer(_gapDetector, _contradictionResolver);
        _brainService = new AutonomousBusinessBrainService(_synthesizer, _brainRepo);

        _checkpointRepo = new InMemoryCheckpointRepository();
        _budgetEnforcer = new ReasoningBudgetEnforcer();
        _formulationEngine = new WorkFormulationEngine();
        _coordinator = new ResponsibilityCycleCoordinator(_brainService, _budgetEnforcer, _formulationEngine, _checkpointRepo);
        _responsibilityService = new DurableResponsibilityService(_coordinator, _checkpointRepo);

        _controller = new DurableResponsibilityController(_responsibilityService);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _controller.ControllerContext.HttpContext.Request.Headers["X-Tenant-ID"] = "tenant-resp";
    }

    // =========================================================================
    // Family 1: Constitutional Invariants & Law I37 (RESP01 - RESP08)
    // =========================================================================

    [Fact]
    public void RESP01_PrimaryInvariant_ContainsAll26SubLaws()
    {
        DurableResponsibilityInvariants.PrimaryInvariant.Should().Be(
            "CONTINUOUS != UNBOUNDED != UNSUPERVISED != STATELESS");

        DurableResponsibilityInvariants.I37_A_ContinuousNotUnbounded.Should().StartWith("I37-A");
        DurableResponsibilityInvariants.I37_B_CycleStateProgression.Should().StartWith("I37-B");
        DurableResponsibilityInvariants.I37_C_BoundedReasoningBudget.Should().StartWith("I37-C");
        DurableResponsibilityInvariants.I37_D_WorkFormulationGating.Should().StartWith("I37-D");
        DurableResponsibilityInvariants.I37_E_GovernanceGateEnforcement.Should().StartWith("I37-E");
        DurableResponsibilityInvariants.I37_F_NonExecutionSovereignty.Should().StartWith("I37-F");
        DurableResponsibilityInvariants.I37_G_MultiTenantCycleIsolation.Should().StartWith("I37-G");
        DurableResponsibilityInvariants.I37_H_TenantPenetrationDefense.Should().StartWith("I37-H");
        DurableResponsibilityInvariants.I37_I_HumanPauseOverride.Should().StartWith("I37-I");
        DurableResponsibilityInvariants.I37_J_CheckpointImmutability.Should().StartWith("I37-J");
        DurableResponsibilityInvariants.I37_K_EpistemicGrounding.Should().StartWith("I37-K");
        DurableResponsibilityInvariants.I37_L_AntiThrashingCadence.Should().StartWith("I37-L");
        DurableResponsibilityInvariants.I37_M_CrashRecoveryResilience.Should().StartWith("I37-M");
        DurableResponsibilityInvariants.I37_N_DeterministicAuditTrace.Should().StartWith("I37-N");
        DurableResponsibilityInvariants.I37_O_FailClosedOnBudgetBreach.Should().StartWith("I37-O");
        DurableResponsibilityInvariants.I37_P_NoRogueTaskCreation.Should().StartWith("I37-P");
        DurableResponsibilityInvariants.I37_Q_PriorityLexicographicOrder.Should().StartWith("I37-Q");
        DurableResponsibilityInvariants.I37_R_SimulatedSegregation.Should().StartWith("I37-R");
        DurableResponsibilityInvariants.I37_S_ZeroDirectSelfMutation.Should().StartWith("I37-S");
        DurableResponsibilityInvariants.I37_T_EpistemicUnknownRespect.Should().StartWith("I37-T");
        DurableResponsibilityInvariants.I37_U_Batch6FirewallRespect.Should().StartWith("I37-U");
        DurableResponsibilityInvariants.I37_V_ContradictionHalt.Should().StartWith("I37-V");
        DurableResponsibilityInvariants.I37_W_ResourceDebtTracking.Should().StartWith("I37-W");
        DurableResponsibilityInvariants.I37_X_GracefulCycleCompletion.Should().StartWith("I37-X");
        DurableResponsibilityInvariants.I37_Y_PersonaSpecialization.Should().StartWith("I37-Y");
        DurableResponsibilityInvariants.I37_Z_OperatingHeartbeatIntegrity.Should().StartWith("I37-Z");
    }

    [Fact]
    public void RESP02_ContinuousNotUnbounded_CodifiedInPrimaryLaw()
    {
        DurableResponsibilityInvariants.PrimaryInvariant.Should().Contain("CONTINUOUS");
        DurableResponsibilityInvariants.PrimaryInvariant.Should().Contain("UNBOUNDED");
        DurableResponsibilityInvariants.PrimaryInvariant.Should().Contain("UNSUPERVISED");
        DurableResponsibilityInvariants.PrimaryInvariant.Should().Contain("STATELESS");
    }

    [Fact]
    public void RESP03_ContinuousOperationStructuredInDiscreteCycles()
    {
        DurableResponsibilityInvariants.I37_A_ContinuousNotUnbounded.Should().Contain("discrete checkpointed cycles, never unbounded while(true) loops");
    }

    [Fact]
    public void RESP04_CycleStateProgression_FollowsStrictOrdering()
    {
        DurableResponsibilityInvariants.I37_B_CycleStateProgression.Should().Contain("Init -> Sample -> Reason -> Formulate -> Govern -> Dispatch -> Checkpoint -> Complete");
    }

    [Fact]
    public void RESP05_BoundedReasoningBudget_EnforcedInLaw()
    {
        DurableResponsibilityInvariants.I37_C_BoundedReasoningBudget.Should().Contain("max iterations, timeout, compute caps");
    }

    [Fact]
    public void RESP06_WorkFormulation_MustRespectOARACapacity()
    {
        DurableResponsibilityInvariants.I37_D_WorkFormulationGating.Should().Contain("respect 3.9.7 OARA capacity allocations");
    }

    [Fact]
    public void RESP07_HighConsequenceWork_RequiresPRG1HumanApproval()
    {
        DurableResponsibilityInvariants.I37_E_GovernanceGateEnforcement.Should().Contain("must halt for PRG-1 human approval before dispatch");
    }

    [Fact]
    public void RESP08_NonExecutionSovereignty_CannotExecuteDirectMutations()
    {
        DurableResponsibilityInvariants.I37_F_NonExecutionSovereignty.Should().Contain("cannot execute external mutations directly");
    }

    // =========================================================================
    // Family 2: Bounded Reasoning Budget & Anti-Daemon Safeguards (RESP09 - RESP16)
    // =========================================================================

    [Fact]
    public void RESP09_Budget_DefaultsToSensibleLimits()
    {
        var budget = new BoundedReasoningBudget();
        budget.MaxIterations.Should().Be(5);
        budget.Timeout.Should().Be(TimeSpan.FromSeconds(30));
        budget.TokenLimit.Should().Be(4000);
        budget.IterationsConsumed.Should().Be(0);
    }

    [Fact]
    public void RESP10_IterationConsumption_TrackedAccurately()
    {
        var budget = new BoundedReasoningBudget();
        var valid = _budgetEnforcer.TryConsumeIteration(budget, 500, TimeSpan.FromSeconds(2), out var reason);

        valid.Should().BeTrue();
        reason.Should().BeEmpty();
        budget.IterationsConsumed.Should().Be(1);
        budget.TokensConsumed.Should().Be(500);
    }

    [Fact]
    public void RESP11_ExceedingMaxIterations_TriggersBreach()
    {
        var budget = new BoundedReasoningBudget { MaxIterations = 3, IterationsConsumed = 3 };
        var valid = _budgetEnforcer.TryConsumeIteration(budget, 100, TimeSpan.FromSeconds(1), out var reason);

        valid.Should().BeFalse();
        reason.Should().Contain("iteration cap exceeded");
    }

    [Fact]
    public void RESP12_ExceedingTokenLimit_TriggersBreach()
    {
        var budget = new BoundedReasoningBudget { TokenLimit = 1000, TokensConsumed = 900 };
        var valid = _budgetEnforcer.TryConsumeIteration(budget, 200, TimeSpan.FromSeconds(1), out var reason);

        valid.Should().BeFalse();
        reason.Should().Contain("token budget exceeded");
    }

    [Fact]
    public void RESP13_ExceedingTimeout_TriggersBreach()
    {
        var budget = new BoundedReasoningBudget { Timeout = TimeSpan.FromSeconds(10) };
        var valid = _budgetEnforcer.TryConsumeIteration(budget, 100, TimeSpan.FromSeconds(15), out var reason);

        valid.Should().BeFalse();
        reason.Should().Contain("exceeded timeout");
    }

    [Fact]
    public async Task RESP14_BudgetBreach_TerminatesCycleCleanlyInAbortedState()
    {
        // Custom coordinator where budget is pre-exhausted
        var tightBudgetEnforcer = new ExhaustedBudgetEnforcer();
        var coord = new ResponsibilityCycleCoordinator(_brainService, tightBudgetEnforcer, _formulationEngine, _checkpointRepo);

        var cycle = await coord.ExecuteCycleStepAsync("tenant-resp", CycleTriggerReason.ScheduledHeartbeat);
        cycle.State.Should().Be(ResponsibilityCycleState.CycleAborted);
        cycle.CompletedUtc.Should().NotBeNull();
    }

    private sealed class ExhaustedBudgetEnforcer : IReasoningBudgetEnforcer
    {
        public bool TryConsumeIteration(BoundedReasoningBudget budget, int tokens, TimeSpan elapsed, out string rejectionReason)
        {
            rejectionReason = "Test simulated budget breach";
            return false;
        }
    }

    [Fact]
    public void RESP15_WithinBudgetLimits_AllowsSuccessfulProgression()
    {
        var budget = new BoundedReasoningBudget();
        var valid = _budgetEnforcer.TryConsumeIteration(budget, 100, TimeSpan.FromSeconds(1), out var reason);
        valid.Should().BeTrue();
        reason.Should().BeEmpty();
    }

    [Fact]
    public void RESP16_AntiDaemonProtection_GuaranteesFiniteTermination()
    {
        var budget = new BoundedReasoningBudget();
        budget.IsBreached(TimeSpan.FromSeconds(35)).Should().BeTrue();
    }

    // =========================================================================
    // Family 3: State Progression & Epistemic Lifecycle (RESP17 - RESP24)
    // =========================================================================

    [Fact]
    public async Task RESP17_CycleProgression_CompletesCleanly()
    {
        var cycle = await _responsibilityService.ExecuteSingleCycleAsync("tenant-resp");
        cycle.Should().NotBeNull();
        cycle.State.Should().Be(ResponsibilityCycleState.CycleCompleted);
        cycle.CompletedUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task RESP18_CycleInitialized_AssignsSequenceNumberAndTimestamp()
    {
        var cycle = await _responsibilityService.ExecuteSingleCycleAsync("tenant-resp");
        cycle.CycleSequenceNumber.Should().BeGreaterThan(0);
        cycle.StartedUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RESP19_StateSampled_ConsumesCognitiveStateFromBrain()
    {
        var cycle = await _responsibilityService.ExecuteSingleCycleAsync("tenant-resp");
        cycle.SampledCognitiveState.Should().NotBeNull();
        cycle.SampledCognitiveState!.SnapshotId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RESP20_ReasoningBounded_UpdatesIterationCount()
    {
        var cycle = await _responsibilityService.ExecuteSingleCycleAsync("tenant-resp");
        cycle.Budget.IterationsConsumed.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RESP21_WorkFormulated_PopulatesFormulatedWorkItems()
    {
        var cycle = await _responsibilityService.ExecuteSingleCycleAsync("tenant-resp");
        cycle.FormulatedWorkItems.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RESP22_GovernanceChecked_DistinguishesTiers()
    {
        var cycle = await _responsibilityService.ExecuteSingleCycleAsync("tenant-resp");
        cycle.FormulatedWorkItems.Should().Contain(w => w.ConsequenceTier.StartsWith("Tier"));
    }

    [Fact]
    public async Task RESP23_Dispatched_MarksPreClearedItemsDispatched()
    {
        var cycle = await _responsibilityService.ExecuteSingleCycleAsync("tenant-resp");
        var autonomousItems = cycle.FormulatedWorkItems.Where(w => !w.RequiresHumanApproval).ToList();
        autonomousItems.Should().OnlyContain(w => w.IsDispatched);
    }

    [Fact]
    public async Task RESP24_Checkpointed_PersistsCryptographicCheckpoint()
    {
        var cycle = await _responsibilityService.ExecuteSingleCycleAsync("tenant-resp");
        var latestCp = await _checkpointRepo.GetLatestCheckpointAsync("tenant-resp");
        latestCp.Should().NotBeNull();
        latestCp!.CycleId.Should().Be(cycle.CycleId);
    }

    // =========================================================================
    // Family 4: Cognitive State Sampling & Priority Translation (RESP25 - RESP32)
    // =========================================================================

    [Fact]
    public async Task RESP25_Cycle_SamplesCurrentCognitiveState()
    {
        var brainState = await _brainService.ForceCognitiveSynthesisAsync("tenant-resp");
        var cycle = await _responsibilityService.ExecuteSingleCycleAsync("tenant-resp");

        cycle.SampledCognitiveState.Should().NotBeNull();
        cycle.SampledCognitiveState!.OverallHealth.Should().Be(brainState.OverallHealth);
    }

    [Fact]
    public async Task RESP26_AttentionPriorities_MappedToFormulatedWork()
    {
        var cycle = await _responsibilityService.ExecuteSingleCycleAsync("tenant-resp");
        var priorityWork = cycle.FormulatedWorkItems.FirstOrDefault(w => w.Domain == "CustomerRetention");

        priorityWork.Should().NotBeNull();
        priorityWork!.Title.Should().Contain("CustomerRetention");
    }

    [Fact]
    public async Task RESP27_RecentMaterialDeltas_GenerateInvestigationWork()
    {
        var cycle = await _responsibilityService.ExecuteSingleCycleAsync("tenant-resp");
        var deltaWork = cycle.FormulatedWorkItems.FirstOrDefault(w => w.Domain == "Commercial");

        deltaWork.Should().NotBeNull();
        deltaWork!.Title.Should().Contain("Investigate material delta");
    }

    [Fact]
    public async Task RESP28_SubMaterialDeltas_DoNotGenerateWorkItems()
    {
        var state = new ExecutiveCognitiveState();
        state.RecentDeltas.Add(new EnterpriseDeltasRecord { MetricOrEntity = "SubNoise", MaterialityScore = 0.05 });

        var formulated = await _formulationEngine.FormulateWorkAsync("tenant-resp", state, Array.Empty<DurableResponsibilityMapping>());
        formulated.Any(w => w.Title.Contains("SubNoise")).Should().BeFalse();
    }

    [Fact]
    public void RESP29_EpistemicGaps_HaltSpeculativeWork()
    {
        DurableResponsibilityInvariants.I37_T_EpistemicUnknownRespect.Should().Contain("halt speculative work in unobserved domains");
    }

    [Fact]
    public void RESP30_ContradictionHalt_CodifiedInI37V()
    {
        DurableResponsibilityInvariants.I37_V_ContradictionHalt.Should().Contain("Critical cognitive contradictions halt automated work formulation");
    }

    [Fact]
    public void RESP31_EpistemicGrounding_CodifiedInI37K()
    {
        DurableResponsibilityInvariants.I37_K_EpistemicGrounding.Should().Contain("grounded in verified or live cognitive state from Batch 4.0 Brain");
    }

    [Fact]
    public async Task RESP32_SampledSnapshotId_RecordedInCheckpoint()
    {
        var cycle = await _responsibilityService.ExecuteSingleCycleAsync("tenant-resp");
        var cp = await _checkpointRepo.GetLatestCheckpointAsync("tenant-resp");

        cp!.StateSnapshotId.Should().Be(cycle.SampledCognitiveState!.SnapshotId);
    }

    // =========================================================================
    // Family 5: Work Formulation & OARA Capacity Conformance (RESP33 - RESP40)
    // =========================================================================

    [Fact]
    public async Task RESP33_RequiredCapacity_MatchesOARAAllocation()
    {
        var cycle = await _responsibilityService.ExecuteSingleCycleAsync("tenant-resp");
        var priorityWork = cycle.FormulatedWorkItems.FirstOrDefault(w => w.Domain == "CustomerRetention");

        priorityWork.Should().NotBeNull();
        priorityWork!.RequiredCapacityPercentage.Should().Be(35.0);
    }

    [Fact]
    public async Task RESP34_AssignedWorkerRole_PreservedFromMapping()
    {
        var mappings = new List<DurableResponsibilityMapping>
        {
            new() { AttentionArea = "CustomerRetention", AssignedWorkerRole = "RetentionSpecialistWorker" }
        };

        var state = await _brainService.GetCurrentCognitiveStateAsync("tenant-resp");
        var formulated = await _formulationEngine.FormulateWorkAsync("tenant-resp", state, mappings);

        var retentionItem = formulated.FirstOrDefault(w => w.Domain == "CustomerRetention");
        retentionItem!.TargetWorkerRole.Should().Be("RetentionSpecialistWorker");
    }

    [Fact]
    public async Task RESP35_DefaultWorkerRole_AssignedWhenNoMapping()
    {
        var state = await _brainService.GetCurrentCognitiveStateAsync("tenant-resp");
        var formulated = await _formulationEngine.FormulateWorkAsync("tenant-resp", state, Array.Empty<DurableResponsibilityMapping>());

        var item = formulated.FirstOrDefault(w => w.Domain == "CustomerRetention");
        item!.TargetWorkerRole.Should().Be("OperationsWorker");
    }

    [Fact]
    public async Task RESP36_HighUrgency_PromotesToTier3HardGovernance()
    {
        var state = new ExecutiveCognitiveState();
        state.AttentionPriorities.Add(new AttentionPriorityItem { Area = "EmergencyLeak", UrgencyScore = 0.85 });

        var formulated = await _formulationEngine.FormulateWorkAsync("tenant-resp", state, Array.Empty<DurableResponsibilityMapping>());
        formulated[0].ConsequenceTier.Should().Be("Tier3_HardGovernance");
        formulated[0].RequiresHumanApproval.Should().BeTrue();
    }

    [Fact]
    public async Task RESP37_ModerateUrgency_AssignsTier2SoftGovernance()
    {
        var state = new ExecutiveCognitiveState();
        state.AttentionPriorities.Add(new AttentionPriorityItem { Area = "RoutineMaintenance", UrgencyScore = 0.50 });

        var formulated = await _formulationEngine.FormulateWorkAsync("tenant-resp", state, Array.Empty<DurableResponsibilityMapping>());
        formulated[0].ConsequenceTier.Should().Be("Tier2_SoftGovernance");
        formulated[0].RequiresHumanApproval.Should().BeFalse();
    }

    [Fact]
    public async Task RESP38_MultiplePriorities_FormulateDistinctItems()
    {
        var state = new ExecutiveCognitiveState();
        state.AttentionPriorities.Add(new AttentionPriorityItem { Area = "P1", UrgencyScore = 0.5 });
        state.AttentionPriorities.Add(new AttentionPriorityItem { Area = "P2", UrgencyScore = 0.6 });

        var formulated = await _formulationEngine.FormulateWorkAsync("tenant-resp", state, Array.Empty<DurableResponsibilityMapping>());
        formulated.Should().HaveCount(2);
    }

    [Fact]
    public async Task RESP39_CumulativeCapacity_TrackedAcrossFormulatedItems()
    {
        var cycle = await _responsibilityService.ExecuteSingleCycleAsync("tenant-resp");
        var totalCapacity = cycle.FormulatedWorkItems.Sum(w => w.RequiredCapacityPercentage);
        totalCapacity.Should().BeGreaterThan(0.0);
    }

    [Fact]
    public void RESP40_ResourceDebtTracking_CodifiedInI37W()
    {
        DurableResponsibilityInvariants.I37_W_ResourceDebtTracking.Should().Contain("record cumulative resource consumption to prevent hidden organizational debt");
    }

    // =========================================================================
    // Family 6: PRG-1 Human Governance & Tier Gating (RESP41 - RESP48)
    // =========================================================================

    [Fact]
    public void RESP41_Tier3Work_ExplicitlyRequiresHumanApproval()
    {
        var item = new FormulatedWorkItem { ConsequenceTier = "Tier3_HardGovernance" };
        item.RequiresHumanApproval.Should().BeTrue();
    }

    [Fact]
    public void RESP42_Tier2Work_DoesNotBlockAutomatedDispatch()
    {
        var item = new FormulatedWorkItem { ConsequenceTier = "Tier2_SoftGovernance" };
        item.RequiresHumanApproval.Should().BeFalse();
    }

    [Fact]
    public async Task RESP43_Tier3Work_RemainsUndispatchedPendingApproval()
    {
        var cycle = await _responsibilityService.ExecuteSingleCycleAsync("tenant-resp");
        var tier3Items = cycle.FormulatedWorkItems.Where(w => w.RequiresHumanApproval).ToList();

        if (tier3Items.Count > 0)
        {
            tier3Items.Should().OnlyContain(w => !w.IsDispatched);
        }
    }

    [Fact]
    public void RESP44_PRG1HumanEscalation_RequiredForTier3()
    {
        DurableResponsibilityInvariants.I37_E_GovernanceGateEnforcement.Should().Contain("halt for PRG-1 human approval before dispatch");
    }

    [Fact]
    public async Task RESP45_CycleDoesNotSelfApprove_Tier3Work()
    {
        var cycle = await _responsibilityService.ExecuteSingleCycleAsync("tenant-resp");
        foreach (var item in cycle.FormulatedWorkItems.Where(w => w.RequiresHumanApproval))
        {
            item.IsDispatched.Should().BeFalse("Tier 3/4 work cannot be self-dispatched without human approval (I37-E)");
        }
    }

    [Fact]
    public async Task RESP46_DispatchedMissionIds_CreatedOnlyForPreClearedItems()
    {
        var cycle = await _responsibilityService.ExecuteSingleCycleAsync("tenant-resp");
        var autonomousCount = cycle.FormulatedWorkItems.Count(w => !w.RequiresHumanApproval);
        cycle.DispatchedMissionIds.Count.Should().Be(autonomousCount);
    }

    [Fact]
    public async Task RESP47_CheckpointDispatchedWorkCount_ReflectsOnlyDispatched()
    {
        var cycle = await _responsibilityService.ExecuteSingleCycleAsync("tenant-resp");
        var cp = await _checkpointRepo.GetLatestCheckpointAsync("tenant-resp");

        cp!.DispatchedWorkCount.Should().Be(cycle.DispatchedMissionIds.Count);
    }

    [Fact]
    public async Task RESP48_CheckpointFormulatedCount_ReflectsTotalFormulated()
    {
        var cycle = await _responsibilityService.ExecuteSingleCycleAsync("tenant-resp");
        var cp = await _checkpointRepo.GetLatestCheckpointAsync("tenant-resp");

        cp!.FormulatedWorkCount.Should().Be(cycle.FormulatedWorkItems.Count);
    }

    // =========================================================================
    // Family 7: Checkpoint Immutability & SHA-256 Hashing (RESP49 - RESP56)
    // =========================================================================

    [Fact]
    public void RESP49_Checkpoint_ComputesValidSha256Hash()
    {
        var cp = new ResponsibilityCycleCheckpoint
        {
            TenantId = "tenant-resp",
            CycleId = "c1",
            SequenceNumber = 1,
            State = ResponsibilityCycleState.CycleCompleted
        };
        cp.ComputeCheckpointHash();

        cp.CheckpointHash.Should().NotBeNullOrWhiteSpace();
        cp.CheckpointHash.Length.Should().Be(64, "SHA-256 hex string must be 64 characters (I37-J)");
    }

    [Fact]
    public void RESP50_IdenticalCheckpointInputs_ProduceIdenticalHash()
    {
        var now = DateTime.UtcNow;
        var cp1 = new ResponsibilityCycleCheckpoint { TenantId = "t", CycleId = "c", SequenceNumber = 1, CheckpointedUtc = now };
        cp1.ComputeCheckpointHash();

        var cp2 = new ResponsibilityCycleCheckpoint { TenantId = "t", CycleId = "c", SequenceNumber = 1, CheckpointedUtc = now };
        cp2.ComputeCheckpointHash();

        cp1.CheckpointHash.Should().Be(cp2.CheckpointHash);
    }

    [Fact]
    public void RESP51_ChangedCheckpointState_ProducesDifferentHash()
    {
        var now = DateTime.UtcNow;
        var cp1 = new ResponsibilityCycleCheckpoint { TenantId = "t", CycleId = "c", SequenceNumber = 1, State = ResponsibilityCycleState.Dispatched, CheckpointedUtc = now };
        cp1.ComputeCheckpointHash();

        var cp2 = new ResponsibilityCycleCheckpoint { TenantId = "t", CycleId = "c", SequenceNumber = 1, State = ResponsibilityCycleState.CycleCompleted, CheckpointedUtc = now };
        cp2.ComputeCheckpointHash();

        cp1.CheckpointHash.Should().NotBe(cp2.CheckpointHash);
    }

    [Fact]
    public void RESP52_Cycle_ComputesValidSha256Hash()
    {
        var cycle = new DurableResponsibilityCycle
        {
            TenantId = "tenant-resp",
            CycleId = "cyc-1",
            CycleSequenceNumber = 10
        };
        cycle.ComputeCycleHash();

        cycle.CycleHash.Should().NotBeNullOrWhiteSpace();
        cycle.CycleHash.Length.Should().Be(64);
    }

    [Fact]
    public async Task RESP53_Checkpoint_PersistedAndRetrievable()
    {
        await _responsibilityService.ExecuteSingleCycleAsync("tenant-resp");
        var checkpoints = await _responsibilityService.ListCheckpointsAsync("tenant-resp");
        checkpoints.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RESP54_Checkpoints_OrderedBySequenceDescending()
    {
        await _responsibilityService.ExecuteSingleCycleAsync("tenant-resp");
        await _responsibilityService.ExecuteSingleCycleAsync("tenant-resp");

        var checkpoints = await _responsibilityService.ListCheckpointsAsync("tenant-resp");
        checkpoints[0].SequenceNumber.Should().BeGreaterThan(checkpoints[1].SequenceNumber);
    }

    [Fact]
    public void RESP55_CrashRecoveryResilience_CodifiedInI37M()
    {
        DurableResponsibilityInvariants.I37_M_CrashRecoveryResilience.Should().Contain("recover state deterministically from the latest valid checkpoint");
    }

    [Fact]
    public async Task RESP56_AuditTrace_RecordsTriggerReason()
    {
        var cycle = await _responsibilityService.ExecuteSingleCycleAsync("tenant-resp", CycleTriggerReason.AttentionThresholdExceeded);
        cycle.TriggerReason.Should().Be(CycleTriggerReason.AttentionThresholdExceeded);
    }

    // =========================================================================
    // Family 8: Emergency Pause, Resume & Fail-Closed Control (RESP57 - RESP64)
    // =========================================================================

    [Fact]
    public async Task RESP57_HumanSupervisor_CanPauseCycles()
    {
        var paused = await _responsibilityService.PauseCyclesAsync("tenant-pause", "HumanAdmin", "Security Audit");
        paused.Should().BeTrue();
        _responsibilityService.IsPaused("tenant-pause").Should().BeTrue();
    }

    [Fact]
    public async Task RESP58_PausedTenant_ReturnsPausedByHumanCycleState()
    {
        await _responsibilityService.PauseCyclesAsync("tenant-pause2", "HumanAdmin", "Manual Hold");
        var cycle = await _responsibilityService.ExecuteSingleCycleAsync("tenant-pause2");

        cycle.State.Should().Be(ResponsibilityCycleState.PausedByHuman);
    }

    [Fact]
    public async Task RESP59_PausedTenant_DoesNotFormulateOrDispatchWork()
    {
        await _responsibilityService.PauseCyclesAsync("tenant-pause3", "HumanAdmin", "Hold");
        var cycle = await _responsibilityService.ExecuteSingleCycleAsync("tenant-pause3");

        cycle.FormulatedWorkItems.Should().BeEmpty();
        cycle.DispatchedMissionIds.Should().BeEmpty();
    }

    [Fact]
    public async Task RESP60_HumanSupervisor_CanResumeCycles()
    {
        await _responsibilityService.PauseCyclesAsync("tenant-resume", "HumanAdmin", "Hold");
        var resumed = await _responsibilityService.ResumeCyclesAsync("tenant-resume", "HumanAdmin");

        resumed.Should().BeTrue();
        _responsibilityService.IsPaused("tenant-resume").Should().BeFalse();
    }

    [Fact]
    public async Task RESP61_ResumedCycle_ProceedsNormallyToCompletion()
    {
        await _responsibilityService.PauseCyclesAsync("tenant-resume2", "HumanAdmin", "Hold");
        await _responsibilityService.ResumeCyclesAsync("tenant-resume2", "HumanAdmin");

        var cycle = await _responsibilityService.ExecuteSingleCycleAsync("tenant-resume2");
        cycle.State.Should().Be(ResponsibilityCycleState.CycleCompleted);
    }

    [Fact]
    public async Task RESP62_PauseState_IsStrictlyTenantSpecific()
    {
        await _responsibilityService.PauseCyclesAsync("tenant-A-pause", "Admin", "Hold");
        _responsibilityService.IsPaused("tenant-A-pause").Should().BeTrue();
        _responsibilityService.IsPaused("tenant-B-unpaused").Should().BeFalse();
    }

    [Fact]
    public async Task RESP63_SequenceNumber_IncrementsDeterministically()
    {
        var c1 = await _responsibilityService.ExecuteSingleCycleAsync("tenant-seq");
        var c2 = await _responsibilityService.ExecuteSingleCycleAsync("tenant-seq");

        c2.CycleSequenceNumber.Should().Be(c1.CycleSequenceNumber + 1);
    }

    [Fact]
    public async Task RESP64_PauseAndResumeEndpoints_WorkViaController()
    {
        var pauseResult = await _controller.Pause(new PauseCycleRequest { HumanSupervisorId = "CEO" }) as OkObjectResult;
        pauseResult.Should().NotBeNull();

        var resumeResult = await _controller.Resume(new ResumeCycleRequest { HumanSupervisorId = "CEO" }) as OkObjectResult;
        resumeResult.Should().NotBeNull();
    }

    // =========================================================================
    // Family 9: Non-Execution Principle & Firewall Sovereignty (RESP65 - RESP70)
    // =========================================================================

    [Fact]
    public void RESP65_ResponsibilityService_HasZeroDirectExecutionMethods()
    {
        var methods = typeof(IDurableResponsibilityService).GetMethods();
        var execMethods = methods.Where(m => m.Name.Contains("Mutate", StringComparison.OrdinalIgnoreCase) ||
                                            m.Name.Contains("Permit", StringComparison.OrdinalIgnoreCase) ||
                                            m.Name.Contains("Deploy", StringComparison.OrdinalIgnoreCase)).ToList();

        execMethods.Should().BeEmpty("Responsibility loop cannot possess direct execution or permit issuance methods (I37-F)");
    }

    [Fact]
    public void RESP66_Controller_DoesNotExposePermitOrExecutionEndpoints()
    {
        var methods = typeof(DurableResponsibilityController).GetMethods();
        var permitMethods = methods.Where(m => m.Name.Contains("Permit", StringComparison.OrdinalIgnoreCase) ||
                                              m.Name.Contains("ExecuteMutation", StringComparison.OrdinalIgnoreCase)).ToList();

        permitMethods.Should().BeEmpty("Responsibility controller must never expose execution or permit endpoints (I37-F)");
    }

    [Fact]
    public void RESP67_Batch6FirewallRespect_CodifiedInI37U()
    {
        DurableResponsibilityInvariants.I37_U_Batch6FirewallRespect.Should().Contain("All dispatched work destined for real-world connectors must possess a valid Batch 6 execution permit");
    }

    [Fact]
    public void RESP68_SimulatedSegregation_CodifiedInI37R()
    {
        DurableResponsibilityInvariants.I37_R_SimulatedSegregation.Should().Contain("Simulations evaluated during cycle reasoning cannot dispatch live production work items");
    }

    [Fact]
    public void RESP69_ZeroDirectSelfMutation_CodifiedInI37S()
    {
        DurableResponsibilityInvariants.I37_S_ZeroDirectSelfMutation.Should().Contain("cannot rewrite cycle policies or increase its own reasoning caps");
    }

    [Fact]
    public void RESP70_OperatingHeartbeatIntegrity_CodifiedInI37Z()
    {
        DurableResponsibilityInvariants.I37_Z_OperatingHeartbeatIntegrity.Should().Contain("serving human intent without replacing human authority");
    }

    // =========================================================================
    // Family 10: Multi-Tenant Isolation & Adversarial Integrity (RESP71 - RESP75)
    // =========================================================================

    [Fact]
    public void RESP71_MultiTenantIsolation_CodifiedInI37G()
    {
        DurableResponsibilityInvariants.I37_G_MultiTenantCycleIsolation.Should().Contain("Responsibility cycles, checkpoints, and mappings are strictly isolated by TenantId");
    }

    [Fact]
    public async Task RESP72_TenantA_CannotViewTenantB_Cycles()
    {
        await _responsibilityService.ExecuteSingleCycleAsync("tenant-alpha");
        await _responsibilityService.ExecuteSingleCycleAsync("tenant-beta");

        var alphaCycles = await _responsibilityService.ListCyclesAsync("tenant-alpha");
        alphaCycles.Should().OnlyContain(c => c.TenantId == "tenant-alpha");
        alphaCycles.Should().NotContain(c => c.TenantId == "tenant-beta");
    }

    [Fact]
    public async Task RESP73_Adversarial_CrossTenantCycleAccess_ThrowsUnauthorizedAccessException()
    {
        var betaCycle = await _responsibilityService.ExecuteSingleCycleAsync("tenant-beta-secret");

        var act = async () => await _checkpointRepo.GetCycleByIdAsync("tenant-alpha", betaCycle.CycleId);
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Cross-tenant responsibility cycle access violation*");
    }

    [Fact]
    public async Task RESP74_MissingTenantId_ThrowsArgumentException()
    {
        var act = async () => await _checkpointRepo.SaveCycleAsync(new DurableResponsibilityCycle { TenantId = "" });
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void RESP75_Adversarial_UnmeteredLoopAttempt_HaltedByBoundedBudget()
    {
        var budget = new BoundedReasoningBudget { MaxIterations = 1 };
        _budgetEnforcer.TryConsumeIteration(budget, 100, TimeSpan.FromSeconds(1), out _);
        var secondAttempt = _budgetEnforcer.TryConsumeIteration(budget, 100, TimeSpan.FromSeconds(1), out var reason);

        secondAttempt.Should().BeFalse();
        reason.Should().Contain("iteration cap exceeded");
    }
}
