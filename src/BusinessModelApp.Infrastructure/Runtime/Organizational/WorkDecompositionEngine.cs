using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Responsibilities;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Domain.Runtime.Organizational;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational
{
    public class WorkDecompositionEngine : IWorkDecompositionEngine
    {
        private readonly IOrganizationalWorkRepository _workRepository;
        private readonly IWorkManagerRunStore _runStore;

        public WorkDecompositionEngine(
            IOrganizationalWorkRepository workRepository,
            IWorkManagerRunStore runStore)
        {
            _workRepository = workRepository ?? throw new ArgumentNullException(nameof(workRepository));
            _runStore = runStore ?? throw new ArgumentNullException(nameof(runStore));
        }

        public async Task<(bool Success, WorkPlan? Plan, MissionGraphProposal? Proposal, string? Error)> DecomposeWorkAsync(
            WorkItem item,
            CancellationToken ct = default)
        {
            if (item == null)
                return (false, null, null, "WorkItem cannot be null.");

            if (string.IsNullOrWhiteSpace(item.Objective?.Statement))
                return (false, null, null, "WorkItem objective statement cannot be empty.");

            var planId = $"PLAN-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
            var graphIdStr = $"DAG-{item.WorkId}";

            // 1. Construct WorkPlan
            var plan = new WorkPlan
            {
                PlanId = planId,
                WorkId = item.WorkId,
                SequenceType = "GovernedDAG",
                DecomposedMissions = new List<string>
                {
                    $"Perceive:{item.Title}",
                    $"Hypothesize:{item.Title}",
                    $"Simulate:{item.Title}",
                    $"Checkpoint:{item.Title}",
                    $"Verify:{item.Title}"
                },
                Checkpoints = new List<string>
                {
                    "PreFlightEvidenceCheck",
                    "ExecutionAuthorizationCheckpoint",
                    "PostOutcomeEvidenceCheck"
                },
                FallbackStrategy = "SafeStop",
                CreatedUtc = DateTime.UtcNow
            };

            await _workRepository.SaveWorkPlanAsync(item.TenantId, plan, ct);

            // 2. Construct MissionGraphProposal nodes
            var pNodes = new List<ProposedNode>();
            var pEdges = new List<ProposedEdge>();

            // Node 1: Perception
            var n1 = new ProposedNode
            {
                NodeId = $"N1-{item.WorkId}",
                Title = $"Perceive Telemetry for {item.Title}",
                NodeType = MissionNodeType.Perception,
                Description = "Collect and ground certified reality telemetry."
            };
            pNodes.Add(n1);

            // Node 2: Hypothesis
            var n2 = new ProposedNode
            {
                NodeId = $"N2-{item.WorkId}",
                Title = $"Synthesize Strategy for {item.Title}",
                NodeType = MissionNodeType.Hypothesis,
                Description = "Synthesize strategic hypotheses and causal models."
            };
            pNodes.Add(n2);
            pEdges.Add(new ProposedEdge { SourceNodeId = n1.NodeId, TargetNodeId = n2.NodeId, Type = EdgeType.Sequential });

            // Node 3: Simulation
            var n3 = new ProposedNode
            {
                NodeId = $"N3-{item.WorkId}",
                Title = $"Simulate Counterfactual for {item.Title}",
                NodeType = MissionNodeType.Simulation,
                Description = "Simulate counterfactual scenarios on Pareto frontier."
            };
            pNodes.Add(n3);
            pEdges.Add(new ProposedEdge { SourceNodeId = n2.NodeId, TargetNodeId = n3.NodeId, Type = EdgeType.Sequential });

            // Node 4: ExecutionAuthorizationCheckpoint
            // Invariant I26-Q: Checkpoint ≠ Permit. Checkpoint cannot issue or sign Permit.
            var n4 = new ProposedNode
            {
                NodeId = $"N4-CHECKPOINT-{item.WorkId}",
                Title = $"ExecutionAuthorizationCheckpoint for {item.Title}",
                NodeType = MissionNodeType.Approval,
                RequiresHumanApproval = item.RiskTier >= WorkRiskTier.R2_ExternalBounded,
                Description = "Execution authorization checkpoint: indicates downstream Batch 6 authorization may be required."
            };
            pNodes.Add(n4);
            pEdges.Add(new ProposedEdge { SourceNodeId = n3.NodeId, TargetNodeId = n4.NodeId, Type = EdgeType.Sequential });

            // Node 5: Verification
            var n5 = new ProposedNode
            {
                NodeId = $"N5-{item.WorkId}",
                Title = $"Verify Evidence for {item.Title}",
                NodeType = MissionNodeType.Verification,
                Description = "Verify real-world outcome telemetry and compute variance."
            };
            pNodes.Add(n5);
            pEdges.Add(new ProposedEdge { SourceNodeId = n4.NodeId, TargetNodeId = n5.NodeId, Type = EdgeType.Sequential });

            // 3. Compile MissionGraphProposal (without directly mutating MissionGraph)
            var proposal = new MissionGraphProposal
            {
                ProposalId = Guid.NewGuid(),
                MissionId = new MissionId(Guid.NewGuid()),
                Title = $"Autonomous organizational work decomposition for {item.WorkId}: {item.Title}",
                Description = item.Objective?.Statement ?? "Organizational work decomposition",
                ProposedNodes = pNodes,
                ProposedEdges = pEdges,
                ProposedAutonomyTier = item.RiskTier >= WorkRiskTier.R3_Consequential
                    ? AutonomyTier.L3_Prepare
                    : AutonomyTier.L4_ExecuteWithApproval
            };

            await _runStore.SaveMissionProposalLinkAsync(item.TenantId, item.WorkId, proposal, ct);

            // 4. Update WorkItem linkage
            item.MissionGraphId = graphIdStr;
            await _workRepository.SaveWorkItemAsync(item, ct);

            return (true, plan, proposal, null);
        }
    }
}
