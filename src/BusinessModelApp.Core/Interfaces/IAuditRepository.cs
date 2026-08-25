using BusinessModelApp.Core.DTOs;
using BusinessModelApp.Core.DTOs.Audit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessModelApp.Core.Interfaces
{
    public interface IAuditRepository
    {
        Task<AuditLogDto> CreateAuditLogAsync(AuditLogDto log);
        Task<IEnumerable<AuditLogDto>> GetAuditLogsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<IEnumerable<AuditLogDto>> GetAuditLogsByUserAsync(string userId);
        Task<IEnumerable<AuditLogDto>> GetAuditLogsByEntityAsync(string entityName, string entityId);
        Task<IEnumerable<AuditLogDto>> GetAuditLogsByActionAsync(string action);
        Task<IEnumerable<AuditLogDto>> GetAuditLogsBySeverityAsync(string severity);
        Task<IEnumerable<AuditLogDto>> GetAuditLogsByStatusAsync(string status);
        Task<IEnumerable<AuditLogDto>> GetCriticalAuditLogsAsync();
        Task<IEnumerable<AuditLogDto>> GetFailedAuditLogsAsync();
        Task<AuditSummaryDto> GetAuditSummaryAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<IEnumerable<AuditTrendDto>> GetAuditTrendsAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<AuditRiskDto>> GetAuditRisksAsync();
        Task<IEnumerable<AuditLogDto>> GetRecentAuditLogsAsync(int topN = 10);
        Task<IEnumerable<AuditLogDto>> GetAuditLogsByIpAsync(string ipAddress);
        Task<IEnumerable<AuditLogDto>> GetAuditLogsByLocationAsync(string location);
        Task<IEnumerable<AuditLogDto>> GetAuditLogsByDeviceAsync(string deviceInfo);
        Task<IEnumerable<AuditLogDto>> GetAuditLogsByApplicationAsync(string applicationName);
        Task<IEnumerable<AuditLogDto>> GetAuditLogsByEnvironmentAsync(string environment);
        Task<IEnumerable<AuditLogDto>> GetAuditLogsByTagAsync(string tag);
        Task<IEnumerable<AuditLogDto>> GetAuditLogsByCategoryAsync(string category);
    }
}
