using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Agents
{
    public enum MissionMode
    {
        Simulation = 0,
        LiveProduction = 1
    }

    public enum MissionStatus
    {
        Draft = 0,
        Running = 1,
        Paused = 2,
        Completed = 3,
        Aborted = 4
    }

    public class AgentMission
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid WorkspaceId { get; set; }
        public Guid OrganizationId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Objective { get; set; } = string.Empty;
        public string TargetIndustry { get; set; } = "Enterprise BFSI";
        public int TargetProspectCount { get; set; } = 25;
        public decimal TargetValueINR { get; set; } = 2500000m;

        public MissionMode Mode { get; set; } = MissionMode.Simulation;
        public AutonomyLevel AutonomyLevel { get; set; } = AutonomyLevel.Level3_ControlledAutonomy;
        public MissionStatus Status { get; set; } = MissionStatus.Draft;

        public MissionWallet Wallet { get; set; } = MissionWallet.CreateDefault(5000m);
        public AgentMemory Memory { get; set; } = new AgentMemory();
        public List<AgentTask> Tasks { get; set; } = new List<AgentTask>();

        // Real-time Pipeline Counters
        public int CompaniesResearched { get; set; } = 0;
        public int ProspectsDiscovered { get; set; } = 0;
        public int QualifiedCount { get; set; } = 0;
        public int OutreachSent { get; set; } = 0;
        public int ResponsesReceived { get; set; } = 0;
        public int PositiveConversations { get; set; } = 0;
        public int OpportunitiesCreated { get; set; } = 0;
        public decimal PipelineValueGeneratedINR { get; set; } = 0m;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
