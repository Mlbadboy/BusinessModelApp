using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Reality;
using BusinessModelApp.Core.Interfaces.Runtime.Reality;

namespace BusinessModelApp.Infrastructure.Runtime.Reality
{
    /// <summary>
    /// Governs human approval lifecycle, payload cryptographic digests,
    /// expiration SLAs, and permit generation for the Batch 6 Execution Firewall.
    /// </summary>
    public sealed class HumanApprovalManager : IHumanApprovalManager
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ApprovalRequest>> _tenantApprovals = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, List<ApprovalAuditEntry>>> _tenantAuditTrails = new();
        private readonly object _syncLock = new();

        public Task<ApprovalRequest> SubmitApprovalRequestAsync(ApprovalRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.TenantId))
                throw new ArgumentException("TenantId is required.", nameof(request));
            if (string.IsNullOrWhiteSpace(request.ApprovalId))
                throw new ArgumentException("ApprovalId is required.", nameof(request));

            // Verify payload digest
            var computedDigest = ComputeSha256(request.PayloadJson);
            if (!string.IsNullOrEmpty(request.PayloadDigest) &&
                !string.Equals(request.PayloadDigest, computedDigest, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Payload integrity check failed: computed SHA-256 does not match provided digest.");
            }

            var canonicalRequest = request with { PayloadDigest = computedDigest };
            var tenantDict = _tenantApprovals.GetOrAdd(request.TenantId, _ => new ConcurrentDictionary<string, ApprovalRequest>());
            tenantDict[request.ApprovalId] = canonicalRequest;

            RecordAuditEntry(request.TenantId, request.ApprovalId, ApprovalState.Draft, ApprovalState.Requested, "System", "Approval request registered.");

            return Task.FromResult(canonicalRequest);
        }

        public Task<ApprovalRequest?> GetApprovalRequestAsync(string tenantId, string approvalId)
        {
            if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(approvalId))
                return Task.FromResult<ApprovalRequest?>(null);

            if (_tenantApprovals.TryGetValue(tenantId, out var tenantDict) &&
                tenantDict.TryGetValue(approvalId, out var request))
            {
                // Check on-read expiration
                if (request.IsExpired(DateTimeOffset.UtcNow))
                {
                    var expired = request with { State = ApprovalState.Expired };
                    tenantDict[approvalId] = expired;
                    RecordAuditEntry(tenantId, approvalId, request.State, ApprovalState.Expired, "SystemTimer", "Request expired past SLA.");
                    return Task.FromResult<ApprovalRequest?>(expired);
                }

                return Task.FromResult<ApprovalRequest?>(request);
            }

            return Task.FromResult<ApprovalRequest?>(null);
        }

        public Task<IReadOnlyList<ApprovalRequest>> GetPendingApprovalsAsync(string tenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                return Task.FromResult<IReadOnlyList<ApprovalRequest>>(Array.Empty<ApprovalRequest>());

            var now = DateTimeOffset.UtcNow;
            if (_tenantApprovals.TryGetValue(tenantId, out var tenantDict))
            {
                var list = new List<ApprovalRequest>();
                foreach (var kvp in tenantDict)
                {
                    var req = kvp.Value;
                    if (req.IsExpired(now))
                    {
                        var expired = req with { State = ApprovalState.Expired };
                        tenantDict[kvp.Key] = expired;
                        RecordAuditEntry(tenantId, req.ApprovalId, req.State, ApprovalState.Expired, "SystemTimer", "Request expired past SLA.");
                        continue;
                    }

                    if (req.State == ApprovalState.Requested || req.State == ApprovalState.UnderReview)
                    {
                        list.Add(req);
                    }
                }

                return Task.FromResult<IReadOnlyList<ApprovalRequest>>(list.OrderByDescending(r => r.RequestedAt).ToList());
            }

            return Task.FromResult<IReadOnlyList<ApprovalRequest>>(Array.Empty<ApprovalRequest>());
        }

        public Task<IReadOnlyList<ApprovalRequest>> GetApprovalHistoryAsync(string tenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                return Task.FromResult<IReadOnlyList<ApprovalRequest>>(Array.Empty<ApprovalRequest>());

            if (_tenantApprovals.TryGetValue(tenantId, out var tenantDict))
            {
                return Task.FromResult<IReadOnlyList<ApprovalRequest>>(
                    tenantDict.Values.OrderByDescending(r => r.RequestedAt).ToList()
                );
            }

            return Task.FromResult<IReadOnlyList<ApprovalRequest>>(Array.Empty<ApprovalRequest>());
        }

        public Task<ExecutionPermit> ApproveRequestAsync(
            string tenantId,
            string approvalId,
            string reviewerId,
            string? expectedPayloadDigest = null,
            string? notes = null)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId is required.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(approvalId)) throw new ArgumentException("ApprovalId is required.", nameof(approvalId));
            if (string.IsNullOrWhiteSpace(reviewerId)) throw new ArgumentException("ReviewerId is required.", nameof(reviewerId));

            if (!_tenantApprovals.TryGetValue(tenantId, out var tenantDict) ||
                !tenantDict.TryGetValue(approvalId, out var request))
            {
                throw new KeyNotFoundException($"Approval request {approvalId} not found for tenant {tenantId}.");
            }

            var now = DateTimeOffset.UtcNow;
            if (request.IsExpired(now))
            {
                tenantDict[approvalId] = request with { State = ApprovalState.Expired };
                RecordAuditEntry(tenantId, approvalId, request.State, ApprovalState.Expired, reviewerId, "Approval attempted on expired request.");
                throw new InvalidOperationException($"Cannot approve request {approvalId}: SLA has expired.");
            }

            if (request.State != ApprovalState.Requested && request.State != ApprovalState.UnderReview)
            {
                throw new InvalidOperationException($"Cannot approve request {approvalId} in state {request.State}.");
            }

            // Payload verification
            var currentDigest = ComputeSha256(request.PayloadJson);
            if (!string.Equals(request.PayloadDigest, currentDigest, StringComparison.OrdinalIgnoreCase))
            {
                tenantDict[approvalId] = request with { State = ApprovalState.Invalidated };
                RecordAuditEntry(tenantId, approvalId, request.State, ApprovalState.Invalidated, reviewerId, "Payload digest mismatch: request invalidated.");
                throw new InvalidOperationException("CRITICAL INTEGRITY VIOLATION: Stored payload has been tampered with. Request invalidated.");
            }

            if (!string.IsNullOrEmpty(expectedPayloadDigest) &&
                !string.Equals(expectedPayloadDigest, currentDigest, StringComparison.OrdinalIgnoreCase))
            {
                tenantDict[approvalId] = request with { State = ApprovalState.Invalidated };
                RecordAuditEntry(tenantId, approvalId, request.State, ApprovalState.Invalidated, reviewerId, "Reviewer expected digest mismatch: request invalidated.");
                throw new InvalidOperationException("Approval rejected: Expected payload digest does not match current payload.");
            }

            // Generate cryptographic permit for Batch 6 Firewall
            var permitId = $"PERMIT-{Guid.NewGuid():N}";
            var authoritySignature = ComputeSha256($"{permitId}:{approvalId}:{tenantId}:{request.PayloadDigest}:{reviewerId}:{now:O}");
            var permit = new ExecutionPermit(
                PermitId: permitId,
                ApprovalId: approvalId,
                TenantId: tenantId,
                MissionId: request.MissionId,
                NodeId: request.NodeId,
                TargetSystem: request.TargetSystem,
                PayloadDigest: request.PayloadDigest,
                IssuedAt: now,
                ExpiresAt: now.AddHours(2), // 2 hour permit window
                ApprovedBy: reviewerId,
                AuthoritySignature: authoritySignature
            );

            var approvedRequest = request with
            {
                State = ApprovalState.Approved,
                ReviewerId = reviewerId,
                DecidedAt = now,
                DecisionNotes = notes,
                PermitToken = permitId
            };

            tenantDict[approvalId] = approvedRequest;
            RecordAuditEntry(tenantId, approvalId, request.State, ApprovalState.Approved, reviewerId, $"Approved by {reviewerId}. Permit {permitId} issued.");

            return Task.FromResult(permit);
        }

        public Task<ApprovalRequest> RejectRequestAsync(
            string tenantId,
            string approvalId,
            string reviewerId,
            string reason)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId is required.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(approvalId)) throw new ArgumentException("ApprovalId is required.", nameof(approvalId));
            if (string.IsNullOrWhiteSpace(reviewerId)) throw new ArgumentException("ReviewerId is required.", nameof(reviewerId));

            if (!_tenantApprovals.TryGetValue(tenantId, out var tenantDict) ||
                !tenantDict.TryGetValue(approvalId, out var request))
            {
                throw new KeyNotFoundException($"Approval request {approvalId} not found for tenant {tenantId}.");
            }

            var now = DateTimeOffset.UtcNow;
            var rejected = request with
            {
                State = ApprovalState.Rejected,
                ReviewerId = reviewerId,
                DecidedAt = now,
                RejectionReason = reason
            };

            tenantDict[approvalId] = rejected;
            RecordAuditEntry(tenantId, approvalId, request.State, ApprovalState.Rejected, reviewerId, $"Rejected by {reviewerId}: {reason}");

            return Task.FromResult(rejected);
        }

        public Task<ApprovalRequest> RequestChangesAsync(
            string tenantId,
            string approvalId,
            string reviewerId,
            string changesRequested)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId is required.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(approvalId)) throw new ArgumentException("ApprovalId is required.", nameof(approvalId));
            if (string.IsNullOrWhiteSpace(reviewerId)) throw new ArgumentException("ReviewerId is required.", nameof(reviewerId));

            if (!_tenantApprovals.TryGetValue(tenantId, out var tenantDict) ||
                !tenantDict.TryGetValue(approvalId, out var request))
            {
                throw new KeyNotFoundException($"Approval request {approvalId} not found for tenant {tenantId}.");
            }

            var now = DateTimeOffset.UtcNow;
            var updated = request with
            {
                State = ApprovalState.ChangesRequested,
                ReviewerId = reviewerId,
                DecidedAt = now,
                ChangesRequestedNotes = changesRequested
            };

            tenantDict[approvalId] = updated;
            RecordAuditEntry(tenantId, approvalId, request.State, ApprovalState.ChangesRequested, reviewerId, $"Changes requested by {reviewerId}: {changesRequested}");

            return Task.FromResult(updated);
        }

        public Task<int> CheckAndExpireApprovalsAsync(string tenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId) || !_tenantApprovals.TryGetValue(tenantId, out var tenantDict))
                return Task.FromResult(0);

            var now = DateTimeOffset.UtcNow;
            var expiredCount = 0;

            foreach (var kvp in tenantDict)
            {
                if (kvp.Value.IsExpired(now))
                {
                    tenantDict[kvp.Key] = kvp.Value with { State = ApprovalState.Expired };
                    RecordAuditEntry(tenantId, kvp.Key, kvp.Value.State, ApprovalState.Expired, "SystemTimer", "Expired past SLA.");
                    expiredCount++;
                }
            }

            return Task.FromResult(expiredCount);
        }

        public Task<IReadOnlyList<ApprovalAuditEntry>> GetAuditTrailAsync(string tenantId, string approvalId)
        {
            if (_tenantAuditTrails.TryGetValue(tenantId, out var tenantAudits) &&
                tenantAudits.TryGetValue(approvalId, out var list))
            {
                lock (_syncLock)
                {
                    return Task.FromResult<IReadOnlyList<ApprovalAuditEntry>>(list.ToList());
                }
            }

            return Task.FromResult<IReadOnlyList<ApprovalAuditEntry>>(Array.Empty<ApprovalAuditEntry>());
        }

        private void RecordAuditEntry(string tenantId, string approvalId, ApprovalState oldState, ApprovalState newState, string by, string details)
        {
            var auditDict = _tenantAuditTrails.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, List<ApprovalAuditEntry>>());
            var list = auditDict.GetOrAdd(approvalId, _ => new List<ApprovalAuditEntry>());
            var auditId = $"AUDIT-{Guid.NewGuid():N}";
            var now = DateTimeOffset.UtcNow;
            var hash = ComputeSha256($"{auditId}:{approvalId}:{oldState}:{newState}:{by}:{now:O}");

            var entry = new ApprovalAuditEntry(
                AuditId: auditId,
                ApprovalId: approvalId,
                TenantId: tenantId,
                PreviousState: oldState,
                NewState: newState,
                InitiatedBy: by,
                Timestamp: now,
                Details: details,
                IntegrityHash: hash
            );

            lock (_syncLock)
            {
                list.Add(entry);
            }
        }

        private static string ComputeSha256(string raw)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw ?? string.Empty));
            return Convert.ToHexString(bytes);
        }
    }
}
