using System;

namespace BusinessModelApp.Core.Domain.Reality
{
    /// <summary>
    /// Authoritative observed commercial state certified by the Reality Engine.
    /// Charlie can "desire" a lead to be qualified; Reality determines whether it actually is.
    /// </summary>
    public class ObservedCommercialState
    {
        public bool DomainVerified { get; set; } = false;
        public bool IdentityConfirmed { get; set; } = false;
        public bool CorporateEmailVerified { get; set; } = false;
        public bool RegistryVerified { get; set; } = false;
        public bool BudgetConfirmed { get; set; } = false;
        public bool AuthorityVerified { get; set; } = false;
        public bool PaymentSettled { get; set; } = false;

        public string? RealityVerificationRunId { get; set; }
        public string? GroundingEvidenceHash { get; set; }
        public DateTime? LastVerifiedAt { get; set; }
        public VerificationStatus OverallVerificationStatus { get; set; } = VerificationStatus.Unverified;
        public string VerificationSummary { get; set; } = "Unverified - Pending authoritative reality observation.";
    }

    /// <summary>
    /// Authoritative observed delivery state certified by the Reality Engine.
    /// </summary>
    public class ObservedDeliveryState
    {
        public bool GitCommitVerified { get; set; } = false;
        public string? VerifiedCommitSha { get; set; }
        public bool BuildPassed { get; set; } = false;
        public int VerifiedPassedTests { get; set; } = 0;
        public int VerifiedFailedTests { get; set; } = 0;
        public bool SecurityScanClean { get; set; } = false;
        public bool DeploymentLive { get; set; } = false;
        public string? VerifiedEndpointUrl { get; set; }
        public bool ClientAcceptanceSigned { get; set; } = false;
        public string? ClientSignatureToken { get; set; }

        public string? RealityVerificationRunId { get; set; }
        public string? GroundingEvidenceHash { get; set; }
        public DateTime? LastVerifiedAt { get; set; }
        public VerificationStatus OverallVerificationStatus { get; set; } = VerificationStatus.Unverified;
        public string VerificationSummary { get; set; } = "Unverified - Pending CI or UAT confirmation.";
    }
}
