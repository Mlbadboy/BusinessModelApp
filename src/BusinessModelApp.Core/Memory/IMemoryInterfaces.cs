using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Decisions;
using BusinessModelApp.Core.Domain.Reality;

namespace BusinessModelApp.Core.Memory
{
    public class EpisodicEventRecord
    {
        public Guid EventId { get; set; } = Guid.NewGuid();
        public Guid WorkspaceId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string PayloadJson { get; set; } = "{}";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class SemanticFactRecord
    {
        public Guid FactId { get; set; } = Guid.NewGuid();
        public Guid WorkspaceId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Predicate { get; set; } = string.Empty;
        public string ObjectValue { get; set; } = string.Empty;
        public string EvidenceHash { get; set; } = string.Empty;
        public double Confidence { get; set; } = 1.0;
        public DateTime GroundedAt { get; set; } = DateTime.UtcNow;
    }

    public class OrganizationalDoctrineRecord
    {
        public Guid DoctrineId { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string OperationalRuleText { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0";
        public int PromotionIteration { get; set; } = 1;
        public DateTime PromotedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Chronological fact and event recording.
    /// </summary>
    public interface IEpisodicMemory
    {
        Task RecordEventAsync(EpisodicEventRecord eventRecord, CancellationToken ct = default);
        Task<IReadOnlyList<EpisodicEventRecord>> GetRecentEventsAsync(Guid workspaceId, int count = 20, CancellationToken ct = default);
    }

    /// <summary>
    /// Verified knowledge and entity ontologies grounded by evidence.
    /// </summary>
    public interface ISemanticMemory
    {
        Task StoreFactAsync(SemanticFactRecord fact, CancellationToken ct = default);
        Task<IReadOnlyList<SemanticFactRecord>> QueryFactsAsync(Guid workspaceId, string subject, CancellationToken ct = default);
    }

    /// <summary>
    /// Historic decisions, alternatives considered, economic trade-offs, and rationale.
    /// </summary>
    public interface IDecisionMemory
    {
        Task RecordDecisionAsync(DecisionRecord decision, CancellationToken ct = default);
        Task<DecisionRecord?> GetDecisionAsync(Guid decisionId, CancellationToken ct = default);
        Task<IReadOnlyList<DecisionRecord>> GetDecisionsForObjectiveAsync(Guid objectiveId, CancellationToken ct = default);
    }

    /// <summary>
    /// Promoted company operational doctrines and playbooks.
    /// </summary>
    public interface IOrganizationalMemory
    {
        Task<IReadOnlyList<OrganizationalDoctrineRecord>> GetActiveDoctrinesAsync(Guid workspaceId, CancellationToken ct = default);
    }
}
