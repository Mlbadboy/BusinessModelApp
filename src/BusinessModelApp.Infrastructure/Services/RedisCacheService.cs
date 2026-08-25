using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using StackExchange.Redis.KeyspaceIsolation;

namespace BusinessModelApp.Infrastructure.Services
{
    public class RedisCacheService : ICacheService, IDisposable
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly RedisCacheOptions _options;
        private readonly ILogger<RedisCacheService> _logger;
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);
        private readonly ConcurrentDictionary<string, byte> _subscriptions = new ConcurrentDictionary<string, byte>();
        private bool _disposed;
        private IDatabase _database;
        private ISubscriber _subscriber;

        public RedisCacheService(
            IOptions<RedisCacheOptions> options,
            ILogger<RedisCacheService> logger)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _redis = CreateConnection();
        }

        private IConnectionMultiplexer CreateConnection()
        {
            try
            {
                var config = ConfigurationOptions.Parse(_options.ConnectionString);
                config.AbortOnConnectFail = false;
                config.ConnectRetry = _options.ConnectRetry;
                config.ConnectTimeout = _options.ConnectTimeout;
                config.DefaultDatabase = _options.Database;
                config.KeepAlive = 60; // 60 seconds
                config.ReconnectRetryPolicy = new ExponentialRetry(5000);
                config.SyncTimeout = _options.SyncTimeout;
                
                if (!string.IsNullOrEmpty(_options.InstanceName))
                {
                    config.DefaultDatabase = null; // Reset to use instance name as key prefix
                }

                var connection = ConnectionMultiplexer.Connect(config);
                connection.ConnectionFailed += (sender, e) =>
                {
                    _logger.LogError(e.Exception, "Redis connection failed: {FailureType}", e.FailureType);
                };

                connection.ConnectionRestored += (sender, e) =>
                {
                    _logger.LogInformation("Redis connection restored");
                };

                connection.ErrorMessage += (sender, e) =>
                {
                    _logger.LogError("Redis error: {Message}", e.Message);
                };

                connection.InternalError += (sender, e) =>
                {
                    _logger.LogError(e.Exception, "Redis internal error");
                };

                _logger.LogInformation("Redis cache service initialized");
                return connection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to Redis");
                throw new InvalidOperationException("Failed to connect to Redis", ex);
            }
        }

        private IDatabase Database
        {
            get
            {
                if (_database != null) return _database;

                _database = !string.IsNullOrEmpty(_options.InstanceName) 
                    ? _redis.GetDatabase().WithKeyPrefix(_options.InstanceName + ":") 
                    : _redis.GetDatabase(_options.Database);
                
                return _database;
            }
        }

        private ISubscriber Subscriber => _subscriber ??= _redis.GetSubscriber();

        public async Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

            try
            {
                var value = await Database.StringGetAsync(key).ConfigureAwait(false);
                if (value.IsNull)
                    return null;

                return JsonSerializer.Deserialize<T>(value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache item with key: {Key}", key);
                return null;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

            if (value == null)
            {
                await RemoveAsync(key, cancellationToken).ConfigureAwait(false);
                return;
            }

            try
            {
                var serializedValue = JsonSerializer.Serialize(value);
                var expiration = GetExpiration(expiry);
                
                await Database.StringSetAsync(
                    key, 
                    serializedValue, 
                    expiration,
                    When.Always,
                    CommandFlags.FireAndForget).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache item with key: {Key}", key);
            }
        }

        public async Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

            try
            {
                return await Database.KeyDeleteAsync(key).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache item with key: {Key}", key);
                return false;
            }
        }

        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

            try
            {
                return await Database.KeyExistsAsync(key).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if cache key exists: {Key}", key);
                return false;
            }
        }

        public async Task<T> GetOrCreateAsync<T>(
            string key, 
            Func<Task<T>> factory, 
            TimeSpan? expiry = null, 
            CancellationToken cancellationToken = default) where T : class
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            var value = await GetAsync<T>(key, cancellationToken).ConfigureAwait(false);
            if (value != null)
                return value;

            value = await factory().ConfigureAwait(false);
            if (value != null)
            {
                await SetAsync(key, value, expiry, cancellationToken).ConfigureAwait(false);
            }

            return value;
        }

        public async Task<T> GetOrCreateAsync<T>(
            string key, 
            Func<T> factory, 
            TimeSpan? expiry = null, 
            CancellationToken cancellationToken = default) where T : class
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            var value = await GetAsync<T>(key, cancellationToken).ConfigureAwait(false);
            if (value != null)
                return value;

            value = factory();
            if (value != null)
            {
                await SetAsync(key, value, expiry, cancellationToken).ConfigureAwait(false);
            }

            return value;
        }

        public async Task<T> GetOrCreateAsync<T>(
            string key, 
            Func<Task<T>> factory, 
            Func<CacheOptions> optionsFactory, 
            CancellationToken cancellationToken = default) where T : class
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            if (optionsFactory == null)
                throw new ArgumentNullException(nameof(optionsFactory));

            var options = optionsFactory();
            if (options.BypassCache)
                return await factory().ConfigureAwait(false);

            var value = await GetAsync<T>(key, cancellationToken).ConfigureAwait(false);
            if (value != null)
            {
                if (options.RefreshOnAccess && options.SlidingExpiration.HasValue)
                {
                    await KeyExpireAsync(key, options.SlidingExpiration, cancellationToken).ConfigureAwait(false);
                }
                return value;
            }

            value = await factory().ConfigureAwait(false);
            if (value != null)
            {
                var expiration = options.AbsoluteExpiration ?? options.SlidingExpiration;
                if (options.Jitter.HasValue && expiration.HasValue)
                {
                    var rnd = new Random();
                    var jitterMs = (long)(rnd.NextDouble() * options.Jitter.Value.TotalMilliseconds);
                    expiration = expiration.Value.Add(TimeSpan.FromMilliseconds(jitterMs));
                }
                
                await SetAsync(key, value, expiration, cancellationToken).ConfigureAwait(false);
            }

            return value;
        }

        public async Task<T> GetOrCreateAsync<T>(
            string key, 
            Func<T> factory, 
            Func<CacheOptions> optionsFactory, 
            CancellationToken cancellationToken = default) where T : class
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            if (optionsFactory == null)
                throw new ArgumentNullException(nameof(optionsFactory));

            var options = optionsFactory();
            if (options.BypassCache)
                return factory();

            var value = await GetAsync<T>(key, cancellationToken).ConfigureAwait(false);
            if (value != null)
            {
                if (options.RefreshOnAccess && options.SlidingExpiration.HasValue)
                {
                    await KeyExpireAsync(key, options.SlidingExpiration, cancellationToken).ConfigureAwait(false);
                }
                return value;
            }

            value = factory();
            if (value != null)
            {
                var expiration = options.AbsoluteExpiration ?? options.SlidingExpiration;
                if (options.Jitter.HasValue && expiration.HasValue)
                {
                    var rnd = new Random();
                    var jitterMs = (long)(rnd.NextDouble() * options.Jitter.Value.TotalMilliseconds);
                    expiration = expiration.Value.Add(TimeSpan.FromMilliseconds(jitterMs));
                }
                
                await SetAsync(key, value, expiration, cancellationToken).ConfigureAwait(false);
            }

            return value;
        }

        public async Task<IDictionary<string, T>> GetManyAsync<T>(
            string[] keys, 
            CancellationToken cancellationToken = default) where T : class
        {
            if (keys == null)
                throw new ArgumentNullException(nameof(keys));

            if (keys.Length == 0)
                return new Dictionary<string, T>();

            try
            {
                var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
                var values = await Database.StringGetAsync(redisKeys).ConfigureAwait(false);
                
                var result = new Dictionary<string, T>();
                for (int i = 0; i < keys.Length; i++)
                {
                    if (!values[i].IsNull)
                    {
                        result[keys[i]] = JsonSerializer.Deserialize<T>(values[i]);
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting multiple cache items");
                return keys.ToDictionary(k => k, k => (T)null);
            }
        }

        public async Task SetManyAsync<T>(
            IDictionary<string, T> items, 
            TimeSpan? expiry = null, 
            CancellationToken cancellationToken = default) where T : class
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            if (items.Count == 0)
                return;

            try
            {
                var batch = Database.CreateBatch();
                var tasks = new List<Task>();
                var expiration = GetExpiration(expiry);

                foreach (var item in items)
                {
                    if (item.Value == null)
                    {
                        tasks.Add(batch.KeyDeleteAsync(item.Key));
                    }
                    else
                    {
                        var serializedValue = JsonSerializer.Serialize(item.Value);
                        tasks.Add(batch.StringSetAsync(
                            item.Key, 
                            serializedValue, 
                            expiration,
                            When.Always,
                            CommandFlags.FireAndForget));
                    }
                }


                batch.Execute();
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting multiple cache items");
            }
        }

        public async Task ClearAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var endpoints = _redis.GetEndPoints();
                foreach (var endpoint in endpoints)
                {
                    var server = _redis.GetServer(endpoint);
                    await server.FlushDatabaseAsync(_options.Database).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache");
                throw;
            }
        }

        public async Task<long> GetRemainingTimeToLiveAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

            try
            {
                var ttl = await Database.KeyTimeToLiveAsync(key).ConfigureAwait(false);
                return ttl?.Ticks > 0 ? (long)ttl.Value.TotalSeconds : -1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting TTL for key: {Key}", key);
                return -1;
            }
        }

        public async Task<bool> KeyExpireAsync(string key, TimeSpan? expiry, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

            try
            {
                return await Database.KeyExpireAsync(key, GetExpiration(expiry)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting expiration for key: {Key}", key);
                return false;
            }
        }

        public async Task PublishAsync<T>(string channel, T message, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(channel))
                throw new ArgumentException("Channel cannot be null or empty", nameof(channel));

            try
            {
                var serializedMessage = JsonSerializer.Serialize(message);
                await Subscriber.PublishAsync(channel, serializedMessage, CommandFlags.FireAndForget).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing message to channel: {Channel}", channel);
                throw;
            }
        }

        public async Task SubscribeAsync<T>(string channel, Func<T, Task> handler, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(channel))
                throw new ArgumentException("Channel cannot be null or empty", nameof(channel));
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            try
            {
                if (_subscriptions.TryAdd(channel, 0))
                {
                    await Subscriber.SubscribeAsync(channel, async (_, value) =>
                    {
                        try
                        {
                            var message = JsonSerializer.Deserialize<T>(value);
                            await handler(message).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error handling message on channel: {Channel}", channel);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing to channel: {Channel}", channel);
                throw;
            }
        }

        public async Task<IDistributedLock> AcquireLockAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Lock key cannot be null or empty", nameof(key));

            var lockKey = $"lock:{key}";
            var lockId = Guid.NewGuid().ToString("N");
            var acquired = false;

            try
            {
                acquired = await Database.LockTakeAsync(lockKey, lockId, expiry).ConfigureAwait(false);
                if (acquired)
                {
                    return new RedisDistributedLock(
                        lockKey, 
                        lockId, 
                        this, 
                        DateTime.UtcNow,
                        expiry);
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error acquiring lock for key: {Key}", key);
                return null;
            }
        }

        public async Task<bool> ReleaseLockAsync(IDistributedLock lockObject, CancellationToken cancellationToken = default)
        {
            if (lockObject == null)
                throw new ArgumentNullException(nameof(lockObject));
            if (!(lockObject is RedisDistributedLock redisLock))
                throw new ArgumentException("Invalid lock object type", nameof(lockObject));

            if (!redisLock.IsAcquired)
                return true;

            try
            {
                var released = await Database.LockReleaseAsync(redisLock.Resource, redisLock.LockId).ConfigureAwait(false);
                if (released)
                {
                    redisLock.MarkReleased();
                }
                return released;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error releasing lock for key: {Key}", redisLock.Resource);
                return false;
            }
        }

        public async Task<CacheStats> GetStatsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var endpoints = _redis.GetEndPoints();
                var stats = new CacheStats();
                
                foreach (var endpoint in endpoints)
                {
                    var server = _redis.GetServer(endpoint);
                    var info = await server.InfoAsync("stats").ConfigureAwait(false);
                    
                    // Parse Redis info to get basic stats
                    // Note: This is a simplified version - in a real implementation, you'd want to parse more fields
                    foreach (var section in info)
                    {
                        if (section.Key == "keyspace")
                        {
                            // Example: "db0:keys=1,expires=0,avg_ttl=0"
                            foreach (var db in section)
                            {
                                if (db.Key == $"db{_options.Database}")
                                {
                                    var parts = db.Value.Split(',');
                                    foreach (var part in parts)
                                    {
                                        var kv = part.Split('=');
                                        if (kv.Length == 2)
                                        {
                                            if (kv[0] == "keys" && long.TryParse(kv[1], out var keyCount))
                                            {
                                                stats.TotalItems = keyCount;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                
                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache statistics");
                return new CacheStats();
            }
        }

        private TimeSpan? GetExpiration(TimeSpan? expiry)
        {
            if (!expiry.HasValue)
                return _options.DefaultExpiration;
                
            if (expiry.Value <= TimeSpan.Zero)
                return null; // No expiration
                
            var maxExpiry = _options.MaxExpiration ?? TimeSpan.MaxValue;
            return expiry.Value > maxExpiry ? maxExpiry : expiry.Value;
        }

        #region IDisposable Implementation

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _connectionLock?.Dispose();
                    _redis?.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        ~RedisCacheService()
        {
            Dispose(disposing: false);
        }

        #endregion

        #region Private Classes

        private class RedisDistributedLock : IDistributedLock
        {
            private readonly RedisCacheService _cacheService;
            private bool _disposed;
            private bool _isReleased;

            public string LockId { get; }
            public string Resource { get; }
            public bool IsAcquired => !_isReleased && !_disposed;
            public DateTime AcquiredAt { get; }
            public TimeSpan TimeRemaining => AcquiredAt + Timeout - DateTime.UtcNow;
            public TimeSpan Timeout { get; }

            public RedisDistributedLock(
                string resource, 
                string lockId, 
                RedisCacheService cacheService,
                DateTime acquiredAt,
                TimeSpan timeout)
            {
                Resource = resource ?? throw new ArgumentNullException(nameof(resource));
                LockId = lockId ?? throw new ArgumentNullException(nameof(lockId));
                _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
                AcquiredAt = acquiredAt;
                Timeout = timeout;
            }

            public void MarkReleased()
            {
                _isReleased = true;
            }

            public async Task RenewAsync(TimeSpan expiry, CancellationToken cancellationToken = default)
            {
                if (_isReleased || _disposed)
                    throw new InvalidOperationException("Lock has been released or disposed");

                await _cacheService.KeyExpireAsync(Resource, expiry, cancellationToken).ConfigureAwait(false);
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                try
                {
                    if (!_isReleased)
                    {
                        _cacheService.ReleaseLockAsync(this).GetAwaiter().GetResult();
                    }
                }
                finally
                {
                    _disposed = true;
                }
            }

            public async ValueTask DisposeAsync()
            {
                if (_disposed)
                    return;

                try
                {
                    if (!_isReleased)
                    {
                        await _cacheService.ReleaseLockAsync(this).ConfigureAwait(false);
                    }
                }
                finally
                {
                    _disposed = true;
                }
            }
        }

        #endregion
    }

    public class RedisCacheOptions
    {
        public string ConnectionString { get; set; } = "localhost";
        public string InstanceName { get; set; }
        public int Database { get; set; } = 0;
        public TimeSpan? DefaultExpiration { get; set; } = TimeSpan.FromMinutes(30);
        public TimeSpan? MaxExpiration { get; set; } = TimeSpan.FromDays(7);
        public int ConnectRetry { get; set; } = 3;
        public int SyncTimeout { get; set; } = 5000; // 5 seconds
        public int ConnectTimeout { get; set; } = 5000; // 5 seconds
    }
}
