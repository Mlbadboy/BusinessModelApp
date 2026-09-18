using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth
{
    /// <summary>
    /// Role type within a commercial buying committee.
    /// </summary>
    public enum BuyingRoleType
    {
        EconomicBuyer,
        TechnicalEvaluator,
        ExecutiveSponsor,
        Champion,
        EndUser,
        LegalGatekeeper,
        Procurement
    }

    /// <summary>
    /// Classification of prospect response to governed outreach.
    /// </summary>
    public enum OutreachReplyClassification
    {
        None,
        Interested,
        RequestDemo,
        Objection,
        Referral,
        Unsubscribe,
        DoNotContact
    }

    /// <summary>
    /// Meeting outcome state in qualification pipeline.
    /// </summary>
    public enum MeetingOutcomeState
    {
        Scheduled,
        CompletedAdvanceToProposal,
        CompletedFurtherDiscovery,
        Rescheduled,
        NoShow,
        Disqualified
    }

    /// <summary>
    /// Churn risk level for customer health monitoring.
    /// </summary>
    public enum ChurnRiskLevel
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Member of a prospective account buying committee.
    /// </summary>
    public sealed record BuyingCommitteeMember
    {
        public string MemberId { get; init; } = Guid.NewGuid().ToString("N");
        public string Name { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public BuyingRoleType RoleType { get; init; } = BuyingRoleType.Champion;
        public string InfluenceLevel { get; init; } = "Medium";
        public string ContactEmail { get; init; } = string.Empty;
        public string LinkedInProfileUrl { get; init; } = string.Empty;
    }

    /// <summary>
    /// Account prospect discovered and qualified against ICP criteria.
    /// </summary>
    public sealed class AccountProspect
    {
        public string ProspectId { get; }
        public string CompanyName { get; }
        public string Industry { get; }
        public int EstimatedEmployeeCount { get; }
        public decimal EstimatedAnnualRevenue { get; }
        public decimal IcpScore { get; } // 0.0m to 1.0m
        public string ICPQualificationSummary { get; }
        public DateTime DiscoveredAtUtc { get; }
        public List<BuyingCommitteeMember> BuyingCommittee { get; } = new();

        public AccountProspect(
            string companyName,
            string industry,
            int estimatedEmployeeCount,
            decimal estimatedAnnualRevenue,
            decimal icpScore,
            string icpQualificationSummary,
            string? prospectId = null,
            DateTime? discoveredAtUtc = null)
        {
            if (string.IsNullOrWhiteSpace(companyName))
                throw new ArgumentException("Company name is required.", nameof(companyName));
            if (icpScore < 0.0m || icpScore > 1.0m)
                throw new ArgumentException("ICP score must be between 0.0 and 1.0.", nameof(icpScore));

            ProspectId = prospectId ?? Guid.NewGuid().ToString("N");
            CompanyName = companyName.Trim();
            Industry = industry?.Trim() ?? "Unknown";
            EstimatedEmployeeCount = estimatedEmployeeCount;
            EstimatedAnnualRevenue = estimatedAnnualRevenue;
            IcpScore = icpScore;
            ICPQualificationSummary = icpQualificationSummary ?? string.Empty;
            DiscoveredAtUtc = discoveredAtUtc ?? DateTime.UtcNow;
        }

        public void AddCommitteeMember(BuyingCommitteeMember member)
        {
            if (member == null) throw new ArgumentNullException(nameof(member));
            BuyingCommittee.Add(member);
        }
    }

    /// <summary>
    /// Governed outbound outreach message record compliant with anti-spam and PRG governance.
    /// </summary>
    public sealed class GovernedOutboundEngagement
    {
        public string EngagementId { get; }
        public string ProspectId { get; }
        public string RecipientEmail { get; }
        public string Channel { get; }
        public string SubjectLine { get; }
        public string MessageBody { get; }
        public bool AntiSpamComplianceVerified { get; }
        public string GovernedSignoffDigest { get; }
        public DateTime SentAtUtc { get; }
        public DateTime? RepliedAtUtc { get; private set; }
        public OutreachReplyClassification ReplyClassification { get; private set; } = OutreachReplyClassification.None;
        public decimal SentimentScore { get; private set; } // -1.0 to 1.0

        public GovernedOutboundEngagement(
            string prospectId,
            string recipientEmail,
            string channel,
            string subjectLine,
            string messageBody,
            bool antiSpamComplianceVerified,
            string governedSignoffDigest,
            string? engagementId = null,
            DateTime? sentAtUtc = null)
        {
            if (string.IsNullOrWhiteSpace(prospectId))
                throw new ArgumentException("Prospect ID is required.", nameof(prospectId));
            if (string.IsNullOrWhiteSpace(recipientEmail))
                throw new ArgumentException("Recipient email is required.", nameof(recipientEmail));
            if (!antiSpamComplianceVerified)
                throw new InvalidOperationException("Cannot record outbound engagement without anti-spam compliance verification.");
            if (string.IsNullOrWhiteSpace(governedSignoffDigest))
                throw new InvalidOperationException("Cannot record outbound engagement without governed signoff digest.");

            EngagementId = engagementId ?? Guid.NewGuid().ToString("N");
            ProspectId = prospectId;
            RecipientEmail = recipientEmail.Trim();
            Channel = channel ?? "Email";
            SubjectLine = subjectLine?.Trim() ?? string.Empty;
            MessageBody = messageBody?.Trim() ?? string.Empty;
            AntiSpamComplianceVerified = antiSpamComplianceVerified;
            GovernedSignoffDigest = governedSignoffDigest;
            SentAtUtc = sentAtUtc ?? DateTime.UtcNow;
        }

        public void RecordReply(OutreachReplyClassification classification, decimal sentimentScore, DateTime? repliedAtUtc = null)
        {
            ReplyClassification = classification;
            SentimentScore = Math.Clamp(sentimentScore, -1.0m, 1.0m);
            RepliedAtUtc = repliedAtUtc ?? DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Commercial meeting pipeline item for discovery, qualification, or demo.
    /// </summary>
    public sealed class CommercialMeetingRecord
    {
        public string MeetingId { get; }
        public string ProspectId { get; }
        public string Title { get; }
        public DateTime ScheduledAtUtc { get; }
        public DateTime? CompletedAtUtc { get; private set; }
        public string Agenda { get; }
        public MeetingOutcomeState Outcome { get; private set; } = MeetingOutcomeState.Scheduled;
        public string DiscoveryNotes { get; private set; } = string.Empty;
        public bool MeddpicQualified { get; private set; }

        public CommercialMeetingRecord(
            string prospectId,
            string title,
            DateTime scheduledAtUtc,
            string agenda,
            string? meetingId = null)
        {
            if (string.IsNullOrWhiteSpace(prospectId))
                throw new ArgumentException("Prospect ID is required.", nameof(prospectId));
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Meeting title is required.", nameof(title));

            MeetingId = meetingId ?? Guid.NewGuid().ToString("N");
            ProspectId = prospectId;
            Title = title.Trim();
            ScheduledAtUtc = scheduledAtUtc;
            Agenda = agenda ?? string.Empty;
        }

        public void ConcludeMeeting(MeetingOutcomeState outcome, string discoveryNotes, bool meddpicQualified, DateTime? completedAtUtc = null)
        {
            Outcome = outcome;
            DiscoveryNotes = discoveryNotes ?? string.Empty;
            MeddpicQualified = meddpicQualified;
            CompletedAtUtc = completedAtUtc ?? DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Commercial proposal with solution scope, pricing, and guaranteed margin floor.
    /// </summary>
    public sealed class CommercialProposalRecord
    {
        public string ProposalId { get; }
        public string ProspectId { get; }
        public string SolutionTitle { get; }
        public string ScopeSummary { get; }
        public decimal ProposedPrice { get; }
        public decimal EstimatedCostBasis { get; }
        public decimal GrossMarginPercent { get; }
        public decimal ProjectedCustomerRoiMultiple { get; }
        public int EstimatedPaybackMonths { get; }
        public bool IsApproved { get; private set; }
        public string? ApprovalAuthority { get; private set; }
        public DateTime CreatedAtUtc { get; }

        public CommercialProposalRecord(
            string prospectId,
            string solutionTitle,
            string scopeSummary,
            decimal proposedPrice,
            decimal estimatedCostBasis,
            decimal projectedCustomerRoiMultiple,
            int estimatedPaybackMonths,
            string? proposalId = null,
            DateTime? createdAtUtc = null)
        {
            if (string.IsNullOrWhiteSpace(prospectId))
                throw new ArgumentException("Prospect ID is required.", nameof(prospectId));
            if (string.IsNullOrWhiteSpace(solutionTitle))
                throw new ArgumentException("Solution title is required.", nameof(solutionTitle));
            if (proposedPrice <= 0)
                throw new ArgumentException("Proposed price must be positive.", nameof(proposedPrice));
            if (estimatedCostBasis < 0)
                throw new ArgumentException("Cost basis cannot be negative.", nameof(estimatedCostBasis));

            decimal marginPercent = ((proposedPrice - estimatedCostBasis) / proposedPrice) * 100m;
            if (marginPercent < GrowthConstitutionalInvariants.MinGrossMarginPercent)
            {
                throw new InvalidOperationException(
                    $"Proposal gross margin {marginPercent:F1}% violates constitutional floor of {GrowthConstitutionalInvariants.MinGrossMarginPercent:F1}% (Law I41-G).");
            }

            ProposalId = proposalId ?? Guid.NewGuid().ToString("N");
            ProspectId = prospectId;
            SolutionTitle = solutionTitle.Trim();
            ScopeSummary = scopeSummary ?? string.Empty;
            ProposedPrice = proposedPrice;
            EstimatedCostBasis = estimatedCostBasis;
            GrossMarginPercent = marginPercent;
            ProjectedCustomerRoiMultiple = projectedCustomerRoiMultiple;
            EstimatedPaybackMonths = estimatedPaybackMonths;
            CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow;
        }

        public void ApproveProposal(string approvalAuthority)
        {
            if (string.IsNullOrWhiteSpace(approvalAuthority))
                throw new ArgumentException("Approval authority required.", nameof(approvalAuthority));
            IsApproved = true;
            ApprovalAuthority = approvalAuthority.Trim();
        }
    }

    /// <summary>
    /// Commercial negotiation record tracking concessions and governance boundary signoff.
    /// </summary>
    public sealed class CommercialNegotiationRecord
    {
        public string NegotiationId { get; }
        public string ProposalId { get; }
        public decimal InitialProposedPrice { get; }
        public decimal FinalAgreedPrice { get; }
        public decimal FinalGrossMarginPercent { get; }
        public string ConcessionsGrantedSummary { get; }
        public string ConcessionsReceivedSummary { get; }
        public string Prg1SignoffId { get; }
        public bool IsClosedWon { get; }
        public DateTime ConcludedAtUtc { get; }

        public CommercialNegotiationRecord(
            string proposalId,
            decimal initialProposedPrice,
            decimal finalAgreedPrice,
            decimal estimatedCostBasis,
            string concessionsGrantedSummary,
            string concessionsReceivedSummary,
            string prg1SignoffId,
            bool isClosedWon,
            string? negotiationId = null,
            DateTime? concludedAtUtc = null)
        {
            if (string.IsNullOrWhiteSpace(proposalId))
                throw new ArgumentException("Proposal ID is required.", nameof(proposalId));
            if (string.IsNullOrWhiteSpace(prg1SignoffId))
                throw new InvalidOperationException("Negotiation conclusion requires PRG-1 governance signoff (Law I41-H).");

            decimal margin = ((finalAgreedPrice - estimatedCostBasis) / finalAgreedPrice) * 100m;
            if (isClosedWon && margin < GrowthConstitutionalInvariants.MinGrossMarginPercent)
            {
                throw new InvalidOperationException(
                    $"Negotiated closed-won price results in {margin:F1}% gross margin, violating the {GrowthConstitutionalInvariants.MinGrossMarginPercent:F1}% floor (Law I41-G).");
            }

            NegotiationId = negotiationId ?? Guid.NewGuid().ToString("N");
            ProposalId = proposalId;
            InitialProposedPrice = initialProposedPrice;
            FinalAgreedPrice = finalAgreedPrice;
            FinalGrossMarginPercent = margin;
            ConcessionsGrantedSummary = concessionsGrantedSummary ?? string.Empty;
            ConcessionsReceivedSummary = concessionsReceivedSummary ?? string.Empty;
            Prg1SignoffId = prg1SignoffId;
            IsClosedWon = isClosedWon;
            ConcludedAtUtc = concludedAtUtc ?? DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Fully structured commercial contract record with immutable cryptographic digest.
    /// </summary>
    public sealed class CommercialContractRecord
    {
        public string ContractId { get; }
        public string ProspectId { get; }
        public string ProposalId { get; }
        public decimal TotalContractValue { get; }
        public string PaymentTerms { get; }
        public string ContractDigestSha256 { get; }
        public string CounterpartySignatory { get; }
        public string CharlieSignatory { get; }
        public bool IsFullyExecuted { get; }
        public DateTime ExecutedAtUtc { get; }

        public CommercialContractRecord(
            string prospectId,
            string proposalId,
            decimal totalContractValue,
            string paymentTerms,
            string contractDigestSha256,
            string counterpartySignatory,
            string charlieSignatory,
            bool isFullyExecuted,
            string? contractId = null,
            DateTime? executedAtUtc = null)
        {
            if (string.IsNullOrWhiteSpace(prospectId))
                throw new ArgumentException("Prospect ID is required.", nameof(prospectId));
            if (string.IsNullOrWhiteSpace(proposalId))
                throw new ArgumentException("Proposal ID is required.", nameof(proposalId));
            if (string.IsNullOrWhiteSpace(contractDigestSha256))
                throw new ArgumentException("Contract cryptographic digest is mandatory.", nameof(contractDigestSha256));
            if (totalContractValue <= 0)
                throw new ArgumentException("Total contract value must be positive.", nameof(totalContractValue));

            ContractId = contractId ?? Guid.NewGuid().ToString("N");
            ProspectId = prospectId;
            ProposalId = proposalId;
            TotalContractValue = totalContractValue;
            PaymentTerms = paymentTerms ?? "Net 30";
            ContractDigestSha256 = contractDigestSha256;
            CounterpartySignatory = counterpartySignatory ?? string.Empty;
            CharlieSignatory = charlieSignatory ?? string.Empty;
            IsFullyExecuted = isFullyExecuted;
            ExecutedAtUtc = executedAtUtc ?? DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Customer success, onboarding, value realization, and retention health tracker.
    /// </summary>
    public sealed class CustomerRetentionAndHealthRecord
    {
        public string CustomerId { get; }
        public string ContractId { get; }
        public string CompanyName { get; }
        public DateTime OnboardingStartedUtc { get; }
        public DateTime? FirstValueRealizedUtc { get; private set; }
        public int? TimeToValueDays { get; private set; }
        public List<string> MilestonesAchieved { get; } = new();
        public decimal HealthScore { get; private set; } // 0.0 to 100.0
        public ChurnRiskLevel ChurnRisk { get; private set; } = ChurnRiskLevel.Low;
        public decimal? LatestNpsScore { get; private set; } // -100 to 100
        public bool ExpansionIdentified { get; private set; }
        public string ExpansionNotes { get; private set; } = string.Empty;

        public CustomerRetentionAndHealthRecord(
            string customerId,
            string contractId,
            string companyName,
            DateTime? onboardingStartedUtc = null)
        {
            if (string.IsNullOrWhiteSpace(customerId))
                throw new ArgumentException("Customer ID is required.", nameof(customerId));
            if (string.IsNullOrWhiteSpace(contractId))
                throw new ArgumentException("Contract ID is required.", nameof(contractId));
            if (string.IsNullOrWhiteSpace(companyName))
                throw new ArgumentException("Company name is required.", nameof(companyName));

            CustomerId = customerId;
            ContractId = contractId;
            CompanyName = companyName.Trim();
            OnboardingStartedUtc = onboardingStartedUtc ?? DateTime.UtcNow;
            HealthScore = 100.0m;
        }

        public void RecordFirstValueRealized(DateTime realizedAtUtc)
        {
            FirstValueRealizedUtc = realizedAtUtc;
            TimeToValueDays = Math.Max(1, (int)(realizedAtUtc - OnboardingStartedUtc).TotalDays);
        }

        public void AddMilestone(string milestone)
        {
            if (!string.IsNullOrWhiteSpace(milestone))
                MilestonesAchieved.Add(milestone.Trim());
        }

        public void UpdateHealth(decimal score, ChurnRiskLevel risk, decimal? nps = null)
        {
            HealthScore = Math.Clamp(score, 0.0m, 100.0m);
            ChurnRisk = risk;
            if (nps.HasValue) LatestNpsScore = Math.Clamp(nps.Value, -100m, 100m);
        }

        public void FlagExpansionOpportunity(string notes)
        {
            ExpansionIdentified = true;
            ExpansionNotes = notes ?? string.Empty;
        }
    }
}
