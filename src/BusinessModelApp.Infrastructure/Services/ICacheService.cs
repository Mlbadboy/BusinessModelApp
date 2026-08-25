using System;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessModelApp.Infrastructure.Services
{
    public interface ICacheService
    {
        // Basic get/set operations
        Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class;
        Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
        
        // Advanced operations
        Task<T> GetOrCreateAsync<T>(
            string key, 
            Func<Task<T>> factory, 
            TimeSpan? expiry = null, 
            CancellationToken cancellationToken = default) where T : class;
            
        Task<T> GetOrCreateAsync<T>(
            string key, 
            Func<T> factory, 
            TimeSpan? expiry = null, 
            CancellationToken cancellationToken = default) where T : class;
            
        Task<T> GetOrCreateAsync<T>(
            string key, 
            Func<Task<T>> factory, 
            Func<CacheOptions> optionsFactory, 
            CancellationToken cancellationToken = default) where T : class;
            
        Task<T> GetOrCreateAsync<T>(
            string key, 
            Func<T> factory, 
            Func<CacheOptions> optionsFactory, 
            CancellationToken cancellationToken = default) where T : class;
            
        // Batch operations
        Task<System.Collections.Generic.IDictionary<string, T>> GetManyAsync<T>(
            string[] keys, 
            CancellationToken cancellationToken = default) where T : class;
            
        Task SetManyAsync<T>(
            System.Collections.Generic.IDictionary<string, T> items, 
            TimeSpan? expiry = null, 
            CancellationToken cancellationToken = default) where T : class;
            
        // Cache management
        Task ClearAsync(CancellationToken cancellationToken = default);
        Task<long> GetRemainingTimeToLiveAsync(string key, CancellationToken cancellationToken = default);
        Task<bool> KeyExpireAsync(string key, TimeSpan? expiry, CancellationToken cancellationToken = default);
        
        // Pub/Sub
        Task PublishAsync<T>(string channel, T message, CancellationToken cancellationToken = default);
        Task SubscribeAsync<T>(string channel, Func<T, Task> handler, CancellationToken cancellationToken = default);
        
        // Distributed lock
        Task<IDistributedLock> AcquireLockAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default);
        Task<bool> ReleaseLockAsync(IDistributedLock lockObject, CancellationToken cancellationToken = default);
        
        // Metrics and stats
        Task<CacheStats> GetStatsAsync(CancellationToken cancellationToken = default);
    }
    
    public interface IDistributedLock : IAsyncDisposable, IDisposable
    {
        string LockId { get; }
        string Resource { get; }
        bool IsAcquired { get; }
        DateTime AcquiredAt { get; }
        TimeSpan TimeRemaining { get; }
        Task RenewAsync(TimeSpan expiry, CancellationToken cancellationToken = default);
    }
    
    public class CacheOptions
    {
        public TimeSpan? AbsoluteExpiration { get; set; }
        public TimeSpan? SlidingExpiration { get; set; }
        public TimeSpan? Jitter { get; set; }
        public bool RefreshOnAccess { get; set; } = true;
        public int? Priority { get; set; }
        public bool BypassCache { get; set; }
        public Func<object, TimeSpan?> CustomExpiration { get; set; }
    }
    
    public class CacheStats
    {
        public long TotalItems { get; set; }
        public long HitCount { get; set; }
        public long MissCount { get; set; }
        public long EvictionCount { get; set; }
        public long TotalSizeInBytes { get; set; }
        public DateTimeOffset? OldestItem { get; set; }
        public DateTimeOffset? NewestItem { get; set; }
        public double HitRatio => TotalHits > 0 ? (double)HitCount / TotalHits : 0;
        public long TotalHits => HitCount + MissCount;
    }
}
