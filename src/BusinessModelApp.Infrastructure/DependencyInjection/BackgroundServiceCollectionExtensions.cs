using BusinessModelApp.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;

namespace BusinessModelApp.Infrastructure.DependencyInjection
{
    public static class BackgroundServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the background cleanup service to the service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
        /// <param name="configureOptions">A delegate to configure the <see cref="CleanupBackgroundServiceOptions"/>.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddBackgroundCleanupService(
            this IServiceCollection services,
            Action<CleanupBackgroundServiceOptions> configureOptions = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Configure options if provided
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            else
            {
                // Add default options if none provided
                services.AddOptions<CleanupBackgroundServiceOptions>();
            }

            // Register the background service
            services.AddSingleton<IHostedService, CleanupBackgroundService>();

            return services;
        }

        /// <summary>
        /// Adds the background cleanup service to the service collection with the specified options.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
        /// <param name="options">The <see cref="CleanupBackgroundServiceOptions"/> to use.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddBackgroundCleanupService(
            this IServiceCollection services,
            CleanupBackgroundServiceOptions options)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            // Register the options
            services.AddSingleton(Options.Create(options));

            // Register the background service
            services.AddSingleton<IHostedService, CleanupBackgroundService>();

            return services;
        }
    }
}
