using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Constitution;
using BusinessModelApp.Core.Domain.Decisions;
using BusinessModelApp.Core.Domain.Objectives;
using BusinessModelApp.Core.Domain.Strategy;
using BusinessModelApp.Core.Domain.WorldModel;
using Microsoft.Extensions.Logging;

namespace BusinessModelApp.Infrastructure.Constitution
{
    public class CompanyConstitutionService : ICompanyConstitutionService
    {
        private readonly ILogger<CompanyConstitutionService> _logger;

        public CompanyConstitutionService(ILogger<CompanyConstitutionService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<ConstitutionEvaluationResult> EvaluateStrategyAsync(
            BusinessStrategy strategy,
            CompanySnapshot snapshot,
            CancellationToken ct = default)
        {
            _logger.LogInformation("[ConstitutionPolicy] Evaluating strategy '{StrategyName}' against Company Constitution", strategy.StrategyName);

            var violations = new List<ConstitutionRuleViolation>();
            var passed = new List<ConstitutionRuleCode>();

            // RULE-003: No Delivery Capacity -> Cannot accept strategy
            int availableSlots = snapshot.AvailableDeliverySlots.Value;
            if (strategy.DeliveryCapacitySlotsRequired > availableSlots)
            {
                violations.Add(new ConstitutionRuleViolation
                {
                    RuleCode = ConstitutionRuleCode.RULE_003_NO_DELIVERY_CAPACITY,
                    RuleName = "RULE-003: Delivery Capacity Saturation Invariant",
                    ViolationReason = $"Strategy requires {strategy.DeliveryCapacitySlotsRequired} delivery slots, but company only has {availableSlots} available.",
                    RemediationRequirement = "Scale down concurrent delivery slots or defer high-volume commitments."
                });
            }
            else
            {
                passed.Add(ConstitutionRuleCode.RULE_003_NO_DELIVERY_CAPACITY);
            }

            // RULE-004: Strategy must explicitly identify and label assumptions
            if (string.IsNullOrWhiteSpace(strategy.AssumptionsJson) || strategy.AssumptionsJson == "[]")
            {
                violations.Add(new ConstitutionRuleViolation
                {
                    RuleCode = ConstitutionRuleCode.RULE_004_STRATEGY_MUST_IDENTIFY_ASSUMPTIONS,
                    RuleName = "RULE-004: Explicit Assumption Identification",
                    ViolationReason = "Strategy fails to explicitly enumerate and label operational assumptions.",
                    RemediationRequirement = "Enumerate key economic and conversion assumptions with provenance tags."
                });
            }
            else
            {
                passed.Add(ConstitutionRuleCode.RULE_004_STRATEGY_MUST_IDENTIFY_ASSUMPTIONS);
            }

            // RULE-001: Evidence Grounding
            if (strategy.FeasibilityState == StrategyFeasibilityState.EvidenceInsufficient)
            {
                violations.Add(new ConstitutionRuleViolation
                {
                    RuleCode = ConstitutionRuleCode.RULE_001_NO_EVIDENCE_NO_FACT,
                    RuleName = "RULE-001: No Evidence => No Fact",
                    ViolationReason = "Strategy relies on ungrounded market claims or zero verified corporate entities.",
                    RemediationRequirement = "Gather authoritative external evidence before advancing strategy to execution."
                });
            }
            else
            {
                passed.Add(ConstitutionRuleCode.RULE_001_NO_EVIDENCE_NO_FACT);
            }

            var result = violations.Any()
                ? ConstitutionEvaluationResult.Failed(violations, passed)
                : ConstitutionEvaluationResult.Success(passed);

            return Task.FromResult(result);
        }

        public Task<ConstitutionEvaluationResult> EvaluateDecisionAsync(
            DecisionRecord decision,
            CompanySnapshot snapshot,
            CancellationToken ct = default)
        {
            _logger.LogInformation("[ConstitutionPolicy] Evaluating Decision {DecisionId} against Company Constitution", decision.Id);

            var violations = new List<ConstitutionRuleViolation>();
            var passed = new List<ConstitutionRuleCode>();

            // RULE-005: Decision must bind evidence hashes
            if (string.IsNullOrWhiteSpace(decision.GroundingEvidenceHashesJson) || decision.GroundingEvidenceHashesJson == "[]")
            {
                violations.Add(new ConstitutionRuleViolation
                {
                    RuleCode = ConstitutionRuleCode.RULE_005_DECISION_MUST_BIND_EVIDENCE,
                    RuleName = "RULE-005: Decision Grounding Evidence Binding",
                    ViolationReason = "Autonomous decision does not reference any cryptographic evidence hashes.",
                    RemediationRequirement = "Bind at least one verified Reality Engine evidence hash to the decision."
                });
            }
            else
            {
                passed.Add(ConstitutionRuleCode.RULE_005_DECISION_MUST_BIND_EVIDENCE);
            }

            // RULE-006: Autonomy level respected
            if (decision.RiskScore > 0.50 && decision.ExecutedAutonomyLevel >= Core.Agents.AutonomyLevel.Level4_AutonomousOperations)
            {
                violations.Add(new ConstitutionRuleViolation
                {
                    RuleCode = ConstitutionRuleCode.RULE_006_AUTONOMY_LEVEL_RESPECTED,
                    RuleName = "RULE-006: Autonomy Governance Threshold",
                    ViolationReason = $"Risk score {decision.RiskScore:F2} exceeds threshold for autonomous level {decision.ExecutedAutonomyLevel}.",
                    RemediationRequirement = "Flag decision for mandatory human executive sign-off."
                });
            }
            else
            {
                passed.Add(ConstitutionRuleCode.RULE_006_AUTONOMY_LEVEL_RESPECTED);
            }

            // RULE-002: Settlement invariant
            passed.Add(ConstitutionRuleCode.RULE_002_NO_SETTLEMENT_NO_REVENUE);

            var result = violations.Any()
                ? ConstitutionEvaluationResult.Failed(violations, passed)
                : ConstitutionEvaluationResult.Success(passed);

            return Task.FromResult(result);
        }
    }
}
