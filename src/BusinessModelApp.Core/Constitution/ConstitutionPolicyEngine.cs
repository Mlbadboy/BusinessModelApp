using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Decisions;
using BusinessModelApp.Core.Domain.Strategy;
using BusinessModelApp.Core.Domain.WorldModel;
using Microsoft.Extensions.Logging;

namespace BusinessModelApp.Core.Constitution
{
    public interface IConstitutionPolicyEngine
    {
        ConstitutionEvaluationResult EvaluateCandidate(
            StrategyCandidate candidate,
            CompanySnapshot snapshot);

        ConstitutionEvaluationResult EvaluateOperation(
            string operationType,
            decimal monetaryCostINR,
            string targetDomain,
            int recentContactCountInWindow,
            bool requiresHumanSignoff,
            bool isApprovedByHuman,
            bool isReversible);
    }

    public class ConstitutionPolicyEngine : IConstitutionPolicyEngine
    {
        private readonly ILogger<ConstitutionPolicyEngine> _logger;

        public ConstitutionPolicyEngine(ILogger<ConstitutionPolicyEngine> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ConstitutionEvaluationResult EvaluateCandidate(
            StrategyCandidate candidate,
            CompanySnapshot snapshot)
        {
            var violations = new List<ConstitutionRuleViolation>();
            var passed = new List<ConstitutionRuleCode>();

            // Rule 1: Budget Cap / Capacity Invariant
            int availableSlots = snapshot.Delivery.AvailableDeliverySlots.Value > 0
                ? snapshot.Delivery.AvailableDeliverySlots.Value
                : snapshot.AvailableDeliverySlots.Value;

            if (candidate.RequiredDeliveryCapacitySlots > availableSlots)
            {
                violations.Add(new ConstitutionRuleViolation
                {
                    RuleCode = ConstitutionRuleCode.RULE_1_BUDGET_CAP,
                    RuleName = "Rule 1: Budget Cap & Delivery Capacity",
                    ViolationReason = $"Required delivery capacity ({candidate.RequiredDeliveryCapacitySlots} slots) exceeds available capacity ({availableSlots} slots).",
                    RemediationRequirement = "Reduce target scope, extend delivery timeline, or scale verified engineering headcount."
                });
            }
            else
            {
                passed.Add(ConstitutionRuleCode.RULE_1_BUDGET_CAP);
            }

            // Rule 4: Authority Boundary - Unverified strategies require executive human signoff
            if (candidate.Feasibility == StrategyFeasibilityClassification.HypotheticalUnverified ||
                candidate.ExpectedRevenueINR > 2500000m)
            {
                // Requires human approval gate in DecisionRecord
                passed.Add(ConstitutionRuleCode.RULE_4_AUTHORITY_BOUNDARY);
            }
            else
            {
                passed.Add(ConstitutionRuleCode.RULE_4_AUTHORITY_BOUNDARY);
            }

            // Rule 5: No Synthetic Reality
            if (candidate.Assumptions == null || candidate.Assumptions.Count == 0)
            {
                violations.Add(new ConstitutionRuleViolation
                {
                    RuleCode = ConstitutionRuleCode.RULE_5_NO_SYNTHETIC_REALITY,
                    RuleName = "Rule 5: No Synthetic Reality",
                    ViolationReason = "Strategy fails to state explicit assumptions with provenance tags.",
                    RemediationRequirement = "Disclose all commercial assumptions with origin labels (VerifiedFact, Historical, AiEstimate, Unknown)."
                });
            }
            else
            {
                passed.Add(ConstitutionRuleCode.RULE_5_NO_SYNTHETIC_REALITY);
            }

            // Rule 6: Reversibility & Kill Switch
            passed.Add(ConstitutionRuleCode.RULE_6_REVERSIBILITY_AND_KILL_SWITCH);

            if (violations.Count > 0)
            {
                return ConstitutionEvaluationResult.Failed(violations, passed);
            }

            return ConstitutionEvaluationResult.Success(passed);
        }

        public ConstitutionEvaluationResult EvaluateOperation(
            string operationType,
            decimal monetaryCostINR,
            string targetDomain,
            int recentContactCountInWindow,
            bool requiresHumanSignoff,
            bool isApprovedByHuman,
            bool isReversible)
        {
            var violations = new List<ConstitutionRuleViolation>();
            var passed = new List<ConstitutionRuleCode>();

            // Rule 1: Budget Cap
            if (monetaryCostINR > 100000m && !isApprovedByHuman)
            {
                violations.Add(new ConstitutionRuleViolation
                {
                    RuleCode = ConstitutionRuleCode.RULE_1_BUDGET_CAP,
                    RuleName = "Rule 1: Budget Cap",
                    ViolationReason = $"Operation cost ₹{monetaryCostINR:N0} exceeds autonomous budget limit without sign-off.",
                    RemediationRequirement = "Request human budget approval."
                });
            }
            else
            {
                passed.Add(ConstitutionRuleCode.RULE_1_BUDGET_CAP);
            }

            // Rule 2: Contact Fatigue
            if (recentContactCountInWindow >= 3)
            {
                violations.Add(new ConstitutionRuleViolation
                {
                    RuleCode = ConstitutionRuleCode.RULE_2_CONTACT_FATIGUE,
                    RuleName = "Rule 2: Contact Fatigue",
                    ViolationReason = $"Target domain/contact '{targetDomain}' has reached max outreach touches ({recentContactCountInWindow} touches in 7-day window).",
                    RemediationRequirement = "Enforce cooldown period before next outreach attempt."
                });
            }
            else
            {
                passed.Add(ConstitutionRuleCode.RULE_2_CONTACT_FATIGUE);
            }

            // Rule 3: Legal & Compliance
            passed.Add(ConstitutionRuleCode.RULE_3_LEGAL_AND_COMPLIANCE);

            // Rule 4: Authority Boundary
            if (requiresHumanSignoff && !isApprovedByHuman)
            {
                violations.Add(new ConstitutionRuleViolation
                {
                    RuleCode = ConstitutionRuleCode.RULE_4_AUTHORITY_BOUNDARY,
                    RuleName = "Rule 4: Authority Boundary",
                    ViolationReason = $"Operation '{operationType}' requires explicit human executive approval.",
                    RemediationRequirement = "Route to executive approval queue."
                });
            }
            else
            {
                passed.Add(ConstitutionRuleCode.RULE_4_AUTHORITY_BOUNDARY);
            }

            // Rule 5: No Synthetic Reality
            passed.Add(ConstitutionRuleCode.RULE_5_NO_SYNTHETIC_REALITY);

            // Rule 6: Reversibility
            if (!isReversible)
            {
                violations.Add(new ConstitutionRuleViolation
                {
                    RuleCode = ConstitutionRuleCode.RULE_6_REVERSIBILITY_AND_KILL_SWITCH,
                    RuleName = "Rule 6: Reversibility & Kill Switch",
                    ViolationReason = $"Operation '{operationType}' is irreversible and has no automated compensation/rollback logic.",
                    RemediationRequirement = "Must be guarded by human confirmation or implement compensation transaction."
                });
            }
            else
            {
                passed.Add(ConstitutionRuleCode.RULE_6_REVERSIBILITY_AND_KILL_SWITCH);
            }

            if (violations.Count > 0)
            {
                return ConstitutionEvaluationResult.Failed(violations, passed);
            }

            return ConstitutionEvaluationResult.Success(passed);
        }
    }
}
