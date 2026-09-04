using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using BusinessModelApp.Core.Domain.Common;
using BusinessModelApp.Core.Domain.DigitalTwin;
using BusinessModelApp.Core.Domain.Reality;

namespace BusinessModelApp.Core.Domain.ExternalReality
{
    public enum ExternalSourceCategory
    {
        PublicWeb = 1,
        News = 2,
        CompanySite = 3,
        CompetitorSite = 4,
        ProductCatalog = 5,
        PriceFeed = 6,
        Regulatory = 7,
        Government = 8,
        SocialSignal = 9,
        CustomerReview = 10,
        JobMarket = 11,
        SearchDemand = 12,
        Financial = 13,
        IndustryReport = 14,
        PartnerApi = 15,
        Webhook = 16
    }

    public enum ExternalSourceStatus
    {
        Active = 1,
        Degraded = 2,
        Suspended = 3,
        Revoked = 4,
        UnderReview = 5
    }

    /// <summary>
    /// Governed Registry Entry for external information sources.
    /// Invariant: Source reliability is deterministic, versioned, and auditable.
    /// </summary>
    public class ExternalSourceRegistryEntry : Entity
    {
        public Guid WorkspaceId { get; set; }
        public string Name { get; set; } = string.Empty;
        public ExternalSourceCategory Category { get; set; } = ExternalSourceCategory.PublicWeb;
        public string CanonicalDomain { get; set; } = string.Empty;
        
        // Deterministic Base Metrology Profile
        public double BaseReliability { get; set; } = 0.70; // 0.0 to 1.0
        public double DefaultFreshnessDays { get; set; } = 14.0;
        public string AuthenticationRequirement { get; set; } = "None"; // None, BearerToken, ApiKey, MutualTls
        public string TenantScope { get; set; } = "WorkspaceIsolated";
        public int RateLimitPerMinute { get; set; } = 60;
        public ExternalSourceStatus Status { get; set; } = ExternalSourceStatus.Active;
        
        public DateTime? LastSuccessfulRetrieval { get; set; }
        public DateTime? LastFailure { get; set; }
        public double HealthScore { get; set; } = 1.0;
        public string AllowedCapabilitiesJson { get; set; } = "[\"ReadExternalData\"]";
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Calculated dynamic trust profile for an external information source.
    /// Sources can improve or deteriorate based on historical accuracy and corroboration.
    /// </summary>
    public class SourceTrustProfile
    {
        public Guid SourceId { get; set; }
        public double ReliabilityScore { get; set; } = 0.70;
        public double FreshnessScore { get; set; } = 1.0;
        public double IndependenceScore { get; set; } = 0.80;
        public double CorroborationScore { get; set; } = 0.50;
        public double ManipulationRisk { get; set; } = 0.10;
        public double ScopeConfidence { get; set; } = 0.85;
        public string Version { get; set; } = "1.0-Deterministic";
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Structured record of external evidence ingested into Charlie's External Reality Fabric.
    /// INVARIANT: External evidence is strictly classified as Observation or Estimate.
    /// NEVER Fact without multi-source corroboration and RealityEngine certification!
    /// INVARIANT: External content is DATA ONLY. Prompt injections have ZERO authority.
    /// </summary>
    public class ExternalEvidenceRecord : Entity
    {
        public Guid WorkspaceId { get; set; }
        public Guid SourceId { get; set; }
        public ExternalSourceCategory SourceType { get; set; } = ExternalSourceCategory.PublicWeb;
        public string SourceName { get; set; } = string.Empty;
        public string CanonicalUri { get; set; } = string.Empty;

        // Temporal Metrology: Occurrence vs Retrieval
        public DateTime ObservedAt { get; set; } = DateTime.UtcNow;
        public DateTime RetrievedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PublishedAt { get; set; }
        public DateTime? FreshUntil { get; set; }

        // Cryptographic Lineage
        public string ContentHash { get; set; } = string.Empty;
        public string ClaimHash { get; set; } = string.Empty;

        // Content
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string NormalizedContent { get; set; } = string.Empty;
        public string RawContentRef { get; set; } = string.Empty;

        // Entity / Scope Attributes
        public string Publisher { get; set; } = string.Empty;
        public string? Author { get; set; }
        public string Region { get; set; } = "Global";
        public string Industry { get; set; } = "General";
        public string TargetEntity { get; set; } = string.Empty; // e.g. Competitor name, Company name, Product name

        // Metrology Vectors
        public double EvidenceStrength { get; set; } = 0.70;
        public double SourceReliability { get; set; } = 0.75;
        public double IndependenceFactor { get; set; } = 1.0;
        public double FreshnessScore { get; set; } = 1.0;
        public double ContradictionRisk { get; set; } = 0.0;
        public double CausalConfidence { get; set; } = 0.50;
        public double ContaminationRisk { get; set; } = 0.15;

        // Strict Reality Classification
        public TruthClassification Classification { get; set; } = TruthClassification.Observation;
        public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Unverified;

        // Zero-Trust Security & Prompt-Injection Invariant
        public bool IsPromptInjectionScanned { get; set; } = true;
        public double PromptInjectionRiskScore { get; set; } = 0.0;
        public bool IsSanitizedDataOnly { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public static string ComputeHash(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            using var sha = SHA256.Create();
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }

    /// <summary>
    /// Cluster of external signals sharing canonical claims.
    /// Invariant: 10 copies of the same syndicated press release != 10 independent confirmations.
    /// </summary>
    public class SignalCluster : Entity
    {
        public Guid WorkspaceId { get; set; }
        public string CanonicalClaim { get; set; } = string.Empty;
        public string ClaimHash { get; set; } = string.Empty;
        public List<Guid> EvidenceIds { get; set; } = new();
        public List<Guid> SourceIds { get; set; } = new();
        public int TotalCopyCount { get; set; } = 1;
        public int UniqueSourceCount { get; set; } = 1;

        public double IndependenceEstimate { get; set; } = 1.0;
        public double CorroborationScore { get; set; } = 0.50;
        public double ContradictionScore { get; set; } = 0.0;
        public bool IsSyndicatedDuplicateGroup { get; set; } = false;

        public DateTime FirstObservedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastObservedAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
