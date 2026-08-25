using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BusinessModelApp.Infrastructure.DependencyInjection
{
    public static class CacheServiceCollectionExtensions
    {
        public static IServiceCollection AddCacheServices(this IServiceCollection services, Action<MemoryCacheOptions> setupAction = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Register IMemoryCache
            if (setupAction != null)
            {
                services.AddMemoryCache(setupAction);
            }
            else
            {
                services.AddMemoryCache();
            }

            // Register our cache service implementation
            services.AddSingleton<Core.Interfaces.ICacheService, MemoryCacheService>();

            return services;
        }
    }
}
