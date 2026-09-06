using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Domain.Runtime.Fleet;
using BusinessModelApp.Core.Interfaces.Missions;
using BusinessModelApp.Core.Interfaces.Runtime.Fleet;

namespace BusinessModelApp.Infrastructure.Runtime.Fleet
{
    public class AgentFleetPipelineCoordinator : IAgentFleetPipelineCoordinator
    {
        private readonly IFleetOrchestrator _fleetOrchestrator;
        private readonly IWorkerLeaseCoordinator _leaseCoordinator;
        private readonly IAgentOutcomeAdmissionGate _admissionGate;
        private readonly IMissionGraphAuditLedger _auditLedger;

        public AgentFleetPipelineCoordinator(
            IFleetOrchestrator fleetOrchestrator,
            IWorkerLeaseCoordinator leaseCoordinator,
            IAgentOutcomeAdmissionGate admissionGate,
            IMissionGraphAuditLedger auditLedger)
        {
            _fleetOrchestrator = fleetOrchestrator ?? throw new ArgumentNullException(nameof(fleetOrchestrator));
            _leaseCoordinator = leaseCoordinator ?? throw new ArgumentNullException(nameof(leaseCoordinator));
            _admissionGate = admissionGate ?? throw new ArgumentNullException(nameof(admissionGate));
            _auditLedger = auditLedger ?? throw new ArgumentNullException(nameof(auditLedger));
        }

        public async Task<IntegratedExecutionStepResult> ExecuteStepAsync(
            MissionGraph graph,
            TenantMissionPolicyContext tenantPolicy,
            Func<FencingEnvelope, Task<AgentOutcomeProposal>> agentExecutor,
            CancellationToken ct = default)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            if (tenantPolicy == null) throw new ArgumentNullException(nameof(tenantPolicy));
            if (agentExecutor == null) throw new ArgumentNullException(nameof(agentExecutor));

            // 1. Locate Next Ready Node in DAG
            var node = graph.Nodes.Values.FirstOrDefault(n => n.State == MissionNodeState.Ready);
            if (node == null)
            {
                return IntegratedExecutionStepResult.Failed("NoReadyNode", "No node in Ready state found in graph.");
            }

            // 2. Kill Switch Check
            if (tenantPolicy.IsEmergencyKillActive || graph.State == MissionGraphState.Killed)
            {
                node.State = MissionNodeState.Killed;
                graph.State = MissionGraphState.Killed;

                await _auditLedger.RecordEventAsync(new MissionGraphAuditEntry
                {
                    GraphId = graph.GraphId,
                    Version = graph.Version,
                    EventType = "MissionEmergencyKillTriggered",
                    Details = $"Execution aborted because emergency kill switch is active for node '{node.NodeId.Value}'."
                }, ct);

                return IntegratedExecutionStepResult.Failed(
                    "KillSwitchActive",
                    "Emergency kill switch is active.",
                    node.NodeId,
                    nodeState: MissionNodeState.Killed);
            }

            // 3. Fleet Selection & Dispatch
            var worker = await _fleetOrchestrator.DispatchNodeAsync(graph, node, ct);
            if (worker == null)
            {
                return IntegratedExecutionStepResult.Failed(
                    "NoHealthyWorkerAvailable",
                    "No healthy worker available in designated pool.",
                    node.NodeId,
                    nodeState: node.State);
            }

            // 4. Lease Acquisition & Monotonic Fence Token Issuance
            var agentInstanceId = AgentInstanceId.New();
            var leaseGrant = await _leaseCoordinator.AcquireNodeLeaseAsync(
                graph.WorkspaceId,
                graph.GraphId,
                graph.Version,
                node.NodeId,
                worker.WorkerId,
                agentInstanceId,
                node.ExecutionPolicy.Timeout > TimeSpan.Zero ? node.ExecutionPolicy.Timeout : TimeSpan.FromMinutes(5),
                ct);

            if (!leaseGrant.IsGranted || leaseGrant.Lease == null)
            {
                return IntegratedExecutionStepResult.Failed(
                    "LeaseDenied",
                    leaseGrant.FailureReason ?? "Failed to acquire node execution lease.",
                    node.NodeId,
                    worker.WorkerId,
                    nodeState: node.State);
            }

            var lease = leaseGrant.Lease;
            var fenceToken = leaseGrant.FenceToken;
            var attemptId = leaseGrant.AttemptId;

            // 5. Transition Node State to Running
            node.State = MissionNodeState.Running;

            var envelope = new FencingEnvelope
            {
                WorkspaceId = graph.WorkspaceId,
                MissionGraphId = graph.GraphId,
                GraphVersion = graph.Version,
                MissionNodeId = node.NodeId,
                AttemptId = attemptId,
                LeaseId = lease.LeaseId,
                FenceToken = fenceToken,
                WorkerId = worker.WorkerId,
                AgentInstanceId = agentInstanceId
            };

            // 6. Capability Check
            if (node.ExecutionPolicy.RequiredCapabilityId != null &&
                !tenantPolicy.RegisteredCapabilityIds.Contains(node.ExecutionPolicy.RequiredCapabilityId.ToString()))
            {
                node.State = MissionNodeState.Failed;
                await _leaseCoordinator.ReleaseLeaseAsync(lease.LeaseId, fenceToken, ct);

                await _auditLedger.RecordEventAsync(new MissionGraphAuditEntry
                {
                    GraphId = graph.GraphId,
                    Version = graph.Version,
                    EventType = "CapabilityRevokedExecutionHalted",
                    Details = $"Execution halted: required capability '{node.ExecutionPolicy.RequiredCapabilityId}' is unregistered or revoked."
                }, ct);

                return IntegratedExecutionStepResult.Failed(
                    "CapabilityRevoked",
                    $"Required capability '{node.ExecutionPolicy.RequiredCapabilityId}' is unregistered or revoked.",
                    node.NodeId,
                    worker.WorkerId,
                    lease.LeaseId,
                    fenceToken,
                    attemptId,
                    MissionNodeState.Failed);
            }

            // 7. Agent Execution Harness
            AgentOutcomeProposal proposal;
            try
            {
                proposal = await agentExecutor(envelope);
            }
            catch (Exception ex)
            {
                // Worker crashed / threw exception
                node.State = MissionNodeState.Failed;
                node.LastEffect = NodeExecutionEffect.UnknownEffect;
                await _leaseCoordinator.ReleaseLeaseAsync(lease.LeaseId, fenceToken, ct);

                await _auditLedger.RecordEventAsync(new MissionGraphAuditEntry
                {
                    GraphId = graph.GraphId,
                    Version = graph.Version,
                    EventType = "WorkerCrashUnknownEffect",
                    Details = $"Worker '{worker.WorkerId.Value}' crashed during execution. Effect reconciliation required: {ex.Message}"
                }, ct);

                return IntegratedExecutionStepResult.Failed(
                    "WorkerCrashed",
                    $"Worker crashed during execution: {ex.Message}",
                    node.NodeId,
                    worker.WorkerId,
                    lease.LeaseId,
                    fenceToken,
                    attemptId,
                    MissionNodeState.Failed,
                    NodeExecutionEffect.UnknownEffect);
            }

            // 8. Outcome Admission Gate
            var admission = await _admissionGate.AdmitOutcomeProposalAsync(proposal, graph, tenantPolicy, ct);
            if (!admission.IsAdmitted)
            {
                await _leaseCoordinator.ReleaseLeaseAsync(lease.LeaseId, fenceToken, ct);

                return IntegratedExecutionStepResult.Failed(
                    "OutcomeAdmissionRejected",
                    admission.FailureReason ?? "Outcome proposal rejected by admission gate.",
                    node.NodeId,
                    worker.WorkerId,
                    lease.LeaseId,
                    fenceToken,
                    attemptId,
                    node.State,
                    node.LastEffect);
            }

            // 9. Release Lease
            await _leaseCoordinator.ReleaseLeaseAsync(lease.LeaseId, fenceToken, ct);

            // 10. Audit & Checkpoint Recording
            await _auditLedger.RecordEventAsync(new MissionGraphAuditEntry
            {
                GraphId = graph.GraphId,
                Version = graph.Version,
                EventType = "NodeExecutionAdmitted",
                Details = $"Node '{node.NodeId.Value}' execution admitted. ResultingState={admission.ResultingNodeState}, Effect={admission.ResultingEffect}"
            }, ct);

            await _auditLedger.RecordEventAsync(new MissionGraphAuditEntry
            {
                GraphId = graph.GraphId,
                Version = graph.Version,
                EventType = "MissionGraphCheckpoint",
                Details = $"Checkpoint committed for graph '{graph.GraphId.Value}' version '{graph.Version.Value}' after node '{node.NodeId.Value}' completion."
            }, ct);

            if (node.State != MissionNodeState.Succeeded)
            {
                return IntegratedExecutionStepResult.Failed(
                    "NodeExecutionFailed",
                    node.FailureReason ?? "Node execution did not succeed.",
                    node.NodeId,
                    worker.WorkerId,
                    lease.LeaseId,
                    fenceToken,
                    attemptId,
                    node.State,
                    admission.ResultingEffect);
            }

            // 11. Advance DAG (Unlock Downstream Nodes)
            var unlockedNodes = new List<MissionNodeId>();
            foreach (var candidate in graph.Nodes.Values.Where(n => n.State == MissionNodeState.Pending))
            {
                var inboundDependencies = graph.Dependencies.Where(d => d.DependentNodeId == candidate.NodeId).ToList();
                if (inboundDependencies.Count > 0)
                {
                    bool allSatisfied = inboundDependencies.All(dep =>
                        graph.Nodes.TryGetValue(dep.RequiredNodeId.Value, out var req) &&
                        req.State == MissionNodeState.Succeeded);

                    if (allSatisfied)
                    {
                        candidate.State = MissionNodeState.Ready;
                        unlockedNodes.Add(candidate.NodeId);
                    }
                }
            }

            return IntegratedExecutionStepResult.Succeeded(
                node.NodeId,
                worker.WorkerId,
                lease.LeaseId,
                fenceToken,
                attemptId,
                node.State,
                admission.ResultingEffect,
                unlockedNodes,
                admission.AuditEntryId);
        }
    }
}
