using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BusinessModelApp.Core.Domain.Common;
using BusinessModelApp.Core.Domain.Reality;

namespace BusinessModelApp.Core.Domain.DigitalTwin
{
    /// <summary>
    /// Strict Reality Classification for the Company Digital Twin.
    /// Categories are mutually distinct and non-interchangeable.
    /// </summary>
    public enum TruthClassification
    {
        Fact = 1,         // Grounded in verified external evidence, cryptographic hashes, or reconciled ledgers
        Estimate = 2,     // Statistical projections, formulaic approximations, industry benchmark defaults
        Hypothesis = 3,   // Strategic scenario assumptions, AI simulations, ungrounded proposals
        Observation = 4,  // Raw external telemetry, connector feeds prior to corroboration
        Learning = 5,     // Institutional memory, historical playbooks, heuristic guidelines
        Unknown = 6       // Insufficient or absent evidence (First-class state: NEVER fake zero or synthetic)
    }

    /// <summary>
    /// The 22 canonical enterprise dimensions of Charlie's Company Digital Twin.
    /// </summary>
    public enum DigitalTwinDimension
    {
        Financial = 1,
        Revenue = 2,
        Commercial = 3,
        Customers = 4,
        Prospects = 5,
        Opportunities = 6,
        Products = 7,
        Services = 8,
        Employees = 9,
        Departments = 10,
        Sales = 11,
        Marketing = 12,
        Operations = 13,
        Delivery = 14,
        Inventory = 15,
        Suppliers = 16,
        Partners = 17,
        Contracts = 18,
        Connectors = 19,
        MarketSignals = 20,
        Risks = 21,
        StrategicState = 22
    }

    public enum ConflictResolutionStatus
    {
        Unresolved = 1,
        Resolved = 2,
        Superseded = 3
    }

    /// <summary>
    /// Conflict representation when multiple data sources report divergent values for the same property.
    /// Charlie NEVER arbitrarily resolves or silently collapses conflicts.
    /// </summary>
    public class DigitalTwinConflictRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid WorkspaceId { get; set; }
        public string FieldPath { get; set; } = string.Empty;
        public string SourceA { get; set; } = string.Empty;
        public string ValueA { get; set; } = string.Empty;
        public double ConfidenceA { get; set; }
        public DateTime ObservedAtA { get; set; } = DateTime.UtcNow;

        public string SourceB { get; set; } = string.Empty;
        public string ValueB { get; set; } = string.Empty;
        public double ConfidenceB { get; set; }
        public DateTime ObservedAtB { get; set; } = DateTime.UtcNow;

        public ConflictResolutionStatus Status { get; set; } = ConflictResolutionStatus.Unresolved;
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
        public string? ResolutionNote { get; set; }
        public string? ResolvedBy { get; set; }
    }

    /// <summary>
    /// Field-level state representation in the Digital Twin carrying complete metrology and provenance.
    /// </summary>
    public class DigitalTwinFieldState
    {
        public string FieldPath { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public DigitalTwinDimension Dimension { get; set; }
        public string DisplayValue { get; set; } = "UNKNOWN";
        public string? RawValueJson { get; set; }
        public TruthClassification Classification { get; set; } = TruthClassification.Unknown;
        public double Confidence { get; set; } = 0.0;
        public List<Guid> EvidenceRecordIds { get; set; } = new();
        public List<string> GroundingEvidenceHashes { get; set; } = new();
        public string Source { get; set; } = "Unknown";
        public DateTime ObservedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastVerifiedAt { get; set; }
        public FreshnessState Freshness { get; set; } = FreshnessState.UNKNOWN;
        public TimeSpan EvidenceAge => DateTime.UtcNow > ObservedAt ? DateTime.UtcNow - ObservedAt : TimeSpan.Zero;
        public bool IsDisputed { get; set; } = false;
        public Guid? ActiveConflictId { get; set; }
        public string Note { get; set; } = string.Empty;

        public bool IsGroundedFact =>
            Classification == TruthClassification.Fact &&
            EvidenceRecordIds.Count > 0 &&
            Freshness == FreshnessState.VERIFIED;

        public static DigitalTwinFieldState Unknown(string fieldPath, string fieldName, DigitalTwinDimension dimension, string note = "No verified evidence available.")
        {
            return new DigitalTwinFieldState
            {
                FieldPath = fieldPath,
                FieldName = fieldName,
                Dimension = dimension,
                DisplayValue = "UNKNOWN",
                Classification = TruthClassification.Unknown,
                Confidence = 0.0,
                Source = "None",
                Freshness = FreshnessState.UNKNOWN,
                Note = note
            };
        }

        public static DigitalTwinFieldState Fact<T>(
            string fieldPath,
            string fieldName,
            DigitalTwinDimension dimension,
            T value,
            string displayValue,
            string source,
            Guid evidenceId,
            string evidenceHash,
            double confidence = 1.0,
            FreshnessState freshness = FreshnessState.VERIFIED,
            string note = "Certified by Reality Engine.")
        {
            return new DigitalTwinFieldState
            {
                FieldPath = fieldPath,
                FieldName = fieldName,
                Dimension = dimension,
                DisplayValue = displayValue,
                RawValueJson = JsonSerializer.Serialize(value),
                Classification = TruthClassification.Fact,
                Confidence = confidence,
                Source = source,
                EvidenceRecordIds = new List<Guid> { evidenceId },
                GroundingEvidenceHashes = new List<string> { evidenceHash },
                ObservedAt = DateTime.UtcNow,
                LastVerifiedAt = DateTime.UtcNow,
                Freshness = freshness,
                Note = note
            };
        }

        public static DigitalTwinFieldState Estimate<T>(
            string fieldPath,
            string fieldName,
            DigitalTwinDimension dimension,
            T value,
            string displayValue,
            string source,
            double confidence,
            string note)
        {
            return new DigitalTwinFieldState
            {
                FieldPath = fieldPath,
                FieldName = fieldName,
                Dimension = dimension,
                DisplayValue = displayValue,
                RawValueJson = JsonSerializer.Serialize(value),
                Classification = TruthClassification.Estimate,
                Confidence = confidence,
                Source = source,
                ObservedAt = DateTime.UtcNow,
                Freshness = FreshnessState.VERIFIED,
                Note = note
            };
        }

        public static DigitalTwinFieldState Hypothesis<T>(
            string fieldPath,
            string fieldName,
            DigitalTwinDimension dimension,
            T value,
            string displayValue,
            string source,
            string note)
        {
            return new DigitalTwinFieldState
            {
                FieldPath = fieldPath,
                FieldName = fieldName,
                Dimension = dimension,
                DisplayValue = displayValue,
                RawValueJson = JsonSerializer.Serialize(value),
                Classification = TruthClassification.Hypothesis,
                Confidence = 0.3,
                Source = source,
                ObservedAt = DateTime.UtcNow,
                Freshness = FreshnessState.VERIFIED,
                Note = note
            };
        }

        public static DigitalTwinFieldState Observation<T>(
            string fieldPath,
            string fieldName,
            DigitalTwinDimension dimension,
            T value,
            string displayValue,
            string source,
            double confidence,
            string note)
        {
            return new DigitalTwinFieldState
            {
                FieldPath = fieldPath,
                FieldName = fieldName,
                Dimension = dimension,
                DisplayValue = displayValue,
                RawValueJson = JsonSerializer.Serialize(value),
                Classification = TruthClassification.Observation,
                Confidence = confidence,
                Source = source,
                ObservedAt = DateTime.UtcNow,
                Freshness = FreshnessState.VERIFIED,
                Note = note
            };
        }

        public static DigitalTwinFieldState Learning<T>(
            string fieldPath,
            string fieldName,
            DigitalTwinDimension dimension,
            T value,
            string displayValue,
            string source,
            string note)
        {
            return new DigitalTwinFieldState
            {
                FieldPath = fieldPath,
                FieldName = fieldName,
                Dimension = dimension,
                DisplayValue = displayValue,
                RawValueJson = JsonSerializer.Serialize(value),
                Classification = TruthClassification.Learning,
                Confidence = 0.5,
                Source = source,
                ObservedAt = DateTime.UtcNow,
                Freshness = FreshnessState.VERIFIED,
                Note = note
            };
        }
    }

    /// <summary>
    /// Dimension state grouping fields belonging to one of the 22 business dimensions.
    /// </summary>
    public class DigitalTwinDimensionState
    {
        public DigitalTwinDimension Dimension { get; set; }
        public string DimensionName { get; set; } = string.Empty;
        public List<DigitalTwinFieldState> Fields { get; set; } = new();

        public int TotalFields => Fields.Count;
        public int KnownFieldsCount => Fields.Count(f => f.Classification != TruthClassification.Unknown);
        public int UnknownFieldsCount => Fields.Count(f => f.Classification == TruthClassification.Unknown);
        public int StaleFieldsCount => Fields.Count(f => f.Freshness == FreshnessState.STALE || f.Freshness == FreshnessState.UNKNOWN);
        public int DisputedFieldsCount => Fields.Count(f => f.IsDisputed);
        public double AverageConfidence => Fields.Any(f => f.Classification != TruthClassification.Unknown)
            ? Fields.Where(f => f.Classification != TruthClassification.Unknown).Average(f => f.Confidence)
            : 0.0;
    }

    /// <summary>
    /// Complete structured snapshot of the company Digital Twin at a specific instant.
    /// </summary>
    public class DigitalTwinState
    {
        public Guid WorkspaceId { get; set; }
        public DateTime AsOfUtc { get; set; } = DateTime.UtcNow;
        public Dictionary<DigitalTwinDimension, DigitalTwinDimensionState> Dimensions { get; set; } = new();
        public List<DigitalTwinConflictRecord> ActiveConflicts { get; set; } = new();
        public DigitalTwinHealthReport HealthReport { get; set; } = new();
        public int TotalTrackedFields => Dimensions.Values.Sum(d => d.TotalFields);
    }

    /// <summary>
    /// Governed point-in-time snapshot entity for the Company Digital Twin.
    /// Immutable, append-only, and cryptographically verified with IntegrityHash.
    /// </summary>
    public class DigitalTwinSnapshot : Entity
    {
        public Guid WorkspaceId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string SourceVersion { get; set; } = "1.0";
        public string RealityVersion { get; set; } = "1.0";
        public string EvidenceVersion { get; set; } = "1.0";
        public string TwinVersion { get; set; } = "2.0";
        public string IntegrityHash { get; set; } = string.Empty;
        public string SerializedStateJson { get; set; } = "{}";

        public static string ComputeIntegrityHash(Guid workspaceId, DateTime createdAt, string payloadJson)
        {
            using var sha = SHA256.Create();
            string raw = $"{workspaceId:N}|{createdAt:O}|{payloadJson}";
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }

    public class DigitalTwinSnapshotSummary
    {
        public Guid SnapshotId { get; set; }
        public Guid WorkspaceId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string IntegrityHash { get; set; } = string.Empty;
        public int TotalFields { get; set; }
        public double EvidenceCoverage { get; set; }
        public double FreshnessScore { get; set; }
    }

    public class DigitalTwinDiffItem
    {
        public string FieldPath { get; set; } = string.Empty;
        public string DiffType { get; set; } = string.Empty; // Added, Removed, Changed, BecameStale, BecameUnknown, ConfidenceIncreased, ConfidenceDecreased, ClassificationChanged, EvidenceChanged
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public TruthClassification? OldClassification { get; set; }
        public TruthClassification? NewClassification { get; set; }
        public double? OldConfidence { get; set; }
        public double? NewConfidence { get; set; }
        public string Details { get; set; } = string.Empty;
    }

    public class DigitalTwinDiff
    {
        public Guid SnapshotAId { get; set; }
        public Guid SnapshotBId { get; set; }
        public DateTime ComparedAtUtc { get; set; } = DateTime.UtcNow;
        public List<DigitalTwinDiffItem> Changes { get; set; } = new();
        public string Summary { get; set; } = string.Empty;
    }

    public class DigitalTwinHealthReport
    {
        public double EvidenceCoveragePercent { get; set; } // % of fields backed by empirical evidence
        public double FreshnessPercent { get; set; }        // % of fields in VERIFIED/Fresh state
        public double UnknownRatioPercent { get; set; }     // % of fields in Unknown state
        public double StaleRatioPercent { get; set; }       // % of fields in Stale or Expired state
        public double ConflictRatioPercent { get; set; }    // % of fields in Disputed / Conflict state
        public double AverageConfidence { get; set; }       // Mean confidence of known fields
        public int ActiveConnectorCount { get; set; }       // Telemetry integrations active
        public int TotalTrackedFields { get; set; }
    }

    /// <summary>
    /// Explicit invariants preventing illegitimate promotion between reality classifications.
    /// Invariant: ESTIMATE -> FACT, HYPOTHESIS -> FACT, LEARNING -> FACT, OBSERVATION -> FACT, UNKNOWN -> FACT
    /// are strictly forbidden without governed evidence-based promotion.
    /// </summary>
    public static class TruthClassificationPromotionGuard
    {
        public static void ValidatePromotion(
            TruthClassification from,
            TruthClassification to,
            double confidence,
            bool hasEvidence,
            bool isCorroborated,
            bool isStale = false,
            bool isDirectAiPromotion = false)
        {
            if (isDirectAiPromotion)
            {
                throw new InvalidOperationException("AI cannot directly create or promote any metric to FACT. All facts require external governed evidence.");
            }

            if (to == TruthClassification.Fact)
            {
                if (from == TruthClassification.Estimate)
                {
                    throw new InvalidOperationException("Invariant Violation: An ESTIMATE cannot be directly converted into a FACT without empirical evidence.");
                }
                if (from == TruthClassification.Hypothesis)
                {
                    throw new InvalidOperationException("Invariant Violation: A HYPOTHESIS cannot be directly promoted to FACT. Hypotheses require empirical testing and external evidence.");
                }
                if (from == TruthClassification.Learning)
                {
                    throw new InvalidOperationException("Invariant Violation: A LEARNING heuristic cannot be promoted to FACT. Learnings inform strategy but do not constitute current business truth.");
                }
                if (from == TruthClassification.Unknown)
                {
                    throw new InvalidOperationException("Invariant Violation: UNKNOWN cannot be directly promoted to FACT without discovery and verification of evidence.");
                }
                if (from == TruthClassification.Observation && !isCorroborated)
                {
                    throw new InvalidOperationException("Invariant Violation: An uncorroborated OBSERVATION cannot be promoted to FACT. It must be corroborated through the Evidence Graph.");
                }

                if (!hasEvidence)
                {
                    throw new InvalidOperationException("Invariant Violation: A metric cannot be classified as FACT without backing EvidenceRecord IDs and cryptographic hashes.");
                }

                if (confidence < 0.70)
                {
                    throw new InvalidOperationException($"Invariant Violation: Promotion to FACT requires a minimum confidence score of 0.70. Current confidence: {confidence:F2}.");
                }

                if (isStale)
                {
                    throw new InvalidOperationException("Invariant Violation: Stale or expired evidence cannot be promoted to or maintained as a current FACT.");
                }
            }
        }
    }
}
