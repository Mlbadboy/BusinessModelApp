using System;

namespace BusinessModelApp.Core.Dtos.Analytics
{
    public class BusinessKPIDto
    {
        public string Name { get; set; }
        public decimal Value { get; set; }
        public decimal Target { get; set; }
        public string Unit { get; set; }
    }
}
