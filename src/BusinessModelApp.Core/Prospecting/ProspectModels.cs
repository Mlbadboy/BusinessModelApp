using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Prospecting
{
    public enum SignalType
    {
        AITransformation = 1,
        RegulatoryCompliance = 2,
        CloudMigration = 3,
        ExecutiveHiring = 4,
        TechnologyModernization = 5,
        CostOptimization = 6
    }

    public class ProspectSignal
    {
        public string SignalId { get; set; } = $"SIG-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        public SignalType Type { get; set; } = SignalType.AITransformation;
        public string Headline { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
        public string EvidenceHash { get; set; } = string.Empty;
        public decimal Confidence { get; set; } = 0.90m;
    }

    public class DiscoveredCandidateAccount
    {
        public string CompanyName { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public string Geography { get; set; } = "India";
        public int Headcount { get; set; } = 1000;
        public decimal EstimatedAnnualRevenueINR { get; set; } = 500000000m;
        public List<ProspectSignal> Signals { get; set; } = new List<ProspectSignal>();
        public decimal ICPScore { get; set; } = 0.0m;
        public string EvidenceToken { get; set; } = string.Empty;
        public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;
    }

    public class CandidateDecisionMaker
    {
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ExecutivePersona { get; set; } = "Chief Digital Officer";
        public string CorporateEmail { get; set; } = string.Empty;
        public string LinkedInProfileUrl { get; set; } = string.Empty;
        public decimal AuthorityScore { get; set; } = 0.92m;
        public bool IsVerified { get; set; } = true;
        public string VerificationSource { get; set; } = "Corporate Domain & Executive Registry";
    }

    public class VerifiedProspectLead
    {
        public string ProvenanceToken { get; set; } = $"PRV-{Guid.NewGuid().ToString().Substring(0, 12).ToUpper()}";
        public DiscoveredCandidateAccount Account { get; set; } = new DiscoveredCandidateAccount();
        public CandidateDecisionMaker DecisionMaker { get; set; } = new CandidateDecisionMaker();
        public decimal ICPScore { get; set; }
        public decimal AIQualificationScore { get; set; }
        public decimal ConfidenceScore { get; set; } = 0.94m;
        public string QualificationRationale { get; set; } = string.Empty;
        public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;
    }

    public class ProspectHuntRequest
    {
        public string Industry { get; set; } = "Enterprise BFSI";
        public string Geography { get; set; } = "India";
        public int MinHeadcount { get; set; } = 500;
        public decimal TargetDealValueINR { get; set; } = 2500000m;
        public int MaxCandidates { get; set; } = 10;
    }

    public class ProspectHuntResult
    {
        public string HuntSessionId { get; set; } = $"HUNT-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        public int TotalCandidatesDiscovered { get; set; }
        public int QualifiedCandidateCount { get; set; }
        public List<DiscoveredCandidateAccount> CandidateAccounts { get; set; } = new List<DiscoveredCandidateAccount>();
        public List<VerifiedProspectLead> VerifiedLeads { get; set; } = new List<VerifiedProspectLead>();
    }
}
