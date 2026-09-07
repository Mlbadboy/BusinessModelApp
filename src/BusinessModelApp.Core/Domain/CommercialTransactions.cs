using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Commercial
{
    public enum ProposalQuoteStage
    {
        Draft,
        AwaitingExecutiveAuthorization,
        SentToClient,
        ClientApproved,
        PaymentRequested,
        PaidAndClosed,
        Rejected
    }

    public class CommercialProposalQuote
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid WorkspaceId { get; set; }
        public Guid? OpportunityHypothesisId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string ClientEmail { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ScopeOfWork { get; set; } = string.Empty;
        public List<string> DeliverableItems { get; set; } = new();
        public decimal TotalAmountINR { get; set; }
        public string Currency { get; set; } = "INR";
        public ProposalQuoteStage Stage { get; set; } = ProposalQuoteStage.Draft;
        public string PaymentProvider { get; set; } = "Razorpay"; // Razorpay | Stripe
        public string PaymentLinkId { get; set; } = string.Empty;
        public string PaymentUrl { get; set; } = string.Empty;
        public DateTime? PaymentRequestedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public string EvidenceKey { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum DeliveryPhase
    {
        RequirementsGathering,
        UXUIDesign,
        CoreEngineering,
        QAAndSecurityAudit,
        ProductionDeployment,
        CustomerHandoverAndSuccess,
        Completed
    }

    public class DeliveryTaskItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid DeliveryMissionId { get; set; }
        public string Role { get; set; } = string.Empty; // RequirementsAnalyst, UXDesigner, FrontendEngineer, etc.
        public string Title { get; set; } = string.Empty;
        public string ArtifactName { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class DeliveryMission
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid WorkspaceId { get; set; }
        public Guid CommercialProposalQuoteId { get; set; }
        public string ProjectTitle { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public decimal ProjectValueINR { get; set; }
        public DeliveryPhase CurrentPhase { get; set; } = DeliveryPhase.RequirementsGathering;
        public int OverallProgressPercentage { get; set; } = 0;
        public List<DeliveryTaskItem> Tasks { get; set; } = new();
        public string LiveDeploymentUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
    }
}
