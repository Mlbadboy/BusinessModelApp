namespace BusinessModelApp.Core.DTOs
{
    public class Recommendation
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Category { get; set; }
        public double ConfidenceScore { get; set; }
        public string Reasoning { get; set; }
        public string Source { get; set; }
        public string ActionLink { get; set; }
    }

}
