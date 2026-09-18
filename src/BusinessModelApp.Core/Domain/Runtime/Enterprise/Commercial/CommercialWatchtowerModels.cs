using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Commercial
{
    public class RevenueLineageNode
    {
        public string Stage { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string EvidenceDigestSha256 { get; set; } = string.Empty;
        public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
        public bool IsVerified { get; set; } = false;
    }

    public class RevenueLineageAuditReport
    {
        public string LineageId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string ReceiptId { get; set; } = string.Empty;
        public decimal RealizedAmountINR { get; set; } = 0m;
        public bool IsLineageUnbroken { get; set; } = false;
        public List<RevenueLineageNode> TraceNodes { get; set; } = new();
        public List<string> Defects { get; set; } = new();
        public DateTime AuditedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public class CommercialPipelineMetrics
    {
        public string TenantId { get; set; } = string.Empty;
        public int TotalOpportunities { get; set; } = 0;
        public decimal TotalPipelineValueINR { get; set; } = 0m;
        public decimal TotalWonDealsValueINR { get; set; } = 0m;
        public decimal TotalInvoicedValueINR { get; set; } = 0m;
        public decimal TotalCollectedCashINR { get; set; } = 0m;
        public decimal AverageGrossMarginPercent { get; set; } = 0m;
        public int StalledOpportunitiesCount { get; set; } = 0;
        public int ActiveProposalsCount { get; set; } = 0;
        public int PendingInvoicesCount { get; set; } = 0;
        public DateTime ComputedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public class CommercialIdempotencyRecord
    {
        public string IdempotencyKey { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public string ResourceId { get; set; } = string.Empty;
        public string ExecutionDigestSha256 { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
