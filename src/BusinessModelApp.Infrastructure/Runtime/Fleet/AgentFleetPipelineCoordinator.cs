using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.ExternalReality;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Domain.Runtime.Fleet;
using BusinessModelApp.Core.Domain.Runtime.Reputation;
using BusinessModelApp.Core.Interfaces.Missions;
using BusinessModelApp.Core.Interfaces.Runtime.Fleet;
using BusinessModelApp.Core.Interfaces.Runtime.Reputation;

namespace BusinessModelApp.Infrastructure.Runtime.Fleet
{
    public class AgentFleetPipelineCoordinator : IAgentFleetPipelineCoordinator
    {
        private readonly IFleetOrchestrator _fleetOrchestrator;
        private readonly IWorkerLeaseCoordinator _leaseCoordinator;
        private readonly IAgentOutcomeAdmissionGate _admissionGate;
        private readonly IMissionGraphAuditLedger _auditLedger;
        private readonly IEmpiricalPerformanceEngine? _performanceEngine;
        private readonly ICausalAttributionEngine? _attributionEngine;
        private readonly ICalibrationEngine? _calibrationEngine;

        public AgentFleetPipelineCoordinator(
            IFleetOrchestrator fleetOrchestrator,
            IWorkerLeaseCoordinator leaseCoordinator,
            IAgentOutcomeAdmissionGate admissionGate,
            IMissionGraphAuditLedger auditLedger,
            IEmpiricalPerformanceEngine? performanceEngine = null,
            ICausalAttributionEngine? attributionEngine = null,
            ICalibrationEngine? calibrationEngine = null)
        {
            _fleetOrchestrator = fleetOrchestrator ?? throw new ArgumentNullException(nameof(fleetOrchestrator));
            _leaseCoordinator = leaseCoordinator ?? throw new ArgumentNullException(nameof(leaseCoordinator));
            _admissionGate = admissionGate ?? throw new ArgumentNullException(nameof(admissionGate));
            _auditLedger = auditLedger ?? throw new ArgumentNullException(nameof(auditLedger));
            _performanceEngine = performanceEngine;
            _attributionEngine = attributionEngine;
            _calibrationEngine = calibrationEngine;
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
                // NodeState (Blocked) != EffectState (UnknownEffect)
                // Never allow Failed to imply NoEffect!
                node.State = MissionNodeState.Blocked;
                node.LastEffect = NodeExecutionEffect.UnknownEffect;
                node.FailureReason = $"Worker crashed during execution. Effect reconciliation required before retry or progression: {ex.Message}";
                await _leaseCoordinator.ReleaseLeaseAsync(lease.LeaseId, fenceToken, ct);

                await _auditLedger.RecordEventAsync(new MissionGraphAuditEntry
                {
                    GraphId = graph.GraphId,
                    Version = graph.Version,
                    EventType = "WorkerCrashUnknownEffect",
                    Details = $"Worker '{worker.WorkerId.Value}' crashed during execution. Node '{node.NodeId.Value}' placed in Blocked state with UnknownEffect. Reconciliation required: {ex.Message}"
                }, ct);

                if (_performanceEngine != null)
                {
                    var crashToken = new ReputationEvidenceToken
                    {
                        WorkspaceId = graph.WorkspaceId,
                        AttemptId = attemptId,
                        GraphId = graph.GraphId,
                        NodeId = node.NodeId,
                        AgentDefinitionId = AgentDefinitionId.From(node.NodeType.ToString()),
                        WorkerId = worker.WorkerId,
                        CapabilityId = node.ExecutionPolicy.RequiredCapabilityId ?? new CapabilityId("default", "v1"),
                        IsSuccessfulExecution = false,
                        IsSecurityViolation = false,
                        IsUnknownEffectCrash = true
                    };
                    crashToken = crashToken with { TokenHash = ReputationEvidenceToken.ComputeTokenHash(crashToken) };
                    await _performanceEngine.ProcessEvidenceTokenAsync(crashToken, ct);
                }

                return IntegratedExecutionStepResult.Failed(
                    "WorkerCrashed",
                    node.FailureReason,
                    node.NodeId,
                    worker.WorkerId,
                    lease.LeaseId,
                    fenceToken,
                    attemptId,
                    MissionNodeState.Blocked,
                    NodeExecutionEffect.UnknownEffect);
            }

            // 8. Outcome Admission Gate
            var admission = await _admissionGate.AdmitOutcomeProposalAsync(proposal, graph, tenantPolicy, ct);
            if (!admission.IsAdmitted)
            {
                await _leaseCoordinator.ReleaseLeaseAsync(lease.LeaseId, fenceToken, ct);

                if (_performanceEngine != null)
                {
                    bool isSecurityFault = admission.FailureReason?.Contains("Fencing") == true ||
                                          admission.FailureReason?.Contains("tampered") == true ||
                                          admission.FailureReason?.Contains("Tenant") == true;
                    var rejToken = new ReputationEvidenceToken
                    {
                        WorkspaceId = graph.WorkspaceId,
                        AttemptId = attemptId,
                        GraphId = graph.GraphId,
                        NodeId = node.NodeId,
                        AgentDefinitionId = AgentDefinitionId.From(node.NodeType.ToString()),
                        WorkerId = worker.WorkerId,
                        CapabilityId = node.ExecutionPolicy.RequiredCapabilityId ?? new CapabilityId("default", "v1"),
                        IsSuccessfulExecution = false,
                        IsSecurityViolation = isSecurityFault,
                        IsUnknownEffectCrash = false
                    };
                    rejToken = rejToken with { TokenHash = ReputationEvidenceToken.ComputeTokenHash(rejToken) };
                    await _performanceEngine.ProcessEvidenceTokenAsync(rejToken, ct);
                }

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

            // 10b. Emit Empirical Reputation Evidence Token (Batch 3.4)
            if (_performanceEngine != null)
            {
                var attribution = _attributionEngine != null
                    ? await _attributionEngine.EvaluateAttributionAsync(attemptId, node, proposal, node.VerificationResult ?? new NodeVerificationResult { IsVerified = true }, ct)
                    : new CausalAttributionRecord { AttemptId = attemptId, Level = AttributionLevel.A4_Deterministic, AttributionConfidence = 1.0 };

                var calibration = _calibrationEngine != null
                    ? await _calibrationEngine.CalculateCalibrationAsync(attemptId, node, proposal, node.VerificationResult ?? new NodeVerificationResult { IsVerified = true }, StructuredDomainContext.Default, MarketRegimeState.Stable, ct)
                    : new OutcomeCalibrationRecord { AttemptId = attemptId, NodeId = node.NodeId, GraphId = graph.GraphId, DiscrepancyScore = 0.0 };

                var successToken = new ReputationEvidenceToken
                {
                    WorkspaceId = graph.WorkspaceId,
                    AttemptId = attemptId,
                    GraphId = graph.GraphId,
                    NodeId = node.NodeId,
                    AgentDefinitionId = AgentDefinitionId.From(node.NodeType.ToString()),
                    WorkerId = worker.WorkerId,
                    CapabilityId = node.ExecutionPolicy.RequiredCapabilityId ?? new CapabilityId("default", "v1"),
                    Attribution = attribution,
                    Calibration = calibration,
                    VerifiedEvidenceHash = admission.VerifiedEvidenceHash ?? string.Empty,
                    AuditEntryId = admission.AuditEntryId,
                    IsSuccessfulExecution = admission.IsAdmitted && node.State == MissionNodeState.Succeeded,
                    IsSecurityViolation = false,
                    IsUnknownEffectCrash = false,
                    TokensConsumed = proposal.TokensConsumed,
                    CostUsdConsumed = proposal.CostUsdConsumed,
                    Duration = TimeSpan.FromMilliseconds(200)
                };
                successToken = successToken with { TokenHash = ReputationEvidenceToken.ComputeTokenHash(successToken) };
                await _performanceEngine.ProcessEvidenceTokenAsync(successToken, ct);
            }

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

        public async Task<ReconciliationOutcomeResult> ReconcileNodeEffectAsync(
            MissionGraph graph,
            MissionNodeId nodeId,
            NodeExecutionEffect terminalEffect,
            TenantMissionPolicyContext tenantPolicy,
            CancellationToken ct = default)
        {
            // 1. Tenant boundary enforcement
            if (graph.WorkspaceId != tenantPolicy.WorkspaceId)
            {
                return ReconciliationOutcomeResult.Rejected(
                    $"Tenant mismatch: graph tenant '{graph.WorkspaceId}' does not match context tenant '{tenantPolicy.WorkspaceId}'.",
                    nodeId);
            }

            // 2. Node lookup
            if (!graph.Nodes.TryGetValue(nodeId.Value, out var node))
            {
                return ReconciliationOutcomeResult.Rejected(
                    $"Node '{nodeId.Value}' not found in mission graph '{graph.GraphId.Value}'.",
                    nodeId);
            }

            // 3. Reject reconciliation to UnknownEffect (must resolve deterministically)
            if (terminalEffect == NodeExecutionEffect.UnknownEffect)
            {
                return ReconciliationOutcomeResult.Rejected(
                    "Deterministic reconciliation requires terminal effect (NoEffect, EffectSucceeded, or EffectFailed). UnknownEffect is invalid as target.",
                    nodeId);
            }

            // 4. Idempotency & Stale Check
            if (node.LastEffect == terminalEffect)
            {
                return ReconciliationOutcomeResult.IdempotentNoop(nodeId, node.State, node.LastEffect);
            }

            if (node.LastEffect != NodeExecutionEffect.UnknownEffect)
            {
                return ReconciliationOutcomeResult.Rejected(
                    $"Node '{nodeId.Value}' is not awaiting reconciliation (current effect is {node.LastEffect}, state is {node.State}). Stale reconciliation attempt rejected.",
                    nodeId);
            }

            // 5. Apply deterministic reconciliation based on verified terminal effect
            switch (terminalEffect)
            {
                case NodeExecutionEffect.NoEffect:
                    // External side-effect did not execute -> safe to return to Ready for retry
                    node.State = MissionNodeState.Ready;
                    node.LastEffect = NodeExecutionEffect.NoEffect;
                    node.FailureReason = null;

                    await _auditLedger.RecordEventAsync(new MissionGraphAuditEntry
                    {
                        GraphId = graph.GraphId,
                        Version = graph.Version,
                        EventType = "NodeEffectReconciled",
                        Details = $"Node '{nodeId.Value}' reconciled as NoEffect. Blind retry hazard cleared; node reset to Ready state."
                    }, ct);

                    return ReconciliationOutcomeResult.NoEffectRetryPermitted(nodeId);

                case NodeExecutionEffect.EffectSucceeded:
                    // External side-effect executed successfully -> prevent duplicate retry, complete node, advance DAG
                    node.State = MissionNodeState.Succeeded;
                    node.LastEffect = NodeExecutionEffect.EffectSucceeded;
                    node.FailureReason = null;

                    bool unlockedDownstream = false;
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
                                unlockedDownstream = true;
                            }
                        }
                    }

                    await _auditLedger.RecordEventAsync(new MissionGraphAuditEntry
                    {
                        GraphId = graph.GraphId,
                        Version = graph.Version,
                        EventType = "NodeEffectReconciled",
                        Details = $"Node '{nodeId.Value}' reconciled as EffectSucceeded. Duplicate retry blocked; node transitioned to Succeeded (unlockedDownstream={unlockedDownstream})."
                    }, ct);

                    return ReconciliationOutcomeResult.SucceededCompleted(nodeId, unlockedDownstream);

                case NodeExecutionEffect.EffectFailed:
                    // External side-effect executed and failed -> governed retry if budget remains
                    bool retryPermitted = node.CurrentAttempt < node.ExecutionPolicy.MaxRetries;
                    if (retryPermitted)
                    {
                        node.State = MissionNodeState.Ready;
                        node.LastEffect = NodeExecutionEffect.EffectFailed;
                        node.FailureReason = "Reconciled as EffectFailed; governed retry permitted.";
                    }
                    else
                    {
                        node.State = MissionNodeState.Failed;
                        node.LastEffect = NodeExecutionEffect.EffectFailed;
                        node.FailureReason = "Reconciled as EffectFailed; retry budget exhausted.";
                    }

                    await _auditLedger.RecordEventAsync(new MissionGraphAuditEntry
                    {
                        GraphId = graph.GraphId,
                        Version = graph.Version,
                        EventType = "NodeEffectReconciled",
                        Details = $"Node '{nodeId.Value}' reconciled as EffectFailed. ResultingState={node.State}, RetryPermitted={retryPermitted}."
                    }, ct);

                    return ReconciliationOutcomeResult.FailedTerminal(nodeId, retryPermitted);

                default:
                    return ReconciliationOutcomeResult.Rejected($"Unsupported reconciliation effect: {terminalEffect}", nodeId);
            }
        }
    }
}
