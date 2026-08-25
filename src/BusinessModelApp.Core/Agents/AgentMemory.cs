using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Agents
{
    public class EpisodicEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public AgentRole Role { get; set; }
        public string Summary { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string? EvidenceId { get; set; }
        public string? ConfidenceLevel { get; set; }
    }

    public class AgentMemory
    {
        public Guid MissionId { get; set; }
        public Dictionary<string, string> WorkingContext { get; set; } = new Dictionary<string, string>();
        public List<EpisodicEvent> Timeline { get; set; } = new List<EpisodicEvent>();
        public HashSet<string> VerifiedEvidenceIds { get; set; } = new HashSet<string>();
        public List<string> DiscoveredCompanies { get; set; } = new List<string>();
        public List<string> QualifiedDecisionMakers { get; set; } = new List<string>();

        public void AddObservation(AgentRole role, string summary, string details, string? evidenceId = null, string? confidence = null)
        {
            if (!string.IsNullOrEmpty(evidenceId))
            {
                VerifiedEvidenceIds.Add(evidenceId);
            }

            Timeline.Add(new EpisodicEvent
            {
                Role = role,
                Summary = summary,
                Details = details,
                EvidenceId = evidenceId,
                ConfidenceLevel = confidence
            });
        }
    }
}
