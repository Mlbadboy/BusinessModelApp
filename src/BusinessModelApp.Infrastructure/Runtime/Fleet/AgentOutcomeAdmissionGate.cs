using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Domain.Runtime.Fleet;
using BusinessModelApp.Core.Interfaces.Missions;
using BusinessModelApp.Core.Interfaces.Runtime.Fleet;

namespace BusinessModelApp.Infrastructure.Runtime.Fleet
{
    public class AgentOutcomeAdmissionGate : IAgentOutcomeAdmissionGate
    {
        private readonly IWorkerLeaseCoordinator _leaseCoordinator;
        private readonly INodeVerificationEngine _verificationEngine;
        private readonly IMissionGraphAuditLedger _auditLedger;

        public AgentOutcomeAdmissionGate(
            IWorkerLeaseCoordinator leaseCoordinator,
            INodeVerificationEngine verificationEngine,
            IMissionGraphAuditLedger auditLedger)
        {
            _leaseCoordinator = leaseCoordinator ?? throw new ArgumentNullException(nameof(leaseCoordinator));
            _verificationEngine = verificationEngine ?? throw new ArgumentNullException(nameof(verificationEngine));
            _auditLedger = auditLedger ?? throw new ArgumentNullException(nameof(auditLedger));
        }

        public async Task<OutcomeAdmissionResult> AdmitOutcomeProposalAsync(
            AgentOutcomeProposal proposal,
            MissionGraph graph,
            TenantMissionPolicyContext tenantPolicy,
            CancellationToken ct = default)
        {
            if (proposal == null)
            {
                return OutcomeAdmissionResult.Rejected("Proposal cannot be null.");
            }

            if (graph == null)
            {
                return OutcomeAdmissionResult.Rejected("Graph cannot be null.");
            }

            var envelope = proposal.Envelope;
            if (envelope == null)
            {
                return OutcomeAdmissionResult.Rejected("Fencing envelope cannot be null.");
            }

            // 0. Tenant Isolation check
            if (tenantPolicy != null && tenantPolicy.WorkspaceId != envelope.WorkspaceId)
            {
                await RecordAuditAsync(graph.GraphId, graph.Version, "OutcomeTenantMismatch",
                    $"Tenant isolation violation: policy workspace '{tenantPolicy.WorkspaceId}' does not match envelope workspace '{envelope.WorkspaceId}'.", ct);
                return OutcomeAdmissionResult.Rejected("Tenant isolation violation: policy workspace does not match envelope workspace.");
            }

            // 1. Validate Fencing Envelope (Multi-dimensional check)
            var fencingResult = await _leaseCoordinator.ValidateEnvelopeAsync(envelope, graph, ct);
            if (!fencingResult.IsValid)
            {
                await RecordAuditAsync(graph.GraphId, graph.Version, "OutcomeFencingRejected",
                    $"Fencing envelope validation failed: {fencingResult.FailureReason}", ct);
                return OutcomeAdmissionResult.Rejected($"Fencing rejected: {fencingResult.FailureReason}");
            }

            // Check if node exists in graph
            if (!graph.Nodes.TryGetValue(envelope.MissionNodeId.Value, out var node))
            {
                return OutcomeAdmissionResult.Rejected($"Node '{envelope.MissionNodeId.Value}' does not exist in graph.");
            }

            // 2. UnknownEffect Handling (Worker crash / timeout / partition)
            if (proposal.ReportedStatus == ExecutionOutcomeStatus.Unknown ||
                proposal.ReportedStatus == ExecutionOutcomeStatus.TimedOut)
            {
                node.State = MissionNodeState.Blocked;
                node.LastEffect = NodeExecutionEffect.UnknownEffect;
                node.FailureReason = proposal.FailureReason ?? "Operation yielded UnknownEffect; blind retry prohibited.";

                await _leaseCoordinator.ReleaseLeaseAsync(envelope.LeaseId, envelope.FenceToken, ct);

                await RecordAuditAsync(graph.GraphId, graph.Version, "OutcomeUnknownEffect",
                    $"Node '{node.NodeId.Value}' transitioned to Blocked with UnknownEffect. Reason: {node.FailureReason}", ct);

                return OutcomeAdmissionResult.Admitted(MissionNodeState.Blocked, NodeExecutionEffect.UnknownEffect);
            }

            // 3. Anti-Privilege Escalation Wall (Invariant I13 & I13-A)
            // Cognitive nodes (Analyze, Investigate, Research, Reason) CANNOT report execution side-effects
            if (node.NodeType != MissionNodeType.Execute && node.NodeType != MissionNodeType.Execution)
            {
                if (proposal.OutputPayloadJson.Contains("\"consequential_effect\":true", StringComparison.OrdinalIgnoreCase) ||
                    proposal.OutputPayloadJson.Contains("\"execution_permit\"", StringComparison.OrdinalIgnoreCase))
                {
                    node.State = MissionNodeState.Failed;
                    node.LastEffect = NodeExecutionEffect.EffectFailed;
                    node.FailureReason = "Privilege escalation detected: cognitive node attempted to report execution side-effect.";

                    await _leaseCoordinator.ReleaseLeaseAsync(envelope.LeaseId, envelope.FenceToken, ct);

                    await RecordAuditAsync(graph.GraphId, graph.Version, "PrivilegeEscalationBlocked",
                        $"Node '{node.NodeId.Value}' attempted unauthorized consequential execution.", ct);

                    return OutcomeAdmissionResult.Rejected("Privilege escalation blocked: cognitive nodes cannot report consequential side-effects.");
                }
            }

            // 4. Failure Reporting
            if (proposal.ReportedStatus == ExecutionOutcomeStatus.Failed)
            {
                node.State = MissionNodeState.Failed;
                node.LastEffect = NodeExecutionEffect.EffectFailed;
                node.FailureReason = proposal.FailureReason ?? "Agent reported execution failure.";

                await _leaseCoordinator.ReleaseLeaseAsync(envelope.LeaseId, envelope.FenceToken, ct);

                await RecordAuditAsync(graph.GraphId, graph.Version, "NodeExecutionFailed",
                    $"Node '{node.NodeId.Value}' failed: {node.FailureReason}", ct);

                return OutcomeAdmissionResult.Admitted(MissionNodeState.Failed, NodeExecutionEffect.EffectFailed);
            }

            // 5. Hierarchical Budget Clamping
            if (proposal.TokensConsumed > node.ExecutionPolicy.MaxBudgetTokens)
            {
                node.State = MissionNodeState.Failed;
                node.FailureReason = $"Token budget exceeded: consumed {proposal.TokensConsumed}, max {node.ExecutionPolicy.MaxBudgetTokens}.";

                await _leaseCoordinator.ReleaseLeaseAsync(envelope.LeaseId, envelope.FenceToken, ct);
                return OutcomeAdmissionResult.Rejected(node.FailureReason);
            }

            if (proposal.CostUsdConsumed > node.ExecutionPolicy.MaxCostUsd)
            {
                node.State = MissionNodeState.Failed;
                node.FailureReason = $"Cost budget exceeded: consumed ${proposal.CostUsdConsumed}, max ${node.ExecutionPolicy.MaxCostUsd}.";

                await _leaseCoordinator.ReleaseLeaseAsync(envelope.LeaseId, envelope.FenceToken, ct);
                return OutcomeAdmissionResult.Rejected(node.FailureReason);
            }

            // 6. Anti-Manufactured Evidence Verification (Verify-Before-Claim)
            var verification = await _verificationEngine.VerifyNodeOutcomeAsync(
                node,
                proposal.OutputPayloadJson,
                proposal.ProducedArtifacts,
                ct);

            if (!verification.IsVerified)
            {
                node.State = MissionNodeState.Failed;
                node.FailureReason = $"Verification failed: {verification.FailureReason}";

                await _leaseCoordinator.ReleaseLeaseAsync(envelope.LeaseId, envelope.FenceToken, ct);

                await RecordAuditAsync(graph.GraphId, graph.Version, "OutcomeVerificationFailed",
                    $"Node '{node.NodeId.Value}' failed evidence verification: {verification.FailureReason}", ct);

                return OutcomeAdmissionResult.Rejected($"Evidence verification failed: {verification.FailureReason}");
            }

            // 7. Success State Transition
            node.State = MissionNodeState.Succeeded;
            node.LastEffect = NodeExecutionEffect.EffectSucceeded;
            node.VerificationResult = verification;
            node.CompletedAt = DateTimeOffset.UtcNow;

            // Consume budget
            graph.Budget.Consume(proposal.TokensConsumed, proposal.CostUsdConsumed);

            // Release lease
            await _leaseCoordinator.ReleaseLeaseAsync(envelope.LeaseId, envelope.FenceToken, ct);

            await RecordAuditAsync(graph.GraphId, graph.Version, "NodeSucceeded",
                $"Node '{node.NodeId.Value}' succeeded and verified. EvidenceHash: {verification.EvidenceHash}", ct);

            return OutcomeAdmissionResult.Admitted(
                MissionNodeState.Succeeded,
                NodeExecutionEffect.EffectSucceeded,
                verification.EvidenceHash);
        }

        private async Task RecordAuditAsync(
            MissionGraphId graphId,
            MissionGraphVersion version,
            string eventType,
            string details,
            CancellationToken ct)
        {
            await _auditLedger.RecordEventAsync(new MissionGraphAuditEntry
            {
                GraphId = graphId,
                Version = version,
                EventType = eventType,
                Details = details,
                Sha256Hash = string.Empty
            }, ct);
        }
    }
}
