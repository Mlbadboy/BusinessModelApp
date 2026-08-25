using System;
using System.Threading.Tasks;

namespace BusinessModelApp.Core.Services
{
    public interface IDataExportService
    {
        Task<byte[]> ExportRevenueAnalysisToExcelAsync();
        Task<byte[]> ExportExpenseAnalysisToExcelAsync();
        Task<byte[]> ExportStrategyAnalysisToExcelAsync();
        Task<byte[]> ExportAuditLogToExcelAsync(DateTime startDate, DateTime endDate);
        Task<string> ExportToPDFAsync<T>(T data, string templateName);
    }
}
