using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessModelApp.Core.Interfaces
{
    public interface ICacheService
    {
        Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default);
        Task SetAsync<T>(string key, T value, MemoryCacheEntryOptions options, CancellationToken cancellationToken = default);
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);
        Task<CacheStats> GetStatsAsync(CancellationToken cancellationToken = default);
    }

    public class CacheStats
    {
        public long TotalCacheHits { get; set; }
        public long TotalCacheMisses { get; set; }
        public long CurrentEntryCount { get; set; }
        public double HitRatio { get; set; }
        public long CurrentSize { get; set; }
        public long SizeLimit { get; set; }
    }
}
