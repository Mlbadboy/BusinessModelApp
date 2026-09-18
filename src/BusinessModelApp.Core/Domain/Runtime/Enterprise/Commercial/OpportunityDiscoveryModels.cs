using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Commercial
{
    public class MarketSignalItem
    {
        public string SignalId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty; // e.g., "WebAudit", "JobPostings", "NewsAPI", "SEC_Filing"
        public string RawContent { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public string Geography { get; set; } = string.Empty;
        public decimal FreshnessScore { get; set; } = 1.0m; // 0.0 (stale) to 1.0 (fresh)
        public DateTime DetectedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public class ICPProfile
    {
        public string ProfileId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string ProfileName { get; set; } = string.Empty;
        public List<string> TargetIndustries { get; set; } = new();
        public List<string> TargetGeographies { get; set; } = new();
        public int MinEmployees { get; set; } = 10;
        public int MaxEmployees { get; set; } = 5000;
        public decimal MinTargetRevenueINR { get; set; } = 500000m;
        public List<string> RequiredKeywords { get; set; } = new();
        public List<string> NegativeKeywords { get; set; } = new();
    }

    public class GroundedOpportunity
    {
        public string OpportunityId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public List<string> CorroboratedEvidenceIds { get; set; } = new();
        public string IdentifiedProblem { get; set; } = string.Empty;
        public decimal QuantifiedBusinessImpactINR { get; set; } = 0m;
        public decimal ICPScore { get; set; } = 0m; // 0.0 to 1.0
        public decimal CommercialFitScore { get; set; } = 0m; // 0.0 to 1.0
        public decimal EstimatedDealValueINR { get; set; } = 0m;
        public decimal RiskScore { get; set; } = 0m; // 0.0 (low) to 1.0 (high)
        public decimal ConfidenceScore { get; set; } = 0m; // 0.0 to 1.0
        public string RecommendedNextAction { get; set; } = string.Empty;
        public string EvaluationExplanation { get; set; } = string.Empty;
        public bool IsGrounded => CorroboratedEvidenceIds != null && CorroboratedEvidenceIds.Count > 0;
        public DateTime DiscoveredAtUtc { get; set; } = DateTime.UtcNow;
    }
}
