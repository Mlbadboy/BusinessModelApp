using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Infrastructure.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BusinessModelApp.Api.Health
{
    public class AppDbContextHealthCheck : IHealthCheck
    {
        private readonly AppDbContext _context;

        public AppDbContextHealthCheck(AppDbContext context)
        {
            _context = context;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
                return canConnect
                    ? HealthCheckResult.Healthy("Database connection verified.")
                    : HealthCheckResult.Unhealthy("Database connection failed.");
            }
            catch
            {
                // Zero-Trust: Do not expose connection string or exception details
                return HealthCheckResult.Unhealthy("Database connection unavailable.");
            }
        }
    }
}
