using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessModelApp.Core.Interfaces
{
    public interface IMemoryCacheService
    {
        Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default);
        Task SetAsync<T>(string key, T value, MemoryCacheEntryOptions options, CancellationToken cancellationToken = default);
    }

    public interface IDistributedLockService
    {
        Task<IAsyncDisposable> AcquireLockAsync(string key, TimeSpan expiry, TimeSpan timeout, CancellationToken cancellationToken = default);
    }
}
