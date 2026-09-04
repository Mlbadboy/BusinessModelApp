using System;
using System.Collections.Generic;
using System.Linq;

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

    public class MemoryHypothesis
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string HypothesisText { get; set; } = string.Empty;
        public string TargetEntity { get; set; } = string.Empty;
        public double SubjectiveConfidence { get; set; } = 0.5;
        public DateTime FormulatedAt { get; set; } = DateTime.UtcNow;
        public bool IsVerifiedFact { get; private set; } = false;
        public Guid? CorroboratingEvidenceId { get; private set; }

        public void PromoteWithEvidence(Guid evidenceId)
        {
            CorroboratingEvidenceId = evidenceId;
            IsVerifiedFact = true;
        }
    }

    public class SemanticFact
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Subject { get; set; } = string.Empty;
        public string Relationship { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public Guid GroundingEvidenceId { get; set; }
        public string GroundingEvidenceHash { get; set; } = string.Empty;
    }

    /// <summary>
    /// Persistent 5-Compartment Agent Memory.
    /// 
    /// CRITICAL INVARIANT:
    /// Agent memory is NOT truth.
    /// Storing a thought or memory does not make it a VERIFIED_FACT.
    /// A hypothesis remains unverified until backed by an authoritative EvidenceRecord.
    /// </summary>
    public class AgentMemory
    {
        public Guid AgentId { get; set; }
        public Guid MissionId { get; set; }

        // 1. Working Memory (Current scratchpad and task context)
        public Dictionary<string, string> WorkingMemory { get; set; } = new();
        public Dictionary<string, string> WorkingContext
        {
            get => WorkingMemory;
            set => WorkingMemory = value;
        }

        // 2. Episodic Memory (Chronological actions, inputs, outputs)
        public List<EpisodicEvent> EpisodicMemory { get; set; } = new();
        public List<EpisodicEvent> Timeline
        {
            get => EpisodicMemory;
            set => EpisodicMemory = value;
        }

        // 3. Semantic Memory (Grounded business knowledge)
        public List<SemanticFact> SemanticMemory { get; set; } = new();

        // 4. Evidence References (Immutable cryptographic pointers)
        public HashSet<string> EvidenceReferences { get; set; } = new();
        public HashSet<string> VerifiedEvidenceIds
        {
            get => EvidenceReferences;
            set => EvidenceReferences = value;
        }

        // 5. Hypotheses (Unverified AI inferences - MUST NOT be treated as facts)
        public List<MemoryHypothesis> Hypotheses { get; set; } = new();

        public List<string> DiscoveredCompanies { get; set; } = new();
        public List<string> QualifiedDecisionMakers { get; set; } = new();

        public void AddObservation(AgentRole role, string summary, string details, string? evidenceId = null, string? confidence = null)
        {
            if (!string.IsNullOrEmpty(evidenceId))
            {
                EvidenceReferences.Add(evidenceId);
            }

            EpisodicMemory.Add(new EpisodicEvent
            {
                Role = role,
                Summary = summary,
                Details = details,
                EvidenceId = evidenceId,
                ConfidenceLevel = confidence
            });
        }

        public MemoryHypothesis RecordHypothesis(string text, string targetEntity, double confidence = 0.5)
        {
            var hypothesis = new MemoryHypothesis
            {
                HypothesisText = text,
                TargetEntity = targetEntity,
                SubjectiveConfidence = confidence
            };
            Hypotheses.Add(hypothesis);
            return hypothesis;
        }

        public bool PromoteHypothesisToFact(Guid hypothesisId, Guid evidenceId, string evidenceHash)
        {
            var hypothesis = Hypotheses.FirstOrDefault(h => h.Id == hypothesisId);
            if (hypothesis == null) return false;

            hypothesis.PromoteWithEvidence(evidenceId);
            EvidenceReferences.Add(evidenceId.ToString());

            SemanticMemory.Add(new SemanticFact
            {
                Subject = hypothesis.TargetEntity,
                Relationship = "VerifiedCondition",
                Value = hypothesis.HypothesisText,
                GroundingEvidenceId = evidenceId,
                GroundingEvidenceHash = evidenceHash
            });

            return true;
        }
    }
}
