using System;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessModelApp.Core.Reality
{
    /// <summary>
    /// The Authoritative Truth Engine for Charlie OS v5.
    /// Core Invariant 1: "Charlie may reason about reality, but only the Reality Engine may certify reality."
    /// Core Invariant 2: NO EVIDENCE => NO FACT.
    /// Core Invariant 3: NO EXTERNAL CONFIRMATION => NO SUCCESS.
    /// </summary>
    public interface IRealityEngine
    {
        /// <summary>
        /// Composable Multi-Source Verification of company legitimacy.
        /// Verifies Registry + Domain/MX + Regulatory Filings + Cross-Source consistency.
        /// </summary>
        Task<RealityVerificationResult> VerifyCompanyLegitimacyAsync(
            string legalName,
            string domain,
            string? registryNumber,
            Guid workspaceId,
            CancellationToken ct = default);

        /// <summary>
        /// Verifies decision-maker email deliverability and alignment with corporate domain.
        /// </summary>
        Task<RealityVerificationResult> VerifyDecisionMakerAsync(
            string fullName,
            string email,
            string expectedDomain,
            Guid workspaceId,
            CancellationToken ct = default);

        /// <summary>
        /// Verifies that payment was legitimately settled via external payment gateway webhook.
        /// Core Invariant 4: NO RECONCILIATION => NO REVENUE.
        /// </summary>
        Task<RealityVerificationResult> VerifyPaymentSettlementAsync(
            string gatewayTransactionId,
            decimal expectedAmountINR,
            Guid workspaceId,
            CancellationToken ct = default);

        /// <summary>
        /// Verifies that software build compiled and test suite passed with ZERO failures.
        /// </summary>
        Task<RealityVerificationResult> VerifySoftwareBuildAsync(
            string gitRepoUrl,
            string commitHash,
            int passedTests,
            int failedTests,
            Guid workspaceId,
            CancellationToken ct = default);

        /// <summary>
        /// Verifies formal client UAT acceptance via cryptographic signature token.
        /// </summary>
        Task<RealityVerificationResult> VerifyClientAcceptanceAsync(
            string acceptanceToken,
            Guid projectId,
            Guid workspaceId,
            CancellationToken ct = default);
    }
}
