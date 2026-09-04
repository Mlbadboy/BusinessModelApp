using System;
using System.Collections.Generic;
using System.Linq;

namespace BusinessModelApp.Core.Agents
{
    public class BlackboardFact
    {
        public Guid FactId { get; set; } = Guid.NewGuid();
        public string Subject { get; set; } = string.Empty;
        public string Statement { get; set; } = string.Empty;
        public Guid EvidenceRecordId { get; set; }
        public string EvidenceHash { get; set; } = string.Empty;
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }

    public class BlackboardHypothesis
    {
        public Guid HypothesisId { get; set; } = Guid.NewGuid();
        public string Statement { get; set; } = string.Empty;
        public string ProposedByAgent { get; set; } = string.Empty;
        public double Confidence { get; set; } = 0.5;
        public bool IsValidated { get; set; } = false;
        public DateTime ProposedAt { get; set; } = DateTime.UtcNow;
    }

    public class BlackboardTaskItem
    {
        public Guid TaskId { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public AgentRole AssignedAgentRole { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, InProgress, Completed, Blocked
        public string? ResultPayloadJson { get; set; }
    }

    /// <summary>
    /// Shared Blackboard for stigmergic collaboration among autonomous agents in the Business Hive.
    /// Invariant: Separates verified facts (with evidence hashes) from ungrounded hypotheses.
    /// </summary>
    public class MissionBlackboard
    {
        public Guid MissionId { get; set; }
        public Guid ObjectiveId { get; set; }
        public string ObjectiveTitle { get; set; } = string.Empty;
        public decimal TargetRevenueINR { get; set; }

        public List<BlackboardFact> KnownFacts { get; set; } = new();
        public List<BlackboardHypothesis> Hypotheses { get; set; } = new();
        public List<string> Constraints { get; set; } = new();
        public List<string> Decisions { get; set; } = new();
        public List<string> OpenQuestions { get; set; } = new();
        public List<BlackboardTaskItem> Tasks { get; set; } = new();
        public List<string> Risks { get; set; } = new();
        public List<string> Blockers { get; set; } = new();
        public HashSet<string> EvidenceReferences { get; set; } = new();

        public void PostFact(string subject, string statement, Guid evidenceId, string evidenceHash)
        {
            KnownFacts.Add(new BlackboardFact
            {
                Subject = subject,
                Statement = statement,
                EvidenceRecordId = evidenceId,
                EvidenceHash = evidenceHash
            });
            EvidenceReferences.Add(evidenceHash);
        }

        public void PostHypothesis(string statement, string agentName, double confidence = 0.5)
        {
            Hypotheses.Add(new BlackboardHypothesis
            {
                Statement = statement,
                ProposedByAgent = agentName,
                Confidence = confidence
            });
        }

        public void AddTask(string title, AgentRole role)
        {
            Tasks.Add(new BlackboardTaskItem
            {
                Title = title,
                AssignedAgentRole = role,
                Status = "Pending"
            });
        }
    }
}
