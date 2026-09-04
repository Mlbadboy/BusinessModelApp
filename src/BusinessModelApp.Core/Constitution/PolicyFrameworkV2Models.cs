using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Constitution
{
    public enum PolicyCategory
    {
        SafetyAndLegal = 1,     // Highest priority
        BudgetAndFinancial = 2,
        ContactAndFatigue = 3,
        Operational = 4,
        Preference = 5          // Lowest priority
    }

    public enum PolicyOutcome
    {
        Allow = 1,
        AllowWithLimits = 2,
        RequireSimulation = 3,
        RequireSecondAgent = 4,
        RequireHumanApproval = 5,
        Deny = 6,
        EmergencyStop = 7       // Most restrictive
    }

    public class PolicyEvaluationContext
    {
        public string AgentId { get; set; } = string.Empty;
        public string Capability { get; set; } = string.Empty;
        public string TargetEntity { get; set; } = string.Empty;
        public decimal SpendAmountINR { get; set; } = 0m;
        public int PriorContactCountInLast30Days { get; set; } = 0;
        public string TenantId { get; set; } = "TENANT_DEFAULT";
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public class PolicyRuleResult
    {
        public string RuleName { get; set; } = string.Empty;
        public PolicyCategory Category { get; set; }
        public int Priority { get; set; }
        public PolicyOutcome Outcome { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class PolicyEvaluationSummary
    {
        public PolicyOutcome FinalOutcome { get; set; } = PolicyOutcome.Allow;
        public List<PolicyRuleResult> EvaluatedRules { get; set; } = new();
        public List<string> DenialReasons { get; set; } = new();
        public bool IsBlocked => FinalOutcome == PolicyOutcome.Deny || FinalOutcome == PolicyOutcome.EmergencyStop;
        public bool RequiresHumanApproval => FinalOutcome == PolicyOutcome.RequireHumanApproval;
        public DateTime EvaluatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public interface IPolicyRule
    {
        string RuleName { get; }
        PolicyCategory Category { get; }
        int Priority { get; }
        PolicyRuleResult Evaluate(PolicyEvaluationContext context);
    }

    public interface IPolicyEngineV2
    {
        void RegisterRule(IPolicyRule rule);
        PolicyEvaluationSummary Evaluate(PolicyEvaluationContext context);
    }
}
