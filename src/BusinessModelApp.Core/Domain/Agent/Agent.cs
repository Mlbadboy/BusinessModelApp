using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Agent
{
    public class Agent
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool IsOnline { get; set; }
        public DateTime LastActive { get; set; }
        public string Status { get; set; }
        public Dictionary<string, double> PerformanceMetrics { get; set; }
        public List<string> AssignedTasks { get; set; }
        public string Department { get; set; }
        public int CompletedTasks { get; set; }
        public double EfficiencyRating { get; set; }
        public Dictionary<string, int> SkillLevels { get; set; }
        public AgentAvailability Availability { get; set; }

        public Agent()
        {
            PerformanceMetrics = new Dictionary<string, double>();
            AssignedTasks = new List<string>();
            SkillLevels = new Dictionary<string, int>();
        }
    }

    public class AgentAvailability
    {
        public bool IsAvailable { get; set; }
        public DateTime? NextAvailableTime { get; set; }
        public string CurrentActivity { get; set; }
        public int WorkloadPercentage { get; set; }
    }
}
