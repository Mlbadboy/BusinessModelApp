using BusinessModelApp.Core.DTOs.Revenue;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessModelApp.Core.Services
{
    public interface IRevenueService
    {
        Task<IEnumerable<RevenueSourceDto>> GetAllRevenueSourcesAsync();
        Task<RevenueSourceDto> GetRevenueSourceByIdAsync(string id);
        Task<RevenueSourceDto> CreateRevenueSourceAsync(RevenueSourceDto source);
        Task UpdateRevenueSourceAsync(RevenueSourceDto source);
        Task DeleteRevenueSourceAsync(string id);
        Task<IEnumerable<RevenueMetricDto>> GetRevenueMetricsAsync();
        Task<RevenuePerformanceDto> GetRevenuePerformanceAsync();
        Task<DTOs.Revenue.RevenueAnalysisDto> GetRevenueAnalysisAsync();
    }
}
