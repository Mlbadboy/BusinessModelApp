using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Commercial
{
    /// <summary>
    /// Law I40 — Commercial Operations Sovereignty.
    /// Non-negotiable constitutional invariants governing all commercial state transitions,
    /// evidence verifications, outreach authorizations, pricing guardrails, and revenue attribution.
    /// </summary>
    public static class CommercialConstitutionalInvariants
    {
        public const string Axiom =
            "OPPORTUNITY != LEAD != QUALIFIED PROSPECT != ENGAGEMENT != MEETING != PROPOSAL != NEGOTIATION != DEAL != CONTRACT != DELIVERY != INVOICE != PAYMENT != COLLECTED CASH != REALIZED REVENUE != PROFIT";

        public const string LawI40A_OpportunityNotCustomer =
            "I40-A: Opportunity cannot create a customer.";

        public const string LawI40B_LeadQualificationNotDeal =
            "I40-B: Lead qualification cannot create a deal.";

        public const string LawI40C_AgentBeliefNotTruth =
            "I40-C: Agent belief cannot create commercial truth.";

        public const string LawI40D_ConversationNotCommitment =
            "I40-D: Conversation cannot equal commitment.";

        public const string LawI40E_ProposalNotAcceptance =
            "I40-E: Proposal cannot equal acceptance.";

        public const string LawI40F_ContractNotPayment =
            "I40-F: Contract cannot equal payment.";

        public const string LawI40G_InvoiceNotCollectedCash =
            "I40-G: Invoice cannot equal collected cash.";

        public const string LawI40H_PipelineNotRevenue =
            "I40-H: Pipeline cannot equal revenue.";

        public const string LawI40I_ForecastNotRevenue =
            "I40-I: Forecast cannot equal revenue.";

        public const string LawI40J_SimulationNotOutcome =
            "I40-J: Simulation cannot equal commercial outcome.";

        public const string LawI40K_AgentActivityNotProgress =
            "I40-K: Agent activity cannot equal business progress.";

        public const string LawI40L_CrmStateNotAuthoritativeEvidence =
            "I40-L: CRM state cannot override authoritative evidence.";

        public const string LawI40M_CommunicationRequiresAuthorization =
            "I40-M: Communication requires governed capability authorization.";

        public const string LawI40N_PricingRequiresAuthority =
            "I40-N: Material pricing changes require appropriate authority.";

        public const string LawI40O_ContractExecutionRequiresGovernance =
            "I40-O: Contract execution requires appropriate governance.";

        public const string LawI40P_FinancialActionsUnderBatch6 =
            "I40-P: Financial actions remain under Batch 6.";

        public const string LawI40Q_RevenueRequiresFinancialEvidence =
            "I40-Q: Revenue recognition requires verified financial evidence.";

        public const string LawI40R_RevenueAttributionRequiresLineage =
            "I40-R: Revenue attribution requires immutable lineage.";

        public const string LawI40S_FailedActionsCannotAdvanceState =
            "I40-S: Failed actions cannot silently advance the sales state.";

        public const string LawI40T_UnknownEffectNotSuccess =
            "I40-T: UnknownEffect cannot be treated as success.";

        public const string LawI40U_PreventDuplicateCommercialEffects =
            "I40-U: Duplicate commercial effects must be prevented.";

        public const string LawI40V_AgentsCannotManufactureOutcomes =
            "I40-V: Agents cannot manufacture customers, meetings, deals or revenue.";

        public const string LawI40W_AutonomyCannotBypassGovernance =
            "I40-W: Commercial autonomy cannot bypass human governance.";

        public const string LawI40X_OptimizationCannotOverrideConstraints =
            "I40-X: Revenue optimization cannot override business constraints.";

        public const string LawI40Y_RevenueCannotCreateAuthority =
            "I40-Y: Revenue generation cannot create authority.";

        public const string LawI40Z_SubordinateToBatch6 =
            "I40-Z: All commercial automation remains subordinate to Batch 6.";

        public static readonly IReadOnlyList<string> AllLaws = new List<string>
        {
            LawI40A_OpportunityNotCustomer,
            LawI40B_LeadQualificationNotDeal,
            LawI40C_AgentBeliefNotTruth,
            LawI40D_ConversationNotCommitment,
            LawI40E_ProposalNotAcceptance,
            LawI40F_ContractNotPayment,
            LawI40G_InvoiceNotCollectedCash,
            LawI40H_PipelineNotRevenue,
            LawI40I_ForecastNotRevenue,
            LawI40J_SimulationNotOutcome,
            LawI40K_AgentActivityNotProgress,
            LawI40L_CrmStateNotAuthoritativeEvidence,
            LawI40M_CommunicationRequiresAuthorization,
            LawI40N_PricingRequiresAuthority,
            LawI40O_ContractExecutionRequiresGovernance,
            LawI40P_FinancialActionsUnderBatch6,
            LawI40Q_RevenueRequiresFinancialEvidence,
            LawI40R_RevenueAttributionRequiresLineage,
            LawI40S_FailedActionsCannotAdvanceState,
            LawI40T_UnknownEffectNotSuccess,
            LawI40U_PreventDuplicateCommercialEffects,
            LawI40V_AgentsCannotManufactureOutcomes,
            LawI40W_AutonomyCannotBypassGovernance,
            LawI40X_OptimizationCannotOverrideConstraints,
            LawI40Y_RevenueCannotCreateAuthority,
            LawI40Z_SubordinateToBatch6
        };
    }
}
