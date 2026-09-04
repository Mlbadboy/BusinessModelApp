using System;
using System.Collections.Generic;
using BusinessModelApp.Core.Domain.WorldModel;

namespace BusinessModelApp.Core.Domain.Reality
{
    public enum EvidenceGraphSourceType
    {
        OfficialFiling = 1,
        CompanyWebsite = 2,
        FinancialAudit = 3,
        LinkedIn = 4,
        ExternalDatabase = 5,
        ExecutiveInput = 6,
        SimulatedObservation = 7
    }

    public enum ClaimPromotionStatus
    {
        RawObservation = 1,
        Corroborated = 2,
        PromotedToTruthMetric = 3,
        Revoked = 4
    }

    public class EvidenceSourceNode
    {
        public Guid SourceId { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public EvidenceGraphSourceType SourceType { get; set; }
        public double ReliabilityScore { get; set; } = 0.8; // 0.0 to 1.0
        public DateTime ObservedAtUtc { get; set; } = DateTime.UtcNow;
        public bool IsRevoked { get; set; } = false;
        public string? RevocationReason { get; set; }
    }

    public class EvidenceClaim
    {
        public Guid ClaimId { get; set; } = Guid.NewGuid();
        public string Subject { get; set; } = string.Empty;
        public string Property { get; set; } = string.Empty;
        public string ClaimedValue { get; set; } = string.Empty;
        public List<Guid> CorroboratingSourceIds { get; set; } = new();
        public List<Guid> ContradictingSourceIds { get; set; } = new();
        public double ConfidenceScore { get; set; } = 0.0;
        public ClaimPromotionStatus PromotionStatus { get; set; } = ClaimPromotionStatus.RawObservation;
        public Guid? PromotedTruthMetricId { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime LastEvaluatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public interface IEvidenceGraph
    {
        EvidenceSourceNode AddSource(string name, EvidenceGraphSourceType type, double reliability);
        EvidenceClaim AddClaim(string subject, string property, string claimedValue);
        void LinkSourceToClaim(Guid claimId, Guid sourceId, bool isCorroborating);
        double RecalculateClaimConfidence(Guid claimId);
        TruthMetric<T> PromoteToTruthMetric<T>(Guid claimId, T typedValue);
        void RevokeSource(Guid sourceId, string reason);
        EvidenceClaim? GetClaim(Guid claimId);
        EvidenceSourceNode? GetSource(Guid sourceId);
    }
}
