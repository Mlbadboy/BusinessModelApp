using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Domain.Runtime.Fleet;
using BusinessModelApp.Core.Interfaces.Runtime.Fleet;

namespace BusinessModelApp.Infrastructure.Runtime.Fleet
{
    public class FleetOrchestrator : IFleetOrchestrator
    {
        private readonly IAgentFleetStore _fleetStore;
        private readonly ConcurrentDictionary<Guid, int> _tenantActiveWorkers = new();

        public FleetOrchestrator(IAgentFleetStore fleetStore)
        {
            _fleetStore = fleetStore ?? throw new ArgumentNullException(nameof(fleetStore));
        }

        public async Task<WorkerProcessRecord?> DispatchNodeAsync(
            MissionGraph graph,
            MissionNodeRecord node,
            CancellationToken ct = default)
        {
            if (graph == null || node == null)
                return null;

            if (node.State != MissionNodeState.Ready)
                return null;

            var poolType = MapNodeToPool(node.NodeType);
            var availableWorkers = await _fleetStore.GetAvailableWorkersAsync(poolType, ct);

            // Filter for Healthy workers
            var healthyWorker = availableWorkers.FirstOrDefault(w => w.HealthStatus == WorkerHealthStatus.Healthy);
            if (healthyWorker == null)
            {
                return null; // No healthy workers available in pool
            }

            // Track tenant active count
            _tenantActiveWorkers.AddOrUpdate(graph.WorkspaceId, 1, (_, count) => count + 1);

            return healthyWorker;
        }

        private static WorkerPoolType MapNodeToPool(MissionNodeType nodeType)
        {
            return nodeType switch
            {
                MissionNodeType.Observe => WorkerPoolType.CognitiveAnalyst,
                MissionNodeType.Perception => WorkerPoolType.CognitiveAnalyst,
                MissionNodeType.Investigate => WorkerPoolType.DomainResearcher,
                MissionNodeType.Research => WorkerPoolType.DomainResearcher,
                MissionNodeType.Analyze => WorkerPoolType.CognitiveAnalyst,
                MissionNodeType.Analysis => WorkerPoolType.CognitiveAnalyst,
                MissionNodeType.Reason => WorkerPoolType.CognitiveAnalyst,
                MissionNodeType.Hypothesis => WorkerPoolType.CognitiveAnalyst,
                MissionNodeType.Simulate => WorkerPoolType.StrategySimulator,
                MissionNodeType.Simulation => WorkerPoolType.StrategySimulator,
                MissionNodeType.Decide => WorkerPoolType.CognitiveAnalyst,
                MissionNodeType.Decision => WorkerPoolType.CognitiveAnalyst,
                MissionNodeType.Prepare => WorkerPoolType.CognitiveAnalyst,
                MissionNodeType.Approve => WorkerPoolType.CognitiveAnalyst,
                MissionNodeType.Approval => WorkerPoolType.CognitiveAnalyst,
                MissionNodeType.Execute => WorkerPoolType.GovernedExecutor,
                MissionNodeType.Execution => WorkerPoolType.GovernedExecutor,
                MissionNodeType.Verify => WorkerPoolType.VerificationAuditor,
                MissionNodeType.Verification => WorkerPoolType.VerificationAuditor,
                MissionNodeType.Compensate => WorkerPoolType.GovernedExecutor,
                MissionNodeType.Compensation => WorkerPoolType.GovernedExecutor,
                _ => WorkerPoolType.CognitiveAnalyst
            };
        }
    }
}
