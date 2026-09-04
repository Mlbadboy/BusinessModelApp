using System;
using System.Collections.Generic;
using System.Linq;
using BusinessModelApp.Core.Domain.Objectives;
using BusinessModelApp.Core.Domain.Reality;

namespace BusinessModelApp.Core.Domain.WorldModel
{
    /// <summary>
    /// An evidence-grounded atomic metric within Charlie's Company World Model.
    /// Every truth metric carries explicit provenance, verification status, and cryptographic hashes.
    /// 
    /// CRITICAL INVARIANT (Phase 1 v1.2):
    /// Agent memory is NOT truth.
    /// An agent's working memory or hypothesis cannot become VERIFIED_FACT unless supported by external evidence.
    /// Chain: Agent Memory -> Hypothesis -> Evidence -> TruthMetric -> Verified Company World Model.
    /// </summary>
    public class TruthMetric<T>
    {
        public T Value { get; set; } = default!;
        
        // Provenance specification
        public MetricProvenanceSource Source { get; set; } = MetricProvenanceSource.Unknown;
        public MetricProvenanceSource Provenance
        {
            get => Source;
            set => Source = value;
        }

        public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Unverified;
        
        public List<Guid> EvidenceRecordIds { get; set; } = new();
        public Guid? EvidenceRecordId
        {
            get => EvidenceRecordIds.FirstOrDefault();
            set
            {
                if (value.HasValue)
                {
                    if (!EvidenceRecordIds.Contains(value.Value))
                    {
                        EvidenceRecordIds.Insert(0, value.Value);
                    }
                }
            }
        }

        public List<string> GroundingEvidenceHashes { get; set; } = new();
        public double EvidenceCoverage { get; set; } = 0.0; // 0.0 = completely ungrounded, 1.0 = fully certified
        
        public DateTime ObservedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastVerifiedAt
        {
            get => ObservedAt;
            set { if (value.HasValue) ObservedAt = value.Value; }
        }

        public DateTime? FreshUntil { get; set; }
        
        public double Confidence { get; set; } = 0.0;
        public double ConfidenceScore
        {
            get => Confidence;
            set => Confidence = value;
        }

        public string Note { get; set; } = string.Empty;

        public bool IsGroundedFact => 
            (Source == MetricProvenanceSource.VerifiedFact || Source == MetricProvenanceSource.HistoricalCompanyData) &&
            VerificationStatus == VerificationStatus.VerifiedFact &&
            EvidenceCoverage > 0.0;

        public static TruthMetric<T> Grounded(T value, Guid evidenceId, string evidenceHash, double confidence = 1.0, string note = "Certified by Reality Engine.")
        {
            return new TruthMetric<T>
            {
                Value = value,
                Source = MetricProvenanceSource.VerifiedFact,
                VerificationStatus = VerificationStatus.VerifiedFact,
                EvidenceRecordIds = new List<Guid> { evidenceId },
                GroundingEvidenceHashes = new List<string> { evidenceHash },
                EvidenceCoverage = 1.0,
                ObservedAt = DateTime.UtcNow,
                Confidence = confidence,
                Note = note
            };
        }

        public static TruthMetric<T> Historical(T value, Guid evidenceId, string evidenceHash, double confidence = 0.95, string note = "Derived from historical company records.")
        {
            return new TruthMetric<T>
            {
                Value = value,
                Source = MetricProvenanceSource.HistoricalCompanyData,
                VerificationStatus = VerificationStatus.VerifiedFact,
                EvidenceRecordIds = new List<Guid> { evidenceId },
                GroundingEvidenceHashes = new List<string> { evidenceHash },
                EvidenceCoverage = 1.0,
                ObservedAt = DateTime.UtcNow,
                Confidence = confidence,
                Note = note
            };
        }

        public static TruthMetric<T> CeoInput(T value, string explanation, double confidence = 1.0)
        {
            return new TruthMetric<T>
            {
                Value = value,
                Source = MetricProvenanceSource.ExplicitCeoInput,
                VerificationStatus = VerificationStatus.VerifiedFact,
                EvidenceCoverage = 0.5,
                ObservedAt = DateTime.UtcNow,
                Confidence = confidence,
                Note = explanation
            };
        }

        public static TruthMetric<T> Estimated(T value, string explanation, double confidence = 0.5)
        {
            return new TruthMetric<T>
            {
                Value = value,
                Source = MetricProvenanceSource.AiEstimate,
                VerificationStatus = VerificationStatus.HypothesisOnly,
                EvidenceCoverage = 0.0,
                ObservedAt = DateTime.UtcNow,
                Confidence = confidence,
                Note = explanation
            };
        }

        public static TruthMetric<T> Unavailable(string explanation)
        {
            return new TruthMetric<T>
            {
                Value = default!,
                Source = MetricProvenanceSource.Unknown,
                VerificationStatus = VerificationStatus.Unverified,
                EvidenceCoverage = 0.0,
                ObservedAt = DateTime.UtcNow,
                Confidence = 0.0,
                Note = explanation
            };
        }
    }
}
