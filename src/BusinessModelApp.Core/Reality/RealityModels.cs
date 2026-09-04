using System;
using System.Collections.Generic;
using BusinessModelApp.Core.Domain.Reality;

namespace BusinessModelApp.Core.Reality
{
    public enum RealityVerificationType
    {
        CompanyLegitimacy = 1,
        CorporateEmailDomain = 2,
        FinancialSettlement = 3,
        SoftwareBuildTests = 4,
        ClientAcceptanceUAT = 5
    }

    public class RealityVerificationResult
    {
        public bool IsVerified { get; set; } = false;
        public VerificationStatus Status { get; set; } = VerificationStatus.Unverified;
        public RealityVerificationType VerificationType { get; set; }

        public string VerificationRunId { get; set; } = Guid.NewGuid().ToString("N");
        public string SubjectIdentifier { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;

        // Composable Verification Evidence Proofs
        public bool RegistryProofVerified { get; set; } = false;
        public bool DomainDnsProofVerified { get; set; } = false;
        public bool RegulatoryFilingProofVerified { get; set; } = false;
        public bool WebhookSignatureProofVerified { get; set; } = false;
        public double CrossSourceConsistencyScore { get; set; } = 0.0;

        // Hash Lineage
        public string RawPayloadHash { get; set; } = string.Empty;
        public string CanonicalPayloadHash { get; set; } = string.Empty;
        public string VerificationSummary { get; set; } = string.Empty;
        public string? FailureReason { get; set; }

        // Associated Evidence Record if verified
        public EvidenceRecord? GeneratedEvidence { get; set; }
        public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;
    }
}
