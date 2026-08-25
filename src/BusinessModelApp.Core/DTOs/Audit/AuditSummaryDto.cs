using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.DTOs.Audit
{
    public class AuditSummaryDto
    {
        public int TotalLogs { get; set; }
        public int SuccessLogs { get; set; }
        public int FailedLogs { get; set; }
        public int CriticalLogs { get; set; }
        public int ErrorLogs { get; set; }
        public int WarningLogs { get; set; }
        public int InfoLogs { get; set; }
        public Dictionary<string, int> ActionCounts { get; set; } = new();
        public Dictionary<string, int> UserCounts { get; set; } = new();
        public Dictionary<string, int> EntityCounts { get; set; } = new();
        public Dictionary<string, int> SeverityCounts { get; set; } = new();
        public List<AuditTrendDto> Trends { get; set; } = new();
    }
}
