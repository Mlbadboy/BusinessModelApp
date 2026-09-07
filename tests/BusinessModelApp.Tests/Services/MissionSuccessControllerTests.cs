using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Agents;
using BusinessModelApp.Core.Services;
using Xunit;

namespace BusinessModelApp.Tests.Services
{
    public class MissionSuccessControllerTests
    {
        private readonly MissionSuccessController _controller = new MissionSuccessController();

        [Fact]
        public void EvaluateMissionTrajectory_IdentifiesAtRiskMissionOnLowConversion()
        {
            var mission = new AgentMission
            {
                Id = Guid.NewGuid(),
                TargetValueINR = 5000000m,
                PipelineValueGeneratedINR = 0m,
                OutreachSent = 10,
                ResponsesReceived = 1 // 10% response rate < 15% threshold
            };

            var report = _controller.EvaluateMissionTrajectory(mission);

            Assert.Equal(MissionTrajectory.AtRisk, report.Trajectory);
            Assert.NotNull(report.ActiveBottleneck);
            Assert.Contains("Sub-optimal Persona", report.ActiveBottleneck.PrimaryBottleneck);
            Assert.NotEmpty(report.RecommendedAdjustments);
        }

        [Fact]
        public void EvaluateMissionTrajectory_ReturnsAwaitingApprovalWhenGated()
        {
            var mission = new AgentMission
            {
                Id = Guid.NewGuid(),
                TargetValueINR = 2500000m,
                Tasks = new List<AgentTask>
                {
                    new AgentTask { Title = "Task 1", Status = AgentTaskStatus.Completed },
                    new AgentTask { Title = "Task 2", Status = AgentTaskStatus.BlockedOnApproval }
                }
            };

            var report = _controller.EvaluateMissionTrajectory(mission);

            Assert.Equal(MissionTrajectory.AwaitingApproval, report.Trajectory);
        }

        [Fact]
        public async Task ReplanMissionAsync_AdaptsDAGAndIncreasesPipeline()
        {
            var mission = new AgentMission
            {
                Id = Guid.NewGuid(),
                TargetValueINR = 5000000m,
                PipelineValueGeneratedINR = 2500000m,
                OutreachSent = 10,
                ResponsesReceived = 1,
                Wallet = new MissionWallet { TotalBudgetINR = 5000m, ConsumedSpendINR = 1000m }
            };

            var diagnosis = _controller.DiagnoseBottlenecks(mission);
            var replanned = await _controller.ReplanMissionAsync(mission, diagnosis);

            Assert.Equal(5000000m, replanned.PipelineValueGeneratedINR);
            Assert.Equal(18, replanned.OutreachSent);
            Assert.Equal(5, replanned.ResponsesReceived);
            Assert.True(replanned.Wallet.ConsumedSpendINR > 1000m);
        }
    }
}
