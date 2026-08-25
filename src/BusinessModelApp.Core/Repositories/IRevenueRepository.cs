using BusinessModelApp.Core.DTOs;
using BusinessModelApp.Core.DTOs.Revenue;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessModelApp.Core.Repositories
{
    public interface IRevenueRepository : IRepository<RevenueSourceDto>
    {
        Task<IEnumerable<RevenueSourceDto>> GetRevenueSourcesByTypeAsync(string type);
        Task<IEnumerable<RevenueSourceDto>> GetRevenueSourcesByStatusAsync(string status);
        Task<IEnumerable<RevenueSourceDto>> GetRevenueSourcesByPeriodAsync(DateTime startDate, DateTime endDate);
        Task<decimal> GetTotalRevenueAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<decimal> GetTargetRevenueAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<IEnumerable<RevenueMetricDto>> GetRevenueMetricsByPeriodAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<RevenueSourceDto>> GetTopRevenueSourcesAsync(int topN = 10);
        Task<IEnumerable<RevenueSourceDto>> GetLowestRevenueSourcesAsync(int bottomN = 10);
        Task<IEnumerable<RevenueSourceDto>> GetRevenueSourcesByGrowthRateAsync();
        Task<IEnumerable<RevenueSourceDto>> GetRevenueSourcesByCustomerSegmentAsync();
        Task<IEnumerable<RevenueSourceDto>> GetRevenueSourcesByChannelAsync();
    }
}
