using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Finance;

namespace BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Finance
{
    public interface IFinancialRealityStore
    {
        Task SaveInvoiceAsync(CommercialInvoice invoice, CancellationToken cancellationToken = default);
        Task<CommercialInvoice?> GetInvoiceAsync(string invoiceId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CommercialInvoice>> ListInvoicesForTenantAsync(string tenantId, CancellationToken cancellationToken = default);

        Task SaveCashReceiptAsync(BankCashReceipt receipt, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<BankCashReceipt>> ListReceiptsForTenantAsync(string tenantId, CancellationToken cancellationToken = default);

        Task SaveCostEntryAsync(DeliveryCostEntry cost, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<DeliveryCostEntry>> ListCostsForCustomerAsync(string customerId, CancellationToken cancellationToken = default);
    }

    public interface IFinancialRealityKernelService
    {
        Task<CommercialInvoice> DraftInvoiceAsync(
            string tenantId,
            string customerId,
            string contractId,
            decimal subtotalINR,
            DateTime dueDateUtc,
            CancellationToken cancellationToken = default);

        Task<CommercialInvoice> ApproveAndIssueInvoiceAsync(
            string invoiceId,
            string batch6PermitId,
            CancellationToken cancellationToken = default);

        Task<BankCashReceipt> ReconcileBankPaymentAsync(
            string invoiceId,
            decimal amountINR,
            string wireReference,
            string statementDigestSha256,
            CancellationToken cancellationToken = default);

        Task<DeliveryCostEntry> RecordDeliveryCostAsync(
            string tenantId,
            string customerId,
            DeliveryCostCategory category,
            decimal amountINR,
            string description,
            CancellationToken cancellationToken = default);

        Task<CustomerProfitabilityReport> GetCustomerProfitabilityAsync(
            string tenantId,
            string customerId,
            CancellationToken cancellationToken = default);

        Task<TreasuryCashPosition> GetTreasuryPositionAsync(
            string tenantId,
            decimal totalBankCashBalanceINR,
            decimal monthlyOutflowINR,
            CancellationToken cancellationToken = default);
    }
}
