using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Interfaces.Missions;

namespace BusinessModelApp.Infrastructure.Runtime.Missions
{
    public class NodeAdmissionGate : INodeAdmissionGate
    {
        public Task<bool> CanAdmitNodeAsync(
            MissionGraph graph,
            MissionNodeRecord node,
            TenantMissionPolicyContext tenantPolicy,
            CancellationToken ct = default)
        {
            if (graph == null || node == null || tenantPolicy == null)
                return Task.FromResult(false);

            // 1. Graph state check
            if (graph.State != MissionGraphState.Active)
                return Task.FromResult(false);

            // 2. Tenant isolation check
            if (graph.WorkspaceId != tenantPolicy.WorkspaceId)
                return Task.FromResult(false);

            // 3. Execution-time capability revalidation (Dual validation)
            if (node.ExecutionPolicy.RequiredCapabilityId.HasValue)
            {
                var capId = node.ExecutionPolicy.RequiredCapabilityId.Value.ToString();
                if (!tenantPolicy.RegisteredCapabilityIds.Contains(capId))
                {
                    // Capability was deregistered or invalid at execution time!
                    return Task.FromResult(false);
                }
            }

            // 4. Execution-time budget check
            if (!graph.Budget.CanReserve(node.ExecutionPolicy.MaxBudgetTokens, node.ExecutionPolicy.MaxCostUsd))
            {
                return Task.FromResult(false);
            }

            // 5. Autonomy tier ceiling revalidation
            if (node.ExecutionPolicy.RequiredAutonomyTier > tenantPolicy.MaxAllowedAutonomyTier)
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
    }
}
