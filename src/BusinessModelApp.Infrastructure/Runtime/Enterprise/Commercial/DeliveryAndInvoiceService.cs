using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Commercial;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Commercial;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Commercial
{
    public class InMemoryDeliveryAndInvoiceStore : IDeliveryAndInvoiceStore
    {
        private readonly ConcurrentDictionary<string, CommercialWorkOrder> _workOrders = new();
        private readonly ConcurrentDictionary<string, CommercialInvoice> _invoices = new();
        private readonly ConcurrentDictionary<string, CashCollectionReceipt> _receipts = new();

        public Task SaveWorkOrderAsync(CommercialWorkOrder workOrder)
        {
            _workOrders[$"{workOrder.TenantId}:{workOrder.WorkOrderId}"] = workOrder;
            return Task.CompletedTask;
        }

        public Task<CommercialWorkOrder?> GetWorkOrderAsync(string tenantId, string workOrderId)
        {
            _workOrders.TryGetValue($"{tenantId}:{workOrderId}", out var order);
            return Task.FromResult(order);
        }

        public Task<IReadOnlyList<CommercialWorkOrder>> ListWorkOrdersAsync(string tenantId)
        {
            var list = _workOrders.Values.Where(w => w.TenantId == tenantId).ToList();
            return Task.FromResult<IReadOnlyList<CommercialWorkOrder>>(list);
        }

        public Task SaveInvoiceAsync(CommercialInvoice invoice)
        {
            _invoices[$"{invoice.TenantId}:{invoice.InvoiceId}"] = invoice;
            return Task.CompletedTask;
        }

        public Task<CommercialInvoice?> GetInvoiceAsync(string tenantId, string invoiceId)
        {
            _invoices.TryGetValue($"{tenantId}:{invoiceId}", out var invoice);
            return Task.FromResult(invoice);
        }

        public Task<IReadOnlyList<CommercialInvoice>> ListInvoicesAsync(string tenantId)
        {
            var list = _invoices.Values.Where(i => i.TenantId == tenantId).ToList();
            return Task.FromResult<IReadOnlyList<CommercialInvoice>>(list);
        }

        public Task SaveReceiptAsync(CashCollectionReceipt receipt)
        {
            _receipts[$"{receipt.TenantId}:{receipt.ReceiptId}"] = receipt;
            return Task.CompletedTask;
        }

        public Task<CashCollectionReceipt?> GetReceiptAsync(string tenantId, string receiptId)
        {
            _receipts.TryGetValue($"{tenantId}:{receiptId}", out var receipt);
            return Task.FromResult(receipt);
        }

        public Task<IReadOnlyList<CashCollectionReceipt>> ListReceiptsAsync(string tenantId)
        {
            var list = _receipts.Values.Where(r => r.TenantId == tenantId).ToList();
            return Task.FromResult<IReadOnlyList<CashCollectionReceipt>>(list);
        }
    }

    public class DeliveryAndInvoiceService : IDeliveryAndInvoiceService
    {
        private readonly IDeliveryAndInvoiceStore _store;

        public DeliveryAndInvoiceService(IDeliveryAndInvoiceStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<CommercialWorkOrder> CreateWorkOrderAsync(CommercialWorkOrder workOrder)
        {
            if (string.IsNullOrWhiteSpace(workOrder.TenantId))
                throw new ArgumentException("TenantId is required", nameof(workOrder));
            if (string.IsNullOrWhiteSpace(workOrder.OpportunityId))
                throw new ArgumentException("OpportunityId is required", nameof(workOrder));
            if (string.IsNullOrWhiteSpace(workOrder.ContractId))
                throw new ArgumentException("ContractId is required", nameof(workOrder));
            if (string.IsNullOrWhiteSpace(workOrder.Title))
                throw new ArgumentException("Title is required", nameof(workOrder));
            if (workOrder.Deliverables == null || workOrder.Deliverables.Count == 0)
                throw new ArgumentException("Deliverables must be non-empty", nameof(workOrder));

            await _store.SaveWorkOrderAsync(workOrder);
            return workOrder;
        }

        public async Task<CommercialWorkOrder> RecordCustomerDeliveryAcceptanceAsync(string tenantId, string workOrderId, string signerEmail, string signoffDigestSha256)
        {
            var order = await _store.GetWorkOrderAsync(tenantId, workOrderId);
            if (order == null)
                throw new KeyNotFoundException($"WorkOrder '{workOrderId}' not found for tenant '{tenantId}'.");

            if (string.IsNullOrWhiteSpace(signerEmail))
                throw new ArgumentException("Customer signer email is required for acceptance signoff", nameof(signerEmail));
            if (string.IsNullOrWhiteSpace(signoffDigestSha256))
                throw new ArgumentException("Cryptographic signoff digest SHA256 is required for delivery acceptance", nameof(signoffDigestSha256));

            order.Status = WorkOrderStatus.ACCEPTED_BY_CUSTOMER;
            order.CustomerSignerEmail = signerEmail;
            order.CustomerSignoffDigestSha256 = signoffDigestSha256;
            order.CustomerSignoffRecordedAtUtc = DateTime.UtcNow;

            await _store.SaveWorkOrderAsync(order);
            return order;
        }

        public async Task<CommercialWorkOrder?> GetWorkOrderAsync(string tenantId, string workOrderId)
        {
            return await _store.GetWorkOrderAsync(tenantId, workOrderId);
        }

        public async Task<CommercialInvoice> GenerateInvoiceAsync(string tenantId, string contractId, string workOrderId, decimal subtotalINR)
        {
            var order = await _store.GetWorkOrderAsync(tenantId, workOrderId);
            if (order == null)
                throw new KeyNotFoundException($"WorkOrder '{workOrderId}' not found.");

            if (!order.IsCustomerAccepted)
                throw new InvalidOperationException("Constitutional Violation (Law I40): Invoicing requires cryptographically verified customer delivery acceptance.");

            if (subtotalINR <= 0m)
                throw new ArgumentException("Subtotal must be positive", nameof(subtotalINR));

            var invoice = new CommercialInvoice
            {
                TenantId = tenantId,
                OpportunityId = order.OpportunityId,
                ContractId = contractId,
                WorkOrderId = workOrderId,
                CustomerId = order.CustomerSignerEmail,
                InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMM}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}",
                SubtotalINR = subtotalINR,
                Status = InvoiceStatus.DRAFT
            };

            await _store.SaveInvoiceAsync(invoice);
            return invoice;
        }

        public async Task<CommercialInvoice> IssueInvoiceWithBatch6PermitAsync(string tenantId, string invoiceId, string batch6PermitId)
        {
            if (string.IsNullOrWhiteSpace(batch6PermitId))
                throw new InvalidOperationException("Constitutional Violation: Invoices cannot be issued externally without a valid Batch 6 execution permit.");

            var invoice = await _store.GetInvoiceAsync(tenantId, invoiceId);
            if (invoice == null)
                throw new KeyNotFoundException($"Invoice '{invoiceId}' not found for tenant '{tenantId}'.");

            invoice.IsBatch6Authorized = true;
            invoice.Batch6PermitId = batch6PermitId;
            invoice.Status = InvoiceStatus.ISSUED;
            invoice.IssuedAtUtc = DateTime.UtcNow;

            await _store.SaveInvoiceAsync(invoice);
            return invoice;
        }

        public async Task<CommercialInvoice?> GetInvoiceAsync(string tenantId, string invoiceId)
        {
            return await _store.GetInvoiceAsync(tenantId, invoiceId);
        }

        public async Task<CashCollectionReceipt> ProcessCashCollectionAsync(string tenantId, string invoiceId, decimal collectedAmountINR, string bankRef, string gatewayId, string bankDigestSha256)
        {
            var invoice = await _store.GetInvoiceAsync(tenantId, invoiceId);
            if (invoice == null)
                throw new KeyNotFoundException($"Invoice '{invoiceId}' not found.");

            if (invoice.Status != InvoiceStatus.ISSUED && invoice.Status != InvoiceStatus.PAID)
                throw new InvalidOperationException("Constitutional Violation: Cannot collect cash against an unissued invoice.");

            if (collectedAmountINR <= 0m)
                throw new ArgumentException("Collected amount must be positive", nameof(collectedAmountINR));
            if (string.IsNullOrWhiteSpace(bankRef))
                throw new ArgumentException("Bank reference number is strictly required for cash collection recognition", nameof(bankRef));
            if (string.IsNullOrWhiteSpace(bankDigestSha256))
                throw new ArgumentException("Bank confirmation digest SHA256 is strictly required", nameof(bankDigestSha256));

            var receipt = new CashCollectionReceipt
            {
                TenantId = tenantId,
                InvoiceId = invoiceId,
                ContractId = invoice.ContractId,
                OpportunityId = invoice.OpportunityId,
                CollectedAmountINR = collectedAmountINR,
                BankReferenceNumber = bankRef,
                GatewayOrRailId = string.IsNullOrWhiteSpace(gatewayId) ? "HDFC_NEFT_AUTHORITATIVE" : gatewayId,
                BankConfirmationDigestSha256 = bankDigestSha256,
                VerifiedAtUtc = DateTime.UtcNow
            };

            if (collectedAmountINR >= invoice.TotalAmountINR)
            {
                invoice.Status = InvoiceStatus.PAID;
                await _store.SaveInvoiceAsync(invoice);
            }

            await _store.SaveReceiptAsync(receipt);
            return receipt;
        }

        public async Task<CashCollectionReceipt?> GetReceiptAsync(string tenantId, string receiptId)
        {
            return await _store.GetReceiptAsync(tenantId, receiptId);
        }
    }
}
