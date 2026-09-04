using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BusinessModelApp.Core.Domain.Execution;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Infrastructure.Data;

namespace BusinessModelApp.Infrastructure.Execution
{
    public class ExecutionLedgerService : IExecutionLedgerService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ExecutionLedgerService> _logger;

        public ExecutionLedgerService(AppDbContext context, ILogger<ExecutionLedgerService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ExecutionReceipt?> CheckIdempotencyAsync(Guid workspaceId, string idempotencyKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey))
                return null;

            var existing = await _context.ExecutionLedgerEntries
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.WorkspaceId == workspaceId && e.IdempotencyKey == idempotencyKey, cancellationToken);

            if (existing != null && existing.Status == ExecutionStatus.Succeeded)
            {
                _logger.LogInformation("Idempotency match found for key '{Key}' in workspace {WorkspaceId}. Returning cached receipt without duplicate side-effect.",
                    idempotencyKey, workspaceId);

                return new ExecutionReceipt
                {
                    ReceiptId = existing.Id,
                    RequestId = existing.RequestId,
                    PermitId = existing.PermitId ?? Guid.Empty,
                    WorkspaceId = existing.WorkspaceId,
                    CapabilityId = existing.CapabilityId,
                    Status = existing.Status,
                    IdempotencyKey = existing.IdempotencyKey,
                    ResultPayloadJson = existing.ResultPayloadJson,
                    ResultHash = existing.ResultHash,
                    ExecutedAtUtc = existing.ExecutedAtUtc,
                    DurationMs = existing.DurationMs,
                    ErrorDetails = existing.ErrorMessage
                };
            }

            return null;
        }

        public async Task RecordEntryAsync(ExecutionLedgerEntry entry, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(entry.RequestHash))
            {
                entry.RequestHash = ComputeHash(entry.RequestPayloadJson);
            }

            if (string.IsNullOrWhiteSpace(entry.ResultHash))
            {
                entry.ResultHash = ComputeHash(entry.ResultPayloadJson);
            }

            _context.ExecutionLedgerEntries.Add(entry);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Recorded ExecutionLedgerEntry {Id} for key '{Key}', capability {Cap}, status {Status}",
                entry.Id, entry.IdempotencyKey, entry.CapabilityId, entry.Status);
        }

        public async Task<List<ExecutionLedgerEntry>> GetRecentEntriesAsync(Guid workspaceId, int limit = 50, CancellationToken cancellationToken = default)
        {
            return await _context.ExecutionLedgerEntries
                .Where(e => e.WorkspaceId == workspaceId)
                .OrderByDescending(e => e.ExecutedAtUtc)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        private string ComputeHash(string content)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(content ?? string.Empty));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
