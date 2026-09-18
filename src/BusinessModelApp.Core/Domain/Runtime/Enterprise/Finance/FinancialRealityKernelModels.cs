using System;
using System.Collections.Generic;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Finance
{
    public enum InvoiceStatus
    {
        Draft = 0,
        ApprovedBatch6 = 1,
        Issued = 2,
        Paid = 3,
        Overdue = 4,
        Cancelled = 5
    }

    public enum DeliveryCostCategory
    {
        AgentCompute = 0,
        ExternalApi = 1,
        CloudInfrastructure = 2,
        PaymentProcessingFee = 3,
        HumanSupervision = 4
    }

    public sealed class CommercialInvoice
    {
        public string InvoiceId { get; init; } = Guid.NewGuid().ToString("N");
        public required string TenantId { get; init; }
        public required string CustomerId { get; init; }
        public required string ContractId { get; init; }
        public decimal SubtotalINR { get; init; }
        public decimal TaxRatePct { get; init; } = 18.0m;
        public decimal TaxAmountINR => SubtotalINR * (TaxRatePct / 100m);
        public decimal TotalAmountINR => SubtotalINR + TaxAmountINR;
        public DateTime DueDateUtc { get; init; }
        public InvoiceStatus Status { get; private set; } = InvoiceStatus.Draft;
        public string Batch6PermitId { get; private set; } = string.Empty;
        public DateTime? IssuedAtUtc { get; private set; }
        public DateTime? PaidAtUtc { get; private set; }

        public void AuthorizeBatch6(string permitId)
        {
            if (string.IsNullOrWhiteSpace(permitId)) throw new ArgumentException("Batch 6 permit required for invoice approval.", nameof(permitId));
            Batch6PermitId = permitId.Trim();
            Status = InvoiceStatus.ApprovedBatch6;
        }

        public void MarkIssued()
        {
            if (Status != InvoiceStatus.ApprovedBatch6)
                throw new InvalidOperationException("Invoice must be Batch 6 approved before issuance.");
            Status = InvoiceStatus.Issued;
            IssuedAtUtc = DateTime.UtcNow;
        }

        public void MarkPaid()
        {
            if (Status != InvoiceStatus.Issued && Status != InvoiceStatus.Overdue)
                throw new InvalidOperationException($"Cannot mark invoice paid from status '{Status}'.");
            Status = InvoiceStatus.Paid;
            PaidAtUtc = DateTime.UtcNow;
        }

        public void CheckOverdue()
        {
            if (Status == InvoiceStatus.Issued && DateTime.UtcNow > DueDateUtc)
            {
                Status = InvoiceStatus.Overdue;
            }
        }
    }

    public sealed class BankCashReceipt
    {
        public string ReceiptId { get; init; } = Guid.NewGuid().ToString("N");
        public required string TenantId { get; init; }
        public required string InvoiceId { get; init; }
        public decimal AmountINR { get; init; }
        public required string BankWireReference { get; init; }
        public required string BankStatementDigestSha256 { get; init; }
        public EpistemicEvidenceLevel EvidenceLevel { get; init; } = EpistemicEvidenceLevel.BankVerifiedCash;
        public DateTime SettledAtUtc { get; init; } = DateTime.UtcNow;

        public BankCashReceipt Validate()
        {
            if (AmountINR <= 0) throw new ArgumentOutOfRangeException(nameof(AmountINR), "Cash receipt amount must be positive.");
            if (string.IsNullOrWhiteSpace(BankWireReference)) throw new ArgumentException("Bank wire reference required.", nameof(BankWireReference));
            if (string.IsNullOrWhiteSpace(BankStatementDigestSha256) || BankStatementDigestSha256.Length != 64)
                throw new ArgumentException("Bank statement digest must be a 64-character SHA-256 hex string.", nameof(BankStatementDigestSha256));
            if (EvidenceLevel != EpistemicEvidenceLevel.BankVerifiedCash)
                throw new InvalidOperationException("BankCashReceipt must have EpistemicEvidenceLevel.BankVerifiedCash (Law I43).");
            return this;
        }
    }

    public sealed class DeliveryCostEntry
    {
        public string CostId { get; init; } = Guid.NewGuid().ToString("N");
        public required string TenantId { get; init; }
        public required string CustomerId { get; init; }
        public DeliveryCostCategory Category { get; init; }
        public decimal AmountINR { get; init; }
        public required string Description { get; init; }
        public DateTime IncurredAtUtc { get; init; } = DateTime.UtcNow;
    }

    public sealed class CustomerProfitabilityReport
    {
        public required string TenantId { get; init; }
        public required string CustomerId { get; init; }
        public decimal TotalInvoicedINR { get; init; }
        public decimal TotalCashCollectedINR { get; init; }
        public decimal TotalDeliveryCostINR { get; init; }
        public decimal GrossContributionMarginINR => TotalCashCollectedINR - TotalDeliveryCostINR;
        public decimal GrossContributionMarginPct => TotalCashCollectedINR > 0
            ? ((TotalCashCollectedINR - TotalDeliveryCostINR) / TotalCashCollectedINR) * 100m
            : 0m;
    }

    public sealed class TreasuryCashPosition
    {
        public required string TenantId { get; init; }
        public decimal TotalBankCashBalanceINR { get; init; }
        public decimal MonthlyCashInflowINR { get; init; }
        public decimal MonthlyCashOutflowINR { get; init; }
        public decimal MonthlyNetBurnINR => Math.Max(0m, MonthlyCashOutflowINR - MonthlyCashInflowINR);
        public decimal RunwayMonths => MonthlyNetBurnINR > 0 ? TotalBankCashBalanceINR / MonthlyNetBurnINR : 999m;

        public decimal Receivables_0_30_Days { get; init; }
        public decimal Receivables_31_60_Days { get; init; }
        public decimal Receivables_61_90_Days { get; init; }
        public decimal Receivables_90_Plus_Days { get; init; }
        public decimal TotalOutstandingReceivablesINR => Receivables_0_30_Days + Receivables_31_60_Days + Receivables_61_90_Days + Receivables_90_Plus_Days;
    }
}
