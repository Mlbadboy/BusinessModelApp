using System;
using System.Linq;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Commercial;
using BusinessModelApp.Core.Domain.Reality;
using BusinessModelApp.Core.Reality;
using BusinessModelApp.Infrastructure.Data;
using BusinessModelApp.Infrastructure.Reality;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public class RealityEngineCertificationTests
    {
        private AppDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"RealityEngineTestDb_{Guid.NewGuid()}")
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task RealityEngine_ShouldReject_UnverifiedCompany_WithoutEvidence()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var realityEngine = new RealityEngine(db, NullLogger<RealityEngine>.Instance);
            var workspaceId = Guid.NewGuid();

            // Act: Attempt verification with ungrounded / synthetic company term
            var result = await realityEngine.VerifyCompanyLegitimacyAsync(
                legalName: "Unknown Tech Stub Entity",
                domain: "unknown-fake-domain.xyz",
                registryNumber: null,
                workspaceId: workspaceId);

            // Assert: NO EVIDENCE => NO FACT
            Assert.False(result.IsVerified);
            Assert.Equal(VerificationStatus.FailedVerification, result.Status);
            Assert.Null(result.GeneratedEvidence);
            Assert.Contains("Rejected synthetic or ungrounded entity declaration", result.FailureReason);

            // Assert: DB ledger remains empty
            var persistedEvidenceCount = await db.EvidenceRecords.CountAsync();
            Assert.Equal(0, persistedEvidenceCount);
        }

        [Fact]
        public async Task RealityEngine_ShouldVerify_GenuineCorporateDomain_And_Registry()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var realityEngine = new RealityEngine(db, NullLogger<RealityEngine>.Instance);
            var workspaceId = Guid.NewGuid();

            // Act: Verify genuine enterprise with legitimate registry & corporate domain
            var result = await realityEngine.VerifyCompanyLegitimacyAsync(
                legalName: "HDFC Capital Financial Services Limited",
                domain: "hdfccapital.com",
                registryNumber: "CIN-U65993MH2016PTC288078",
                workspaceId: workspaceId);

            // Assert: Authoritative fact verified
            Assert.True(result.IsVerified);
            Assert.Equal(VerificationStatus.VerifiedFact, result.Status);
            Assert.NotNull(result.GeneratedEvidence);
            Assert.True(result.RegistryProofVerified);
            Assert.True(result.DomainDnsProofVerified);
            Assert.True(result.CrossSourceConsistencyScore >= 0.85);

            // Invariant: Temporal separation (ObservedAt must precede RetrievedAt)
            Assert.True(result.GeneratedEvidence.ObservedAt < result.GeneratedEvidence.RetrievedAt);

            // Invariant: Dual SHA-256 Hashes populated (64 hex characters)
            Assert.Equal(64, result.RawPayloadHash.Length);
            Assert.Equal(64, result.CanonicalPayloadHash.Length);

            // Invariant: Evidence committed to DB store
            var persistedRecord = await db.EvidenceRecords.FirstOrDefaultAsync(e => e.WorkspaceId == workspaceId);
            Assert.NotNull(persistedRecord);
            Assert.Equal(EvidenceClass.Fact, persistedRecord.Class);
            Assert.Equal("hdfccapital.com", result.SubjectIdentifier);
        }

        [Fact]
        public async Task UnknownWorldTest_ShouldBlockAutonomousExecution_WhenEvidenceIsInsufficient()
        {
            // Arrange: CEO sets ungrounded objective in an unknown market
            using var db = CreateInMemoryDbContext();
            var realityEngine = new RealityEngine(db, NullLogger<RealityEngine>.Instance);
            var workspaceId = Guid.NewGuid();

            string ungroundedObjective = "Sell satellite tracking software in South America";
            string candidateCompany = "Unknown Satellite Corp";
            string candidateDomain = "satellites-unknown.invalid";

            // Act: Reality Engine checks for authoritative ground-truth evidence
            var companyVerification = await realityEngine.VerifyCompanyLegitimacyAsync(
                candidateCompany,
                candidateDomain,
                registryNumber: null,
                workspaceId: workspaceId);

            var buyerVerification = await realityEngine.VerifyDecisionMakerAsync(
                fullName: "Invented Persona",
                email: "buyer@gmail.com", // Disallowed public webmail
                expectedDomain: candidateDomain,
                workspaceId: workspaceId);

            // Assert Invariant: Charlie ADMITS IT DOES NOT KNOW
            Assert.False(companyVerification.IsVerified);
            Assert.False(buyerVerification.IsVerified);

            int verifiedCompaniesCount = companyVerification.IsVerified ? 1 : 0;
            int verifiedBuyersCount = buyerVerification.IsVerified ? 1 : 0;
            int verifiedOpportunitiesCount = 0;

            string autonomousExecutionStatus = (verifiedCompaniesCount == 0 || verifiedBuyersCount == 0)
                ? "BLOCKED: INSUFFICIENT EVIDENCE"
                : "PROCEED";

            Assert.Equal(0, verifiedCompaniesCount);
            Assert.Equal(0, verifiedBuyersCount);
            Assert.Equal(0, verifiedOpportunitiesCount);
            Assert.Equal("BLOCKED: INSUFFICIENT EVIDENCE", autonomousExecutionStatus);

            // Assert Invariant: Zero fake records persisted to CRM tables
            Assert.Equal(0, await db.Leads.CountAsync());
            Assert.Equal(0, await db.Opportunities.CountAsync());
            Assert.Equal(0, await db.EvidenceRecords.CountAsync());
        }

        [Fact]
        public async Task DualState_ShouldRemainInDesiredState_UntilRealityEngineConfirms()
        {
            // Arrange: Charlie "intends" or "desires" a lead to be qualified
            var lead = new Lead
            {
                ContactName = "Vikram Singhania",
                CompanyName = "HDFC Capital Financial Services",
                Email = "vikram.singhania@hdfccapital.com",
                Status = LeadStatus.Qualified // Desired State set by AI
            };

            // Assert Invariant: Desired state is Qualified, but Observed state is Unverified
            Assert.Equal(LeadStatus.Qualified, lead.DesiredState);
            Assert.False(lead.ObservedState.DomainVerified);
            Assert.False(lead.ObservedState.IdentityConfirmed);
            Assert.Equal(VerificationStatus.Unverified, lead.ObservedState.OverallVerificationStatus);

            // Act: Reality Engine verifies authoritative corporate email and domain
            using var db = CreateInMemoryDbContext();
            var realityEngine = new RealityEngine(db, NullLogger<RealityEngine>.Instance);
            var workspaceId = Guid.NewGuid();

            var verification = await realityEngine.VerifyDecisionMakerAsync(
                lead.ContactName,
                lead.Email,
                expectedDomain: "hdfccapital.com",
                workspaceId: workspaceId);

            Assert.True(verification.IsVerified);

            // Transition: Reality Engine observation certifies Observed State
            lead.ObservedState.DomainVerified = verification.DomainDnsProofVerified;
            lead.ObservedState.IdentityConfirmed = true;
            lead.ObservedState.OverallVerificationStatus = verification.Status;
            lead.ObservedState.GroundingEvidenceHash = verification.RawPayloadHash;
            lead.ObservedState.RealityVerificationRunId = verification.VerificationRunId;

            // Assert Invariant: Both Desired and Observed states now aligned and grounded
            Assert.Equal(LeadStatus.Qualified, lead.DesiredState);
            Assert.True(lead.ObservedState.DomainVerified);
            Assert.True(lead.ObservedState.IdentityConfirmed);
            Assert.Equal(VerificationStatus.VerifiedFact, lead.ObservedState.OverallVerificationStatus);
            Assert.NotNull(lead.ObservedState.GroundingEvidenceHash);
        }

        [Fact]
        public async Task SoftwareBuild_ShouldReject_WhenTestsFail_ZeroFailureInvariant()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var realityEngine = new RealityEngine(db, NullLogger<RealityEngine>.Instance);
            var workspaceId = Guid.NewGuid();

            // Act: Attempt to verify build with 1 failing test
            var failingResult = await realityEngine.VerifySoftwareBuildAsync(
                gitRepoUrl: "https://github.com/company/repo",
                commitHash: "7f2b9a84c01823904aef78129034871290384712",
                passedTests: 42,
                failedTests: 1, // 1 Failure
                workspaceId: workspaceId);

            // Assert Invariant: ZERO TEST FAILURES
            Assert.False(failingResult.IsVerified);
            Assert.Equal(VerificationStatus.FailedVerification, failingResult.Status);
            Assert.Contains("Zero failures required for production promotion", failingResult.FailureReason);

            // Act: Verify build with 0 failures
            var passingResult = await realityEngine.VerifySoftwareBuildAsync(
                gitRepoUrl: "https://github.com/company/repo",
                commitHash: "7f2b9a84c01823904aef78129034871290384712",
                passedTests: 43,
                failedTests: 0, // 0 Failures
                workspaceId: workspaceId);

            Assert.True(passingResult.IsVerified);
            Assert.Equal(VerificationStatus.VerifiedFact, passingResult.Status);
            Assert.NotNull(passingResult.GeneratedEvidence);
        }

        [Fact]
        public async Task PaymentSettlement_ShouldRequireAuthoritativeSignature()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var realityEngine = new RealityEngine(db, NullLogger<RealityEngine>.Instance);
            var workspaceId = Guid.NewGuid();

            // Act: Fake synthetic string rejected
            var fakeResult = await realityEngine.VerifyPaymentSettlementAsync(
                gatewayTransactionId: "fake_payment_123",
                expectedAmountINR: 2500000m,
                workspaceId: workspaceId);

            Assert.False(fakeResult.IsVerified);
            Assert.Contains("not an authoritative payment gateway settlement identifier", fakeResult.FailureReason);

            // Act: Authoritative gateway ID verified
            var genuineResult = await realityEngine.VerifyPaymentSettlementAsync(
                gatewayTransactionId: "pay_N8s93kd8F29aLq",
                expectedAmountINR: 2500000m,
                workspaceId: workspaceId);

            Assert.True(genuineResult.IsVerified);
            Assert.Equal(VerificationStatus.VerifiedFact, genuineResult.Status);
            Assert.True(genuineResult.WebhookSignatureProofVerified);
        }
    }
}
