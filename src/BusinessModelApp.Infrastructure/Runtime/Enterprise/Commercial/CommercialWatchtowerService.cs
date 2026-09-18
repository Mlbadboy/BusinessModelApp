using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Commercial;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Commercial;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Commercial
{
    public class CommercialIdempotencyService : ICommercialIdempotencyService
    {
        private readonly ConcurrentDictionary<string, CommercialIdempotencyRecord> _records = new();

        public Task<bool> TryAcquireIdempotencyKeyAsync(string tenantId, string idempotencyKey, string actionType)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey))
                throw new ArgumentException("Idempotency key cannot be empty", nameof(idempotencyKey));

            string compoundKey = $"{tenantId}:{actionType}:{idempotencyKey}";
            bool acquired = !_records.ContainsKey(compoundKey);
            return Task.FromResult(acquired);
        }

        public Task CommitIdempotencyRecordAsync(CommercialIdempotencyRecord record)
        {
            if (string.IsNullOrWhiteSpace(record.IdempotencyKey))
                throw new ArgumentException("Idempotency key required", nameof(record));

            string compoundKey = $"{record.TenantId}:{record.ActionType}:{record.IdempotencyKey}";
            _records[compoundKey] = record;
            return Task.CompletedTask;
        }

        public Task<CommercialIdempotencyRecord?> GetRecordAsync(string tenantId, string idempotencyKey)
        {
            var match = _records.Values.FirstOrDefault(r => r.TenantId == tenantId && r.IdempotencyKey == idempotencyKey);
            return Task.FromResult(match);
        }
    }

    public class CommercialWatchtowerService : ICommercialWatchtowerService
    {
        private readonly IDeliveryAndInvoiceStore _deliveryStore;
        private readonly IProposalAndDealStore _dealStore;
        private readonly IOpportunityDiscoveryStore _oppStore;

        public CommercialWatchtowerService(
            IDeliveryAndInvoiceStore deliveryStore,
            IProposalAndDealStore dealStore,
            IOpportunityDiscoveryStore oppStore)
        {
            _deliveryStore = deliveryStore ?? throw new ArgumentNullException(nameof(deliveryStore));
            _dealStore = dealStore ?? throw new ArgumentNullException(nameof(dealStore));
            _oppStore = oppStore ?? throw new ArgumentNullException(nameof(oppStore));
        }

        public async Task<RevenueLineageAuditReport> TraceRevenueLineageAsync(string tenantId, string receiptId)
        {
            var report = new RevenueLineageAuditReport
            {
                TenantId = tenantId,
                ReceiptId = receiptId
            };

            // 1. Receipt / Cash Collected
            var receipt = await _deliveryStore.GetReceiptAsync(tenantId, receiptId);
            if (receipt == null)
            {
                report.Defects.Add($"Receipt '{receiptId}' not found.");
                return report;
            }

            report.RealizedAmountINR = receipt.CollectedAmountINR;
            report.TraceNodes.Add(new RevenueLineageNode
            {
                Stage = "CASH_COLLECTED",
                EntityId = receipt.ReceiptId,
                EvidenceDigestSha256 = receipt.BankConfirmationDigestSha256,
                RecordedAtUtc = receipt.VerifiedAtUtc,
                IsVerified = receipt.IsBankVerified
            });
            if (!receipt.IsBankVerified)
            {
                report.Defects.Add("Cash collection is missing authoritative bank verification or reference number.");
            }

            // 2. Commercial Invoice
            var invoice = await _deliveryStore.GetInvoiceAsync(tenantId, receipt.InvoiceId);
            if (invoice == null)
            {
                report.Defects.Add($"Invoice '{receipt.InvoiceId}' referenced by receipt was not found.");
                return report;
            }

            report.TraceNodes.Add(new RevenueLineageNode
            {
                Stage = "INVOICED",
                EntityId = invoice.InvoiceId,
                EvidenceDigestSha256 = invoice.Batch6PermitId,
                RecordedAtUtc = invoice.IssuedAtUtc ?? invoice.CreatedAtUtc,
                IsVerified = invoice.IsBatch6Authorized && invoice.Status == InvoiceStatus.PAID
            });
            if (!invoice.IsBatch6Authorized)
            {
                report.Defects.Add("Invoice lacks mandatory Batch 6 execution firewall authorization.");
            }

            // 3. Customer Delivery Acceptance / Work Order
            var workOrder = await _deliveryStore.GetWorkOrderAsync(tenantId, invoice.WorkOrderId);
            if (workOrder == null)
            {
                report.Defects.Add($"WorkOrder '{invoice.WorkOrderId}' not found.");
                return report;
            }

            report.TraceNodes.Add(new RevenueLineageNode
            {
                Stage = "DELIVERY_ACCEPTED",
                EntityId = workOrder.WorkOrderId,
                EvidenceDigestSha256 = workOrder.CustomerSignoffDigestSha256,
                RecordedAtUtc = workOrder.CustomerSignoffRecordedAtUtc ?? workOrder.CreatedAtUtc,
                IsVerified = workOrder.IsCustomerAccepted
            });
            if (!workOrder.IsCustomerAccepted)
            {
                report.Defects.Add("WorkOrder lacks authoritative customer delivery signoff.");
            }

            // 4. Authoritative Contract
            var contract = await _dealStore.GetContractAsync(tenantId, workOrder.ContractId);
            if (contract == null)
            {
                report.Defects.Add($"Contract '{workOrder.ContractId}' not found.");
                return report;
            }

            report.TraceNodes.Add(new RevenueLineageNode
            {
                Stage = "CONTRACT_VERIFIED",
                EntityId = contract.ContractId,
                EvidenceDigestSha256 = contract.SignatureDigestSha256,
                RecordedAtUtc = contract.ExecutedAtUtc,
                IsVerified = contract.IsCryptographicallyVerified
            });
            if (!contract.IsCryptographicallyVerified)
            {
                report.Defects.Add("Contract lacks cryptographic e-signature digest.");
            }

            // 5. Commercial Proposal
            var proposal = await _dealStore.GetProposalAsync(tenantId, contract.ProposalId);
            if (proposal == null)
            {
                report.Defects.Add($"Proposal '{contract.ProposalId}' not found.");
                return report;
            }

            report.TraceNodes.Add(new RevenueLineageNode
            {
                Stage = "PROPOSAL_APPROVED",
                EntityId = proposal.ProposalId,
                EvidenceDigestSha256 = proposal.ProposalId,
                RecordedAtUtc = proposal.CreatedAtUtc,
                IsVerified = proposal.IsApprovedForSubmission && proposal.IsMarginCompliant
            });
            if (!proposal.IsApprovedForSubmission || !proposal.IsMarginCompliant)
            {
                report.Defects.Add("Proposal failed margin threshold (>=35%) or lacked human PRG-1 signoff.");
            }

            // 6. Grounded Opportunity
            var opp = await _oppStore.GetOpportunityAsync(tenantId, proposal.OpportunityId);
            if (opp == null)
            {
                report.Defects.Add($"Opportunity '{proposal.OpportunityId}' not found.");
                return report;
            }

            report.TraceNodes.Add(new RevenueLineageNode
            {
                Stage = "OPPORTUNITY_GROUNDED",
                EntityId = opp.OpportunityId,
                EvidenceDigestSha256 = opp.OpportunityId,
                RecordedAtUtc = opp.DiscoveredAtUtc,
                IsVerified = opp.IsGrounded
            });
            if (!opp.IsGrounded)
            {
                report.Defects.Add("Opportunity lacks non-empty factual evidence groundings.");
            }

            report.IsLineageUnbroken = report.Defects.Count == 0 && report.TraceNodes.All(n => n.IsVerified);
            return report;
        }

        public async Task<CommercialPipelineMetrics> CalculatePipelineMetricsAsync(string tenantId)
        {
            var opps = await _oppStore.ListOpportunitiesAsync(tenantId);
            var proposals = await _dealStore.ListProposalsAsync(tenantId);
            var contracts = await _dealStore.ListContractsAsync(tenantId);
            var invoices = await _deliveryStore.ListInvoicesAsync(tenantId);
            var receipts = await _deliveryStore.ListReceiptsAsync(tenantId);

            decimal totalPipeline = opps.Sum(o => o.EstimatedDealValueINR);
            decimal totalWon = contracts.Sum(c => c.BindingDealValueINR);
            decimal totalInvoiced = invoices.Sum(i => i.TotalAmountINR);
            decimal totalCash = receipts.Where(r => r.IsBankVerified).Sum(r => r.CollectedAmountINR);

            decimal avgMargin = proposals.Any() ? proposals.Average(p => p.ExpectedGrossMarginPercent) : 0m;
            int pendingInvoices = invoices.Count(i => i.Status == InvoiceStatus.ISSUED);

            var metrics = new CommercialPipelineMetrics
            {
                TenantId = tenantId,
                TotalOpportunities = opps.Count,
                TotalPipelineValueINR = totalPipeline,
                TotalWonDealsValueINR = totalWon,
                TotalInvoicedValueINR = totalInvoiced,
                TotalCollectedCashINR = totalCash,
                AverageGrossMarginPercent = Math.Round(avgMargin, 2),
                ActiveProposalsCount = proposals.Count,
                PendingInvoicesCount = pendingInvoices
            };

            return metrics;
        }

        public async Task<IReadOnlyList<GroundedOpportunity>> DetectStalledOpportunitiesAsync(string tenantId, TimeSpan stallThreshold)
        {
            var opps = await _oppStore.ListOpportunitiesAsync(tenantId);
            var cutoff = DateTime.UtcNow - stallThreshold;

            var stalled = opps.Where(o => o.DiscoveredAtUtc < cutoff && o.ICPScore < 0.7m).ToList();
            return stalled;
        }
    }
}
