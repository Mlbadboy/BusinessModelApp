using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Commercial
{
    public enum BuyingCenterPersonaRole
    {
        UNKNOWN = 0,
        ECONOMIC_BUYER = 1,
        DECISION_MAKER = 2,
        TECHNICAL_BUYER = 3,
        CHAMPION = 4,
        INFLUENCER = 5,
        PROCUREMENT = 6
    }

    public class BuyingCenterContact
    {
        public string ContactId { get; set; } = Guid.NewGuid().ToString("N");
        public string FullName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? LinkedInUrl { get; set; }
        public BuyingCenterPersonaRole Role { get; set; } = BuyingCenterPersonaRole.UNKNOWN;
        public string AuthoritativeEvidenceId { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public DateTime ObservedAtUtc { get; set; } = DateTime.UtcNow;
        public decimal FreshnessDays => (decimal)(DateTime.UtcNow - ObservedAtUtc).TotalDays;
        public decimal ConfidenceScore { get; set; } = 0.5m;
        public bool IsVerified => !string.IsNullOrWhiteSpace(AuthoritativeEvidenceId) && ConfidenceScore >= 0.7m;
    }

    public class EnterpriseAccountGraph
    {
        public string AccountId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public string Geography { get; set; } = string.Empty;
        public int EstimatedEmployees { get; set; } = 0;
        public decimal EstimatedAnnualRevenueINR { get; set; } = 0m;
        public List<string> TechStack { get; set; } = new();
        public List<string> BusinessSignals { get; set; } = new();
        public List<string> IdentifiedPainPoints { get; set; } = new();
        public List<BuyingCenterContact> BuyingCenter { get; set; } = new();
        public DateTime LastAuditedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
