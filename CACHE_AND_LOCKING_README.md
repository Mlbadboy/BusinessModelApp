# Caching and Distributed Locking Infrastructure

This document provides an overview of the caching and distributed locking infrastructure implemented in the BusinessModelApp solution.

## Table of Contents
1. [Overview](#overview)
2. [Memory Cache Service](#memory-cache-service)
   - [Features](#features)
   - [Usage](#usage)
   - [Configuration](#configuration)
3. [Distributed Lock Service](#distributed-lock-service)
   - [Features](#features-1)
   - [Usage](#usage-1)
   - [Configuration](#configuration-1)
4. [Background Cleanup Service](#background-cleanup-service)
   - [Features](#features-2)
   - [Usage](#usage-2)
   - [Configuration](#configuration-2)
5. [Best Practices](#best-practices)
6. [Troubleshooting](#troubleshooting)

## Overview

The solution includes a robust caching and distributed locking infrastructure that can be used to improve performance and ensure thread safety in a distributed environment. The main components are:

- **IMemoryCacheService**: A thread-safe in-memory cache with support for various caching patterns.
- **IDistributedLockService**: A distributed locking mechanism built on top of the memory cache.
- **CleanupBackgroundService**: A background service that performs periodic cleanup tasks with distributed locking.

## Memory Cache Service

### Features

- Thread-safe operations
- Support for various expiration policies (absolute, sliding, etc.)
- Size-based eviction
- Cache statistics and monitoring
- Batch operations
- Cache entry size calculation
- Cache stampede protection

### Usage

#### Basic Usage

```csharp
public class MyService
{
    private readonly IMemoryCacheService _cache;
    
    public MyService(IMemoryCacheService cache)
    {
        _cache = cache;
    }
    
    public async Task<MyData> GetDataAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrCreateAsync(key, async () =>
        {
            // This delegate will only execute if the key doesn't exist in cache
            return await FetchDataFromDatabaseAsync(key);
        }, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        }, cancellationToken);
    }
}
```

#### Cache Entry Options

```csharp
var options = new MemoryCacheEntryOptions
{
    // Absolute expiration
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
    
    // Sliding expiration
    SlidingExpiration = TimeSpan.FromMinutes(10),
    
    // Priority (affects eviction order)
    Priority = CacheItemPriority.High,
    
    // Size (for size-based eviction)
    Size = CalculateSize(myData)
};

// Add callbacks
options.RegisterPostEvictionCallback((key, value, reason, state) =>
{
    // Handle eviction
});

await _cache.SetAsync("myKey", myData, options);
```

### Configuration

Register the memory cache service in `Startup.cs`:

```csharp
services.AddMemoryCacheService(options =>
{
    options.SizeLimitInMegabytes = 100; // 100MB cache limit
    options.DefaultExpiration = TimeSpan.FromMinutes(15);
});
```

## Distributed Lock Service

### Features

- Distributed locking across application instances
- Deadlock prevention
- Automatic lock cleanup
- Reentrancy support
- Configurable timeouts and retries

### Usage

```csharp
public class ResourceProcessor
{
    private readonly IDistributedLockService _lockService;
    
    public ResourceProcessor(IDistributedLockService lockService)
    {
        _lockService = lockService;
    }
    
    public async Task ProcessResourceAsync(string resourceId)
    {
        var lockKey = $"process:{resourceId}";
        
        using (var lockHandle = await _lockService.AcquireLockAsync(
            lockKey, 
            TimeSpan.FromSeconds(30), // Lock expiry
            TimeSpan.FromSeconds(10), // Timeout
            retryDelay: TimeSpan.FromMilliseconds(100),
            maxRetryCount: 3))
        {
            if (lockHandle == null)
            {
                throw new Exception("Could not acquire lock for processing");
            }
            
            try 
            {
                // Critical section - only one process can execute this at a time per resourceId
                await ProcessResourceInternalAsync(resourceId);
            }
            finally
            {
                await lockHandle.ReleaseAsync();
            }
        }
    }
}
```

### Configuration

Register the distributed lock service in `Startup.cs`:

```csharp
services.AddDistributedLockService(options =>
{
    options.DefaultExpiry = TimeSpan.FromSeconds(30);
    options.DefaultTimeout = TimeSpan.FromSeconds(10);
    options.RetryDelay = TimeSpan.FromMilliseconds(100);
    options.MaxRetryCount = 3;
});
```

## Background Cleanup Service

### Features

- Periodic execution of cleanup tasks
- Distributed locking to prevent multiple instances from running simultaneously
- Configurable cleanup intervals and retention periods
- Graceful shutdown support

### Usage

The background cleanup service is automatically registered and started when the application starts. It will run the configured cleanup tasks at the specified interval.

### Configuration

Configure the background cleanup service in `appsettings.json`:

```json
{
  "CleanupBackgroundService": {
    "Interval": "01:00:00",  // Run every hour
    "LockTimeout": "00:00:30",
    "RetryDelay": "00:00:05",
    "MaxRetryCount": 3,
    "CleanupExpiredSessions": true,
    "CleanupOldAuditLogs": true,
    "AuditLogRetentionDays": 90,
    "ThrowOnTaskError": false
  }
}
```

Or configure it programmatically in `Startup.cs`:

```csharp
services.AddBackgroundCleanupService(options =>
{
    options.Interval = TimeSpan.FromHours(1);
    options.LockTimeout = TimeSpan.FromSeconds(30);
    options.RetryDelay = TimeSpan.FromSeconds(5);
    options.MaxRetryCount = 3;
    options.CleanupExpiredSessions = true;
    options.CleanupOldAuditLogs = true;
    options.AuditLogRetentionDays = 90;
    options.ThrowOnTaskError = false;
});
```

## Best Practices

### Caching Best Practices

1. **Cache Invalidation**: Always have a strategy for cache invalidation when data changes.
2. **Cache Stampede**: Use the `GetOrCreateAsync` pattern with proper locking to prevent cache stampede.
3. **Memory Management**: Be mindful of cache size limits and set appropriate expiration policies.
4. **Monitoring**: Monitor cache hit/miss ratios and adjust cache sizes and expiration policies as needed.
5. **Fallback**: Always handle cache misses gracefully by falling back to the data source.

### Distributed Locking Best Practices

1. **Short Locks**: Keep lock durations as short as possible to reduce contention.
2. **Timeouts**: Always use timeouts to prevent deadlocks.
3. **Retry Logic**: Implement appropriate retry logic with backoff for lock acquisition.
4. **Cleanup**: Ensure locks are always released, even if an exception occurs.
5. **Lock Granularity**: Use fine-grained locks to reduce contention.

### Background Service Best Practices

1. **Idempotency**: Ensure cleanup tasks are idempotent to handle retries and failures.
2. **Error Handling**: Implement proper error handling and logging.
3. **Graceful Shutdown**: Handle cancellation tokens properly to allow for graceful shutdown.
4. **Monitoring**: Log the start and completion of cleanup tasks for monitoring.

## Troubleshooting

### Common Issues

1. **High Cache Miss Rate**
   - Check if the cache size is sufficient
   - Review expiration policies
   - Consider pre-warming the cache for frequently accessed data

2. **Lock Timeouts**
   - Increase the lock timeout if operations take longer than expected
   - Optimize the critical section to reduce lock duration
   - Check for deadlocks in the application

3. **Memory Pressure**
   - Monitor cache size and adjust limits as needed
   - Review the size calculation for cached items
   - Consider using a distributed cache for larger datasets

### Logging

All services use the standard `ILogger<T>` interface for logging. Ensure that logging is properly configured in your application to capture important events and errors.

### Metrics

The memory cache service provides statistics that can be used for monitoring:

```csharp
var stats = await _cache.GetStatsAsync();
Console.WriteLine($"Hit ratio: {stats.HitRatio:P2}");
Console.WriteLine($"Current size: {stats.CurrentSize} / {stats.SizeLimit}");
```

## Conclusion

The caching and distributed locking infrastructure provides a robust foundation for building scalable and reliable applications. By following the best practices outlined in this document, you can ensure optimal performance and reliability in your application.
