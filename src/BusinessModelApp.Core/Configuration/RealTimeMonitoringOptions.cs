namespace BusinessModelApp.Core.Configuration
{
    public class RealTimeMonitoringOptions
    {
        public int UpdateIntervalSeconds { get; set; } = 60;
        public int ErrorRetryDelaySeconds { get; set; } = 300;
        public int CleanupIntervalMinutes { get; set; } = 10;
        public int InactiveTimeoutMinutes { get; set; } = 30;
    }
}
