using System.Collections.Concurrent;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Simulation;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational.Simulation;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Simulation;

public sealed class SimulationCalibrationService : ISimulationCalibrationService
{
    private readonly ConcurrentDictionary<string, List<SimulationPerformanceRecord>> _recordsByTenant = new();
    private readonly ISimulationTenantIsolation _tenantIsolation;

    public SimulationCalibrationService(ISimulationTenantIsolation tenantIsolation)
    {
        _tenantIsolation = tenantIsolation ?? throw new ArgumentNullException(nameof(tenantIsolation));
    }

    public Task RecordCalibrationAsync(SimulationPerformanceRecord record)
    {
        if (record == null) throw new ArgumentNullException(nameof(record));
        if (string.IsNullOrWhiteSpace(record.TenantId)) throw new ArgumentNullException(nameof(record.TenantId));

        record.CalibrationError = Math.Round(
            Math.Abs(record.PredictedMetricValue - record.ActualMetricValue) / Math.Max(1.0, Math.Abs(record.ActualMetricValue)),
            4);
        record.DirectionalAccuracy = (record.PredictedMetricValue >= 0 && record.ActualMetricValue >= 0) ||
                                     (record.PredictedMetricValue < 0 && record.ActualMetricValue < 0);
        record.ObservedReliability = Math.Round(Math.Clamp(1.0 - record.CalibrationError, 0.0, 1.0), 3);

        var list = _recordsByTenant.GetOrAdd(record.TenantId, _ => new List<SimulationPerformanceRecord>());
        lock (list)
        {
            list.Add(record);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SimulationPerformanceRecord>> GetCalibrationHistoryAsync(string tenantId, string? domain = null)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));

        if (_recordsByTenant.TryGetValue(tenantId, out var list))
        {
            lock (list)
            {
                var query = list.AsEnumerable();
                if (!string.IsNullOrWhiteSpace(domain))
                {
                    query = query.Where(r => string.Equals(r.Domain, domain, StringComparison.OrdinalIgnoreCase));
                }
                return Task.FromResult<IReadOnlyList<SimulationPerformanceRecord>>(query.OrderByDescending(r => r.CalibratedAtUtc).ToList());
            }
        }

        return Task.FromResult<IReadOnlyList<SimulationPerformanceRecord>>(Array.Empty<SimulationPerformanceRecord>());
    }

    public double ComputeHistoricalReliability(IEnumerable<SimulationPerformanceRecord> records)
    {
        if (records == null || !records.Any()) return 0.80; // Baseline prior
        return Math.Round(records.Average(r => r.ObservedReliability), 3);
    }
}
