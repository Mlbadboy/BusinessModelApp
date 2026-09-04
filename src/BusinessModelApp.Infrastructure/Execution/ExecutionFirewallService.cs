using System;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BusinessModelApp.Core.Agents;
using BusinessModelApp.Core.Domain.Execution;
using BusinessModelApp.Core.Interfaces;

namespace BusinessModelApp.Infrastructure.Execution
{
    public class ExecutionFirewallService : IExecutionFirewall
    {
        private readonly IAuthorityDelegationService _authorityService;
        private readonly IRiskEngine _riskEngine;
        private readonly IBudgetGuardService _budgetService;
        private readonly IApprovalGateway _approvalGateway;
        private readonly IConnectorExecutionGateway _connectorGateway;
        private readonly IExecutionLedgerService _ledgerService;
        private readonly IExecutionKillSwitchService _killSwitchService;
        private readonly ILogger<ExecutionFirewallService> _logger;

        public ExecutionFirewallService(
            IAuthorityDelegationService authorityService,
            IRiskEngine riskEngine,
            IBudgetGuardService budgetService,
            IApprovalGateway approvalGateway,
            IConnectorExecutionGateway connectorGateway,
            IExecutionLedgerService ledgerService,
            IExecutionKillSwitchService killSwitchService,
            ILogger<ExecutionFirewallService> logger)
        {
            _authorityService = authorityService;
            _riskEngine = riskEngine;
            _budgetService = budgetService;
            _approvalGateway = approvalGateway;
            _connectorGateway = connectorGateway;
            _ledgerService = ledgerService;
            _killSwitchService = killSwitchService;
            _logger = logger;
        }

        public async Task<ExecutionDecision> EvaluateAsync(ExecutionRequest request, CancellationToken cancellationToken = default)
        {
            // Pillar 1: Constitutional Invariants (Fail-Closed)
            ExecutionConstitution.AssertConstitutionalValidity(
                request.WorkspaceId,
                request.AgentId,
                request.CapabilityId,
                request.IdempotencyKey,
                request.AuditContext);

            // Pillar 2: Hierarchical Emergency Kill Switch Check
            var isHalted = await _killSwitchService.IsHaltedAsync(
                request.WorkspaceId,
                request.MissionId,
                request.AgentId,
                request.CapabilityId,
                cancellationToken);

            if (isHalted)
            {
                _logger.LogCritical("Execution request {RequestId} DENIED: Emergency Kill Switch is active for workspace {WorkspaceId} or target {Cap}",
                    request.RequestId, request.WorkspaceId, request.CapabilityId);

                return new ExecutionDecision
                {
                    IsPermitted = false,
                    Denial = new ExecutionDenial
                    {
                        RequestId = request.RequestId,
                        ViolatingPillar = "KillSwitch",
                        Reason = "Execution halted by active emergency kill switch.",
                        EvaluatedRisk = ExecutionRiskTier.R5_DestructiveOrHighImpact
                    }
                };
            }

            // Pillar 3: Idempotency Verification
            var existingReceipt = await _ledgerService.CheckIdempotencyAsync(request.WorkspaceId, request.IdempotencyKey, cancellationToken);
            if (existingReceipt != null)
            {
                // Already successfully executed - no new permit required, returns cached execution
                return new ExecutionDecision
                {
                    IsPermitted = true,
                    Permit = new ExecutionPermit
                    {
                        PermitId = existingReceipt.PermitId,
                        RequestId = request.RequestId,
                        WorkspaceId = request.WorkspaceId,
                        CapabilityId = request.CapabilityId,
                        PayloadDigest = request.ComputePayloadDigest(),
                        IsConsumed = true
                    }
                };
            }

            // Pillar 4: Deterministic Risk Evaluation
            var riskTier = _riskEngine.EvaluateRisk(request);

            // Pillar 5: Authority Delegation Check
            var hasAuthority = await _authorityService.ValidateDelegationAsync(
                request.WorkspaceId,
                request.AgentId,
                request.CapabilityId,
                request.MonetaryImpactINR,
                cancellationToken);

            if (!hasAuthority)
            {
                _logger.LogWarning("Execution request {RequestId} DENIED: Agent {AgentId} lacks delegated authority for capability {Cap} or amount ₹{Amount}",
                    request.RequestId, request.AgentId, request.CapabilityId, request.MonetaryImpactINR);

                return new ExecutionDecision
                {
                    IsPermitted = false,
                    Denial = new ExecutionDenial
                    {
                        RequestId = request.RequestId,
                        ViolatingPillar = "AuthorityDelegation",
                        Reason = $"Agent '{request.AgentId}' does not possess valid, active delegated authority for '{request.CapabilityId}'.",
                        EvaluatedRisk = riskTier
                    }
                };
            }

            // Pillar 6: Budget & Financial Guard
            if (request.MonetaryImpactINR > 0m)
            {
                var budgetReserved = await _budgetService.ValidateAndReserveBudgetAsync(
                    request.WorkspaceId,
                    request.RequestId,
                    request.MonetaryImpactINR,
                    cancellationToken);

                if (!budgetReserved)
                {
                    return new ExecutionDecision
                    {
                        IsPermitted = false,
                        Denial = new ExecutionDenial
                        {
                            RequestId = request.RequestId,
                            ViolatingPillar = "BudgetGuard",
                            Reason = $"Financial commitment of ₹{request.MonetaryImpactINR:N0} exceeds available wallet balance or daily spend limit.",
                            EvaluatedRisk = riskTier
                        }
                    };
                }
            }

            // Pillar 7: Human Approval Gateway (Mandatory for R4, R5, or unapproved high risk)
            if (riskTier >= ExecutionRiskTier.R4_LegalContract || (riskTier == ExecutionRiskTier.R3_FinancialCommitment && request.MonetaryImpactINR > 50000m))
            {
                var currentDigest = request.ComputePayloadDigest();
                var isApproved = await _approvalGateway.ValidateApprovalAsync(request.RequestId, currentDigest, cancellationToken);

                if (!isApproved)
                {
                    // Create approval request if not exists
                    var approval = await _approvalGateway.CreateApprovalRequestAsync(
                        request,
                        riskTier,
                        $"Autonomous request for {request.CapabilityId} with risk {riskTier} and monetary impact ₹{request.MonetaryImpactINR:N0}",
                        cancellationToken);

                    return new ExecutionDecision
                    {
                        IsPermitted = false,
                        RequiresHumanApproval = true,
                        ApprovalRequestId = approval.Id,
                        Denial = new ExecutionDenial
                        {
                            RequestId = request.RequestId,
                            ViolatingPillar = "HumanApprovalGateway",
                            Reason = $"High risk execution tier ({riskTier}) requires sovereign human approval. Created Approval Request {approval.Id}.",
                            EvaluatedRisk = riskTier
                        }
                    };
                }
            }

            // Pillar 8: Preconditions Validation
            if (request.Preconditions != null && request.Preconditions.Count > 0)
            {
                foreach (var (k, v) in request.Preconditions)
                {
                    if (string.Equals(v, "false", StringComparison.OrdinalIgnoreCase))
                    {
                        return new ExecutionDecision
                        {
                            IsPermitted = false,
                            Denial = new ExecutionDenial
                            {
                                RequestId = request.RequestId,
                                ViolatingPillar = "Preconditions",
                                Reason = $"Precondition '{k}' was not satisfied.",
                                EvaluatedRisk = riskTier
                            }
                        };
                    }
                }
            }

            // Pillar 9 & 10: Cryptographic Permit Generation
            var permitId = Guid.NewGuid();
            var payloadDigest = request.ComputePayloadDigest();
            var permitToken = ExecutionPermit.GeneratePermitToken(
                permitId,
                request.WorkspaceId,
                request.CapabilityId,
                payloadDigest,
                ConnectorExecutionGateway.PermitMasterSecret);

            var permit = new ExecutionPermit
            {
                PermitId = permitId,
                RequestId = request.RequestId,
                WorkspaceId = request.WorkspaceId,
                CapabilityId = request.CapabilityId,
                PayloadDigest = payloadDigest,
                RiskTier = riskTier,
                ApprovedBudgetINR = request.MonetaryImpactINR,
                PermitToken = permitToken,
                IssuedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(2),
                IsConsumed = false
            };

            _logger.LogInformation("Execution Firewall PERMIT ISSUED: Permit {PermitId} for Request {RequestId}, Capability {Cap}, Risk {Risk}",
                permitId, request.RequestId, request.CapabilityId, riskTier);

            return new ExecutionDecision
            {
                IsPermitted = true,
                Permit = permit
            };
        }

        public async Task<ExecutionReceipt> ExecuteAsync(ExecutionRequest request, ExecutionPermit permit, CancellationToken cancellationToken = default)
        {
            if (permit == null)
            {
                throw new SecurityException("[Execution Firewall] Cannot execute without a valid ExecutionPermit.");
            }

            try
            {
                // Dispatch through governed Connector Execution Gateway
                var receipt = await _connectorGateway.DispatchConnectorActionAsync(request, permit, cancellationToken);

                // Settle budget if monetary impact exists
                if (request.MonetaryImpactINR > 0m)
                {
                    await _budgetService.SettleReservationAsync(request.WorkspaceId, request.RequestId, cancellationToken);
                }

                // Record in Immutable Execution Ledger
                await _ledgerService.RecordEntryAsync(new ExecutionLedgerEntry
                {
                    WorkspaceId = request.WorkspaceId,
                    OrganizationId = request.OrganizationId,
                    MissionId = request.MissionId,
                    RequestId = request.RequestId,
                    PermitId = permit.PermitId,
                    IdempotencyKey = request.IdempotencyKey,
                    CapabilityId = request.CapabilityId,
                    ActionTier = request.ActionTier,
                    RiskTier = permit.RiskTier,
                    Status = ExecutionStatus.Succeeded,
                    MonetaryImpactINR = request.MonetaryImpactINR,
                    RequestPayloadJson = request.PayloadJson,
                    ResultPayloadJson = receipt.ResultPayloadJson,
                    ExecutedAtUtc = DateTime.UtcNow,
                    DurationMs = receipt.DurationMs
                }, cancellationToken);

                return receipt;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Execution failed for Request {RequestId}, Capability {Cap}", request.RequestId, request.CapabilityId);

                // Release unconsumed budget reservation
                if (request.MonetaryImpactINR > 0m)
                {
                    await _budgetService.ReleaseReservationAsync(request.WorkspaceId, request.RequestId, cancellationToken);
                }

                // Record failure entry in ledger
                await _ledgerService.RecordEntryAsync(new ExecutionLedgerEntry
                {
                    WorkspaceId = request.WorkspaceId,
                    OrganizationId = request.OrganizationId,
                    MissionId = request.MissionId,
                    RequestId = request.RequestId,
                    PermitId = permit.PermitId,
                    IdempotencyKey = request.IdempotencyKey,
                    CapabilityId = request.CapabilityId,
                    ActionTier = request.ActionTier,
                    RiskTier = permit.RiskTier,
                    Status = ExecutionStatus.Failed,
                    MonetaryImpactINR = request.MonetaryImpactINR,
                    RequestPayloadJson = request.PayloadJson,
                    ResultPayloadJson = "{}",
                    ExecutedAtUtc = DateTime.UtcNow,
                    ErrorMessage = ex.Message
                }, cancellationToken);

                throw;
            }
        }
    }
}
