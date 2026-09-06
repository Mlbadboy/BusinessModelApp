using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.ExternalReality;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Domain.Runtime.Fleet;
using BusinessModelApp.Core.Interfaces.Missions;
using BusinessModelApp.Core.Interfaces.Runtime.Fleet;
using BusinessModelApp.Core.Interfaces.Runtime.Reputation;

namespace BusinessModelApp.Infrastructure.Runtime.Fleet
{
    public class FleetOrchestrator : IFleetOrchestrator
    {
        private readonly IAgentFleetStore _fleetStore;
        private readonly IReputationAwareRouter? _router;
        private readonly ConcurrentDictionary<Guid, int> _tenantActiveWorkers = new();

        public FleetOrchestrator(IAgentFleetStore fleetStore, IReputationAwareRouter? router = null)
        {
            _fleetStore = fleetStore ?? throw new ArgumentNullException(nameof(fleetStore));
            _router = router;
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

            WorkerProcessRecord? selectedWorker = null;

            // 1. If ReputationAwareRouter is provided, let it rank and select the optimal worker empirically
            if (_router != null && availableWorkers.Count > 0)
            {
                var tenantPolicy = new TenantMissionPolicyContext { WorkspaceId = graph.WorkspaceId };
                var routingDecision = await _router.RouteNodeWorkerAsync(graph, node, availableWorkers, tenantPolicy, MarketRegimeState.Stable, ct);
                if (routingDecision.SelectedWorkerId.HasValue)
                {
                    selectedWorker = availableWorkers.FirstOrDefault(w => w.WorkerId == routingDecision.SelectedWorkerId.Value);
                }
            }

            // 2. Fallback to first healthy worker if router is absent or returned no selection
            selectedWorker ??= availableWorkers.FirstOrDefault(w => w.HealthStatus == WorkerHealthStatus.Healthy);
            if (selectedWorker == null)
            {
                return null; // No healthy workers available in pool
            }

            // Track tenant active count
            _tenantActiveWorkers.AddOrUpdate(graph.WorkspaceId, 1, (_, count) => count + 1);

            return selectedWorker;
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
