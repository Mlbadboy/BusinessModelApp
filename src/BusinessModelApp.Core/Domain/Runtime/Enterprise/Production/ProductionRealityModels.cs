using System;
using System.Collections.Generic;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Production
{
    public enum ExternalEffectState
    {
        PendingExecution = 0,
        Success = 1,
        Failed = 2,
        UnknownEffect = 3,
        Compensated = 4
    }

    public sealed class ProductionBusinessIdentity
    {
        public required string TenantId { get; init; }
        public required string LegalBusinessName { get; init; }
        public required string BusinessObjective { get; init; }
        public string TargetMarket { get; init; } = string.Empty;
        public string ICPDescription { get; init; } = string.Empty;
        public required UnitEconomicsPolicy EconomicsPolicy { get; init; }
        public string ContractAuthoritySignatory { get; init; } = string.Empty;
        public string FinancialDisbursementAuthority { get; init; } = string.Empty;
        public DateTime RegisteredAtUtc { get; init; } = DateTime.UtcNow;
    }

    public sealed class ExternalEffectRecord
    {
        public string ExternalEffectId { get; init; } = Guid.NewGuid().ToString("N");
        public required string TenantId { get; init; }
        public required string ConnectorName { get; init; }
        public required string ExternalProvider { get; init; }
        public required string IdempotencyKey { get; init; }
        public string RequestPayloadHashSha256 { get; init; } = string.Empty;
        public string ProviderReferenceId { get; private set; } = string.Empty;
        public ExternalEffectState State { get; private set; } = ExternalEffectState.PendingExecution;
        public string ResponsePayloadHashSha256 { get; private set; } = string.Empty;
        public DateTime ExecutedAtUtc { get; init; } = DateTime.UtcNow;
        public string ErrorDetails { get; private set; } = string.Empty;

        public void MarkSuccess(string providerRef, string responseHash)
        {
            State = ExternalEffectState.Success;
            ProviderReferenceId = providerRef.Trim();
            ResponsePayloadHashSha256 = responseHash.Trim();
        }

        public void MarkUnknown(string details)
        {
            State = ExternalEffectState.UnknownEffect;
            ErrorDetails = details;
        }

        public void Reconcile(ExternalEffectState state, string notes)
        {
            State = state;
            ErrorDetails = notes;
        }
    }

    public sealed class ProductionLineageRecord
    {
        public string LineageId { get; init; } = Guid.NewGuid().ToString("N");
        public required string TenantId { get; init; }
        public required string BusinessObjectiveId { get; init; }
        public required string GrowthObjectiveId { get; init; }
        public required string OpportunityId { get; init; }
        public required string MissionId { get; init; }
        public required string PermitId { get; init; }
        public required string ExternalEffectId { get; init; }
        public required string ContractId { get; init; }
        public required string InvoiceId { get; init; }
        public required string BankPaymentReferenceId { get; init; }
        public decimal RealizedCashINR { get; init; }
        public decimal DirectDeliveryCostsINR { get; init; }
        public decimal AgentComputeCostsINR { get; init; }
        public decimal NetContributionINR => RealizedCashINR - (DirectDeliveryCostsINR + AgentComputeCostsINR);
        public DateTime RecordedAtUtc { get; init; } = DateTime.UtcNow;

        public bool IsCompleteLineage =>
            !string.IsNullOrWhiteSpace(BusinessObjectiveId) &&
            !string.IsNullOrWhiteSpace(OpportunityId) &&
            !string.IsNullOrWhiteSpace(MissionId) &&
            !string.IsNullOrWhiteSpace(PermitId) &&
            !string.IsNullOrWhiteSpace(ExternalEffectId) &&
            !string.IsNullOrWhiteSpace(ContractId) &&
            !string.IsNullOrWhiteSpace(InvoiceId) &&
            !string.IsNullOrWhiteSpace(BankPaymentReferenceId);
    }
}
