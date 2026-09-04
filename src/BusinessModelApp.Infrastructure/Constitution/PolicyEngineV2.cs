using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using BusinessModelApp.Core.Constitution;

namespace BusinessModelApp.Infrastructure.Constitution
{
    public class PolicyEngineV2 : IPolicyEngineV2
    {
        private readonly ConcurrentBag<IPolicyRule> _rules = new();

        public PolicyEngineV2()
        {
            SeedStandardRules();
        }

        private void SeedStandardRules()
        {
            RegisterRule(new LegalCompliancePolicyRule());
            RegisterRule(new BudgetCapPolicyRule());
            RegisterRule(new ContactFatiguePolicyRule());
        }

        public void RegisterRule(IPolicyRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            _rules.Add(rule);
        }

        public PolicyEvaluationSummary Evaluate(PolicyEvaluationContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var summary = new PolicyEvaluationSummary();
            var results = new List<PolicyRuleResult>();

            // Evaluate all registered rules
            foreach (var rule in _rules)
            {
                var result = rule.Evaluate(context);
                results.Add(result);
            }

            // Conflict Resolution Invariant: The MOST RESTRICTIVE outcome strictly prevails
            // Severity hierarchy: EmergencyStop (7) > Deny (6) > RequireHumanApproval (5) > RequireSecondAgent (4) > RequireSimulation (3) > AllowWithLimits (2) > Allow (1)
            var mostRestrictive = results
                .OrderByDescending(r => (int)r.Outcome)
                .ThenBy(r => (int)r.Category) // Higher priority categories break ties
                .FirstOrDefault();

            summary.EvaluatedRules = results;
            summary.FinalOutcome = mostRestrictive?.Outcome ?? PolicyOutcome.Allow;

            foreach (var r in results.Where(r => r.Outcome == PolicyOutcome.Deny || r.Outcome == PolicyOutcome.EmergencyStop))
            {
                summary.DenialReasons.Add($"[{r.Category}] {r.RuleName}: {r.Reason}");
            }

            return summary;
        }
    }

    public class LegalCompliancePolicyRule : IPolicyRule
    {
        public string RuleName => "Rule 1: Legal & Compliance Enforcer";
        public PolicyCategory Category => PolicyCategory.SafetyAndLegal;
        public int Priority => 1;

        public PolicyRuleResult Evaluate(PolicyEvaluationContext context)
        {
            if (context.Capability.Equals("DEPLOY_UNAUDITED_CODE", StringComparison.OrdinalIgnoreCase) ||
                context.Capability.Equals("EXECUTE_UNGOVERNED_PAYMENT", StringComparison.OrdinalIgnoreCase))
            {
                return new PolicyRuleResult
                {
                    RuleName = RuleName,
                    Category = Category,
                    Priority = Priority,
                    Outcome = PolicyOutcome.EmergencyStop,
                    Reason = $"Execution of capability {context.Capability} is strictly illegal under enterprise safety mandate."
                };
            }

            return new PolicyRuleResult
            {
                RuleName = RuleName,
                Category = Category,
                Priority = Priority,
                Outcome = PolicyOutcome.Allow,
                Reason = "Legal compliance satisfied."
            };
        }
    }

    public class BudgetCapPolicyRule : IPolicyRule
    {
        public string RuleName => "Rule 2: Commercial Budget Cap Enforcer";
        public PolicyCategory Category => PolicyCategory.BudgetAndFinancial;
        public int Priority => 2;

        public PolicyRuleResult Evaluate(PolicyEvaluationContext context)
        {
            if (context.SpendAmountINR > 100000m)
            {
                return new PolicyRuleResult
                {
                    RuleName = RuleName,
                    Category = Category,
                    Priority = Priority,
                    Outcome = PolicyOutcome.Deny,
                    Reason = $"Proposed spend of INR {context.SpendAmountINR:N0} exceeds hard enterprise ceiling of INR 100,000."
                };
            }

            if (context.SpendAmountINR > 25000m)
            {
                return new PolicyRuleResult
                {
                    RuleName = RuleName,
                    Category = Category,
                    Priority = Priority,
                    Outcome = PolicyOutcome.RequireHumanApproval,
                    Reason = $"Proposed spend of INR {context.SpendAmountINR:N0} requires executive sign-off (> INR 25,000)."
                };
            }

            return new PolicyRuleResult
            {
                RuleName = RuleName,
                Category = Category,
                Priority = Priority,
                Outcome = PolicyOutcome.Allow,
                Reason = "Budget spend within autonomous authority."
            };
        }
    }

    public class ContactFatiguePolicyRule : IPolicyRule
    {
        public string RuleName => "Rule 3: Contact Fatigue Enforcer";
        public PolicyCategory Category => PolicyCategory.ContactAndFatigue;
        public int Priority => 3;

        public PolicyRuleResult Evaluate(PolicyEvaluationContext context)
        {
            if (context.PriorContactCountInLast30Days >= 5)
            {
                return new PolicyRuleResult
                {
                    RuleName = RuleName,
                    Category = Category,
                    Priority = Priority,
                    Outcome = PolicyOutcome.Deny,
                    Reason = $"Contact limit of 5 communications in 30 days has been reached for {context.TargetEntity}."
                };
            }

            if (context.PriorContactCountInLast30Days >= 3)
            {
                return new PolicyRuleResult
                {
                    RuleName = RuleName,
                    Category = Category,
                    Priority = Priority,
                    Outcome = PolicyOutcome.RequireHumanApproval,
                    Reason = $"Approaching fatigue limit ({context.PriorContactCountInLast30Days} contacts). Human sign-off required."
                };
            }

            return new PolicyRuleResult
            {
                RuleName = RuleName,
                Category = Category,
                Priority = Priority,
                Outcome = PolicyOutcome.Allow,
                Reason = "Contact fatigue within normal bounds."
            };
        }
    }
}
