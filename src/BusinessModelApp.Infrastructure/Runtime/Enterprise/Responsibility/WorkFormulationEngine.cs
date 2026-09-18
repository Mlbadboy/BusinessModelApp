using BusinessModelApp.Core.Domain.Runtime.Enterprise.Brain;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Responsibility;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Responsibility;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Responsibility;

public sealed class WorkFormulationEngine : IWorkFormulationEngine
{
    public Task<IReadOnlyList<FormulatedWorkItem>> FormulateWorkAsync(
        string tenantId,
        ExecutiveCognitiveState cognitiveState,
        IReadOnlyList<DurableResponsibilityMapping> mappings)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (cognitiveState == null) throw new ArgumentNullException(nameof(cognitiveState));

        var items = new List<FormulatedWorkItem>();

        // 1. Process Attention Priorities from Batch 4.0 Brain (OARA-grounded, I37-D, I37-K)
        foreach (var priority in cognitiveState.AttentionPriorities)
        {
            var mapping = mappings?.FirstOrDefault(m => string.Equals(m.AttentionArea, priority.Area, StringComparison.OrdinalIgnoreCase));
            var workerRole = mapping?.AssignedWorkerRole ?? "OperationsWorker";

            // If urgency is very high (>= 0.75), classify as Tier3 requiring PRG-1 Human Governance (I37-E)
            var tier = priority.UrgencyScore >= 0.75 ? "Tier3_HardGovernance" : "Tier2_SoftGovernance";

            items.Add(new FormulatedWorkItem
            {
                WorkItemId = Guid.NewGuid().ToString("N"),
                Title = $"Resolve attention priority: {priority.Area}",
                Domain = priority.Area,
                ConsequenceTier = tier,
                TargetWorkerRole = workerRole,
                RequiredCapacityPercentage = priority.AllocatedCapacityPercentage > 0 ? priority.AllocatedCapacityPercentage : 15.0,
                IsDispatched = false,
                FormulatedUtc = DateTime.UtcNow
            });
        }

        // 2. Process Material Deltas if any
        foreach (var delta in cognitiveState.RecentDeltas.Where(d => d.IsMaterial))
        {
            items.Add(new FormulatedWorkItem
            {
                WorkItemId = Guid.NewGuid().ToString("N"),
                Title = $"Investigate material delta in {delta.Domain}: {delta.MetricOrEntity}",
                Domain = delta.Domain,
                ConsequenceTier = "Tier2_SoftGovernance",
                TargetWorkerRole = "AnalyticsWorker",
                RequiredCapacityPercentage = 10.0,
                IsDispatched = false,
                FormulatedUtc = DateTime.UtcNow
            });
        }

        return Task.FromResult<IReadOnlyList<FormulatedWorkItem>>(items);
    }
}
