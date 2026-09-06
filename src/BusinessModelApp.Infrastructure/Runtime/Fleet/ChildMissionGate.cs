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
    public class ChildMissionGate : IChildMissionGate
    {
        private readonly IDagCompiler _dagCompiler;
        private readonly IMissionGraphStore _graphStore;
        private readonly IMissionGraphAuditLedger _auditLedger;

        public ChildMissionGate(
            IDagCompiler dagCompiler,
            IMissionGraphStore graphStore,
            IMissionGraphAuditLedger auditLedger)
        {
            _dagCompiler = dagCompiler ?? throw new ArgumentNullException(nameof(dagCompiler));
            _graphStore = graphStore ?? throw new ArgumentNullException(nameof(graphStore));
            _auditLedger = auditLedger ?? throw new ArgumentNullException(nameof(auditLedger));
        }

        public async Task<ChildMissionSpawnResult> SpawnChildMissionAsync(
            ChildMissionSpawnRequest request,
            MissionGraph parentGraph,
            TenantMissionPolicyContext tenantPolicy,
            CancellationToken ct = default)
        {
            if (request == null)
            {
                return ChildMissionSpawnResult.Failed("Request cannot be null.");
            }
            if (parentGraph == null)
            {
                return ChildMissionSpawnResult.Failed("Parent graph cannot be null.");
            }

            // 1. Strict Lineage Check
            if (request.ParentGraphId != parentGraph.GraphId)
            {
                return ChildMissionSpawnResult.Failed(
                    $"Lineage mismatch: ParentGraphId '{request.ParentGraphId}' does not match active graph '{parentGraph.GraphId}'.");
            }

            if (!parentGraph.Nodes.ContainsKey(request.ParentNodeId.Value))
            {
                return ChildMissionSpawnResult.Failed(
                    $"Lineage mismatch: ParentNodeId '{request.ParentNodeId.Value}' does not exist in parent graph.");
            }

            // 2. Budget Reservation Check
            if (!parentGraph.Budget.CanReserve(request.ReservedBudgetTokens, request.ReservedBudgetCostUsd))
            {
                return ChildMissionSpawnResult.Failed(
                    $"Cannot spawn child mission: requested budget ({request.ReservedBudgetTokens} tokens, ${request.ReservedBudgetCostUsd}) exceeds parent remaining budget ({parentGraph.Budget.RemainingTokens} tokens, ${parentGraph.Budget.RemainingCostUsd}).");
            }

            // Deduct / reserve budget from parent
            parentGraph.Budget.TryReserve(request.ReservedBudgetTokens, request.ReservedBudgetCostUsd);

            // 3. Compile Child Graph via DAG Compiler
            MissionGraph childGraph;
            try
            {
                childGraph = await _dagCompiler.CompileAsync(request.ChildProposal, tenantPolicy, ct);
            }
            catch (Exception ex)
            {
                // Release reservation on compilation failure
                parentGraph.Budget.ReleaseReservation(request.ReservedBudgetTokens, request.ReservedBudgetCostUsd);
                return ChildMissionSpawnResult.Failed($"Child graph compilation failed: {ex.Message}");
            }

            // 4. Persist Child Mission Record with Lineage
            var childMissionRecord = new MissionRecord
            {
                Id = request.ChildProposal.MissionId,
                WorkspaceId = tenantPolicy.WorkspaceId,
                Title = request.ChildProposal.Title,
                Description = request.ChildProposal.Description,
                Status = MissionStatus.Active,
                OriginatingSignalId = $"ParentAttempt:{request.ParentAttemptId}"
            };

            await _graphStore.SaveMissionAsync(childMissionRecord, ct);
            await _graphStore.SaveGraphAsync(childGraph, ct);

            await _auditLedger.RecordEventAsync(new MissionGraphAuditEntry
            {
                GraphId = parentGraph.GraphId,
                Version = parentGraph.Version,
                EventType = "ChildMissionSpawned",
                Details = $"Spawned child mission {childMissionRecord.Id} (Graph: {childGraph.GraphId}) from node {request.ParentNodeId.Value} under attempt {request.ParentAttemptId}.",
                Sha256Hash = childGraph.VersionHash
            }, ct);

            return ChildMissionSpawnResult.Success(childMissionRecord.Id, childGraph.GraphId);
        }
    }
}
