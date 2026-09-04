using System;

namespace BusinessModelApp.Core.Domain.Objectives
{
    public enum ObjectiveStatus
    {
        Draft = 0,
        Active = 1,
        AtRisk = 2,
        Achieved = 3,
        Missed = 4,
        Suspended = 5
    }

    public enum RevenueBaselineState
    {
        Unavailable = 0,       // No accounting or gateway integration exists
        VerifiedZero = 1,      // Gateway/accounting integration active and confirms 0 transactions
        Verified = 2,          // Authoritative settled payments verified
        PartiallyVerified = 3  // CRM reports revenue but only a fraction is gateway-reconciled
    }

    public enum MetricProvenanceSource
    {
        VerifiedFact = 1,            // Externally observed and certified by Reality Engine
        ExplicitCeoInput = 2,        // Direct executive parameter from user prompt
        HistoricalCompanyData = 3,   // Extracted from verified past company operations
        AiEstimate = 4,              // Inferred/predicted by model; hypothesis only
        Unknown = 5,                 // Data absent; explicitly ungrounded

        // Exact Specification Aliases
        VERIFIED_FACT = 1,
        EXPLICIT_CEO_INPUT = 2,
        HISTORICAL_COMPANY_DATA = 3,
        AI_ESTIMATE = 4,
        UNKNOWN = 5
    }

    public enum StrategyFeasibilityState
    {
        Feasible = 1,
        ConditionallyFeasible = 2,
        CapacityBlocked = 3,
        EvidenceInsufficient = 4,
        EconomicallyUnattractive = 5,
        PolicyBlocked = 6
    }
}
