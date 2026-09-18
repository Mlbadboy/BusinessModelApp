using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Commercial;

namespace BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Commercial
{
    public interface ICommercialWatchtowerService
    {
        Task<RevenueLineageAuditReport> TraceRevenueLineageAsync(string tenantId, string receiptId);
        Task<CommercialPipelineMetrics> CalculatePipelineMetricsAsync(string tenantId);
        Task<IReadOnlyList<GroundedOpportunity>> DetectStalledOpportunitiesAsync(string tenantId, TimeSpan stallThreshold);
    }

    public interface ICommercialIdempotencyService
    {
        Task<bool> TryAcquireIdempotencyKeyAsync(string tenantId, string idempotencyKey, string actionType);
        Task CommitIdempotencyRecordAsync(CommercialIdempotencyRecord record);
        Task<CommercialIdempotencyRecord?> GetRecordAsync(string tenantId, string idempotencyKey);
    }
}
