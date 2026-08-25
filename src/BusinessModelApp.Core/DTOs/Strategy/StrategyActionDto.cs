using System;

namespace BusinessModelApp.Core.DTOs.Strategy
{
    public class StrategyActionDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime DueDate { get; set; }
        public string AssignedTo { get; set; }
        public double Progress { get; set; }
    }
}
