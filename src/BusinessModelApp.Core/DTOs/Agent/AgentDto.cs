using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.DTOs.Agent
{
    public class AgentDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool IsOnline { get; set; }
        public string LastActive { get; set; }
        public string Status { get; set; }
        public IDictionary<string, double> Metrics { get; set; }
        public string Department { get; set; }
        public AgentStatsDto Stats { get; set; }
        public AgentAvailabilityDto Availability { get; set; }
        public ICollection<AgentSkillDto> Skills { get; set; }
    }

    public class AgentStatsDto
    {
        public int CompletedTasks { get; set; }
        public double EfficiencyRating { get; set; }
        public double ResponseTime { get; set; }
        public double TaskSuccessRate { get; set; }
        public int ActiveTasks { get; set; }
        public double AverageTaskDuration { get; set; }
    }

    public class AgentAvailabilityDto
    {
        public bool IsAvailable { get; set; }
        public string NextAvailableTime { get; set; }
        public string CurrentActivity { get; set; }
        public int WorkloadPercentage { get; set; }
        public string ShiftStatus { get; set; }
    }

    public class AgentSkillDto
    {
        public string Name { get; set; }
        public int Level { get; set; }
        public DateTime LastUsed { get; set; }
        public int TimesUsed { get; set; }
    }

    public class AgentMonitoringDto
    {
        public Guid AgentId { get; set; }
        public string Status { get; set; }
        public IDictionary<string, double> RealTimeMetrics { get; set; }
        public ICollection<string> ActiveAlerts { get; set; }
        public string CurrentTaskId { get; set; }
        public double PerformanceScore { get; set; }
        public IDictionary<string, string> SystemResources { get; set; }
    }

    public class AgentUpdateDto
    {
        public string Status { get; set; }
        public IDictionary<string, double> Metrics { get; set; }
        public AgentAvailabilityDto Availability { get; set; }
    }

    public class CreateAgentDto
    {
        public string Name { get; set; }
        public string Department { get; set; }
        public IDictionary<string, int> InitialSkills { get; set; }
    }
}
