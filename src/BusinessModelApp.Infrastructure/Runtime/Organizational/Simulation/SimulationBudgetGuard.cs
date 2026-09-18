using BusinessModelApp.Core.Domain.Runtime.Organizational.Simulation;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational.Simulation;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Simulation;

public sealed class SimulationBudgetGuard : ISimulationBudgetGuard
{
    public void ValidateBudget(SimulationBudget budget)
    {
        if (budget == null) throw new ArgumentNullException(nameof(budget));

        if (budget.MaxAgents <= 0 || budget.MaxAgents > 500_000)
            throw new ArgumentOutOfRangeException(nameof(budget.MaxAgents), "MaxAgents must be between 1 and 500,000.");

        if (budget.MaxSteps <= 0 || budget.MaxSteps > 10_000)
            throw new ArgumentOutOfRangeException(nameof(budget.MaxSteps), "MaxSteps must be between 1 and 10,000.");

        if (budget.MaxComputeSeconds <= 0.0 || budget.MaxComputeSeconds > 3600.0)
            throw new ArgumentOutOfRangeException(nameof(budget.MaxComputeSeconds), "MaxComputeSeconds must be between 0.1 and 3600.");

        if (budget.MaxTokens <= 0)
            throw new ArgumentOutOfRangeException(nameof(budget.MaxTokens), "MaxTokens must be positive.");
    }

    public bool CheckRunWithinBudget(
        SimulationRunMetadata run,
        SimulationBudget budget,
        int currentAgents,
        int currentSteps,
        double elapsedSeconds)
    {
        if (budget == null) return false;

        if (currentAgents > budget.MaxAgents)
        {
            run.State = SimulationLifecycleState.ResourceExhausted;
            run.FailureReason = SimulationFailureReason.BudgetBreach;
            run.FailureMessage = $"Agent count ({currentAgents}) exceeded hard budget limit ({budget.MaxAgents}).";
            return false;
        }

        if (currentSteps > budget.MaxSteps)
        {
            run.State = SimulationLifecycleState.ResourceExhausted;
            run.FailureReason = SimulationFailureReason.BudgetBreach;
            run.FailureMessage = $"Step count ({currentSteps}) exceeded hard budget limit ({budget.MaxSteps}).";
            return false;
        }

        if (elapsedSeconds > budget.MaxComputeSeconds)
        {
            run.State = SimulationLifecycleState.Timeout;
            run.FailureReason = SimulationFailureReason.Timeout;
            run.FailureMessage = $"Compute time ({elapsedSeconds:F1}s) exceeded limit ({budget.MaxComputeSeconds:F1}s).";
            return false;
        }

        return true;
    }
}
