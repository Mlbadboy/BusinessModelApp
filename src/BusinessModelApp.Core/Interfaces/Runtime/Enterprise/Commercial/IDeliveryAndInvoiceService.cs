using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Commercial;

namespace BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Commercial
{
    public interface IDeliveryAndInvoiceStore
    {
        Task SaveWorkOrderAsync(CommercialWorkOrder workOrder);
        Task<CommercialWorkOrder?> GetWorkOrderAsync(string tenantId, string workOrderId);
        Task<IReadOnlyList<CommercialWorkOrder>> ListWorkOrdersAsync(string tenantId);

        Task SaveInvoiceAsync(CommercialInvoice invoice);
        Task<CommercialInvoice?> GetInvoiceAsync(string tenantId, string invoiceId);
        Task<IReadOnlyList<CommercialInvoice>> ListInvoicesAsync(string tenantId);

        Task SaveReceiptAsync(CashCollectionReceipt receipt);
        Task<CashCollectionReceipt?> GetReceiptAsync(string tenantId, string receiptId);
        Task<IReadOnlyList<CashCollectionReceipt>> ListReceiptsAsync(string tenantId);
    }

    public interface IDeliveryAndInvoiceService
    {
        Task<CommercialWorkOrder> CreateWorkOrderAsync(CommercialWorkOrder workOrder);
        Task<CommercialWorkOrder> RecordCustomerDeliveryAcceptanceAsync(string tenantId, string workOrderId, string signerEmail, string signoffDigestSha256);
        Task<CommercialWorkOrder?> GetWorkOrderAsync(string tenantId, string workOrderId);

        Task<CommercialInvoice> GenerateInvoiceAsync(string tenantId, string contractId, string workOrderId, decimal subtotalINR);
        Task<CommercialInvoice> IssueInvoiceWithBatch6PermitAsync(string tenantId, string invoiceId, string batch6PermitId);
        Task<CommercialInvoice?> GetInvoiceAsync(string tenantId, string invoiceId);

        Task<CashCollectionReceipt> ProcessCashCollectionAsync(string tenantId, string invoiceId, decimal collectedAmountINR, string bankRef, string gatewayId, string bankDigestSha256);
        Task<CashCollectionReceipt?> GetReceiptAsync(string tenantId, string receiptId);
    }
}
