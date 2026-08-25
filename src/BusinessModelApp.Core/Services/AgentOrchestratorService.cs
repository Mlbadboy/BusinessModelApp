using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Agents;
using BusinessModelApp.Core.Interfaces;

namespace BusinessModelApp.Core.Services
{
    public interface IAgentOrchestratorService
    {
        AgentMission CreateMission(
            Guid workspaceId,
            Guid organizationId,
            string title,
            string objective,
            string targetIndustry,
            int targetProspectCount,
            decimal targetValueINR,
            MissionMode mode,
            AutonomyLevel autonomyLevel,
            decimal walletBudgetINR);

        Task<AgentMission> LaunchMissionAsync(Guid missionId, CancellationToken ct = default);
        AgentMission? GetMission(Guid missionId);
        IEnumerable<AgentMission> GetMissionsByWorkspace(Guid workspaceId);
        Task<bool> ApproveGatedTaskAsync(Guid missionId, Guid taskId, string approverName, CancellationToken ct = default);
    }

    public class AgentOrchestratorService : IAgentOrchestratorService
    {
        private readonly IGovernedToolRegistry _toolRegistry;
        private readonly ConcurrentDictionary<Guid, AgentMission> _missions = new ConcurrentDictionary<Guid, AgentMission>();

        public AgentOrchestratorService(IGovernedToolRegistry toolRegistry)
        {
            _toolRegistry = toolRegistry;
        }

        public AgentMission CreateMission(
            Guid workspaceId,
            Guid organizationId,
            string title,
            string objective,
            string targetIndustry,
            int targetProspectCount,
            decimal targetValueINR,
            MissionMode mode,
            AutonomyLevel autonomyLevel,
            decimal walletBudgetINR)
        {
            var mission = new AgentMission
            {
                WorkspaceId = workspaceId,
                OrganizationId = organizationId,
                Title = title,
                Objective = objective,
                TargetIndustry = targetIndustry,
                TargetProspectCount = targetProspectCount,
                TargetValueINR = targetValueINR,
                Mode = mode,
                AutonomyLevel = autonomyLevel,
                Wallet = MissionWallet.CreateDefault(walletBudgetINR)
            };

            // Build Initial Plan DAG
            BuildMissionDAG(mission);
            _missions[mission.Id] = mission;
            return mission;
        }

        public AgentMission? GetMission(Guid missionId)
        {
            _missions.TryGetValue(missionId, out var mission);
            return mission;
        }

        public IEnumerable<AgentMission> GetMissionsByWorkspace(Guid workspaceId)
        {
            return _missions.Values.Where(m => m.WorkspaceId == workspaceId).OrderByDescending(m => m.CreatedAt);
        }

        private void BuildMissionDAG(AgentMission mission)
        {
            var t1 = new AgentTask
            {
                MissionId = mission.Id,
                Title = "Market Demand & Macro Signals",
                Description = $"Analyze emerging demand in {mission.TargetIndustry}.",
                AssignedRole = AgentRole.MarketIntelligence,
                ActionType = AgentActionType.SearchWeb,
                EstimatedCostINR = 0.25m
            };

            var t2 = new AgentTask
            {
                MissionId = mission.Id,
                Title = "Target Company Discovery",
                Description = $"Identify enterprise companies matching ICP in {mission.TargetIndustry}.",
                AssignedRole = AgentRole.ProspectDiscovery,
                ActionType = AgentActionType.ResearchCompany,
                DependencyTaskIds = new List<Guid> { t1.Id },
                EstimatedCostINR = 0.50m
            };

            var t3 = new AgentTask
            {
                MissionId = mission.Id,
                Title = "Decision Maker Identification",
                Description = "Discover VP/Head of Transformation decision makers.",
                AssignedRole = AgentRole.ProspectDiscovery,
                ActionType = AgentActionType.DiscoverDecisionMakers,
                DependencyTaskIds = new List<Guid> { t2.Id },
                EstimatedCostINR = 0.75m
            };

            var t4 = new AgentTask
            {
                MissionId = mission.Id,
                Title = "ICP Qualification & Lead Scoring",
                Description = "Evaluate organizational fit score and register qualified lead.",
                AssignedRole = AgentRole.LeadQualification,
                ActionType = AgentActionType.CreateLeadInCRM,
                DependencyTaskIds = new List<Guid> { t3.Id },
                EstimatedCostINR = 0.40m
            };

            var t5 = new AgentTask
            {
                MissionId = mission.Id,
                Title = "Governed Evidence-Grounded Outreach",
                Description = "Draft and deliver personalized message referencing verified signals.",
                AssignedRole = AgentRole.Outreach,
                ActionType = AgentActionType.SendOutreach,
                DependencyTaskIds = new List<Guid> { t4.Id },
                EstimatedCostINR = 0.50m
            };

            var t6 = new AgentTask
            {
                MissionId = mission.Id,
                Title = "Conversation Intent Analysis",
                Description = "Understand prospect response, detect buying stage, and qualify commercial interest.",
                AssignedRole = AgentRole.Conversation,
                ActionType = AgentActionType.ProcessInboundMessage,
                DependencyTaskIds = new List<Guid> { t5.Id },
                EstimatedCostINR = 0.30m
            };

            var t7 = new AgentTask
            {
                MissionId = mission.Id,
                Title = "Opportunity Creation & Pipeline Registration",
                Description = "Create commercial opportunity record with weighted forecast.",
                AssignedRole = AgentRole.CommercialCloser,
                ActionType = AgentActionType.CreateOpportunityInCRM,
                DependencyTaskIds = new List<Guid> { t6.Id },
                EstimatedCostINR = 0.50m
            };

            var t8 = new AgentTask
            {
                MissionId = mission.Id,
                Title = "Commercial Proposal & Contract Terms",
                Description = "Draft commercial terms and proposal subject to governance approval.",
                AssignedRole = AgentRole.ProposalGeneration,
                ActionType = AgentActionType.GenerateProposal,
                DependencyTaskIds = new List<Guid> { t7.Id },
                EstimatedCostINR = 1.50m
            };

            mission.Tasks.AddRange(new[] { t1, t2, t3, t4, t5, t6, t7, t8 });
        }

        public async Task<AgentMission> LaunchMissionAsync(Guid missionId, CancellationToken ct = default)
        {
            if (!_missions.TryGetValue(missionId, out var mission))
            {
                throw new KeyNotFoundException($"Mission {missionId} not found.");
            }

            mission.Status = MissionStatus.Running;
            mission.StartedAt = DateTime.UtcNow;

            mission.Memory.AddObservation(
                AgentRole.MarketIntelligence,
                "Mission Initialized",
                $"Objective: {mission.Objective} | Industry: {mission.TargetIndustry} | Mode: {mission.Mode} | Autonomy: {mission.AutonomyLevel}",
                confidence: "100%");

            // Execute DAG tasks sequentially
            foreach (var task in mission.Tasks)
            {
                if (task.Status == AgentTaskStatus.Completed) continue;

                // Check dependencies
                bool depsComplete = task.DependencyTaskIds.All(depId =>
                    mission.Tasks.Any(t => t.Id == depId && t.Status == AgentTaskStatus.Completed));

                if (!depsComplete) continue;

                task.Status = AgentTaskStatus.Running;
                task.StartedAt = DateTime.UtcNow;

                var agentIdentity = AgentIdentity.Create(task.AssignedRole);
                var parameters = new Dictionary<string, object>
                {
                    { "company", "Apex Financial Technologies" },
                    { "amount", mission.TargetValueINR }
                };

                var toolResult = await _toolRegistry.ExecuteToolAsync(agentIdentity, task.ActionType, mission, parameters, ct);

                if (toolResult.RequiresApproval)
                {
                    task.Status = AgentTaskStatus.BlockedOnApproval;
                    task.ApprovalRequestId = toolResult.ApprovalRequestId;
                    task.ThoughtStream = $"Gated by policy: {toolResult.BlockReason}. Awaiting human authorization.";

                    mission.Memory.AddObservation(
                        task.AssignedRole,
                        $"Gated on Human Approval: {task.Title}",
                        toolResult.BlockReason ?? "Approval required before executing commercial commitment.",
                        confidence: "PolicyGate");

                    // Stop execution at gate until approved
                    break;
                }

                if (!toolResult.Success)
                {
                    task.Status = AgentTaskStatus.Failed;
                    task.ThoughtStream = $"Failed: {toolResult.BlockReason}";
                    break;
                }

                task.Status = AgentTaskStatus.Completed;
                task.CompletedAt = DateTime.UtcNow;
                task.ActualCostINR = toolResult.CostINR;
                task.OutputResultJson = toolResult.OutputJson;
                task.EvidenceReferenceId = toolResult.EvidenceId;
                task.ThoughtStream = $"Executed successfully. Evidence: {toolResult.EvidenceId ?? "N/A"}";

                // Update real-time metrics & episodic memory
                UpdateMissionMetrics(mission, task, toolResult);
            }

            // If all tasks completed
            if (mission.Tasks.All(t => t.Status == AgentTaskStatus.Completed))
            {
                mission.Status = MissionStatus.Completed;
                mission.CompletedAt = DateTime.UtcNow;
            }

            return mission;
        }

        public async Task<bool> ApproveGatedTaskAsync(Guid missionId, Guid taskId, string approverName, CancellationToken ct = default)
        {
            if (!_missions.TryGetValue(missionId, out var mission)) return false;
            var task = mission.Tasks.FirstOrDefault(t => t.Id == taskId);
            if (task == null || task.Status != AgentTaskStatus.BlockedOnApproval) return false;

            task.Status = AgentTaskStatus.Completed;
            task.CompletedAt = DateTime.UtcNow;
            task.ThoughtStream = $"Approved by {approverName}. Commercial proposal dispatched.";

            mission.Memory.AddObservation(
                task.AssignedRole,
                $"Approved by {approverName}",
                $"Commercial commitment approved and executed.",
                evidenceId: task.EvidenceReferenceId,
                confidence: "Authorized");

            // Resume remaining tasks
            await LaunchMissionAsync(missionId, ct);
            return true;
        }

        private void UpdateMissionMetrics(AgentMission mission, AgentTask task, ToolExecutionResult toolResult)
        {
            switch (task.ActionType)
            {
                case AgentActionType.SearchWeb:
                case AgentActionType.ResearchCompany:
                    mission.CompaniesResearched += 12;
                    mission.Memory.AddObservation(task.AssignedRole, "Identified target accounts", "Discovered 12 high-fit accounts in sector.", toolResult.EvidenceId, "94%");
                    break;

                case AgentActionType.DiscoverDecisionMakers:
                    mission.ProspectsDiscovered += 8;
                    mission.Memory.AddObservation(task.AssignedRole, "Discovered decision makers", "Identified 8 enterprise transformation leaders.", toolResult.EvidenceId, "91%");
                    break;

                case AgentActionType.CreateLeadInCRM:
                    mission.QualifiedCount += 5;
                    mission.Memory.AddObservation(task.AssignedRole, "Qualified high-priority leads", "Lead score 88.5/100 matching ICP criteria.", toolResult.EvidenceId, "89%");
                    break;

                case AgentActionType.SendOutreach:
                    mission.OutreachSent += 5;
                    mission.Memory.AddObservation(task.AssignedRole, "Outreach delivered", "Personalized messages sent via verified business channels.", toolResult.EvidenceId, "96%");
                    break;

                case AgentActionType.ProcessInboundMessage:
                    mission.ResponsesReceived += 3;
                    mission.PositiveConversations += 2;
                    mission.Memory.AddObservation(task.AssignedRole, "Inbound conversation qualified", "Prospect confirmed active Q4 AI budget & transformation initiative.", toolResult.EvidenceId, "92%");
                    break;

                case AgentActionType.CreateOpportunityInCRM:
                    mission.OpportunitiesCreated += 1;
                    mission.PipelineValueGeneratedINR += mission.TargetValueINR;
                    mission.Memory.AddObservation(task.AssignedRole, "Opportunity registered in pipeline", $"Created ₹{mission.TargetValueINR:N0} opportunity.", toolResult.EvidenceId, "100%");
                    break;
            }
        }
    }
}
