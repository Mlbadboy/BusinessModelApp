using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth
{
    public static class GrowthConstitutionalInvariants
    {
        public const string Axiom =
            "MARKET SIGNAL != DEMAND != LEAD != CUSTOMER != REVENUE != PROFIT != CUSTOMER VALUE != BUSINESS GROWTH. " +
            "AGENT ACTIVITY != COMMERCIAL PROGRESS != BUSINESS GROWTH. " +
            "REVENUE != PROFIT.";

        public const decimal MinLtvToCacRatio = 3.0m;
        public const int MaxPaybackPeriodMonths = 12;
        public const decimal MinGrossMarginPercent = 35.0m;

        public const string LawI41A_MarketSignalNotDemand =
            "I41-A: A market signal is an epistemic hint, not qualified demand. Demand requires verifiable intent and budget.";

        public const string LawI41B_DemandNotCustomer =
            "I41-B: Demand is an inbound or outbound inquiry, not a customer. A customer requires executed authoritative contracts and delivery acceptance.";

        public const string LawI41C_CustomerCountNotGrowth =
            "I41-C: Increasing vanity customer counts with negative unit margins constitutes capital destruction, not business growth.";

        public const string LawI41D_RevenueNotProfit =
            "I41-D: Revenue is top-line cash flow, not business profit. Profit requires deducting fully loaded COGS, compute, acquisition, and operational costs.";

        public const string LawI41E_AgentActivityNotProgress =
            "I41-E: Agent tokens, tool executions, and reasoning cycles are operating expenditures, never business growth or commercial milestones.";

        public const string LawI41F_UnprofitableRevenueIsDestruction =
            "I41-F: Acquiring or serving customers below variable cost without pre-registered strategic exception destroys enterprise value and is unconstitutional.";

        public const string LawI41G_LtvCacThreshold =
            "I41-G: Blended customer lifetime value to customer acquisition cost ratio must strictly equal or exceed 3.0x (LTV:CAC >= 3.0).";

        public const string LawI41H_PaybackPeriodInvariant =
            "I41-H: Fully loaded customer acquisition cost must be recovered in bank-realized cash within a maximum of 12 operating months (Payback <= 12m).";

        public const string LawI41I_RetentionFoundationOfCompoundValue =
            "I41-I: Sustainable growth requires Net Revenue Retention (NRR) >= 100%. Leaking customer cohorts cannot be masked by top-line acquisition.";

        public const string LawI41J_ChurnCannotBeMasked =
            "I41-J: Churn and revenue contraction must be recognized immediately and deterministically; churned accounts cannot be classified as active.";

        public const string LawI41K_CustomerAttributableUnitEconomics =
            "I41-K: Every customer account must maintain an unbroken unit P&L ledger with direct cost, compute expenditure, and collected cash attribution.";

        public const string LawI41L_GrowthExperimentsPreRegistered =
            "I41-L: Growth experiments must have deterministic pre-registered hypotheses, sample bounds, and stopping invariants prior to launch.";

        public const string LawI41M_FailedExperimentsNotPivots =
            "I41-M: Experiments that fail statistical or economic significance criteria must be formally marked as terminated, never retroactively redefined as successes.";

        public const string LawI41N_PricingMustProtectContribution =
            "I41-N: Packaging and pricing modifications must preserve expected gross margins >= 35.0% and positive net contribution.";

        public const string LawI41O_ExpansionCannotMaskDecay =
            "I41-O: Account expansions must be measured independently from cohort retention decay to prevent aggregate illusion.";

        public const string LawI41P_ReferralRequiresAuthenticatedAdvocate =
            "I41-P: Referral and customer advocacy claims require verifiable active customer credentials and documented satisfaction metrics.";

        public const string LawI41Q_MarketingSpendMustBeAttributable =
            "I41-Q: Outbound and inbound marketing operations must maintain unbroken multi-touch or direct attribution to grounded commercial opportunities.";

        public const string LawI41R_WorkingCapitalRunwayGovernsVelocity =
            "I41-R: Autonomous expansion missions cannot exceed working capital runway thresholds established by corporate treasury governance.";

        public const string LawI41S_DeliveryCapacityConstrainsAcquisition =
            "I41-S: Acquisition velocity must be gated by verified delivery and customer onboarding capacity to prevent SLA breach.";

        public const string LawI41T_ExternalProofAttestationRequired =
            "I41-T: Real-world revenue claims require external proof attestation (bank verification, counterparty identity, signed delivery proof), never internal assertion.";

        public const string LawI41U_HumanStrategicOversight =
            "I41-U: Portfolio growth goals and consequential risk allocations require sovereign human executive governance (PRG-1).";

        public const string LawI41V_CapitalReallocationGradient =
            "I41-V: Autonomous growth missions reallocate capital and agent resources along the positive gradient of verified contribution margin.";

        public const string LawI41W_VanityMetricsShallNotGovern =
            "I41-W: Unqualified impressions, page views, and vanity engagements shall never trigger mission budget expansions.";

        public const string LawI41X_ComputeCostsLoadedIntoCogs =
            "I41-X: All LLM token costs, tool API fees, and infrastructure compute spent on behalf of a customer must be accounted for in that customer's COGS.";

        public const string LawI41Y_DeterministicObjectiveContinuity =
            "I41-Y: Growth Objectives must trace backward through Revenue Objectives to Sovereign Business Objectives without semantic divergence.";

        public const string LawI41Z_SubordinateToFirewallAndConstitution =
            "I41-Z: All autonomous business growth actions remain subordinate to Batch 6 Execution Firewall and the Charlie Enterprise Constitution.";

        public static readonly IReadOnlyList<string> AllLaws = new List<string>
        {
            LawI41A_MarketSignalNotDemand,
            LawI41B_DemandNotCustomer,
            LawI41C_CustomerCountNotGrowth,
            LawI41D_RevenueNotProfit,
            LawI41E_AgentActivityNotProgress,
            LawI41F_UnprofitableRevenueIsDestruction,
            LawI41G_LtvCacThreshold,
            LawI41H_PaybackPeriodInvariant,
            LawI41I_RetentionFoundationOfCompoundValue,
            LawI41J_ChurnCannotBeMasked,
            LawI41K_CustomerAttributableUnitEconomics,
            LawI41L_GrowthExperimentsPreRegistered,
            LawI41M_FailedExperimentsNotPivots,
            LawI41N_PricingMustProtectContribution,
            LawI41O_ExpansionCannotMaskDecay,
            LawI41P_ReferralRequiresAuthenticatedAdvocate,
            LawI41Q_MarketingSpendMustBeAttributable,
            LawI41R_WorkingCapitalRunwayGovernsVelocity,
            LawI41S_DeliveryCapacityConstrainsAcquisition,
            LawI41T_ExternalProofAttestationRequired,
            LawI41U_HumanStrategicOversight,
            LawI41V_CapitalReallocationGradient,
            LawI41W_VanityMetricsShallNotGovern,
            LawI41X_ComputeCostsLoadedIntoCogs,
            LawI41Y_DeterministicObjectiveContinuity,
            LawI41Z_SubordinateToFirewallAndConstitution
        };

        public static bool ValidateAllAxioms() => AllLaws != null && AllLaws.Count == 26;
    }
}
