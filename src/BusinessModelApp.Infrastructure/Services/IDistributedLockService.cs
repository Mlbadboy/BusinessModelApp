using System;
using System.Threading.Tasks;

namespace BusinessModelApp.Infrastructure.Services
{
    public interface IDistributedLockService
    {
        Task<IDisposable> AcquireLockAsync(string key);
    }
}
