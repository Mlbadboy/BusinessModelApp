using BusinessModelApp.Core.DTOs.Analytics;
using System.Threading.Tasks;

namespace BusinessModelApp.Core.Interfaces
{
    public interface IAnalyticsService
    {
        Task<RevenueAnalysisDto> GetRevenueAnalysisAsync();
        Task<FinancialPerformanceDto> GetFinancialPerformanceAsync();
    }
}
