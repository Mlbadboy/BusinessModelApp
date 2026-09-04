using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Agents
{
    public enum AutonomyTier
    {
        L0_Observer = 0,
        L1_Analyst = 1,
        L2_Copilot = 2,
        L3_ApprovalAgent = 3,
        L4_GovernedAutonomous = 4,
        L5_StrategicAutonomous = 5
    }

    public class ShadowModeComparison
    {
        public string ShadowActionId { get; set; } = Guid.NewGuid().ToString("N");
        public string AgentId { get; set; } = string.Empty;
        public string ProposedActionJson { get; set; } = string.Empty;
        public string ActualHumanActionJson { get; set; } = string.Empty;
        public double FidelityScore { get; set; } = 0.0;
        public DateTime EvaluatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public enum LearningCandidateStatus
    {
        Proposed = 0,
        Evaluating = 1,
        ApprovedByGovernance = 2,
        Rejected = 3
    }

    public class LearningCandidate
    {
        public string CandidateId { get; set; } = Guid.NewGuid().ToString("N");
        public string TargetArea { get; set; } = string.Empty;
        public string ProposedImprovement { get; set; } = string.Empty;
        public double BenchmarkScore { get; set; } = 0.0;
        public LearningCandidateStatus Status { get; set; } = LearningCandidateStatus.Proposed;
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public interface IAutonomyManager
    {
        AutonomyTier GetCurrentTier(string agentId);
        void SetTierDirect(string agentId, AutonomyTier tier);
        void RecordShadowComparison(string agentId, string proposedActionJson, string actualHumanActionJson, double fidelityScore);
        double GetAverageShadowFidelity(string agentId);
        bool RequestPromotion(string agentId, AutonomyTier targetTier, string operatorId, bool humanApproved);
    }

    public interface IGovernedLearningLoop
    {
        LearningCandidate ProposeImprovement(string targetArea, string proposedImprovement);
        void RecordBenchmarkEvaluation(string candidateId, double benchmarkScore);
        bool ApproveCandidate(string candidateId, string governanceApproverId);
        IReadOnlyList<LearningCandidate> GetPendingCandidates();
        IReadOnlyList<LearningCandidate> GetApprovedCandidates();
    }
}
