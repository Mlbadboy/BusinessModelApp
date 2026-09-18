using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Commercial;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Commercial;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Commercial
{
    public class InMemoryCommercialKernelStore : ICommercialKernelStore
    {
        private readonly ConcurrentDictionary<string, CommercialStageRecord> _entities = new();

        public Task SaveEntityAsync(CommercialStageRecord entity)
        {
            var key = $"{entity.TenantId}:{entity.CommercialEntityId}";
            _entities[key] = entity;
            return Task.CompletedTask;
        }

        public Task<CommercialStageRecord?> GetEntityAsync(string tenantId, string entityId)
        {
            var key = $"{tenantId}:{entityId}";
            _entities.TryGetValue(key, out var entity);
            return Task.FromResult(entity);
        }

        public Task<IReadOnlyList<CommercialStageRecord>> ListEntitiesAsync(string tenantId)
        {
            var list = _entities.Values.Where(e => e.TenantId == tenantId).ToList();
            return Task.FromResult<IReadOnlyList<CommercialStageRecord>>(list);
        }
    }

    public class CommercialKernelService : ICommercialKernelService
    {
        private readonly ICommercialKernelStore _store;

        public CommercialKernelService(ICommercialKernelStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<CommercialStageRecord> InitializeCommercialEntityAsync(string tenantId, string opportunityId, string accountId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("TenantId is required.", nameof(tenantId));

            var record = new CommercialStageRecord
            {
                TenantId = tenantId,
                OpportunityId = opportunityId,
                AccountId = accountId,
                CurrentStage = CommercialStage.LEAD,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            var genesisAudit = new CommercialStageAuditRecord
            {
                CommercialEntityId = record.CommercialEntityId,
                TenantId = tenantId,
                FromStage = CommercialStage.LEAD,
                ToStage = CommercialStage.LEAD,
                InitiatedByAgentId = "SYSTEM_INITIALIZER",
                AuthoritativeEvidenceId = "GENESIS_SIGNAL",
                EvidenceDigest = "sha256-genesis",
                PreviousBlockHash = "GENESIS",
                TimestampUtc = DateTime.UtcNow
            };
            genesisAudit.CurrentBlockHash = genesisAudit.ComputeHash();

            record.History.Add(genesisAudit);
            record.LatestBlockHash = genesisAudit.CurrentBlockHash;

            await _store.SaveEntityAsync(record);
            return record;
        }

        public async Task<CommercialStageRecord?> GetCommercialEntityAsync(string tenantId, string commercialEntityId)
        {
            return await _store.GetEntityAsync(tenantId, commercialEntityId);
        }

        public async Task<IReadOnlyList<CommercialStageRecord>> ListCommercialEntitiesAsync(string tenantId)
        {
            return await _store.ListEntitiesAsync(tenantId);
        }

        public async Task<CommercialTransitionResult> AttemptTransitionAsync(CommercialTransitionRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var entity = await _store.GetEntityAsync(request.TenantId, request.CommercialEntityId);
            if (entity == null)
            {
                return new CommercialTransitionResult
                {
                    IsSuccess = false,
                    CurrentStage = CommercialStage.LEAD,
                    RejectionReason = $"Commercial entity {request.CommercialEntityId} not found.",
                    ConstitutionalViolations = { "EntityNotFound" }
                };
            }

            var violations = new List<string>();

            // Law I40-S: Failed actions or absence of evidence cannot advance state
            if (string.IsNullOrWhiteSpace(request.AuthoritativeEvidenceId))
            {
                violations.Add(CommercialConstitutionalInvariants.LawI40C_AgentBeliefNotTruth);
            }

            // Progression boundary: Target stage must not exceed +1 stage without verified justification
            var stageDiff = (int)request.TargetStage - (int)entity.CurrentStage;
            if (stageDiff > 1)
            {
                violations.Add(CommercialConstitutionalInvariants.LawI40K_AgentActivityNotProgress + " (Cannot skip commercial stages without individual step verification)");
            }

            // Law I40-A & I40-B & I40-E: Proposal or qualifying cannot equal acceptance or deal
            if (request.TargetStage == CommercialStage.DEAL_WON && string.IsNullOrWhiteSpace(request.HumanSignoffId))
            {
                violations.Add(CommercialConstitutionalInvariants.LawI40W_AutonomyCannotBypassGovernance + " (DEAL_WON requires explicit human PRG-1 sign-off)");
            }

            if (request.TargetStage == CommercialStage.CONTRACT_VERIFIED && string.IsNullOrWhiteSpace(request.EvidenceDigest))
            {
                violations.Add(CommercialConstitutionalInvariants.LawI40O_ContractExecutionRequiresGovernance + " (Contract verification requires cryptographic digest)");
            }

            // Law I40-P, I40-Q, I40-Z: Financial stages subordinate to Batch 6 Execution Firewall
            if (request.TargetStage >= CommercialStage.INVOICED)
            {
                if (!request.IsBatch6Authorized || string.IsNullOrWhiteSpace(request.Batch6PermitId))
                {
                    violations.Add(CommercialConstitutionalInvariants.LawI40P_FinancialActionsUnderBatch6 + " (Financial stage transitions strictly require Batch 6 Execution Firewall Permit)");
                }
            }

            // Revenue Realized requires payment verification
            if (request.TargetStage == CommercialStage.REVENUE_REALIZED && entity.CurrentStage != CommercialStage.PAYMENT_VERIFIED)
            {
                violations.Add(CommercialConstitutionalInvariants.LawI40Q_RevenueRequiresFinancialEvidence + " (Revenue cannot be realized without verified reconciled payment)");
            }

            if (violations.Count > 0)
            {
                return new CommercialTransitionResult
                {
                    IsSuccess = false,
                    CurrentStage = entity.CurrentStage,
                    RejectionReason = string.Join("; ", violations),
                    ConstitutionalViolations = violations
                };
            }

            // Record cryptographic block transition
            var audit = new CommercialStageAuditRecord
            {
                CommercialEntityId = entity.CommercialEntityId,
                TenantId = entity.TenantId,
                FromStage = entity.CurrentStage,
                ToStage = request.TargetStage,
                InitiatedByAgentId = request.InitiatingAgentId,
                AuthoritativeEvidenceId = request.AuthoritativeEvidenceId,
                EvidenceDigest = request.EvidenceDigest,
                HumanSignoffId = request.HumanSignoffId,
                PreviousBlockHash = entity.LatestBlockHash,
                TimestampUtc = DateTime.UtcNow
            };
            audit.CurrentBlockHash = audit.ComputeHash();

            entity.CurrentStage = request.TargetStage;
            entity.UpdatedAtUtc = DateTime.UtcNow;
            entity.LatestBlockHash = audit.CurrentBlockHash;
            entity.CorroboratedEvidenceIds.Add(request.AuthoritativeEvidenceId);
            entity.History.Add(audit);

            await _store.SaveEntityAsync(entity);

            return new CommercialTransitionResult
            {
                IsSuccess = true,
                CurrentStage = entity.CurrentStage,
                AuditRecord = audit
            };
        }

        public async Task<bool> VerifyAuditTrailIntegrityAsync(string tenantId, string commercialEntityId)
        {
            var entity = await _store.GetEntityAsync(tenantId, commercialEntityId);
            if (entity == null || entity.History.Count == 0) return false;

            var prevHash = "GENESIS";
            foreach (var block in entity.History)
            {
                if (block.PreviousBlockHash != prevHash)
                    return false;

                var computed = block.ComputeHash();
                if (computed != block.CurrentBlockHash)
                    return false;

                prevHash = block.CurrentBlockHash;
            }

            return true;
        }
    }
}
