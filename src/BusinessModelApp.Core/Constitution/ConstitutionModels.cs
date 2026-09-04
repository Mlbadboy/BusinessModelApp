using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Constitution
{
    public enum ConstitutionRuleCode
    {
        RULE_1_BUDGET_CAP = 1,
        RULE_2_CONTACT_FATIGUE = 2,
        RULE_3_LEGAL_AND_COMPLIANCE = 3,
        RULE_4_AUTHORITY_BOUNDARY = 4,
        RULE_5_NO_SYNTHETIC_REALITY = 5,
        RULE_6_REVERSIBILITY_AND_KILL_SWITCH = 6,

        // Legacy / Granular Aliases for Full Compatibility
        RULE_001_NO_EVIDENCE_NO_FACT = 101,
        RULE_002_NO_SETTLEMENT_NO_REVENUE = 102,
        RULE_003_NO_DELIVERY_CAPACITY = 103,
        RULE_004_STRATEGY_MUST_IDENTIFY_ASSUMPTIONS = 104,
        RULE_005_DECISION_MUST_BIND_EVIDENCE = 105,
        RULE_006_AUTONOMY_LEVEL_RESPECTED = 106
    }

    public class ConstitutionRuleViolation
    {
        public ConstitutionRuleCode RuleCode { get; set; }
        public string RuleName { get; set; } = string.Empty;
        public string ViolationReason { get; set; } = string.Empty;
        public string RemediationRequirement { get; set; } = string.Empty;
    }

    public class ConstitutionEvaluationResult
    {
        public bool IsCompliant { get; set; } = true;
        public List<ConstitutionRuleViolation> Violations { get; set; } = new();
        public List<ConstitutionRuleCode> PassedRules { get; set; } = new();
        public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;

        public static ConstitutionEvaluationResult Success(List<ConstitutionRuleCode> passedRules) => new()
        {
            IsCompliant = true,
            PassedRules = passedRules,
            EvaluatedAt = DateTime.UtcNow
        };

        public static ConstitutionEvaluationResult Failed(List<ConstitutionRuleViolation> violations, List<ConstitutionRuleCode> passedRules) => new()
        {
            IsCompliant = false,
            Violations = violations,
            PassedRules = passedRules,
            EvaluatedAt = DateTime.UtcNow
        };
    }
}
