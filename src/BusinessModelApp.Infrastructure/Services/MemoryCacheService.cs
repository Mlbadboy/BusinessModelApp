using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessModelApp.Infrastructure.Services
{
    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<MemoryCacheService> _logger;
        private readonly Dictionary<string, List<Func<object, Task>>> _subscribers;
        private long _hits;
        private long _misses;
        private long _evictions;

        public MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _subscribers = new Dictionary<string, List<Func<object, Task>>>();
        }

        public Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
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

        public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
        {
            var options = new MemoryCacheEntryOptions();
            if (expiry.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiry;
            }

            _cache.Set(key, value, options);
            _logger.LogTrace("Cache set for key: {Key}", key);
            return Task.CompletedTask;
        }

        public Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            if (_cache.TryGetValue(key, out _))
            {
                _cache.Remove(key);
                _logger.LogTrace("Cache entry removed for key: {Key}", key);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_cache.TryGetValue(key, out _));
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
        {
            if (await GetAsync<T>(key, cancellationToken) is T cachedValue)
            {
                return cachedValue;
            }

            var value = await factory();
            await SetAsync(key, value, expiry, cancellationToken);
            return value;
        }

        public Task<T> GetOrCreateAsync<T>(string key, Func<T> factory, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
        {
            return GetOrCreateAsync(key, () => Task.FromResult(factory()), expiry, cancellationToken);
        }

        public Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, Func<CacheOptions> optionsFactory, CancellationToken cancellationToken = default) where T : class
        {
            var options = optionsFactory();
            return GetOrCreateAsync(key, factory, options.AbsoluteExpiration, cancellationToken);
        }

        public Task<T> GetOrCreateAsync<T>(string key, Func<T> factory, Func<CacheOptions> optionsFactory, CancellationToken cancellationToken = default) where T : class
        {
            return GetOrCreateAsync(key, () => Task.FromResult(factory()), optionsFactory, cancellationToken);
        }

        public async Task<IDictionary<string, T>> GetManyAsync<T>(string[] keys, CancellationToken cancellationToken = default) where T : class
        {
            var result = new Dictionary<string, T>();
            foreach (var key in keys)
            {
                var value = await GetAsync<T>(key, cancellationToken);
                if (value != null)
                {
                    result[key] = value;
                }
            }
            return result;
        }

        public async Task SetManyAsync<T>(IDictionary<string, T> items, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
        {
            foreach (var item in items)
            {
                await SetAsync(item.Key, item.Value, expiry, cancellationToken);
            }
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            if (_cache is MemoryCache memCache)
            {
                memCache.Compact(1.0);
            }
            return Task.CompletedTask;
        }

        public Task<long> GetRemainingTimeToLiveAsync(string key, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("GetRemainingTimeToLive is not supported by MemoryCache");
        }

        public Task<bool> KeyExpireAsync(string key, TimeSpan? expiry, CancellationToken cancellationToken = default)
        {
            if (_cache.TryGetValue(key, out var value))
            {
                var options = new MemoryCacheEntryOptions();
                if (expiry.HasValue)
                {
                    options.AbsoluteExpirationRelativeToNow = expiry;
                }
                _cache.Set(key, value, options);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task PublishAsync<T>(string channel, T message, CancellationToken cancellationToken = default)
        {
            if (_subscribers.TryGetValue(channel, out var handlers))
            {
                foreach (var handler in handlers)
                {
                    _ = handler(message);
                }
            }
            return Task.CompletedTask;
        }

        public Task SubscribeAsync<T>(string channel, Func<T, Task> handler, CancellationToken cancellationToken = default)
        {
            if (!_subscribers.ContainsKey(channel))
            {
                _subscribers[channel] = new List<Func<object, Task>>();
            }
            _subscribers[channel].Add(async (msg) => await handler((T)msg));
            return Task.CompletedTask;
        }

        public Task<IDistributedLock> AcquireLockAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Distributed locks are not supported by MemoryCache");
        }

        public Task<bool> ReleaseLockAsync(IDistributedLock lockObject, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Distributed locks are not supported by MemoryCache");
        }

        public Task<CacheStats> GetStatsAsync(CancellationToken cancellationToken = default)
        {
            var hits = Interlocked.Read(ref _hits);
            var misses = Interlocked.Read(ref _misses);
            var evictions = Interlocked.Read(ref _evictions);

            var stats = new CacheStats
            {
                TotalItems = GetCurrentEntryCount(),
                HitCount = hits,
                MissCount = misses,
                EvictionCount = evictions,
                TotalSizeInBytes = GetCurrentSize()
            };

            return Task.FromResult(stats);
        }

        private long GetCurrentEntryCount()
        {
            if (_cache is MemoryCache memCache)
            {
                return memCache.Count;
            }
            return 0;
        }

        private long GetCurrentSize()
        {
            if (_cache is MemoryCache memCache)
            {
                var stats = memCache.GetCurrentStatistics();
                return stats?.CurrentEstimatedSize ?? 0;
            }
            return 0;
        }
    }
}
