using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Agents
{
    public enum ExecutivePersona
    {
        CEO = 0,
        CHRO = 1,
        CIO = 2,
        CTO = 3,
        CDO = 4,
        HeadOfLearningDevelopment = 5,
        VPOfTransformation = 6
    }

    public class DecisionMakerProfile
    {
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public ExecutivePersona Persona { get; set; }
        public string Email { get; set; } = string.Empty;
        public string LinkedInUrl { get; set; } = string.Empty;
        public decimal FitScore { get; set; }
        public string EvidenceToken { get; set; } = string.Empty;
        public bool IsVerified { get; set; }
    }

    public class AccountGraph
    {
        public string CompanyName { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public int Headcount { get; set; }
        public List<string> TechStack { get; set; } = new List<string>();
        public List<string> ActiveTransformationSignals { get; set; } = new List<string>();
        public List<DecisionMakerProfile> DecisionMakers { get; set; } = new List<DecisionMakerProfile>();
        public decimal OverallICPScore { get; set; }
        public string GroundingEvidenceId { get; set; } = string.Empty;

        public static decimal CalculateICPScore(int headcount, int signalCount, bool hasTargetPersona, bool hasVerifiedEmail)
        {
            decimal sizeWeight = headcount >= 500 ? 35m : (headcount >= 100 ? 20m : 10m);
            decimal signalWeight = Math.Min(signalCount * 12.5m, 25m);
            decimal personaWeight = hasTargetPersona ? 25m : 10m;
            decimal contactabilityWeight = hasVerifiedEmail ? 15m : 5m;

            return Math.Min(100m, sizeWeight + signalWeight + personaWeight + contactabilityWeight);
        }
    }

    public class MeetingBrief
    {
        public string Id { get; set; } = $"MB-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        public string CompanyName { get; set; } = string.Empty;
        public string TargetExecutive { get; set; } = string.Empty;
        public DateTime ScheduledTime { get; set; }
        public List<string> GroundedEvidenceIds { get; set; } = new List<string>();
        public List<string> DetectedPainPoints { get; set; } = new List<string>();
        public List<string> RecommendedTalkingPoints { get; set; } = new List<string>();
        public decimal PotentialDealValueINR { get; set; }
    }
}
