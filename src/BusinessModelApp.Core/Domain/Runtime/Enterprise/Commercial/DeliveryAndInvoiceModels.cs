using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Commercial
{
    public enum WorkOrderStatus
    {
        SCHEDULED,
        IN_PROGRESS,
        COMPLETED,
        ACCEPTED_BY_CUSTOMER,
        REJECTED
    }

    public class CommercialWorkOrder
    {
        public string WorkOrderId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string OpportunityId { get; set; } = string.Empty;
        public string ContractId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public List<string> Deliverables { get; set; } = new();
        public WorkOrderStatus Status { get; set; } = WorkOrderStatus.SCHEDULED;
        public string CustomerSignoffDigestSha256 { get; set; } = string.Empty;
        public string CustomerSignerEmail { get; set; } = string.Empty;
        public DateTime? CustomerSignoffRecordedAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public bool IsCustomerAccepted => Status == WorkOrderStatus.ACCEPTED_BY_CUSTOMER 
                                          && !string.IsNullOrWhiteSpace(CustomerSignoffDigestSha256);
    }

    public enum InvoiceStatus
    {
        DRAFT,
        APPROVED,
        ISSUED,
        PAID,
        CANCELLED
    }

    public class CommercialInvoice
    {
        public string InvoiceId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string OpportunityId { get; set; } = string.Empty;
        public string ContractId { get; set; } = string.Empty;
        public string WorkOrderId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public decimal SubtotalINR { get; set; } = 0m;
        public decimal TaxRatePercent { get; set; } = 18.0m; // Default GST rate
        public decimal TaxAmountINR => Math.Round(SubtotalINR * (TaxRatePercent / 100m), 2);
        public decimal TotalAmountINR => SubtotalINR + TaxAmountINR;
        public InvoiceStatus Status { get; set; } = InvoiceStatus.DRAFT;
        public bool IsBatch6Authorized { get; set; } = false;
        public string Batch6PermitId { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? IssuedAtUtc { get; set; }
        public DateTime DueDateUtc { get; set; } = DateTime.UtcNow.AddDays(30);
    }

    public class CashCollectionReceipt
    {
        public string ReceiptId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string InvoiceId { get; set; } = string.Empty;
        public string ContractId { get; set; } = string.Empty;
        public string OpportunityId { get; set; } = string.Empty;
        public decimal CollectedAmountINR { get; set; } = 0m;
        public string BankReferenceNumber { get; set; } = string.Empty;
        public string GatewayOrRailId { get; set; } = string.Empty;
        public string BankConfirmationDigestSha256 { get; set; } = string.Empty;
        public DateTime VerifiedAtUtc { get; set; } = DateTime.UtcNow;

        public bool IsBankVerified => !string.IsNullOrWhiteSpace(BankReferenceNumber) 
                                      && !string.IsNullOrWhiteSpace(BankConfirmationDigestSha256)
                                      && CollectedAmountINR > 0m;
    }
}
