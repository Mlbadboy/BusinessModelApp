using BusinessModelApp.Core.Domain.Runtime.Enterprise.Responsibility;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Responsibility;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Responsibility;

public sealed class ReasoningBudgetEnforcer : IReasoningBudgetEnforcer
{
    public bool TryConsumeIteration(
        BoundedReasoningBudget budget,
        int tokens,
        TimeSpan elapsed,
        out string rejectionReason)
    {
        if (budget == null) throw new ArgumentNullException(nameof(budget));

        budget.IterationsConsumed++;
        budget.TokensConsumed += tokens;

        if (budget.IterationsConsumed > budget.MaxIterations)
        {
            rejectionReason = $"Reasoning iteration cap exceeded: {budget.IterationsConsumed} > {budget.MaxIterations}. Bounded reasoning enforcement terminated cycle. (I37-C, I37-O)";
            return false;
        }

        if (budget.TokensConsumed > budget.TokenLimit)
        {
            rejectionReason = $"Reasoning token budget exceeded: {budget.TokensConsumed} > {budget.TokenLimit}. Bounded reasoning enforcement terminated cycle. (I37-C, I37-O)";
            return false;
        }

        if (elapsed > budget.Timeout)
        {
            rejectionReason = $"Reasoning elapsed time {elapsed.TotalSeconds:F2}s exceeded timeout {budget.Timeout.TotalSeconds:F2}s. (I37-C, I37-O)";
            return false;
        }

        rejectionReason = string.Empty;
        return true;
    }
}
