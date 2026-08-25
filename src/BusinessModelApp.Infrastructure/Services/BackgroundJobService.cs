using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BusinessModelApp.Infrastructure.Services
{
    public class BackgroundJobService : IBackgroundJobService, IDisposable
    {
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IRecurringJobManager _recurringJobManager;
        private readonly ILogger<BackgroundJobService> _logger;
        private readonly BackgroundJobOptions _options;
        private bool _disposed;

        public BackgroundJobService(
            IBackgroundJobClient backgroundJobClient,
            IRecurringJobManager recurringJobManager,
            ILogger<BackgroundJobService> logger,
            IOptions<BackgroundJobOptions> options)
        {
            _backgroundJobClient = backgroundJobClient ?? throw new ArgumentNullException(nameof(backgroundJobClient));
            _recurringJobManager = recurringJobManager ?? throw new ArgumentNullException(nameof(recurringJobManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public string Enqueue<T>(string description, Action<T> methodCall) where T : class
        {
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Job description is required", nameof(description));
            if (methodCall == null)
                throw new ArgumentNullException(nameof(methodCall));

            try
            {
                var jobId = _backgroundJobClient.Enqueue<T>(job => methodCall(job));
                _logger.LogInformation("Enqueued background job {JobId}: {Description}", jobId, description);
                return jobId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enqueuing background job: {Description}", description);
                throw new InvalidOperationException($"Failed to enqueue background job: {description}", ex);
            }
        }

        public string Enqueue<T>(string description, Func<T, Task> methodCall) where T : class
        {
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Job description is required", nameof(description));
            if (methodCall == null)
                throw new ArgumentNullException(nameof(methodCall));

            try
            {
                var jobId = _backgroundJobClient.Enqueue<T>(job => methodCall(job));
                _logger.LogInformation("Enqueued async background job {JobId}: {Description}", jobId, description);
                return jobId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enqueuing async background job: {Description}", description);
                throw new InvalidOperationException($"Failed to enqueue async background job: {description}", ex);
            }
        }

        public string Schedule<T>(string description, DateTimeOffset enqueueAt, Action<T> methodCall) where T : class
        {
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Job description is required", nameof(description));
            if (methodCall == null)
                throw new ArgumentNullException(nameof(methodCall));

            try
            {
                var jobId = _backgroundJobClient.Schedule<T>(
                    job => methodCall(job),
                    enqueueAt);
                
                _logger.LogInformation("Scheduled background job {JobId} to run at {EnqueueAt}: {Description}", 
                    jobId, enqueueAt, description);
                
                return jobId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling background job: {Description}", description);
                throw new InvalidOperationException($"Failed to schedule background job: {description}", ex);
            }
        }

        public string Schedule<T>(string description, DateTimeOffset enqueueAt, Func<T, Task> methodCall) where T : class
        {
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Job description is required", nameof(description));
            if (methodCall == null)
                throw new ArgumentNullException(nameof(methodCall));

            try
            {
                var jobId = _backgroundJobClient.Schedule<T>(
                    job => methodCall(job),
                    enqueueAt);
                
                _logger.LogInformation("Scheduled async background job {JobId} to run at {EnqueueAt}: {Description}", 
                    jobId, enqueueAt, description);
                
                return jobId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling async background job: {Description}", description);
                throw new InvalidOperationException($"Failed to schedule async background job: {description}", ex);
            }
        }

        public string Schedule<T>(string description, TimeSpan delay, Action<T> methodCall) where T : class
        {
            return Schedule(description, DateTimeOffset.UtcNow.Add(delay), methodCall);
        }

        public string Schedule<T>(string description, TimeSpan delay, Func<T, Task> methodCall) where T : class
        {
            return Schedule(description, DateTimeOffset.UtcNow.Add(delay), methodCall);
        }

        public string ContinueJobWith<T>(string parentJobId, string description, Action<T> methodCall) where T : class
        {
            if (string.IsNullOrWhiteSpace(parentJobId))
                throw new ArgumentException("Parent job ID is required", nameof(parentJobId));
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Job description is required", nameof(description));
            if (methodCall == null)
                throw new ArgumentNullException(nameof(methodCall));

            try
            {
                var jobId = _backgroundJobClient.ContinueJobWith<T>(
                    parentJobId,
                    job => methodCall(job));
                
                _logger.LogInformation("Created continuation job {JobId} after parent {ParentJobId}: {Description}", 
                    jobId, parentJobId, description);
                
                return jobId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating continuation job for parent {ParentJobId}: {Description}", 
                    parentJobId, description);
                throw new InvalidOperationException($"Failed to create continuation job: {description}", ex);
            }
        }

        public string ContinueJobWith<T>(string parentJobId, string description, Func<T, Task> methodCall) where T : class
        {
            if (string.IsNullOrWhiteSpace(parentJobId))
                throw new ArgumentException("Parent job ID is required", nameof(parentJobId));
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Job description is required", nameof(description));
            if (methodCall == null)
                throw new ArgumentNullException(nameof(methodCall));

            try
            {
                var jobId = _backgroundJobClient.ContinueJobWith<T>(
                    parentJobId,
                    job => methodCall(job));
                
                _logger.LogInformation("Created async continuation job {JobId} after parent {ParentJobId}: {Description}", 
                    jobId, parentJobId, description);
                
                return jobId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating async continuation job for parent {ParentJobId}: {Description}", 
                    parentJobId, description);
                throw new InvalidOperationException($"Failed to create async continuation job: {description}", ex);
            }
        }

        public bool Delete(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId))
                return false;

            try
            {
                bool deleted = _backgroundJobClient.Delete(jobId);
                if (deleted)
                {
                    _logger.LogInformation("Deleted background job {JobId}", jobId);
                }
                else
                {
                    _logger.LogWarning("Failed to delete background job {JobId} - job not found or already deleted", jobId);
                }
                return deleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting background job {JobId}", jobId);
                return false;
            }
        }

        public bool Requeue(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId))
                return false;

            try
            {
                bool requeued = _backgroundJobClient.Requeue(jobId);
                if (requeued)
                {
                    _logger.LogInformation("Requeued background job {JobId}", jobId);
                }
                else
                {
                    _logger.LogWarning("Failed to requeue background job {JobId} - job not found or in progress", jobId);
                }
                return requeued;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requeueing background job {JobId}", jobId);
                return false;
            }
        }

        public async Task<bool> WaitAsync(string jobId, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(jobId))
                return false;

            try
            {
                using (var timeoutCts = new CancellationTokenSource(timeout))
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token))
                {
                    var jobManager = new BackgroundJobClientWrapper(_backgroundJobClient);
                    await jobManager.WaitJobCompletionAsync(jobId, linkedCts.Token);
                    return true;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Waiting for job {JobId} was cancelled by the user", jobId);
                return false;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Timeout while waiting for job {JobId} to complete", jobId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while waiting for job {JobId} to complete", jobId);
                return false;
            }
        }

        public string CreateRecurringJob<T>(
            string recurringJobId,
            string description,
            string cronExpression,
            Action<T> methodCall,
            TimeZoneInfo timeZone = null,
            string queue = "default") where T : class
        {
            if (string.IsNullOrWhiteSpace(recurringJobId))
                throw new ArgumentException("Recurring job ID is required", nameof(recurringJobId));
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Job description is required", nameof(description));
            if (string.IsNullOrWhiteSpace(cronExpression))
                throw new ArgumentException("Cron expression is required", nameof(cronExpression));
            if (methodCall == null)
                throw new ArgumentNullException(nameof(methodCall));

            try
            {
                var timeZoneToUse = timeZone ?? TimeZoneInfo.Utc;
                
                _recurringJobManager.AddOrUpdate<T>(
                    recurringJobId,
                    job => methodCall(job),
                    cronExpression,
                    timeZoneToUse,
                    queue);
                
                _logger.LogInformation("Created/updated recurring job {RecurringJobId} with schedule {CronExpression} in timezone {TimeZone}: {Description}", 
                    recurringJobId, cronExpression, timeZoneToUse.Id, description);
                
                return recurringJobId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating/updating recurring job {RecurringJobId}: {Description}", 
                    recurringJobId, description);
                throw new InvalidOperationException($"Failed to create/update recurring job: {description}", ex);
            }
        }

        public string CreateRecurringJob<T>(
            string recurringJobId,
            string description,
            string cronExpression,
            Func<T, Task> methodCall,
            TimeZoneInfo timeZone = null,
            string queue = "default") where T : class
        {
            if (string.IsNullOrWhiteSpace(recurringJobId))
                throw new ArgumentException("Recurring job ID is required", nameof(recurringJobId));
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Job description is required", nameof(description));
            if (string.IsNullOrWhiteSpace(cronExpression))
                throw new ArgumentException("Cron expression is required", nameof(cronExpression));
            if (methodCall == null)
                throw new ArgumentNullException(nameof(methodCall));

            try
            {
                var timeZoneToUse = timeZone ?? TimeZoneInfo.Utc;
                
                _recurringJobManager.AddOrUpdate<T>(
                    recurringJobId,
                    job => methodCall(job),
                    cronExpression,
                    timeZoneToUse,
                    queue);
                
                _logger.LogInformation("Created/updated async recurring job {RecurringJobId} with schedule {CronExpression} in timezone {TimeZone}: {Description}", 
                    recurringJobId, cronExpression, timeZoneToUse.Id, description);
                
                return recurringJobId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating/updating async recurring job {RecurringJobId}: {Description}", 
                    recurringJobId, description);
                throw new InvalidOperationException($"Failed to create/update async recurring job: {description}", ex);
            }
        }

        public bool RemoveRecurringJob(string recurringJobId)
        {
            if (string.IsNullOrWhiteSpace(recurringJobId))
                return false;

            try
            {
                _recurringJobManager.RemoveIfExists(recurringJobId);
                _logger.LogInformation("Removed recurring job {RecurringJobId}", recurringJobId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing recurring job {RecurringJobId}", recurringJobId);
                return false;
            }
        }

        public bool TriggerRecurringJob(string recurringJobId)
        {
            if (string.IsNullOrWhiteSpace(recurringJobId))
                return false;

            try
            {
                _recurringJobManager.Trigger(recurringJobId);
                _logger.LogInformation("Manually triggered recurring job {RecurringJobId}", recurringJobId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering recurring job {RecurringJobId}", recurringJobId);
                return false;
            }
        }

        public JobDetails GetJobDetails(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId))
                return null;

            try
            {
                var jobMonitoringApi = JobStorage.Current.GetMonitoringApi();
                var jobDetails = jobMonitoringApi.JobDetails(jobId);
                
                if (jobDetails == null)
                    return null;

                var history = jobDetails.History.LastOrDefault();
                
                return new JobDetails
                {
                    Id = jobId,
                    State = history?.StateName,
                    CreatedAt = jobDetails.CreatedAt,
                    StartedAt = history?.StartedAt,
                    CompletedAt = history?.SucceededAt,
                    Status = history?.StateName,
                    Error = history?.ExceptionMessage,
                    Queue = jobDetails.Queue,
                    MethodName = jobDetails.Job?.Method?.Name,
                    TypeName = jobDetails.Job?.Type?.Name,
                    Arguments = jobDetails.Job?.Args,
                    Duration = history?.Duration,
                    RetryAttempt = history?.RetryCount,
                    MaxRetries = jobDetails.Job?.RetryCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting details for job {JobId}", jobId);
                return null;
            }
        }

        public RecurringJobDetails GetRecurringJobDetails(string recurringJobId)
        {
            if (string.IsNullOrWhiteSpace(recurringJobId))
                return null;

            try
            {
                var jobMonitoringApi = JobStorage.Current.GetMonitoringApi();
                var recurringJobs = jobMonitoringApi.RecurringJobs(0, int.MaxValue);
                var recurringJob = recurringJobs.Find(x => x.Id == recurringJobId);
                
                if (recurringJob == null)
                    return null;

                return new RecurringJobDetails
                {
                    Id = recurringJob.Id,
                    Cron = recurringJob.Cron,
                    TimeZone = TimeZoneInfo.FindSystemTimeZoneById(recurringJob.TimeZoneId ?? "UTC"),
                    Queue = recurringJob.Queue,
                    LastExecution = recurringJob.LastExecution,
                    NextExecution = recurringJob.NextExecution,
                    Status = recurringJob.LastJobState,
                    Error = recurringJob.Error,
                    MethodName = recurringJob.Job?.Method?.Name,
                    TypeName = recurringJob.Job?.Type?.Name,
                    Arguments = recurringJob.Job?.Args,
                    CreatedAt = recurringJob.CreatedAt,
                    Removed = recurringJob.Removed,
                    LastJobId = recurringJob.LastJobId,
                    LastJobState = recurringJob.LastJobState
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting details for recurring job {RecurringJobId}", recurringJobId);
                return null;
            }
        }

        #region IDisposable Implementation

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    if (_backgroundJobClient is IDisposable disposableClient)
                    {
                        disposableClient.Dispose();
                    }
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        ~BackgroundJobService()
        {
            Dispose(disposing: false);
        }

        #endregion

        #region Helper Classes

        private class BackgroundJobClientWrapper
        {
            private readonly IBackgroundJobClient _client;

            public BackgroundJobClientWrapper(IBackgroundJobClient client)
            {
                _client = client ?? throw new ArgumentNullException(nameof(client));
            }

            public async Task WaitJobCompletionAsync(string jobId, CancellationToken cancellationToken)
            {
                if (string.IsNullOrEmpty(jobId))
                    throw new ArgumentNullException(nameof(jobId));

                var jobManager = JobStorage.Current.GetMonitoringApi();
                var isCompleted = false;

                while (!isCompleted && !cancellationToken.IsCancellationRequested)
                {
                    var jobDetails = jobManager.JobDetails(jobId);
                    var state = jobDetails?.History?.LastOrDefault()?.StateName;

                    if (state == "Succeeded" || state == "Scheduled")
                    {
                        isCompleted = true;
                    }
                    else if (state == "Failed" || state == "Deleted")
                    {
                        throw new InvalidOperationException($"Job {jobId} failed or was deleted");
                    }
                    else
                    {
                        await Task.Delay(1000, cancellationToken);
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        #endregion
    }

    public class BackgroundJobOptions
    {
        public int RetryAttempts { get; set; } = 3;
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMinutes(5);
        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMinutes(30);
        public string[] Queues { get; set; } = new[] { "default" };
    }
}
