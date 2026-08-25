using System;

namespace BusinessModelApp.Core.Dtos.Analytics
{
    public class FinancialMetricDto
    {
        public string Name { get; set; }
        public decimal Value { get; set; }
        public string Unit { get; set; }
        public DateTime Period { get; set; }
    }
}
