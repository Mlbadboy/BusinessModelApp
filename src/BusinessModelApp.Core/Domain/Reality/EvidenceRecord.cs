using System;
using BusinessModelApp.Core.Domain.Common;

namespace BusinessModelApp.Core.Domain.Reality
{
    /// <summary>
    /// Immutable record of external verifiable evidence.
    /// Ground-truth rule: NO EVIDENCE => NO FACT.
    /// Distinguishes between when external reality occurred (ObservedAt) and when collected (RetrievedAt).
    /// </summary>
    public class EvidenceRecord : Entity
    {
        public Guid WorkspaceId { get; set; }
        
        // 3-Class Information Hierarchy: Fact vs DecisionHypothesis vs Intention
        public EvidenceClass Class { get; set; } = EvidenceClass.Fact;
        public EvidenceSourceType SourceType { get; set; }

        // External System Grounding
        public string SourceSystem { get; set; } = string.Empty;     // e.g. "MCA Official Registry", "Razorpay Webhook", "GitHub API"
        public string SourceIdentifier { get; set; } = string.Empty; // e.g. "CIN: U72200MH...", "pay_29x8f", "commit:7f2b9a"
        public string SourceUri { get; set; } = string.Empty;        // e.g. "https://mca.gov.in/...", "https://api.github.com/..."

        // Temporal Separation: Event Occurrence vs Charlie Ingestion
        public DateTime ObservedAt { get; set; }                     // When the external real-world event occurred
        public DateTime RetrievedAt { get; set; } = DateTime.UtcNow; // When Charlie retrieved and verified the evidence
        public DateTime? FreshUntil { get; set; }                   // Expiration/staleness boundary (e.g. DNS cache TTL, MCA filing period)

        // Cryptographic Lineage & Normalization
        public string RawPayloadHash { get; set; } = string.Empty;       // SHA-256 hash of verbatim external payload
        public string CanonicalPayloadHash { get; set; } = string.Empty; // SHA-256 hash of normalized representation
        public string EvidenceDigest { get; set; } = string.Empty;       // Human/Agent readable summary digest
        public string? PreviousEvidenceHash { get; set; }                // Cryptographic blockchain-style hash chain

        // Verification Governance
        public string VerificationMethod { get; set; } = string.Empty;   // e.g. "DirectWebhookDigest", "PublicRegistryAPI", "DnsMxResolution"
        public string VerificationProvider { get; set; } = "RealityEngine:v5";
        public string VerificationRunId { get; set; } = string.Empty;
        public VerificationStatus Status { get; set; } = VerificationStatus.Unverified;
        public double Confidence { get; set; } = 1.0;                    // Supporting confidence metric (never substitutes for external proof)

        // Binding to Business Graph
        public Guid ParentEntityId { get; set; }
        public string ParentEntityType { get; set; } = string.Empty;     // e.g. "Company", "Lead", "Opportunity", "ProjectTask", "Payment"

        // Versioning & Metadata
        public string EvidenceVersion { get; set; } = "1.0";
        public string MetadataJson { get; set; } = "{}";
    }
}
