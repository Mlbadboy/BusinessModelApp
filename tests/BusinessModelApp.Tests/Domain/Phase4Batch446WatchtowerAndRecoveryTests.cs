using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Commercial;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Commercial;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public class Phase4Batch446WatchtowerAndRecoveryTests
    {
        private readonly InMemoryDeliveryAndInvoiceStore _deliveryStore;
        private readonly InMemoryProposalAndDealStore _dealStore;
        private readonly InMemoryOpportunityDiscoveryStore _oppStore;
        private readonly CommercialWatchtowerService _watchtowerService;
        private readonly CommercialIdempotencyService _idempotencyService;

        public Phase4Batch446WatchtowerAndRecoveryTests()
        {
            _deliveryStore = new InMemoryDeliveryAndInvoiceStore();
            _dealStore = new InMemoryProposalAndDealStore();
            _oppStore = new InMemoryOpportunityDiscoveryStore();
            _watchtowerService = new CommercialWatchtowerService(_deliveryStore, _dealStore, _oppStore);
            _idempotencyService = new CommercialIdempotencyService();
        }

        [Fact]
        public async Task TraceRevenueLineage_CompleteValidCausalChain_ReturnsUnbrokenLineage()
        {
            const string tenantId = "tenant-watch-1";

            // 1. Grounded Opportunity
            var opp = new GroundedOpportunity
            {
                TenantId = tenantId,
                CompanyName = "HealthCare Plus",
                IdentifiedProblem = "Manual triage delays",
                EstimatedDealValueINR = 1500000m,
                ICPScore = 0.85m,
                CorroboratedEvidenceIds = new List<string> { "evidence-hospital-2025" }
            };
            await _oppStore.SaveOpportunityAsync(opp);

            // 2. Approved Proposal
            var proposal = new CommercialProposal
            {
                TenantId = tenantId,
                OpportunityId = opp.OpportunityId,
                Title = "Triage Automation Platform",
                BasePriceINR = 1500000m,
                EstimatedDeliveryCostINR = 600000m, // 60% margin >= 35%
                IsApprovedForSubmission = true
            };
            await _dealStore.SaveProposalAsync(proposal);

            // 3. Cryptographically Verified Contract
            var contract = new AuthoritativeDealContract
            {
                TenantId = tenantId,
                OpportunityId = opp.OpportunityId,
                ProposalId = proposal.ProposalId,
                CustomerSignerName = "Dr. Mehta, CIO",
                CustomerSignerEmail = "mehta@healthcare.org",
                SignatureDigestSha256 = "docusign-sha256-verified-digest",
                VerificationSourceSystem = "DocuSignEnterprise",
                BindingDealValueINR = 1500000m
            };
            await _dealStore.SaveContractAsync(contract);

            // 4. Customer Accepted Work Order
            var workOrder = new CommercialWorkOrder
            {
                TenantId = tenantId,
                OpportunityId = opp.OpportunityId,
                ContractId = contract.ContractId,
                Title = "Milestone 1 Implementation",
                Deliverables = new List<string> { "Model deployment", "EHR connector" },
                Status = WorkOrderStatus.ACCEPTED_BY_CUSTOMER,
                CustomerSignoffDigestSha256 = "customer-signoff-sha256-digest",
                CustomerSignerEmail = "mehta@healthcare.org",
                CustomerSignoffRecordedAtUtc = DateTime.UtcNow
            };
            await _deliveryStore.SaveWorkOrderAsync(workOrder);

            // 5. Batch 6 Authorized & Paid Invoice
            var invoice = new CommercialInvoice
            {
                TenantId = tenantId,
                OpportunityId = opp.OpportunityId,
                ContractId = contract.ContractId,
                WorkOrderId = workOrder.WorkOrderId,
                SubtotalINR = 1500000m,
                IsBatch6Authorized = true,
                Batch6PermitId = "batch6-permit-lineage-1",
                Status = InvoiceStatus.PAID,
                IssuedAtUtc = DateTime.UtcNow
            };
            await _deliveryStore.SaveInvoiceAsync(invoice);

            // 6. Bank-Verified Cash Collection Receipt
            var receipt = new CashCollectionReceipt
            {
                TenantId = tenantId,
                InvoiceId = invoice.InvoiceId,
                ContractId = contract.ContractId,
                OpportunityId = opp.OpportunityId,
                CollectedAmountINR = invoice.TotalAmountINR,
                BankReferenceNumber = "HDFC-RTGS-992019283",
                GatewayOrRailId = "HDFC_NETBANKING",
                BankConfirmationDigestSha256 = "hdfc-bank-crypto-receipt-hash-verified",
                VerifiedAtUtc = DateTime.UtcNow
            };
            await _deliveryStore.SaveReceiptAsync(receipt);

            // Trace backward lineage
            var audit = await _watchtowerService.TraceRevenueLineageAsync(tenantId, receipt.ReceiptId);

            Assert.True(audit.IsLineageUnbroken);
            Assert.Empty(audit.Defects);
            Assert.Equal(6, audit.TraceNodes.Count);
            Assert.All(audit.TraceNodes, node => Assert.True(node.IsVerified));
        }

        [Fact]
        public async Task TraceRevenueLineage_MissingCustomerSignoff_ReturnsDefectAndBrokenLineage()
        {
            const string tenantId = "tenant-watch-2";

            var opp = new GroundedOpportunity
            {
                TenantId = tenantId,
                CompanyName = "FinTech Global",
                CorroboratedEvidenceIds = new List<string> { "evidence-fintech-1" }
            };
            await _oppStore.SaveOpportunityAsync(opp);

            var proposal = new CommercialProposal
            {
                TenantId = tenantId,
                OpportunityId = opp.OpportunityId,
                BasePriceINR = 1000000m,
                EstimatedDeliveryCostINR = 400000m,
                IsApprovedForSubmission = true
            };
            await _dealStore.SaveProposalAsync(proposal);

            var contract = new AuthoritativeDealContract
            {
                TenantId = tenantId,
                OpportunityId = opp.OpportunityId,
                ProposalId = proposal.ProposalId,
                SignatureDigestSha256 = "sig-123",
                VerificationSourceSystem = "DocuSign",
                BindingDealValueINR = 1000000m
            };
            await _dealStore.SaveContractAsync(contract);

            // Work order was NOT accepted by customer!
            var workOrder = new CommercialWorkOrder
            {
                TenantId = tenantId,
                OpportunityId = opp.OpportunityId,
                ContractId = contract.ContractId,
                Status = WorkOrderStatus.IN_PROGRESS // Incomplete!
            };
            await _deliveryStore.SaveWorkOrderAsync(workOrder);

            var invoice = new CommercialInvoice
            {
                TenantId = tenantId,
                WorkOrderId = workOrder.WorkOrderId,
                IsBatch6Authorized = true,
                Status = InvoiceStatus.PAID
            };
            await _deliveryStore.SaveInvoiceAsync(invoice);

            var receipt = new CashCollectionReceipt
            {
                TenantId = tenantId,
                InvoiceId = invoice.InvoiceId,
                CollectedAmountINR = 1180000m,
                BankReferenceNumber = "BANK-REF-99",
                BankConfirmationDigestSha256 = "bank-digest-99"
            };
            await _deliveryStore.SaveReceiptAsync(receipt);

            var audit = await _watchtowerService.TraceRevenueLineageAsync(tenantId, receipt.ReceiptId);

            Assert.False(audit.IsLineageUnbroken);
            Assert.Contains(audit.Defects, d => d.Contains("lacks authoritative customer delivery signoff"));
        }

        [Fact]
        public async Task CalculatePipelineMetrics_AggregatesPipelineValueAndMarginsAccurately()
        {
            const string tenantId = "tenant-watch-3";

            await _oppStore.SaveOpportunityAsync(new GroundedOpportunity
            {
                TenantId = tenantId,
                EstimatedDealValueINR = 500000m
            });
            await _oppStore.SaveOpportunityAsync(new GroundedOpportunity
            {
                TenantId = tenantId,
                EstimatedDealValueINR = 750000m
            });

            await _dealStore.SaveProposalAsync(new CommercialProposal
            {
                TenantId = tenantId,
                BasePriceINR = 1000000m,
                EstimatedDeliveryCostINR = 400000m // 60% margin
            });

            await _dealStore.SaveContractAsync(new AuthoritativeDealContract
            {
                TenantId = tenantId,
                BindingDealValueINR = 1000000m
            });

            await _deliveryStore.SaveReceiptAsync(new CashCollectionReceipt
            {
                TenantId = tenantId,
                CollectedAmountINR = 590000m,
                BankReferenceNumber = "REF-100",
                BankConfirmationDigestSha256 = "CONF-100"
            });

            var metrics = await _watchtowerService.CalculatePipelineMetricsAsync(tenantId);

            Assert.Equal(2, metrics.TotalOpportunities);
            Assert.Equal(1250000m, metrics.TotalPipelineValueINR);
            Assert.Equal(1000000m, metrics.TotalWonDealsValueINR);
            Assert.Equal(590000m, metrics.TotalCollectedCashINR);
            Assert.Equal(60.0m, metrics.AverageGrossMarginPercent);
        }

        [Fact]
        public async Task Idempotency_PreventDuplicateActionExecution()
        {
            const string tenantId = "tenant-idemp-1";
            const string key = "idemp-inv-issue-001";
            const string action = "ISSUE_INVOICE";

            bool canRunFirstTime = await _idempotencyService.TryAcquireIdempotencyKeyAsync(tenantId, key, action);
            Assert.True(canRunFirstTime);

            await _idempotencyService.CommitIdempotencyRecordAsync(new CommercialIdempotencyRecord
            {
                TenantId = tenantId,
                IdempotencyKey = key,
                ActionType = action,
                ResourceId = "inv-101",
                ExecutionDigestSha256 = "execution-hash-ok"
            });

            bool canRunSecondTime = await _idempotencyService.TryAcquireIdempotencyKeyAsync(tenantId, key, action);
            Assert.False(canRunSecondTime); // Prevented duplicate execution!
        }
    }
}
