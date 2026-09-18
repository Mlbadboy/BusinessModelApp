using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce
{
    public enum AgentAutonomyLevel
    {
        L0_MANUAL = 0,             // Human executes, AI advises
        L1_ASSISTED = 1,           // AI drafts, human reviews and executes
        L2_SUPERVISED = 2,         // AI plans and executes within safe boundaries, human approves exceptions
        L3_BOUNDED_AUTONOMOUS = 3  // AI executes fully within strict contract and budget, escalates high-risk to PRG-1
    }

    public enum AgentLifecycleStatus
    {
        DISCOVERED,
        EVALUATING,
        SANDBOXED,
        TESTING,
        RED_TEAM,
        CERTIFICATION_PENDING,
        CERTIFIED,
        SHADOW,
        PROBATION,
        ACTIVE,
        RESTRICTED,
        QUARANTINED,
        RETIRED,
        REVOKED
    }

    public class AgentQuotaAllocation
    {
        public decimal DailyBudgetCap { get; set; } = 1000m; // in INR or tenant currency
        public decimal DailySpent { get; set; } = 0m;
        public int MaxConcurrentSessions { get; set; } = 4;
        public int OaraUnitsAllocated { get; set; } = 10;
        public int MaxSpawnDepth { get; set; } = 2;
        public int MaxChildAgents { get; set; } = 3;
        public int MaxTokensPerCycle { get; set; } = 100000;
        public TimeSpan MaxExecutionTimePerMission { get; set; } = TimeSpan.FromMinutes(30);
    }

    public class AgentEmploymentContract
    {
        public string ContractId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string AgentId { get; set; } = string.Empty;
        public string AgentName { get; set; } = string.Empty;
        public string DepartmentId { get; set; } = string.Empty;
        public string TeamId { get; set; } = string.Empty;
        public string RoleTitle { get; set; } = string.Empty;
        public AgentLifecycleStatus LifecycleStatus { get; set; } = AgentLifecycleStatus.DISCOVERED;
        public AgentAutonomyLevel AutonomyLevel { get; set; } = AgentAutonomyLevel.L1_ASSISTED;
        public int RiskCeiling { get; set; } = 2; // R1 or R2 (R3 requires human PRG-1)
        public List<string> AssignedResponsibilities { get; set; } = new();
        public List<string> AllowedSkillIds { get; set; } = new();
        public List<string> AllowedToolIds { get; set; } = new();
        public List<string> AllowedConnectors { get; set; } = new();
        public List<string> ForbiddenActions { get; set; } = new()
        {
            "UnilateralExternalCommunication",
            "UnilateralPricingCommitment",
            "ContractExecution",
            "PaymentInitiation",
            "DirectCredentialAccess",
            "ExecutionPermitGeneration",
            "SelfBudgetIncrease",
            "SelfPromotion",
            "PolicyModification"
        };
        public string ManagerAgentId { get; set; } = string.Empty;
        public string EscalationPath { get; set; } = "PRG-1/HumanSupervisor";
        public AgentQuotaAllocation Quota { get; set; } = new();
        public DateTime HiredAt { get; set; } = DateTime.UtcNow;
        public DateTime? CertifiedAt { get; set; }
        public string CertifiedBy { get; set; } = string.Empty;
        public DateTime LastAuditedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive => LifecycleStatus == AgentLifecycleStatus.ACTIVE || LifecycleStatus == AgentLifecycleStatus.PROBATION;
    }
}
