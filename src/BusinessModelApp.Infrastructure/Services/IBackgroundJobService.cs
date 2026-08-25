using System;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessModelApp.Infrastructure.Services
{
    public interface IBackgroundJobService
    {
        string Enqueue<T>(string description, Action<T> methodCall) where T : class;
        string Enqueue<T>(string description, Func<T, Task> methodCall) where T : class;
        
        string Schedule<T>(string description, DateTimeOffset enqueueAt, Action<T> methodCall) where T : class;
        string Schedule<T>(string description, DateTimeOffset enqueueAt, Func<T, Task> methodCall) where T : class;
        string Schedule<T>(string description, TimeSpan delay, Action<T> methodCall) where T : class;
        string Schedule<T>(string description, TimeSpan delay, Func<T, Task> methodCall) where T : class;
        
        string ContinueJobWith<T>(string parentJobId, string description, Action<T> methodCall) where T : class;
        string ContinueJobWith<T>(string parentJobId, string description, Func<T, Task> methodCall) where T : class;
        
        bool Delete(string jobId);
        bool Requeue(string jobId);
        
        Task<bool> WaitAsync(string jobId, TimeSpan timeout, CancellationToken cancellationToken = default);
        
        string CreateRecurringJob<T>(
            string recurringJobId,
            string description,
            string cronExpression,
            Action<T> methodCall,
            TimeZoneInfo timeZone = null,
            string queue = "default") where T : class;
            
        string CreateRecurringJob<T>(
            string recurringJobId,
            string description,
            string cronExpression,
            Func<T, Task> methodCall,
            TimeZoneInfo timeZone = null,
            string queue = "default") where T : class;
            
        bool RemoveRecurringJob(string recurringJobId);
        bool TriggerRecurringJob(string recurringJobId);
        
        JobDetails GetJobDetails(string jobId);
        RecurringJobDetails GetRecurringJobDetails(string recurringJobId);
    }
    
    public class JobDetails
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public string State { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; }
        public string Error { get; set; }
        public string Queue { get; set; }
        public string MethodName { get; set; }
        public string TypeName { get; set; }
        public object[] Arguments { get; set; }
        public TimeSpan? Duration { get; set; }
        public int? RetryAttempt { get; set; }
        public int? MaxRetries { get; set; }
    }
    
    public class RecurringJobDetails
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public string Cron { get; set; }
        public TimeZoneInfo TimeZone { get; set; }
        public string Queue { get; set; }
        public DateTime? LastExecution { get; set; }
        public DateTime? NextExecution { get; set; }
        public string Status { get; set; }
        public string Error { get; set; }
        public string MethodName { get; set; }
        public string TypeName { get; set; }
        public object[] Arguments { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool Removed { get; set; }
        public string LastJobId { get; set; }
        public string LastJobState { get; set; }
    }
}
