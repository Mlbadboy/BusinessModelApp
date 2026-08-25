using BusinessModelApp.Core.DTOs.Revenue;
using BusinessModelApp.Core.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace BusinessModelApp.Core.Services
{
    public class RevenueService : IRevenueService
    {
        // TODO: Inject IRevenueRepository when available
        private readonly List<RevenueSourceDto> _revenueSources = new List<RevenueSourceDto>
        {
            new RevenueSourceDto { Id = "1", Name = "Product Sales", Amount = 50000, Type = "Sales", BusinessModelId = 1 },
            new RevenueSourceDto { Id = "2", Name = "Subscription", Amount = 20000, Type = "Recurring", BusinessModelId = 1 }
        };

        public RevenueService()
        {
        }

        public Task<IEnumerable<RevenueSourceDto>> GetAllRevenueSourcesAsync()
        {
            return Task.FromResult((IEnumerable<RevenueSourceDto>)_revenueSources);
        }

        public Task<RevenueSourceDto> GetRevenueSourceByIdAsync(string id)
        {
            var source = _revenueSources.FirstOrDefault(r => r.Id == id);
            return Task.FromResult(source);
        }

        public Task<RevenueSourceDto> CreateRevenueSourceAsync(RevenueSourceDto source)
        {
            source.Id = Guid.NewGuid().ToString();
            _revenueSources.Add(source);
            return Task.FromResult(source);
        }

        public Task UpdateRevenueSourceAsync(RevenueSourceDto source)
        {
            var existing = _revenueSources.FirstOrDefault(x => x.Id == source.Id);
            if (existing != null)
            {
                _revenueSources.Remove(existing);
                _revenueSources.Add(source);
            }
            return Task.CompletedTask;
        }

        public Task DeleteRevenueSourceAsync(string id)
        {
            var source = _revenueSources.FirstOrDefault(r => r.Id == id);
            if (source != null)
            {
                _revenueSources.Remove(source);
            }
            return Task.CompletedTask;
        }

        public Task<IEnumerable<RevenueMetricDto>> GetRevenueMetricsAsync()
        {
            var metrics = new List<RevenueMetricDto>
            {
                new RevenueMetricDto { MetricName = "Total Revenue", Value = 70000, Unit = "USD", Trend = "+5%" },
                new RevenueMetricDto { MetricName = "Average Revenue", Value = 35000, Unit = "USD", Trend = "+2%" },
                new RevenueMetricDto { MetricName = "Revenue Streams", Value = 2, Unit = "Count", Trend = "0" }
            };
            return Task.FromResult((IEnumerable<RevenueMetricDto>)metrics);
        }

        public Task<RevenuePerformanceDto> GetRevenuePerformanceAsync()
        {
            return Task.FromResult(new RevenuePerformanceDto
            {
                RevenueStreamName = "Overall",
                ActualAmount = 70000,
                TargetAmount = 80000,
                Variance = -10000,
                KeyContributors = new List<string> { "Product Sales" }
            });
        }

        public Task<RevenueAnalysisDto> GetRevenueAnalysisAsync()
        {
            return Task.FromResult(new RevenueAnalysisDto
            {
                TotalRevenue = 70000,
                YearOverYearGrowth = 5.5m,
                RevenueBySource = new Dictionary<string, decimal> { { "Sales", 50000 }, { "Recurring", 20000 } }
            });
        }
    }
}
