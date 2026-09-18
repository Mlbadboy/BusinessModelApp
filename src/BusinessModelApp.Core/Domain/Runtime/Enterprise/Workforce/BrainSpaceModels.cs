using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce
{
    public class BrainSpaceAgentNode
    {
        public string AgentId { get; set; } = string.Empty;
        public string AgentName { get; set; } = string.Empty;
        public string RoleTitle { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public AgentLifecycleStatus LifecycleStatus { get; set; } = AgentLifecycleStatus.ACTIVE;
        public string CurrentMission { get; set; } = string.Empty;
        public string ModelId { get; set; } = "omniroute-default";
        public decimal DailyBudget { get; set; } = 1000m;
        public decimal DailySpent { get; set; } = 0m;
        public int RiskCeiling { get; set; } = 2;
        public string HealthStatus { get; set; } = "HEALTHY"; // HEALTHY, DEGRADED, QUARANTINED
        public decimal PerformanceScore { get; set; } = 0.95m;
        public int PendingApprovalCount { get; set; } = 0;
    }

    public class SystemTelemetryData
    {
        public double CpuUsagePercent { get; set; } = 14.5;
        public double MemoryUsageMb { get; set; } = 412.0;
        public int ActiveQueueDepth { get; set; } = 0;
        public double AverageLatencyMs { get; set; } = 210.0;
        public int ActiveAgentCount { get; set; } = 0;
    }

    public class BusinessTelemetryData
    {
        public decimal QualifiedPipeline { get; set; } = 0m;
        public decimal WeightedPipeline { get; set; } = 0m;
        public decimal ClosedWonRevenue { get; set; } = 0m;
        public decimal InvoicedRevenue { get; set; } = 0m;
        public decimal CollectedCashRevenue { get; set; } = 0m;
        public decimal RealizedGrossMargin { get; set; } = 0m;
        public decimal NetContribution { get; set; } = 0m;
        public int ActiveCommercialMissions { get; set; } = 0;
    }

    public class BrainSpaceTelemetrySnapshot
    {
        public string TenantId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public SystemTelemetryData SystemTelemetry { get; set; } = new();
        public BusinessTelemetryData BusinessTelemetry { get; set; } = new();
        public List<BrainSpaceAgentNode> AgentNodes { get; set; } = new();
    }
}
