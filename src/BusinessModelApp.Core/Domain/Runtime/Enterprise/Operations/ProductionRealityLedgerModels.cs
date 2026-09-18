using System;
using System.Security.Cryptography;
using System.Text;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Operations
{
    public enum ProductionRealityEventType
    {
        MarketSignalObserved = 0,
        OutreachDelivered = 1,
        ProspectReplied = 2,
        MeetingHeld = 3,
        DealSigned = 4,
        OnboardingMilestoneCompleted = 5,
        ValueRealized = 6,
        InvoiceIssued = 7,
        PaymentSettled = 8,
        ChurnRecorded = 9,
        ExpansionExecuted = 10,
        ExpenseIncurred = 11
    }

    public sealed class ProductionRealityEvent
    {
        public string EventId { get; init; } = Guid.NewGuid().ToString("N");
        public required string TenantId { get; init; }
        public required string BusinessObjectiveId { get; init; }
        public string GrowthObjectiveId { get; init; } = string.Empty;
        public string BusinessObjectId { get; init; } = string.Empty; // ProspectId, ContractId, InvoiceId
        public ProductionRealityEventType EventType { get; init; }
        public EpistemicEvidenceLevel EvidenceLevel { get; init; } = EpistemicEvidenceLevel.ConnectorObserved;

        // Provider & Connector Metadata
        public string Source { get; init; } = string.Empty; // "CRM", "BankingAPI", "EmailGateway", "DocuSign"
        public string ConnectorName { get; init; } = string.Empty;
        public string ExternalProvider { get; init; } = string.Empty; // "Stripe", "Fedwire", "Salesforce", "GoogleWorkspace"
        public string ExternalReferenceId { get; init; } = string.Empty; // Remote Transaction / Document ID
        public DateTime ObservedAtUtc { get; init; } = DateTime.UtcNow;
        public string EvidenceDigestSha256 { get; init; } = string.Empty;
        public ExternalReconciliationStatus VerificationStatus { get; private set; } = ExternalReconciliationStatus.PendingVerification;
        public string CounterpartyReference { get; init; } = string.Empty;

        // Financial & Economic Impact
        public decimal FinancialImpactINR { get; init; }
        public decimal RevenueImpactINR { get; init; }
        public decimal CostImpactINR { get; init; }
        public decimal NetContributionImpactINR => RevenueImpactINR - CostImpactINR;

        // Governance & Human Verification
        public string? HumanAttestationId { get; private set; }
        public string LineageDigest { get; init; } = string.Empty;
        public string AuditNotes { get; private set; } = string.Empty;

        public bool IsAuthoritative =>
            EvidenceLevel >= EpistemicEvidenceLevel.BankVerifiedCash &&
            VerificationStatus == ExternalReconciliationStatus.Reconciled &&
            !string.IsNullOrWhiteSpace(EvidenceDigestSha256);

        public void Reconcile(string authority, ExternalReconciliationStatus status, string notes = "")
        {
            if (string.IsNullOrWhiteSpace(authority)) throw new ArgumentException("Reconciliation authority required.", nameof(authority));
            VerificationStatus = status;
            HumanAttestationId = authority.Trim();
            AuditNotes = notes;
        }

        public static string ComputeDigest(string rawPayload)
        {
            if (string.IsNullOrEmpty(rawPayload)) return string.Empty;
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawPayload));
            return Convert.ToHexString(bytes);
        }
    }
}
