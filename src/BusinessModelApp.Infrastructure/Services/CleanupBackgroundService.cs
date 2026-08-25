using BusinessModelApp.Infrastructure.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessModelApp.Infrastructure.Services
{
    public class CleanupBackgroundService : BackgroundService
    {
        private readonly IDistributedLockService _lockService;
        private readonly ILogger<CleanupBackgroundService> _logger;
        private readonly CleanupBackgroundServiceOptions _options;
        private readonly IServiceProvider _serviceProvider;

        public CleanupBackgroundService(
            IDistributedLockService lockService,
            IOptions<CleanupBackgroundServiceOptions> options,
            IServiceProvider serviceProvider,
            ILogger<CleanupBackgroundService> logger)
        {
            _lockService = lockService ?? throw new ArgumentNullException(nameof(lockService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Cleanup background service is starting");

            // Initial delay to allow the application to start up
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunCleanupWithLockAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Service is stopping
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in cleanup background service");
                }

                // Wait for the next interval
                await Task.Delay(_options.Interval, stoppingToken);
            }

            _logger.LogInformation("Cleanup background service is stopping");
        }

        private async Task RunCleanupWithLockAsync(CancellationToken cancellationToken)
        {
            var lockKey = "background:cleanup:lock";
            var lockExpiry = _options.Interval + TimeSpan.FromMinutes(5); // Add buffer time

            _logger.LogDebug("Attempting to acquire cleanup lock");

            using (var lockHandle = await _lockService.AcquireLockAsync(
                lockKey,
                lockExpiry,
                _options.LockTimeout,
                _options.RetryDelay,
                _options.MaxRetryCount,
                cancellationToken: cancellationToken))
            {
                if (lockHandle == null)
                {
                    _logger.LogDebug("Could not acquire cleanup lock, another instance is likely running");
                    return;
                }

                try
                {
                    _logger.LogInformation("Acquired cleanup lock, starting cleanup process");
                    
                    // Run the actual cleanup tasks
                    await RunCleanupTasksAsync(cancellationToken);
                    
                    _logger.LogInformation("Cleanup process completed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during cleanup process");
                    throw;
                }
            }
        }

        private async Task RunCleanupTasksAsync(CancellationToken cancellationToken)
        {
            // Example: Clean up expired sessions
            if (_options.CleanupExpiredSessions)
            {
                try
                {
                    _logger.LogDebug("Starting expired sessions cleanup");
                    // var sessionService = _serviceProvider.GetRequiredService<ISessionService>();
                    // var count = await sessionService.CleanupExpiredSessionsAsync(cancellationToken);
                    // _logger.LogInformation("Cleaned up {ExpiredSessionCount} expired sessions", count);
                    await Task.Delay(1000, cancellationToken); // Simulate work
                    _logger.LogInformation("Completed expired sessions cleanup");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error cleaning up expired sessions");
                    if (_options.ThrowOnTaskError)
                        throw;
                }
            }

            // Example: Clean up old audit logs
            if (_options.CleanupOldAuditLogs && _options.AuditLogRetentionDays > 0)
            {
                try
                {
                    _logger.LogDebug("Starting old audit logs cleanup");
                    // var auditService = _serviceProvider.GetRequiredService<IAuditService>();
                    var cutoffDate = DateTime.UtcNow.AddDays(-_options.AuditLogRetentionDays);
                    // var count = await auditService.DeleteAuditLogsOlderThanAsync(cutoffDate, cancellationToken);
                    // _logger.LogInformation("Cleaned up {AuditLogCount} old audit logs", count);
                    await Task.Delay(1000, cancellationToken); // Simulate work
                    _logger.LogInformation("Completed old audit logs cleanup");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error cleaning up old audit logs");
                    if (_options.ThrowOnTaskError)
                        throw;
                }
            }

            // Add more cleanup tasks as needed
        }
    }

    public class CleanupBackgroundServiceOptions
    {
        /// <summary>
        /// Gets or sets the interval between cleanup runs. Default is 1 hour.
        /// </summary>
        public TimeSpan Interval { get; set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// Gets or sets the lock timeout when trying to acquire the cleanup lock. Default is 30 seconds.
        /// </summary>
        public TimeSpan LockTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the delay between lock acquisition retries. Default is 5 seconds.
        /// </summary>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets or sets the maximum number of retry attempts for lock acquisition. Default is 3.
        /// </summary>
        public int MaxRetryCount { get; set; } = 3;

        /// <summary>
        /// Gets or sets a value indicating whether to clean up expired sessions. Default is true.
        /// </summary>
        public bool CleanupExpiredSessions { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to clean up old audit logs. Default is true.
        /// </summary>
        public bool CleanupOldAuditLogs { get; set; } = true;

        /// <summary>
        /// Gets or sets the number of days to retain audit logs. Default is 90 days.
        /// </summary>
        public int AuditLogRetentionDays { get; set; } = 90;

        /// <summary>
        /// Gets or sets a value indicating whether to throw an exception if a cleanup task fails.
        /// If false, errors will be logged but won't stop other cleanup tasks. Default is false.
        /// </summary>
        public bool ThrowOnTaskError { get; set; } = false;
    }
}
