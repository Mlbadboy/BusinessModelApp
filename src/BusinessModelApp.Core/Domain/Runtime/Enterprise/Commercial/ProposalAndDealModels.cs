using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Commercial
{
    public class CommercialProposal
    {
        public string ProposalId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string OpportunityId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ScopeSummary { get; set; } = string.Empty;
        public List<string> Deliverables { get; set; } = new();
        public int EstimatedTimelineWeeks { get; set; } = 4;
        public decimal BasePriceINR { get; set; } = 0m;
        public decimal DiscountPercent { get; set; } = 0m;
        public decimal NetPriceINR => BasePriceINR * (1.0m - (DiscountPercent / 100m));
        public decimal EstimatedDeliveryCostINR { get; set; } = 0m;
        public decimal ExpectedGrossMarginPercent => NetPriceINR > 0 ? ((NetPriceINR - EstimatedDeliveryCostINR) / NetPriceINR) * 100m : 0m;
        public bool IsMarginCompliant => ExpectedGrossMarginPercent >= 35.0m; // Minimum gross margin invariant
        public bool IsApprovedForSubmission { get; set; } = false;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public class NegotiationAnalysis
    {
        public string NegotiationId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string ProposalId { get; set; } = string.Empty;
        public decimal CurrentProposalPriceINR { get; set; } = 0m;
        public decimal CustomerOfferedPriceINR { get; set; } = 0m;
        public decimal RecommendedCounterOfferINR { get; set; } = 0m;
        public decimal FloorPriceINR { get; set; } = 0m; // Absolute minimum acceptable position
        public string ConcessionStrategy { get; set; } = string.Empty;
        public bool HasExecutiveAuthorityToCommit => false; // Law I40: Negotiation Intelligence != Negotiation Authority
        public DateTime EvaluatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public class AuthoritativeDealContract
    {
        public string ContractId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string OpportunityId { get; set; } = string.Empty;
        public string ProposalId { get; set; } = string.Empty;
        public string CustomerSignerName { get; set; } = string.Empty;
        public string CustomerSignerEmail { get; set; } = string.Empty;
        public string SignatureDigestSha256 { get; set; } = string.Empty;
        public string VerificationSourceSystem { get; set; } = string.Empty; // e.g. "DocuSignConnector", "AdobeSign", "CounterSignedPdf"
        public decimal BindingDealValueINR { get; set; } = 0m;
        public bool IsCryptographicallyVerified => !string.IsNullOrWhiteSpace(SignatureDigestSha256) && !string.IsNullOrWhiteSpace(VerificationSourceSystem);
        public DateTime ExecutedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
