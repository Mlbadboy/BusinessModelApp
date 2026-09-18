using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Finance;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Finance;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Finance
{
    public sealed class InMemoryFinancialRealityStore : IFinancialRealityStore
    {
        private readonly ConcurrentDictionary<string, CommercialInvoice> _invoices = new();
        private readonly ConcurrentBag<BankCashReceipt> _receipts = new();
        private readonly ConcurrentBag<DeliveryCostEntry> _costs = new();

        public Task SaveInvoiceAsync(CommercialInvoice invoice, CancellationToken cancellationToken = default)
        {
            _invoices[invoice.InvoiceId] = invoice;
            return Task.CompletedTask;
        }

        public Task<CommercialInvoice?> GetInvoiceAsync(string invoiceId, CancellationToken cancellationToken = default)
        {
            _invoices.TryGetValue(invoiceId, out var inv);
            return Task.FromResult(inv);
        }

        public Task<IReadOnlyList<CommercialInvoice>> ListInvoicesForTenantAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            var list = _invoices.Values.Where(i => i.TenantId == tenantId).ToList();
            return Task.FromResult<IReadOnlyList<CommercialInvoice>>(list);
        }

        public Task SaveCashReceiptAsync(BankCashReceipt receipt, CancellationToken cancellationToken = default)
        {
            _receipts.Add(receipt);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<BankCashReceipt>> ListReceiptsForTenantAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            var list = _receipts.Where(r => r.TenantId == tenantId).ToList();
            return Task.FromResult<IReadOnlyList<BankCashReceipt>>(list);
        }

        public Task SaveCostEntryAsync(DeliveryCostEntry cost, CancellationToken cancellationToken = default)
        {
            _costs.Add(cost);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<DeliveryCostEntry>> ListCostsForCustomerAsync(string customerId, CancellationToken cancellationToken = default)
        {
            var list = _costs.Where(c => c.CustomerId == customerId).ToList();
            return Task.FromResult<IReadOnlyList<DeliveryCostEntry>>(list);
        }
    }

    public sealed class FinancialRealityKernelService : IFinancialRealityKernelService
    {
        private readonly IFinancialRealityStore _store;

        public FinancialRealityKernelService(IFinancialRealityStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<CommercialInvoice> DraftInvoiceAsync(
            string tenantId,
            string customerId,
            string contractId,
            decimal subtotalINR,
            DateTime dueDateUtc,
            CancellationToken cancellationToken = default)
        {
            if (subtotalINR <= 0) throw new ArgumentOutOfRangeException(nameof(subtotalINR), "Invoice subtotal must be positive.");

            var invoice = new CommercialInvoice
            {
                TenantId = tenantId,
                CustomerId = customerId,
                ContractId = contractId,
                SubtotalINR = subtotalINR,
                DueDateUtc = dueDateUtc
            };

            await _store.SaveInvoiceAsync(invoice, cancellationToken);
            return invoice;
        }

        public async Task<CommercialInvoice> ApproveAndIssueInvoiceAsync(
            string invoiceId,
            string batch6PermitId,
            CancellationToken cancellationToken = default)
        {
            var invoice = await _store.GetInvoiceAsync(invoiceId, cancellationToken);
            if (invoice == null) throw new KeyNotFoundException($"Invoice '{invoiceId}' not found.");

            invoice.AuthorizeBatch6(batch6PermitId);
            invoice.MarkIssued();

            await _store.SaveInvoiceAsync(invoice, cancellationToken);
            return invoice;
        }

        public async Task<BankCashReceipt> ReconcileBankPaymentAsync(
            string invoiceId,
            decimal amountINR,
            string wireReference,
            string statementDigestSha256,
            CancellationToken cancellationToken = default)
        {
            var invoice = await _store.GetInvoiceAsync(invoiceId, cancellationToken);
            if (invoice == null) throw new KeyNotFoundException($"Invoice '{invoiceId}' not found.");

            if (invoice.Status != InvoiceStatus.Issued && invoice.Status != InvoiceStatus.Overdue)
            {
                throw new InvalidOperationException($"Cannot reconcile payment for invoice in status '{invoice.Status}'.");
            }

            var receipt = new BankCashReceipt
            {
                TenantId = invoice.TenantId,
                InvoiceId = invoice.InvoiceId,
                AmountINR = amountINR,
                BankWireReference = wireReference,
                BankStatementDigestSha256 = statementDigestSha256
            }.Validate();

            // Mark invoice paid once reconciled with bank evidence
            invoice.MarkPaid();

            await _store.SaveCashReceiptAsync(receipt, cancellationToken);
            await _store.SaveInvoiceAsync(invoice, cancellationToken);
            return receipt;
        }

        public async Task<DeliveryCostEntry> RecordDeliveryCostAsync(
            string tenantId,
            string customerId,
            DeliveryCostCategory category,
            decimal amountINR,
            string description,
            CancellationToken cancellationToken = default)
        {
            if (amountINR <= 0) throw new ArgumentOutOfRangeException(nameof(amountINR), "Cost amount must be positive.");

            var cost = new DeliveryCostEntry
            {
                TenantId = tenantId,
                CustomerId = customerId,
                Category = category,
                AmountINR = amountINR,
                Description = description
            };

            await _store.SaveCostEntryAsync(cost, cancellationToken);
            return cost;
        }

        public async Task<CustomerProfitabilityReport> GetCustomerProfitabilityAsync(
            string tenantId,
            string customerId,
            CancellationToken cancellationToken = default)
        {
            var invoices = await _store.ListInvoicesForTenantAsync(tenantId, cancellationToken);
            var custInvoices = invoices.Where(i => i.CustomerId == customerId).ToList();

            var totalInvoiced = custInvoices.Sum(i => i.TotalAmountINR);

            var receipts = await _store.ListReceiptsForTenantAsync(tenantId, cancellationToken);
            var paidInvoiceIds = custInvoices.Select(i => i.InvoiceId).ToHashSet();
            var totalCollected = receipts.Where(r => paidInvoiceIds.Contains(r.InvoiceId)).Sum(r => r.AmountINR);

            var costs = await _store.ListCostsForCustomerAsync(customerId, cancellationToken);
            var totalCosts = costs.Sum(c => c.AmountINR);

            return new CustomerProfitabilityReport
            {
                TenantId = tenantId,
                CustomerId = customerId,
                TotalInvoicedINR = totalInvoiced,
                TotalCashCollectedINR = totalCollected,
                TotalDeliveryCostINR = totalCosts
            };
        }

        public async Task<TreasuryCashPosition> GetTreasuryPositionAsync(
            string tenantId,
            decimal totalBankCashBalanceINR,
            decimal monthlyOutflowINR,
            CancellationToken cancellationToken = default)
        {
            var receipts = await _store.ListReceiptsForTenantAsync(tenantId, cancellationToken);
            var now = DateTime.UtcNow;
            var thirtyDaysAgo = now.AddDays(-30);
            var monthlyInflow = receipts.Where(r => r.SettledAtUtc >= thirtyDaysAgo).Sum(r => r.AmountINR);

            var invoices = await _store.ListInvoicesForTenantAsync(tenantId, cancellationToken);
            var outstanding = invoices.Where(i => i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.Overdue).ToList();

            decimal rec0_30 = 0m, rec31_60 = 0m, rec61_90 = 0m, rec90Plus = 0m;
            foreach (var inv in outstanding)
            {
                var daysPending = (now - (inv.IssuedAtUtc ?? inv.DueDateUtc)).TotalDays;
                if (daysPending <= 30) rec0_30 += inv.TotalAmountINR;
                else if (daysPending <= 60) rec31_60 += inv.TotalAmountINR;
                else if (daysPending <= 90) rec61_90 += inv.TotalAmountINR;
                else rec90Plus += inv.TotalAmountINR;
            }

            return new TreasuryCashPosition
            {
                TenantId = tenantId,
                TotalBankCashBalanceINR = totalBankCashBalanceINR,
                MonthlyCashInflowINR = monthlyInflow,
                MonthlyCashOutflowINR = monthlyOutflowINR,
                Receivables_0_30_Days = rec0_30,
                Receivables_31_60_Days = rec31_60,
                Receivables_61_90_Days = rec61_90,
                Receivables_90_Plus_Days = rec90Plus
            };
        }
    }
}
