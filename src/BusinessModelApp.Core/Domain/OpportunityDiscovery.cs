using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Commercial
{
    public class DiscoveredBusiness
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid WorkspaceId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // e.g. Real Estate Developer, Dental Clinic, PropTech
        public string City { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string WebsiteUrl { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public double GoogleRating { get; set; }
        public int ReviewCount { get; set; }
        public string GooglePlacesId { get; set; } = string.Empty;
        public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;
    }

    public class WebsiteAuditReport
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid DiscoveredBusinessId { get; set; }
        public int OverallScore { get; set; } // 0 - 100
        public int MobileUXScore { get; set; } // 0 - 100
        public int PerformanceScore { get; set; } // 0 - 100
        public int SEOScore { get; set; } // 0 - 100
        public bool HasModernFunnel { get; set; }
        public bool HasWhatsAppLeadCapture { get; set; }
        public bool HasSSL { get; set; }
        public List<string> DetectedTechnologies { get; set; } = new();
        public List<string> CriticalPainPoints { get; set; } = new();
        public string AuditSummary { get; set; } = string.Empty;
        public DateTime AuditedAt { get; set; } = DateTime.UtcNow;
    }

    public enum OpportunityStatus
    {
        Discovered,
        HypothesisFormulated,
        OutreachReady,
        Engaged,
        Proposed,
        Won,
        Archived
    }

    public class OpportunityHypothesis
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid WorkspaceId { get; set; }
        public Guid DiscoveredBusinessId { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public string HypothesisTitle { get; set; } = string.Empty; // e.g. "Modern Lead-Gen Funnel & WhatsApp CRM Automation"
        public string ProblemDescription { get; set; } = string.Empty;
        public string ProposedSolution { get; set; } = string.Empty;
        public string CommercialPackageName { get; set; } = string.Empty;
        public decimal EstimatedValueINR { get; set; }
        public int OpportunityScore { get; set; } // 0 - 100
        public string EvidenceKey { get; set; } = string.Empty; // e.g. EVD-OPP-9231
        public OpportunityStatus Status { get; set; } = OpportunityStatus.Discovered;
        public DateTime FormulatedAt { get; set; } = DateTime.UtcNow;
    }
}
