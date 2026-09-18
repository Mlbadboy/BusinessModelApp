using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce
{
    public class AgentDefinition
    {
        public string DefinitionId { get; set; } = Guid.NewGuid().ToString("N");
        public string RoleTitle { get; set; } = string.Empty;
        public string SystemPromptTemplate { get; set; } = string.Empty;
        public string DefaultModelId { get; set; } = "omniroute-default";
        public List<string> RequiredSkillIds { get; set; } = new();
        public List<string> RequiredToolIds { get; set; } = new();
        public int RequiredRiskCeiling { get; set; } = 2;
        public AgentAutonomyLevel AutonomyLevel { get; set; } = AgentAutonomyLevel.L1_ASSISTED;
    }

    public class AgentInstance
    {
        public string InstanceId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string AgentId { get; set; } = string.Empty;
        public string DefinitionId { get; set; } = string.Empty;
        public string? ParentInstanceId { get; set; }
        public int SpawnDepth { get; set; } = 0;
        public List<string> ChildInstanceIds { get; set; } = new();
        public string CurrentMissionId { get; set; } = string.Empty;
        public AgentLifecycleStatus Status { get; set; } = AgentLifecycleStatus.ACTIVE;
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? TerminatedAt { get; set; }
    }

    public class AgentSession
    {
        public string SessionId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string AgentId { get; set; } = string.Empty;
        public string InstanceId { get; set; } = string.Empty;
        public string MissionId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? EndedAt { get; set; }
        public bool IsActive => !EndedAt.HasValue;
    }

    public class AgentTrajectoryStep
    {
        public string StepId { get; set; } = Guid.NewGuid().ToString("N");
        public int StepIndex { get; set; }
        public string Thought { get; set; } = string.Empty;
        public string ProposedAction { get; set; } = string.Empty;
        public string ToolName { get; set; } = string.Empty;
        public string ToolInputJson { get; set; } = "{}";
        public string ToolOutputJson { get; set; } = "{}";
        public bool IsSuccess { get; set; }
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    }

    public class AgentTrajectory
    {
        public string TrajectoryId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public string AgentId { get; set; } = string.Empty;
        public string MissionId { get; set; } = string.Empty;
        public List<AgentTrajectoryStep> Steps { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class AgentMemoryEntry
    {
        public string MemoryId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string AgentId { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string MemoryScope { get; set; } = "AgentLocal"; // AgentLocal, SessionContext, MissionContext
        public bool IsCorroborated { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class AgentSpawnRequest
    {
        public string RequestId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string ParentAgentId { get; set; } = string.Empty;
        public string TargetRoleTitle { get; set; } = string.Empty;
        public string MissionId { get; set; } = string.Empty;
        public int RequestedOaraUnits { get; set; } = 2;
    }

    public class AgentSpawnPolicy
    {
        public int MaxSpawnDepth { get; set; } = 2;
        public int MaxChildAgents { get; set; } = 3;
        public int MaxActiveAgentsPerTenant { get; set; } = 32;
    }
}
