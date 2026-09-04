using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using BusinessModelApp.Core.Domain.Objectives;
using BusinessModelApp.Core.Domain.Reality;
using BusinessModelApp.Core.Domain.WorldModel;

namespace BusinessModelApp.Infrastructure.Reality
{
    public class EvidenceGraphService : IEvidenceGraph
    {
        private readonly ConcurrentDictionary<Guid, EvidenceSourceNode> _sources = new();
        private readonly ConcurrentDictionary<Guid, EvidenceClaim> _claims = new();

        public EvidenceSourceNode AddSource(string name, EvidenceGraphSourceType type, double reliability)
        {
            var source = new EvidenceSourceNode
            {
                SourceId = Guid.NewGuid(),
                Name = name,
                SourceType = type,
                ReliabilityScore = Math.Clamp(reliability, 0.0, 1.0),
                ObservedAtUtc = DateTime.UtcNow,
                IsRevoked = false
            };

            _sources[source.SourceId] = source;
            return source;
        }

        public EvidenceClaim AddClaim(string subject, string property, string claimedValue)
        {
            var claim = new EvidenceClaim
            {
                ClaimId = Guid.NewGuid(),
                Subject = subject,
                Property = property,
                ClaimedValue = claimedValue,
                ConfidenceScore = 0.0,
                PromotionStatus = ClaimPromotionStatus.RawObservation,
                CreatedAtUtc = DateTime.UtcNow,
                LastEvaluatedAtUtc = DateTime.UtcNow
            };

            _claims[claim.ClaimId] = claim;
            return claim;
        }

        public void LinkSourceToClaim(Guid claimId, Guid sourceId, bool isCorroborating)
        {
            if (!_claims.TryGetValue(claimId, out var claim))
            {
                throw new KeyNotFoundException($"Claim {claimId} not found");
            }
            if (!_sources.ContainsKey(sourceId))
            {
                throw new KeyNotFoundException($"Source {sourceId} not found");
            }

            if (isCorroborating)
            {
                if (!claim.CorroboratingSourceIds.Contains(sourceId))
                    claim.CorroboratingSourceIds.Add(sourceId);
            }
            else
            {
                if (!claim.ContradictingSourceIds.Contains(sourceId))
                    claim.ContradictingSourceIds.Add(sourceId);
            }

            RecalculateClaimConfidence(claimId);
        }

        public double RecalculateClaimConfidence(Guid claimId)
        {
            if (!_claims.TryGetValue(claimId, out var claim))
            {
                throw new KeyNotFoundException($"Claim {claimId} not found");
            }

            var activeCorroborating = claim.CorroboratingSourceIds
                .Select(id => _sources.TryGetValue(id, out var s) ? s : null)
                .Where(s => s != null && !s.IsRevoked)
                .ToList();

            var activeContradicting = claim.ContradictingSourceIds
                .Select(id => _sources.TryGetValue(id, out var s) ? s : null)
                .Where(s => s != null && !s.IsRevoked)
                .ToList();

            if (!activeCorroborating.Any())
            {
                claim.ConfidenceScore = 0.0;
                if (claim.PromotionStatus == ClaimPromotionStatus.PromotedToTruthMetric)
                {
                    claim.PromotionStatus = ClaimPromotionStatus.Revoked;
                }
                claim.LastEvaluatedAtUtc = DateTime.UtcNow;
                return 0.0;
            }

            // Corroboration formula: 1 - Product(1 - r_i)
            double positiveScore = 1.0 - activeCorroborating.Aggregate(1.0, (acc, s) => acc * (1.0 - s!.ReliabilityScore));

            // Contradiction penalty
            double contradictionPenalty = activeContradicting.Sum(s => s!.ReliabilityScore * 0.4);

            double finalScore = Math.Clamp(positiveScore - contradictionPenalty, 0.0, 1.0);
            claim.ConfidenceScore = Math.Round(finalScore, 4);

            if (claim.ConfidenceScore >= 0.70 && claim.PromotionStatus == ClaimPromotionStatus.RawObservation)
            {
                claim.PromotionStatus = ClaimPromotionStatus.Corroborated;
            }
            else if (claim.ConfidenceScore < 0.60 && claim.PromotionStatus == ClaimPromotionStatus.PromotedToTruthMetric)
            {
                claim.PromotionStatus = ClaimPromotionStatus.Revoked;
            }

            claim.LastEvaluatedAtUtc = DateTime.UtcNow;
            return claim.ConfidenceScore;
        }

        public TruthMetric<T> PromoteToTruthMetric<T>(Guid claimId, T typedValue)
        {
            if (!_claims.TryGetValue(claimId, out var claim))
            {
                throw new KeyNotFoundException($"Claim {claimId} not found");
            }

            // Invariant: Cannot promote to TruthMetric if confidence < 0.70
            if (claim.ConfidenceScore < 0.70)
            {
                throw new InvalidOperationException($"Cannot promote claim {claimId} to TruthMetric: Confidence {claim.ConfidenceScore} is below threshold 0.70");
            }

            var truthMetricId = Guid.NewGuid();
            claim.PromotedTruthMetricId = truthMetricId;
            claim.PromotionStatus = ClaimPromotionStatus.PromotedToTruthMetric;

            return new TruthMetric<T>
            {
                Value = typedValue,
                Confidence = claim.ConfidenceScore,
                Provenance = MetricProvenanceSource.VERIFIED_FACT,
                VerificationStatus = VerificationStatus.VerifiedFact,
                EvidenceRecordIds = new List<Guid>(claim.CorroboratingSourceIds),
                ObservedAt = DateTime.UtcNow,
                EvidenceCoverage = 1.0
            };
        }

        public void RevokeSource(Guid sourceId, string reason)
        {
            if (!_sources.TryGetValue(sourceId, out var source))
            {
                throw new KeyNotFoundException($"Source {sourceId} not found");
            }

            source.IsRevoked = true;
            source.RevocationReason = reason;

            // Recalculate all claims associated with this source
            var impactedClaims = _claims.Values
                .Where(c => c.CorroboratingSourceIds.Contains(sourceId) || c.ContradictingSourceIds.Contains(sourceId))
                .ToList();

            foreach (var claim in impactedClaims)
            {
                RecalculateClaimConfidence(claim.ClaimId);
            }
        }

        public EvidenceClaim? GetClaim(Guid claimId)
        {
            _claims.TryGetValue(claimId, out var claim);
            return claim;
        }

        public EvidenceSourceNode? GetSource(Guid sourceId)
        {
            _sources.TryGetValue(sourceId, out var source);
            return source;
        }
    }
}
