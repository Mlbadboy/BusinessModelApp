using System;

namespace BusinessModelApp.Core.Domain.Reality
{
    public enum EvidenceClass
    {
        Fact = 1,               // Grounded external truth verified by authoritative systems
        DecisionHypothesis = 2, // AI interpretation or predictive estimate (NOT fact)
        Intention = 3           // Desired future state (NOT yet occurred)
    }

    public enum VerificationStatus
    {
        Unverified = 0,
        VerifiedFact = 1,
        HypothesisOnly = 2,
        FailedVerification = 3,
        Stale = 4
    }

    public enum EvidenceSourceType
    {
        CorporateRegistry = 1,  // MCA, SEC, Companies House official filings
        OfficialDomainDNS = 2,  // Authoritative DNS A/AAAA and MX resolution
        RegulatoryGazette = 3,  // Official Gazette, central bank/statutory disclosures
        GitRepository = 4,      // Cryptographic commits, branches, CI test results
        PaymentGateway = 5,     // Razorpay, Stripe cryptographic webhooks
        ClientSignature = 6,    // Cryptographic UAT sign-off token
        Communications = 7      // RFC-compliant SMTP Message-ID, PSTN Call SID
    }

    public enum DesiredCommercialState
    {
        Discovered = 0,
        Contacted = 1,
        Qualified = 2,
        ProposalDispatched = 3,
        Negotiation = 4,
        ClosedWon = 5,
        ClosedLost = 6
    }

    public enum DesiredDeliveryState
    {
        Scoping = 0,
        ArchitectureDesigned = 1,
        CodeGenerated = 2,
        TestsPassing = 3,
        Staged = 4,
        ClientUAT = 5,
        ProductionDeployed = 6,
        Completed = 7
    }
}
