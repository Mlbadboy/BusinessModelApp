using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.DTOs.Strategy
{
    public class BusinessStrategyDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Vision { get; set; } = string.Empty;
        public string Mission { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        public string Status { get; set; } = "Draft";
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<StrategyGoalDto> Goals { get; set; } = new();
        public List<StrategyActionDto> Actions { get; set; } = new();
    }
}
