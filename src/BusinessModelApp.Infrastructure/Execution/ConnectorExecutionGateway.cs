using System;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BusinessModelApp.Core.Domain.Execution;
using BusinessModelApp.Core.Interfaces;

namespace BusinessModelApp.Infrastructure.Execution
{
    public class ConnectorExecutionGateway : IConnectorExecutionGateway
    {
        private readonly ILogger<ConnectorExecutionGateway> _logger;
        public const string PermitMasterSecret = "CharlieOS_PermitHMAC_Secret_2026_ExecutionFirewall";

        public ConnectorExecutionGateway(ILogger<ConnectorExecutionGateway> logger)
        {
            _logger = logger;
        }

        public async Task<ExecutionReceipt> DispatchConnectorActionAsync(
            ExecutionRequest request,
            ExecutionPermit permit,
            CancellationToken cancellationToken = default)
        {
            var start = DateTime.UtcNow;

            // 1. Mandatory Permit Verification
            if (permit == null)
            {
                throw new SecurityException("[Connector Execution Gateway] VIOLATION: Execution attempted without an ExecutionPermit. Fail-Closed.");
            }

            if (permit.IsConsumed)
            {
                throw new SecurityException("[Connector Execution Gateway] VIOLATION: ExecutionPermit is already consumed (Single-Use Violation). Replay attack blocked.");
            }

            if (DateTime.UtcNow > permit.ExpiresAtUtc)
            {
                throw new SecurityException("[Connector Execution Gateway] VIOLATION: ExecutionPermit has expired (TTL exceeded). Fail-Closed.");
            }

            if (!permit.ValidatePermitToken(PermitMasterSecret))
            {
                throw new SecurityException("[Connector Execution Gateway] VIOLATION: Cryptographic HMAC signature on ExecutionPermit is invalid. Tamper attempt detected.");
            }

            // Check payload digest match
            var currentDigest = request.ComputePayloadDigest();
            if (!string.Equals(permit.PayloadDigest, currentDigest, StringComparison.OrdinalIgnoreCase))
            {
                throw new SecurityException("[Connector Execution Gateway] VIOLATION: Request payload digest does not match permit digest. Payload altered after permit issue.");
            }

            // Mark permit consumed
            permit.IsConsumed = true;

            // 2. Dispatch to dumb connector execution adapter
            _logger.LogInformation("Dispatching governed connector action '{Capability}' for Workspace {WorkspaceId}, Permit {PermitId}",
                request.CapabilityId, request.WorkspaceId, permit.PermitId);

            // Simulate deterministic connector execution (CRM, Email, Payment, Razorpay, etc.)
            await Task.Delay(15, cancellationToken); // Realistic execution duration

            var resultPayload = JsonSerializer.Serialize(new
            {
                CapabilityExecuted = request.CapabilityId,
                Success = true,
                TimestampUtc = DateTime.UtcNow,
                ExecutedAmountINR = request.MonetaryImpactINR,
                ExternalTransactionId = $"TXN_{Guid.NewGuid():N}".Substring(0, 16).ToUpperInvariant(),
                ConfirmationNote = $"Successfully processed action under Permit {permit.PermitId}"
            });

            var durationMs = (int)(DateTime.UtcNow - start).TotalMilliseconds;

            using var sha = SHA256.Create();
            var resultHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(resultPayload))).ToLowerInvariant();

            return new ExecutionReceipt
            {
                ReceiptId = Guid.NewGuid(),
                RequestId = request.RequestId,
                PermitId = permit.PermitId,
                WorkspaceId = request.WorkspaceId,
                CapabilityId = request.CapabilityId,
                Status = ExecutionStatus.Succeeded,
                IdempotencyKey = request.IdempotencyKey,
                ResultPayloadJson = resultPayload,
                ResultHash = resultHash,
                ExecutedAtUtc = DateTime.UtcNow,
                DurationMs = durationMs
            };
        }
    }
}
