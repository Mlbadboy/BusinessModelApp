namespace BusinessModelApp.Core.DTOs.Security
{
    public class RateLimitDto
    {
        public string Endpoint { get; set; }
        public int Limit { get; set; }
        public string Timespan { get; set; }
    }
}
