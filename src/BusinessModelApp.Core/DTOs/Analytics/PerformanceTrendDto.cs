using System;

namespace BusinessModelApp.Core.DTOs.Analytics
{
    public class PerformanceTrendDto
    {
        public DateTime Date { get; set; }
        public decimal Value { get; set; }
        public string Description { get; set; }
        public double Target { get; set; }
        public double Variance { get; set; }
    }
}
