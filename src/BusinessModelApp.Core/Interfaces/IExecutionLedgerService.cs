using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Execution;

namespace BusinessModelApp.Core.Interfaces
{
    public interface IExecutionLedgerService
    {
        Task<ExecutionReceipt?> CheckIdempotencyAsync(Guid workspaceId, string idempotencyKey, CancellationToken cancellationToken = default);
        Task RecordEntryAsync(ExecutionLedgerEntry entry, CancellationToken cancellationToken = default);
        Task<List<ExecutionLedgerEntry>> GetRecentEntriesAsync(Guid workspaceId, int limit = 50, CancellationToken cancellationToken = default);
    }
}
