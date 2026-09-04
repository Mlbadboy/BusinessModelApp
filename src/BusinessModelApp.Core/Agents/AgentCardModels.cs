using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Agents
{
    public class AgentTrustScore
    {
        public double OverallScore { get; set; } = 0.85; // 0.0 to 1.0
        public double AccuracyScore { get; set; } = 0.90;
        public double BudgetDisciplineScore { get; set; } = 0.95;
        public double PolicyComplianceScore { get; set; } = 1.0;
        public double HallucinationRate { get; set; } = 0.02; // 0.0 to 1.0 (lower is better)
        public int EvaluatedObservationsCount { get; set; } = 10;
        public DateTime LastEvaluatedAtUtc { get; set; } = DateTime.UtcNow;

        public void Recalculate()
        {
            // Trust score formula: 35% Accuracy + 25% Budget + 30% Policy + 10% (1 - HallucinationRate)
            double raw = (AccuracyScore * 0.35) + 
                         (BudgetDisciplineScore * 0.25) + 
                         (PolicyComplianceScore * 0.30) + 
                         ((1.0 - Math.Clamp(HallucinationRate, 0.0, 1.0)) * 0.10);
            
            OverallScore = Math.Clamp(Math.Round(raw, 4), 0.0, 1.0);
            LastEvaluatedAtUtc = DateTime.UtcNow;
        }
    }

    public class AgentCard
    {
        public string AgentId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public List<string> MissionAuthority { get; set; } = new();
        public List<string> Capabilities { get; set; } = new();
        public List<string> ForbiddenCapabilities { get; set; } = new();
        public string ModelPolicyId { get; set; } = "default-policy";
        public AgentTrustScore TrustScore { get; set; } = new();
        public string EscalationAgentId { get; set; } = "charlie-ceo";
        public string? ParentAgentId { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public bool IsCapabilityPermitted(string capability)
        {
            if (string.IsNullOrWhiteSpace(capability)) return false;

            // Strict Invariant: ForbiddenCapabilities always take precedence over allowed capabilities
            if (ForbiddenCapabilities.Contains(capability, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            return Capabilities.Contains(capability, StringComparer.OrdinalIgnoreCase) ||
                   MissionAuthority.Contains(capability, StringComparer.OrdinalIgnoreCase);
        }
    }

    public interface ITrustScoreEngine
    {
        AgentTrustScore UpdateTrustScore(string agentId, bool taskSucceeded, bool budgetAdhered, bool policyComplied, bool hallucinationDetected);
        bool CanExecuteCapability(AgentCard card, string capability, bool constitutionalPolicyAllows);
    }
}
