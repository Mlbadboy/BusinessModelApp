using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Agents;
using BusinessModelApp.Core.Services;
using Xunit;

namespace BusinessModelApp.Tests.Agents
{
    public class AutonomousReplanningLoopTests
    {
        [Fact]
        public async Task ClosedLoop_AutonomousReplanning_RecoversLaggingMission()
        {
            var policyEngine = new AgentPolicyEngine();
            var toolRegistry = new GovernedToolRegistry(null, policyEngine);
            var successController = new MissionSuccessController();
            var orchestrator = new AgentOrchestratorService(toolRegistry, successController);

            var workspaceId = Guid.NewGuid();
            var orgId = Guid.NewGuid();

            // 1. Create Mission with ₹50L target
            var mission = orchestrator.CreateMission(
                workspaceId,
                orgId,
                "BFSI Autonomous Campaign",
                "Generate ₹50L qualified pipeline in BFSI",
                "Enterprise BFSI",
                25,
                5000000m,
                MissionMode.Simulation,
                AutonomyLevel.Level3_ControlledAutonomy,
                5000m);

            // 2. Launch initial DAG
            var executed = await orchestrator.LaunchMissionAsync(mission.Id);
            Assert.NotNull(executed);

            // 3. Initial phase hits ₹25L out of ₹50L target
            var initialTrajectory = orchestrator.GetTrajectoryReport(mission.Id);
            Assert.NotNull(initialTrajectory);

            // 4. Trigger Autonomous Re-plan to capture remaining pipeline
            var replanned = await orchestrator.ReplanMissionAsync(mission.Id);
            Assert.NotNull(replanned);
            Assert.True(replanned.PipelineValueGeneratedINR >= 5000000m); // Pipeline target achieved or exceeded
            Assert.True(replanned.Tasks.Count >= 10); // 8 initial + 2 adaptive DAG tasks
        }
    }
}
