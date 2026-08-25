using System;

namespace BusinessModelApp.Core.DTOs.Expense
{
    public class ExpenseDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ExpenseType Type { get; set; }
        public string Category { get; set; }
        
        // Amount Details
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }  // Including tax
        
        // Budget and Control
        public decimal BudgetedAmount { get; set; }
        public decimal Variance { get; set; }
        public string VarianceJustification { get; set; }
        public bool RequiresApproval { get; set; }
        
        // Allocation
        public string Department { get; set; }
        public string CostCenter { get; set; }
        public string Project { get; set; }
        public string[] Tags { get; set; }
        
        // Vendor/Payment
        public string VendorName { get; set; }
        public string VendorId { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentStatus { get; set; }  // Pending, Paid, Cancelled
        public string InvoiceNumber { get; set; }
        
        // Timing
        public DateTime ExpenseDate { get; set; }
        public DateTime DueDate { get; set; }
        public string BillingCycle { get; set; }  // One-time, Monthly, Quarterly, Annual
        public bool IsRecurring { get; set; }
        
        // Approval
        public string Status { get; set; }  // Draft, Submitted, Approved, Rejected
        public string ApprovedBy { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public string[] ApprovalComments { get; set; }
        
        // Attachments and Documentation
        public string[] ReceiptUrls { get; set; }
        public string[] SupportingDocuments { get; set; }
        
        // Business Context
        public bool IsBillable { get; set; }
        public string BusinessPurpose { get; set; }
        public int BusinessModelId { get; set; }
        
        // Meta Information
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
    }
}
