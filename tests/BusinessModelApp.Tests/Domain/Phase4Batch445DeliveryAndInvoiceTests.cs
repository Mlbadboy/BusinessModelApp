using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Commercial;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Commercial;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public class Phase4Batch445DeliveryAndInvoiceTests
    {
        private readonly InMemoryDeliveryAndInvoiceStore _store;
        private readonly DeliveryAndInvoiceService _service;

        public Phase4Batch445DeliveryAndInvoiceTests()
        {
            _store = new InMemoryDeliveryAndInvoiceStore();
            _service = new DeliveryAndInvoiceService(_store);
        }

        [Fact]
        public async Task CreateWorkOrder_Valid_InitializesScheduledStatus()
        {
            var workOrder = new CommercialWorkOrder
            {
                TenantId = "tenant-deliv-1",
                OpportunityId = "opp-deliv-1",
                ContractId = "contract-deliv-1",
                Title = "Deploy Custom CRM Connector & Lead Router",
                Deliverables = new List<string> { "API Integration", "Database Migration", "Production Smoke Test Run" }
            };

            var created = await _service.CreateWorkOrderAsync(workOrder);

            Assert.Equal(WorkOrderStatus.SCHEDULED, created.Status);
            Assert.False(created.IsCustomerAccepted);
        }

        [Fact]
        public async Task RecordDeliveryAcceptance_MissingDigest_ThrowsArgumentException()
        {
            var workOrder = new CommercialWorkOrder
            {
                TenantId = "tenant-deliv-1",
                OpportunityId = "opp-deliv-2",
                ContractId = "contract-deliv-2",
                Title = "Autonomous Marketing Bot Delivery",
                Deliverables = new List<string> { "Prompt Engineering Package" }
            };
            await _service.CreateWorkOrderAsync(workOrder);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.RecordCustomerDeliveryAcceptanceAsync("tenant-deliv-1", workOrder.WorkOrderId, "vp@client.com", ""));
        }

        [Fact]
        public async Task GenerateInvoice_BeforeCustomerAcceptance_ThrowsInvalidOperationException()
        {
            var workOrder = new CommercialWorkOrder
            {
                TenantId = "tenant-deliv-1",
                OpportunityId = "opp-deliv-3",
                ContractId = "contract-deliv-3",
                Title = "ERP Sync Engine",
                Deliverables = new List<string> { "ERP Bridge" }
            };
            await _service.CreateWorkOrderAsync(workOrder);

            // Work order is still in SCHEDULED status; not accepted
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.GenerateInvoiceAsync("tenant-deliv-1", "contract-deliv-3", workOrder.WorkOrderId, 500000m));

            Assert.Contains("requires cryptographically verified customer delivery acceptance", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GenerateInvoice_AfterCustomerAcceptance_CalculatesGstTaxCorrectly()
        {
            var workOrder = new CommercialWorkOrder
            {
                TenantId = "tenant-deliv-1",
                OpportunityId = "opp-deliv-4",
                ContractId = "contract-deliv-4",
                Title = "Autonomous Operations Setup",
                Deliverables = new List<string> { "Full Platform Deployment" }
            };
            await _service.CreateWorkOrderAsync(workOrder);

            await _service.RecordCustomerDeliveryAcceptanceAsync(
                "tenant-deliv-1",
                workOrder.WorkOrderId,
                "head.eng@client.com",
                "c3ab8ff13720e8ad9047dd39466b3c8974e592c2fa383d4a3960714caef0c4f2");

            var invoice = await _service.GenerateInvoiceAsync("tenant-deliv-1", "contract-deliv-4", workOrder.WorkOrderId, 1000000m);

            Assert.Equal(InvoiceStatus.DRAFT, invoice.Status);
            Assert.Equal(1000000m, invoice.SubtotalINR);
            Assert.Equal(180000m, invoice.TaxAmountINR); // 18% GST
            Assert.Equal(1180000m, invoice.TotalAmountINR);
        }

        [Fact]
        public async Task IssueInvoice_WithoutBatch6Permit_ThrowsInvalidOperationException()
        {
            var workOrder = new CommercialWorkOrder
            {
                TenantId = "tenant-deliv-1",
                OpportunityId = "opp-deliv-5",
                ContractId = "contract-deliv-5",
                Title = "Batch 6 Gate Check Work Order",
                Deliverables = new List<string> { "Module Alpha" }
            };
            await _service.CreateWorkOrderAsync(workOrder);
            await _service.RecordCustomerDeliveryAcceptanceAsync("tenant-deliv-1", workOrder.WorkOrderId, "buyer@corp.com", "digest-5");

            var invoice = await _service.GenerateInvoiceAsync("tenant-deliv-1", "contract-deliv-5", workOrder.WorkOrderId, 200000m);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.IssueInvoiceWithBatch6PermitAsync("tenant-deliv-1", invoice.InvoiceId, ""));

            Assert.Contains("Batch 6 execution permit", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task IssueInvoice_WithBatch6Permit_MarksIssued()
        {
            var workOrder = new CommercialWorkOrder
            {
                TenantId = "tenant-deliv-1",
                OpportunityId = "opp-deliv-6",
                ContractId = "contract-deliv-6",
                Title = "Delivery with Batch 6 Permit",
                Deliverables = new List<string> { "Module Beta" }
            };
            await _service.CreateWorkOrderAsync(workOrder);
            await _service.RecordCustomerDeliveryAcceptanceAsync("tenant-deliv-1", workOrder.WorkOrderId, "buyer@corp.com", "digest-6");

            var invoice = await _service.GenerateInvoiceAsync("tenant-deliv-1", "contract-deliv-6", workOrder.WorkOrderId, 300000m);
            var issued = await _service.IssueInvoiceWithBatch6PermitAsync("tenant-deliv-1", invoice.InvoiceId, "batch6-permit-invoicing-8891");

            Assert.Equal(InvoiceStatus.ISSUED, issued.Status);
            Assert.True(issued.IsBatch6Authorized);
            Assert.Equal("batch6-permit-invoicing-8891", issued.Batch6PermitId);
            Assert.NotNull(issued.IssuedAtUtc);
        }

        [Fact]
        public async Task ProcessCashCollection_WithoutBankRef_ThrowsArgumentException()
        {
            var workOrder = new CommercialWorkOrder
            {
                TenantId = "tenant-deliv-1",
                OpportunityId = "opp-deliv-7",
                ContractId = "contract-deliv-7",
                Title = "Cash Collection Invariant Check",
                Deliverables = new List<string> { "Module Gamma" }
            };
            await _service.CreateWorkOrderAsync(workOrder);
            await _service.RecordCustomerDeliveryAcceptanceAsync("tenant-deliv-1", workOrder.WorkOrderId, "buyer@corp.com", "digest-7");

            var invoice = await _service.GenerateInvoiceAsync("tenant-deliv-1", "contract-deliv-7", workOrder.WorkOrderId, 400000m);
            await _service.IssueInvoiceWithBatch6PermitAsync("tenant-deliv-1", invoice.InvoiceId, "permit-7");

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ProcessCashCollectionAsync("tenant-deliv-1", invoice.InvoiceId, invoice.TotalAmountINR, "", "HDFC", "digest-hash"));
        }

        [Fact]
        public async Task ProcessCashCollection_WithValidBankReceipt_MarksInvoicePaidAndVerifiesReceipt()
        {
            var workOrder = new CommercialWorkOrder
            {
                TenantId = "tenant-deliv-1",
                OpportunityId = "opp-deliv-8",
                ContractId = "contract-deliv-8",
                Title = "Full Payment Realization Workflow",
                Deliverables = new List<string> { "End-to-end Enterprise AI" }
            };
            await _service.CreateWorkOrderAsync(workOrder);
            await _service.RecordCustomerDeliveryAcceptanceAsync("tenant-deliv-1", workOrder.WorkOrderId, "cfo@enterprise.com", "customer-sig-digest-8");

            var invoice = await _service.GenerateInvoiceAsync("tenant-deliv-1", "contract-deliv-8", workOrder.WorkOrderId, 1000000m);
            await _service.IssueInvoiceWithBatch6PermitAsync("tenant-deliv-1", invoice.InvoiceId, "permit-batch6-8");

            var receipt = await _service.ProcessCashCollectionAsync(
                "tenant-deliv-1",
                invoice.InvoiceId,
                invoice.TotalAmountINR,
                "HDFCN2625091882190",
                "HDFC_CORPORATE_RTGS",
                "bank-confirmation-sha256-hash-validated-29831");

            Assert.True(receipt.IsBankVerified);
            Assert.Equal(1180000m, receipt.CollectedAmountINR);
            Assert.Equal("HDFCN2625091882190", receipt.BankReferenceNumber);

            var updatedInvoice = await _service.GetInvoiceAsync("tenant-deliv-1", invoice.InvoiceId);
            Assert.NotNull(updatedInvoice);
            Assert.Equal(InvoiceStatus.PAID, updatedInvoice.Status);
        }
    }
}
