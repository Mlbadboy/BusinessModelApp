using System;
using System.Linq;
using System.Threading.Tasks;
using BusinessModelApp.Core.Agents;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Core.Services;
using Moq;
using Xunit;

namespace BusinessModelApp.Tests.Agents
{
    public class ClosedRevenueLoopTests
    {
        [Fact]
        public async Task Closed_Revenue_Loop_Executes_Mission_Gating_At_Commercial_Proposal_And_Completing_On_Approval()
        {
            // Arrange
            var mockCommercialRepo = new Mock<ICommercialRepository>();
            var toolRegistry = new GovernedToolRegistry(mockCommercialRepo.Object);
            var orchestrator = new AgentOrchestratorService(toolRegistry);

            var workspaceId = Guid.NewGuid();
            var orgId = Guid.NewGuid();

            // 1. Create Mission: ₹25L Pipeline Generation
            var mission = orchestrator.CreateMission(
                workspaceId,
                orgId,
                title: "BFSI Autonomous Revenue Campaign",
                objective: "Generate ₹25L qualified pipeline from BFSI accounts",
                targetIndustry: "Enterprise BFSI",
                targetProspectCount: 25,
                targetValueINR: 2500000m,
                mode: MissionMode.Simulation,
                autonomyLevel: AutonomyLevel.Level3_ControlledAutonomy,
                walletBudgetINR: 5000m);

            Assert.Equal(8, mission.Tasks.Count);
            Assert.Equal(MissionStatus.Draft, mission.Status);

            // 2. Launch Mission: Should execute tasks 1 to 7 and pause at Task 8 (Proposal Approval Gate)
            var runningMission = await orchestrator.LaunchMissionAsync(mission.Id);

            Assert.Equal(MissionStatus.Running, runningMission.Status);
            Assert.True(runningMission.CompaniesResearched > 0);
            Assert.True(runningMission.ProspectsDiscovered > 0);
            Assert.True(runningMission.QualifiedCount > 0);
            Assert.True(runningMission.OutreachSent > 0);
            Assert.True(runningMission.ResponsesReceived > 0);
            Assert.True(runningMission.OpportunitiesCreated > 0);
            Assert.Equal(2500000m, runningMission.PipelineValueGeneratedINR);

            // Task 8 should be blocked on approval
            var proposalTask = runningMission.Tasks.Last();
            Assert.Equal("Commercial Proposal & Contract Terms", proposalTask.Title);
            Assert.Equal(AgentTaskStatus.BlockedOnApproval, proposalTask.Status);
            Assert.NotNull(proposalTask.ApprovalRequestId);

            // 3. Human Leader Approves Gated Proposal
            bool approved = await orchestrator.ApproveGatedTaskAsync(mission.Id, proposalTask.Id, "CEO Mayur");
            Assert.True(approved);

            // 4. Mission Completed and Full Loop Verified
            var completedMission = orchestrator.GetMission(mission.Id);
            Assert.NotNull(completedMission);
            Assert.Equal(MissionStatus.Completed, completedMission.Status);
            Assert.NotNull(completedMission.CompletedAt);
            Assert.True(completedMission.Memory.Timeline.Count >= 7);
            Assert.True(completedMission.Memory.VerifiedEvidenceIds.Count > 0);
        }
    }
}
