using BusinessModelApp.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessModelApp.Infrastructure.Services
{
    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<MemoryCacheService> _logger;
        private long _hits;
        private long _misses;

        public MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            if (_cache.TryGetValue(key, out T value))
            {
                Interlocked.Increment(ref _hits);
                _logger.LogTrace("Cache hit for key: {Key}", key);
                return Task.FromResult(value);
            }

            Interlocked.Increment(ref _misses);
            _logger.LogTrace("Cache miss for key: {Key}", key);
            return Task.FromResult<T>(default);
        }

        public Task SetAsync<T>(string key, T value, MemoryCacheEntryOptions options, CancellationToken cancellationToken = default)
        {
            _cache.Set(key, value, options ?? new MemoryCacheEntryOptions());
            _logger.LogTrace("Cache set for key: {Key}", key);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _cache.Remove(key);
            _logger.LogTrace("Cache entry removed for key: {Key}", key);
            return Task.CompletedTask;
        }

        public Task<CacheStats> GetStatsAsync(CancellationToken cancellationToken = default)
        {
            long total = _hits + _misses;
            double hitRatio = total > 0 ? (double)_hits / total : 0;

            var stats = new CacheStats
            {
                TotalCacheHits = _hits,
                TotalCacheMisses = _misses,
                HitRatio = hitRatio,
                CurrentEntryCount = 0,
                CurrentSize = 0,
                SizeLimit = 0
            };

            return Task.FromResult(stats);
        }
    }
}
