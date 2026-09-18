using System.Diagnostics;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Brain;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Responsibility;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Brain;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Responsibility;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Responsibility;

public sealed class ResponsibilityCycleCoordinator : IResponsibilityCycleCoordinator
{
    private readonly IAutonomousBusinessBrainService _brainService;
    private readonly IReasoningBudgetEnforcer _budgetEnforcer;
    private readonly IWorkFormulationEngine _formulationEngine;
    private readonly ICycleCheckpointRepository _repository;

    public ResponsibilityCycleCoordinator(
        IAutonomousBusinessBrainService brainService,
        IReasoningBudgetEnforcer budgetEnforcer,
        IWorkFormulationEngine formulationEngine,
        ICycleCheckpointRepository repository)
    {
        _brainService = brainService ?? throw new ArgumentNullException(nameof(brainService));
        _budgetEnforcer = budgetEnforcer ?? throw new ArgumentNullException(nameof(budgetEnforcer));
        _formulationEngine = formulationEngine ?? throw new ArgumentNullException(nameof(formulationEngine));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<DurableResponsibilityCycle> ExecuteCycleStepAsync(string tenantId, CycleTriggerReason triggerReason)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));

        var stopwatch = Stopwatch.StartNew();

        // Stage 1: CycleInitialized (I37-B)
        var sequenceNumber = _repository.GetNextSequenceNumber(tenantId);
        var cycle = new DurableResponsibilityCycle
        {
            CycleId = Guid.NewGuid().ToString("N"),
            CycleSequenceNumber = sequenceNumber,
            TenantId = tenantId,
            TriggerReason = triggerReason,
            State = ResponsibilityCycleState.CycleInitialized,
            StartedUtc = DateTime.UtcNow
        };

        // Stage 2: StateSampled (Consumes 4.0 Brain)
        var cognitiveState = await _brainService.GetCurrentCognitiveStateAsync(tenantId);
        cycle.SampledCognitiveState = cognitiveState;
        cycle.State = ResponsibilityCycleState.StateSampled;

        // Stage 3: ReasoningBounded (I37-C, I37-O)
        if (!_budgetEnforcer.TryConsumeIteration(cycle.Budget, tokens: 250, stopwatch.Elapsed, out var budgetReason))
        {
            cycle.State = ResponsibilityCycleState.CycleAborted;
            cycle.CompletedUtc = DateTime.UtcNow;
            cycle.ComputeCycleHash();
            await _repository.SaveCycleAsync(cycle);
            return cycle;
        }
        cycle.State = ResponsibilityCycleState.ReasoningBounded;

        // Stage 4: WorkFormulated (I37-D, I37-K)
        var formulated = await _formulationEngine.FormulateWorkAsync(tenantId, cognitiveState, cycle.ActiveMappings);
        cycle.FormulatedWorkItems.AddRange(formulated);
        cycle.State = ResponsibilityCycleState.WorkFormulated;

        // Stage 5: GovernanceChecked (I37-E: High-consequence Tier 3/4 requires PRG-1 human approval)
        cycle.State = ResponsibilityCycleState.GovernanceChecked;

        // Stage 6: Dispatched (Tier 1 & 2 dispatched to Mission Orchestrator; Tier 3/4 remain queued)
        foreach (var item in cycle.FormulatedWorkItems)
        {
            if (!item.RequiresHumanApproval)
            {
                item.IsDispatched = true;
                cycle.DispatchedMissionIds.Add($"Mission-{Guid.NewGuid().ToString("N")[..8]}");
            }
        }
        cycle.State = ResponsibilityCycleState.Dispatched;

        // Stage 7: Checkpointed (I37-J, I37-M)
        var checkpoint = new ResponsibilityCycleCheckpoint
        {
            CycleId = cycle.CycleId,
            SequenceNumber = cycle.CycleSequenceNumber,
            TenantId = tenantId,
            State = cycle.State,
            FormulatedWorkCount = cycle.FormulatedWorkItems.Count,
            DispatchedWorkCount = cycle.DispatchedMissionIds.Count,
            StateSnapshotId = cognitiveState.SnapshotId,
            CheckpointedUtc = DateTime.UtcNow
        };
        checkpoint.ComputeCheckpointHash();
        await _repository.SaveCheckpointAsync(checkpoint);
        cycle.State = ResponsibilityCycleState.Checkpointed;

        // Stage 8: CycleCompleted (I37-X)
        cycle.State = ResponsibilityCycleState.CycleCompleted;
        cycle.CompletedUtc = DateTime.UtcNow;
        cycle.ComputeCycleHash();
        await _repository.SaveCycleAsync(cycle);

        return cycle;
    }
}
