using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce
{
    public enum SideEffectClass
    {
        READ_ONLY = 0,
        LOCAL_MUTATION = 1,
        EXTERNAL_MUTATION = 2,
        FINANCIAL_TRANSACTION = 3,
        IRREVERSIBLE = 4
    }

    public enum SkillLifecycleState
    {
        DISCOVER,
        ANALYZE,
        THREAT_MODEL,
        BUILD,
        TEST,
        RED_TEAM,
        CERTIFICATION_PENDING,
        CERTIFIED,
        SHADOW,
        PROBATION,
        ACTIVE,
        DEPRECATED,
        REVOKED
    }

    public class GovernedTool
    {
        public string ToolId { get; set; } = string.Empty; // e.g. "Browser.Navigate", "CRM.CreateLead", "Email.SendDraft"
        public string CapabilityId { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0.0";
        public string Provider { get; set; } = "CharlieCore";
        public string Owner { get; set; } = "Engineering";
        public int RiskTier { get; set; } = 1; // 1 = Low, 2 = Medium, 3 = High, 4 = Critical
        public string InputSchemaJson { get; set; } = "{}";
        public string OutputSchemaJson { get; set; } = "{}";
        public string TenantScope { get; set; } = "*";
        public List<string> AllowedAgents { get; set; } = new();
        public List<string> AllowedEnvironments { get; set; } = new() { "Production", "Staging", "Development" };
        public decimal BudgetCostPerCall { get; set; } = 0.05m; // in INR
        public SideEffectClass SideEffect { get; set; } = SideEffectClass.READ_ONLY;
        public bool IsReversible { get; set; } = true;
        public string RequiredPolicy { get; set; } = "StandardToolAccessPolicy";
        public string RequiredAuthority { get; set; } = "StandardWorker";
        public string CredentialScope { get; set; } = "None";
        public bool IsActive { get; set; } = true;
    }

    public class GovernedSkill
    {
        public string SkillId { get; set; } = string.Empty; // e.g. "SALES_PROSPECT_RESEARCH", "PROPOSAL_SYNTHESIS"
        public string Version { get; set; } = "1.0.0";
        public string Description { get; set; } = string.Empty;
        public List<string> RequiredTools { get; set; } = new();
        public List<string> RequiredCapabilities { get; set; } = new();
        public List<string> RequiredModels { get; set; } = new();
        public int RiskTier { get; set; } = 2;
        public List<string> Dependencies { get; set; } = new();
        public string SopDefinition { get; set; } = string.Empty;
        public string ThreatModel { get; set; } = "StandardThreatModel";
        public SkillLifecycleState LifecycleState { get; set; } = SkillLifecycleState.DISCOVER;
        public string Owner { get; set; } = "System";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CertifiedAt { get; set; }
        public string CertifiedBy { get; set; } = string.Empty;
        public bool IsActive => LifecycleState == SkillLifecycleState.ACTIVE || LifecycleState == SkillLifecycleState.PROBATION;
    }

    public class ToolExecutionResolutionRequest
    {
        public string TenantId { get; set; } = string.Empty;
        public string AgentId { get; set; } = string.Empty;
        public string ToolId { get; set; } = string.Empty;
        public string InputJson { get; set; } = "{}";
        public int RiskLevel { get; set; } = 1;
    }

    public class ToolExecutionResolutionResult
    {
        public bool IsAuthorized { get; set; }
        public string CapabilityId { get; set; } = string.Empty;
        public string RejectionReason { get; set; } = string.Empty;
        public bool RequiresHumanApproval { get; set; }
        public decimal EstimatedCost { get; set; }
    }
}
