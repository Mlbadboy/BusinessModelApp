using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.DTOs.Audit
{
    public class AuditLogDto
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string UserId { get; set; }
        public string Action { get; set; }
        public string Details { get; set; }
    }

    public class AuditLogDtoDetailed
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Action { get; set; }
        public string EntityName { get; set; }
        public string EntityId { get; set; }
        public string ActionType { get; set; } // Create, Update, Delete, Read
        public string PreviousValue { get; set; }
        public string NewValue { get; set; }
        public string ChangeType { get; set; } // Major, Minor, Critical
        public string Status { get; set; } // Success, Failed, Pending
        public string Reason { get; set; }
        public string IpAddress { get; set; }
        public string DeviceInfo { get; set; }
        public string Location { get; set; }
        public DateTime Timestamp { get; set; }
        public string CorrelationId { get; set; }
        public string SessionId { get; set; }
        public string ApplicationName { get; set; }
        public string Environment { get; set; }
        public string Source { get; set; }
        public string Target { get; set; }
        public string Tags { get; set; }
        public string Notes { get; set; }
        public string Category { get; set; }
        public string Severity { get; set; } // Info, Warning, Error, Critical
        public string AuditTrail { get; set; }
        public string Metadata { get; set; }
    }

    public class AuditTrendDto
    {
        public DateTime Date { get; set; }
        public int TotalLogs { get; set; }
        public int SuccessLogs { get; set; }
        public int FailedLogs { get; set; }
        public int CriticalLogs { get; set; }
        public int ErrorLogs { get; set; }
        public int WarningLogs { get; set; }
        public int InfoLogs { get; set; }
        public string Period { get; set; }
    }

    public class AuditRiskDto
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public decimal Impact { get; set; }
        public decimal Likelihood { get; set; }
        public string Severity { get; set; }
        public string Status { get; set; }
        public string Mitigation { get; set; }
        public DateTime LastOccurrence { get; set; }
        public int OccurrenceCount { get; set; }
    }
}
