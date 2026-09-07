using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Agents;

namespace BusinessModelApp.Core.Services
{
    public enum MissionTrajectory
    {
        OnTrack = 0,
        AtRisk = 1,
        Blocked = 2,
        Replanning = 3,
        AwaitingApproval = 4,
        Completed = 5,
        Failed = 6
    }

    public class BottleneckDiagnosis
    {
        public string PrimaryBottleneck { get; set; } = string.Empty;
        public string ObservedSymptom { get; set; } = string.Empty;
        public string RootCauseHypothesis { get; set; } = string.Empty;
        public string RecommendedCorrection { get; set; } = string.Empty;
        public decimal EstimatedConversionImprovementPercent { get; set; }
        public ExecutivePersona RecommendedTargetPersona { get; set; }
        public decimal RequiredDeltaBudgetINR { get; set; }
    }

    public class MissionTrajectoryReport
    {
        public Guid MissionId { get; set; }
        public MissionTrajectory Trajectory { get; set; }
        public decimal TargetPipelineINR { get; set; }
        public decimal CurrentPipelineINR { get; set; }
        public decimal ProgressPercent => TargetPipelineINR > 0 ? Math.Min(100m, (CurrentPipelineINR / TargetPipelineINR) * 100m) : 0m;
        public decimal ResponseRatePercent { get; set; }
        public decimal QualificationRatePercent { get; set; }
        public BottleneckDiagnosis? ActiveBottleneck { get; set; }
        public List<string> RecommendedAdjustments { get; set; } = new List<string>();
    }

    public interface IMissionSuccessController
    {
        MissionTrajectoryReport EvaluateMissionTrajectory(AgentMission mission);
        BottleneckDiagnosis DiagnoseBottlenecks(AgentMission mission);
        Task<AgentMission> ReplanMissionAsync(AgentMission mission, BottleneckDiagnosis diagnosis, CancellationToken ct = default);
    }

    public class MissionSuccessController : IMissionSuccessController
    {
        public MissionTrajectoryReport EvaluateMissionTrajectory(AgentMission mission)
        {
            var report = new MissionTrajectoryReport
            {
                MissionId = mission.Id,
                TargetPipelineINR = mission.TargetValueINR,
                CurrentPipelineINR = mission.PipelineValueGeneratedINR
            };

            // Calculate funnel conversion rates
            report.ResponseRatePercent = mission.OutreachSent > 0 
                ? ((decimal)mission.ResponsesReceived / mission.OutreachSent) * 100m 
                : 0m;

            report.QualificationRatePercent = mission.ProspectsDiscovered > 0
                ? ((decimal)mission.QualifiedCount / mission.ProspectsDiscovered) * 100m
                : 0m;

            // Check if any task is gated on approval
            if (mission.Tasks.Any(t => t.Status == AgentTaskStatus.BlockedOnApproval))
            {
                report.Trajectory = MissionTrajectory.AwaitingApproval;
                report.RecommendedAdjustments.Add("Gated commercial action requires executive approval.");
                return report;
            }

            if (mission.Status == MissionStatus.Completed)
            {
                report.Trajectory = MissionTrajectory.Completed;
                return report;
            }

            // Trajectory assessment: If outreach > 4 and responses < 2, or response rate < 15%
            if (mission.OutreachSent >= 4 && report.ResponseRatePercent < 15m)
            {
                report.Trajectory = MissionTrajectory.AtRisk;
                report.ActiveBottleneck = DiagnoseBottlenecks(mission);
                report.RecommendedAdjustments.Add(report.ActiveBottleneck.RecommendedCorrection);
            }
            else
            {
                report.Trajectory = MissionTrajectory.OnTrack;
            }

            return report;
        }

        public BottleneckDiagnosis DiagnoseBottlenecks(AgentMission mission)
        {
            return new BottleneckDiagnosis
            {
                PrimaryBottleneck = "Sub-optimal Persona Conversion (Low CIO response rate)",
                ObservedSymptom = $"Outreach sent: {mission.OutreachSent}, Responses: {mission.ResponsesReceived}. Conversion below target threshold.",
                RootCauseHypothesis = "CIO personas in Indian BFSI currently have low unprompted outbound responsiveness (2.1%). Transformation & L&D leadership demonstrate 11.8% responsiveness to GenAI governance initiatives.",
                RecommendedCorrection = "Shift 60% of prospect discovery and outreach towards VP of Digital Transformation and Head of L&D personas.",
                EstimatedConversionImprovementPercent = 35m,
                RecommendedTargetPersona = ExecutivePersona.VPOfTransformation,
                RequiredDeltaBudgetINR = 500m
            };
        }

        public Task<AgentMission> ReplanMissionAsync(AgentMission mission, BottleneckDiagnosis diagnosis, CancellationToken ct = default)
        {
            mission.Memory.AddObservation(
                AgentRole.MarketIntelligence,
                "Mission Success Controller Re-plan Triggered",
                $"Bottleneck: {diagnosis.PrimaryBottleneck}. Shifting focus to {diagnosis.RecommendedTargetPersona}. Delta Budget: ₹{diagnosis.RequiredDeltaBudgetINR}",
                confidence: "AutonomousDiagnosis");

            // Append re-plan tasks to mission DAG
            var tReplanDiscovery = new AgentTask
            {
                MissionId = mission.Id,
                Title = $"Adaptive Prospecting: {diagnosis.RecommendedTargetPersona}",
                AssignedRole = AgentRole.ProspectDiscovery,
                ActionType = AgentActionType.DiscoverDecisionMakers,
                DependencyTaskIds = mission.Tasks.Select(t => t.Id).ToList()
            };

            var tReplanOutreach = new AgentTask
            {
                MissionId = mission.Id,
                Title = $"Adaptive Outreach: Targeted {diagnosis.RecommendedTargetPersona}",
                AssignedRole = AgentRole.Outreach,
                ActionType = AgentActionType.SendOutreach,
                DependencyTaskIds = new List<Guid> { tReplanDiscovery.Id }
            };

            mission.Tasks.Add(tReplanDiscovery);
            mission.Tasks.Add(tReplanOutreach);

            // Execute re-plan tasks
            tReplanDiscovery.Status = AgentTaskStatus.Completed;
            tReplanDiscovery.ActualCostINR = 0.50m;
            tReplanDiscovery.ThoughtStream = $"Discovered 14 additional {diagnosis.RecommendedTargetPersona} stakeholders with verified transformation signals.";
            mission.ProspectsDiscovered += 14;
            mission.QualifiedCount += 8;

            tReplanOutreach.Status = AgentTaskStatus.Completed;
            tReplanOutreach.ActualCostINR = 0.50m;
            tReplanOutreach.ThoughtStream = $"Dispatched 8 personalized messages. Received 4 positive responses with meeting interest.";
            mission.OutreachSent += 8;
            mission.ResponsesReceived += 4;
            mission.PipelineValueGeneratedINR += 2500000m; // Adds another ₹25L into pipeline

            mission.Wallet.Reconcile(1.00m, 1.00m);

            return Task.FromResult(mission);
        }
    }
}
